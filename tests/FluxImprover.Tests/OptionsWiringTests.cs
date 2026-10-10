namespace FluxImprover.Tests;

using AwesomeAssertions;
using FluxImprover.Enrichment;
using FluxImprover.Evaluation;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.QAGeneration;
using FluxImprover.QueryPreprocessing;
using FluxImprover.Services;
using NSubstitute;
using Xunit;

/// <summary>
/// One fact per option that was once public and unread (see <see cref="OptionsReachabilityRosterTests"/>): each sets the
/// option to a non-default value and observes the effect through the substituted model, so the fact fails if the option
/// stops being honoured.
/// </summary>
public sealed class OptionsWiringTests
{
    private const string Score = @"{""score"": 0.6, ""reasoning"": ""because"", ""answerable"": true, ""evidence"": ""e"", ""claims"": [{""claim"": ""c"", ""supported"": true}]}";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ---- EvaluationOptions ------------------------------------------------------------------------------------------

    [Fact]
    public async Task QAFilter_DisabledMetrics_AreNeverRequested_AndDoNotGateThePair()
    {
        var completion = Substitute.For<ITextGenerationService>();
        var systemPrompts = new List<string?>();
        completion.CompleteAsync(Arg.Any<string>(), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                lock (systemPrompts) systemPrompts.Add(ci.ArgAt<CompletionOptions?>(1)?.SystemPrompt);
                return @"{""score"": 0.9, ""reasoning"": ""ok""}";
            });
        var filter = new QAFilterService(new FaithfulnessEvaluator(completion), new RelevancyEvaluator(completion), new AnswerabilityEvaluator(completion));
        var pair = new GeneratedQAPair { Question = "Q", Answer = "A", Context = "C" };

        var passed = await filter.FilterAsync(
            [pair],
            new QAFilterOptions
            {
                // Strict floors on the two disabled metrics: they must not apply.
                MinFaithfulness = 0.95,
                MinAnswerability = 0.95,
                Evaluation = new EvaluationOptions { EnableFaithfulness = false, EnableAnswerability = false },
            },
            Ct);

        systemPrompts.Should().ContainSingle().Which.Should().Contain("relevancy");
        passed.Should().ContainSingle();
        passed[0].Evaluation!.Faithfulness.Should().BeNull();
        passed[0].Evaluation!.Answerability.Should().BeNull();
        passed[0].Evaluation!.Relevancy.Should().BeApproximately(0.9, 0.001);
        passed[0].Evaluation!.OverallScore.Should().BeApproximately(0.9, 0.001);
    }

    [Fact]
    public async Task Evaluators_PassThreshold_DecidesIsPassed()
    {
        var completion = Substitute.For<ITextGenerationService>();
        completion.CompleteAsync(Arg.Any<string>(), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>()).Returns(Score);
        var strict = new EvaluationOptions { PassThreshold = 0.8f };

        var results = new[]
        {
            await new FaithfulnessEvaluator(completion).EvaluateAsync("ctx", "answer", strict, Ct),
            await new RelevancyEvaluator(completion).EvaluateAsync("question", "answer", strict, "ctx", Ct),
            await new AnswerabilityEvaluator(completion).EvaluateAsync("ctx", "question", strict, Ct),
        };

        // A 0.6 score passes the old fixed 0.5 bar but not the configured 0.8.
        results.Should().AllSatisfy(r =>
        {
            r.Score.Should().BeApproximately(0.6, 0.001);
            r.PassThreshold.Should().BeApproximately(0.8, 0.001);
            r.IsPassed.Should().BeFalse();
        });
    }

    [Fact]
    public async Task Evaluators_IncludeDetailsFalse_ReturnScoreWithoutDetails()
    {
        var completion = Substitute.For<ITextGenerationService>();
        completion.CompleteAsync(Arg.Any<string>(), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>()).Returns(Score);
        var lean = new EvaluationOptions { IncludeDetails = false };

        var results = new[]
        {
            await new FaithfulnessEvaluator(completion).EvaluateAsync("ctx", "answer", lean, Ct),
            await new RelevancyEvaluator(completion).EvaluateAsync("question", "answer", lean, "ctx", Ct),
            await new AnswerabilityEvaluator(completion).EvaluateAsync("ctx", "question", lean, Ct),
        };

        results.Should().AllSatisfy(r =>
        {
            r.Score.Should().BeApproximately(0.6, 0.001);
            r.Details.Should().BeEmpty();
        });

        // Positive control: the same reply carries details by default.
        (await new FaithfulnessEvaluator(completion).EvaluateAsync("ctx", "answer", cancellationToken: Ct)).Details
            .Should().ContainKeys("reasoning", "claims");
    }

    [Theory]
    [InlineData("faithfulness")]
    [InlineData("relevancy")]
    [InlineData("answerability")]
    [InlineData("qa-filter")]
    public async Task EvaluationBatches_RunUpToMaxDegreeOfParallelismAtOnce(string batch)
    {
        var gated = new GatedCompletion(@"{""score"": 0.9, ""reasoning"": ""ok""}");
        var options = new EvaluationOptions { MaxDegreeOfParallelism = 2 };
        var items = Enumerable.Range(0, 5).Select(i => ($"text {i}", $"other {i}")).ToList();

        Func<Task<int>> run = batch switch
        {
            "faithfulness" => async () => (await new FaithfulnessEvaluator(gated.Service).EvaluateBatchAsync(items, options, Ct)).Count,
            "relevancy" => async () => (await new RelevancyEvaluator(gated.Service).EvaluateBatchAsync(items, options, Ct)).Count,
            "answerability" => async () => (await new AnswerabilityEvaluator(gated.Service).EvaluateBatchAsync(items, options, Ct)).Count,
            _ => async () =>
            {
                var filter = new QAFilterService(new FaithfulnessEvaluator(gated.Service), new RelevancyEvaluator(gated.Service), new AnswerabilityEvaluator(gated.Service));
                var pairs = items.Select(i => new GeneratedQAPair { Question = i.Item1, Answer = i.Item2, Context = "ctx" }).ToList();
                var only = new EvaluationOptions { MaxDegreeOfParallelism = 2, EnableFaithfulness = false, EnableAnswerability = false };
                return (await filter.FilterAsync(pairs, new QAFilterOptions { Evaluation = only }, Ct)).Count;
            },
        };

        var count = await gated.RunAsync(expectedInFlight: 2, run);

        count.Should().Be(5);
        gated.Peak.Should().Be(2);
    }

    [Fact]
    public async Task EvaluationBatch_WithParallelProcessingOff_RunsOneAtATime()
    {
        var gated = new GatedCompletion(@"{""score"": 0.9, ""reasoning"": ""ok""}");
        var items = Enumerable.Range(0, 3).Select(i => ($"ctx {i}", $"answer {i}")).ToList();

        await gated.RunAsync(expectedInFlight: 2, () =>
            new FaithfulnessEvaluator(gated.Service).EvaluateBatchAsync(items, new EvaluationOptions { EnableParallelProcessing = false }, Ct));

        gated.Peak.Should().Be(1);
    }

    // ---- EnrichmentOptions / ConditionalEnrichmentOptions ------------------------------------------------------------

    [Theory]
    [InlineData("chunks")]
    [InlineData("summaries")]
    [InlineData("keywords")]
    public async Task EnrichmentBatches_RunUpToMaxDegreeOfParallelismAtOnce(string batch)
    {
        var gated = new GatedCompletion(@"[""one"", ""two""]");
        var options = new EnrichmentOptions { MaxDegreeOfParallelism = 2, EnableKeywordExtraction = batch != "chunks" };
        var texts = Enumerable.Range(0, 5).Select(i => $"text number {i} with some words").ToList();

        Func<Task<int>> run = batch switch
        {
            "chunks" => async () =>
                (await new ChunkEnrichmentService(new SummarizationService(gated.Service), new KeywordExtractionService(gated.Service))
                    .EnrichBatchAsync(texts.Select((t, i) => new Chunk { Id = $"c{i}", Content = t }), options, Ct)).Count,
            "summaries" => async () => (await new SummarizationService(gated.Service).SummarizeBatchAsync(texts, options, Ct)).Count,
            _ => async () => (await new KeywordExtractionService(gated.Service).ExtractKeywordsBatchAsync(texts, options, Ct)).Count,
        };

        var count = await gated.RunAsync(expectedInFlight: 2, run);

        count.Should().Be(5);
        gated.Peak.Should().Be(2);
    }

    [Fact]
    public async Task ChunkEnrichment_DomainGlossary_ExpandsTheTextTheModelReads()
    {
        var summarization = Substitute.For<ISummarizationService>();
        summarization.SummarizeAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>()).Returns("summary");
        var keywords = Substitute.For<IKeywordExtractionService>();
        keywords.ExtractKeywordsAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>()).Returns(new List<string> { "k" });
        var service = new ChunkEnrichmentService(summarization, keywords);
        var chunk = new Chunk { Id = "c", Content = "The PLC talks to the HMI." };

        var enriched = await service.EnrichAsync(
            chunk,
            new EnrichmentOptions { ConditionalOptions = new ConditionalEnrichmentOptions { DomainGlossary = new Glossary() } },
            Ct);

        const string expanded = "The PLC (programmable logic controller) talks to the HMI.";
        await summarization.Received(1).SummarizeAsync(expanded, Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>());
        await keywords.Received(1).ExtractKeywordsAsync(expanded, Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>());
        enriched.Content.Should().Be(chunk.Content);
    }

    // ---- QueryPreprocessingOptions ----------------------------------------------------------------------------------

    [Fact]
    public async Task ClassifyIntent_BelowMinIntentConfidence_IsReportedAsGeneral()
    {
        var completion = Substitute.For<ITextGenerationService>();
        completion.CompleteAsync(Arg.Any<string>(), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns(@"{""intent"": ""Comparison"", ""confidence"": 0.6}");
        var service = new QueryPreprocessingService(completion);

        var (llmIntent, llmConfidence) = await service.ClassifyIntentAsync(
            "tell me about databases", new QueryPreprocessingOptions { MinIntentConfidence = 0.8f }, Ct);
        var (heuristicIntent, heuristicConfidence) = await service.ClassifyIntentAsync(
            "where are the logs", new QueryPreprocessingOptions { MinIntentConfidence = 0.8f, UseLlmIntentClassification = false }, Ct);

        (llmIntent, llmConfidence).Should().Be((QueryClassification.General, 0.6));
        (heuristicIntent, heuristicConfidence).Should().Be((QueryClassification.General, 0.7));

        // Positive control: at the default floor the model's answer stands.
        (await service.ClassifyIntentAsync("tell me about databases", cancellationToken: Ct)).Intent.Should().Be(QueryClassification.Comparison);
    }

    [Fact]
    public async Task KeywordAndSynonymPrompts_CarryTheConfiguredLanguage_AndNothingWhenUnset()
    {
        var completion = Substitute.For<ITextGenerationService>();
        var prompts = new List<string>();
        completion.CompleteAsync(Arg.Do<string>(p => prompts.Add(p)), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns(@"[""term""]");
        var service = new QueryPreprocessingService(completion);
        var korean = new QueryPreprocessingOptions { Language = "Korean" };

        await service.ExtractKeywordsAsync("database connection pool", korean, Ct);
        await service.ExpandWithSynonymsAsync("database connection pool", korean, Ct);
        await service.ExtractKeywordsAsync("database connection pool", cancellationToken: Ct);
        await service.ExpandWithSynonymsAsync("database connection pool", cancellationToken: Ct);

        prompts.Should().HaveCount(4);
        prompts.Take(2).Should().AllSatisfy(p => p.Should().Contain("Write the terms in this language: Korean."));
        prompts.Skip(2).Should().AllSatisfy(p => p.Should().NotContain("Write the terms in this language"));
    }

    // ---- QAGenerationOptions ----------------------------------------------------------------------------------------

    [Fact]
    public async Task QAGeneration_DifficultyDistribution_IsInThePrompt()
    {
        var completion = Substitute.For<ITextGenerationService>();
        var prompts = new List<string>();
        completion.CompleteAsync(Arg.Do<string>(p => prompts.Add(p)), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns(@"{""qa_pairs"": []}");
        var generator = new QAGeneratorService(completion);

        await generator.GenerateAsync(
            "Paris is the capital of France.",
            new QAGenerationOptions { DifficultyDistribution = new DifficultyDistribution { Easy = 0.1f, Medium = 0.1f, Hard = 0.8f } },
            cancellationToken: Ct);
        await generator.GenerateAsync(
            "Paris is the capital of France.",
            new QAGenerationOptions { DifficultyDistribution = new DifficultyDistribution { Easy = 0f, Medium = 0f, Hard = 0f } },
            cancellationToken: Ct);

        prompts[0].Should().Contain("Difficulty mix: about 10% easy, 10% medium, 80% hard questions");
        prompts[1].Should().NotContain("Difficulty mix");
    }

    // ---- helpers ----------------------------------------------------------------------------------------------------

    private sealed class Glossary : IDomainGlossary
    {
        public string ExpandTerms(string text) => text.Replace("PLC", "PLC (programmable logic controller)", StringComparison.Ordinal);

        public string? GetExpansion(string term) => term == "PLC" ? "programmable logic controller" : null;
    }

    /// <summary>
    /// A completion service whose calls all wait on one gate, counting how many are in flight at once. The batch is
    /// released once the expected number is in flight (or after a bounded wait, so a sequential batch fails rather than hangs).
    /// </summary>
    private sealed class GatedCompletion
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _inFlight;
        private int _peak;

        public GatedCompletion(string response)
        {
            Service = Substitute.For<ITextGenerationService>();
            Service.CompleteAsync(Arg.Any<string>(), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>())
                .Returns(_ => AnswerAsync(response));
        }

        public ITextGenerationService Service { get; }

        public int Peak => Volatile.Read(ref _peak);

        public async Task<T> RunAsync<T>(int expectedInFlight, Func<Task<T>> batch)
        {
            var running = batch();
            var deadline = DateTime.UtcNow.AddSeconds(2);
            while (Volatile.Read(ref _inFlight) < expectedInFlight && !running.IsCompleted && DateTime.UtcNow < deadline)
            {
                await Task.Delay(10, Ct);
            }

            // Room for an unbounded batch to overshoot before the gate opens.
            await Task.Delay(50, Ct);
            _gate.TrySetResult();
            return await running;
        }

        private async Task<string> AnswerAsync(string response)
        {
            var now = Interlocked.Increment(ref _inFlight);
            int seen;
            while ((seen = Volatile.Read(ref _peak)) < now && Interlocked.CompareExchange(ref _peak, now, seen) != seen)
            {
            }

            try
            {
                await _gate.Task.ConfigureAwait(false);
                return response;
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }
    }
}

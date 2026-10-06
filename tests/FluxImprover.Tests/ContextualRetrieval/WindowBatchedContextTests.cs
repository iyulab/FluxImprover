using Flux.Abstractions;
namespace FluxImprover.Tests.ContextualRetrieval;

using AwesomeAssertions;
using FluxImprover.ContextualRetrieval;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.Services;
using NSubstitute;
using Xunit;

/// <summary>
/// <see cref="ContextualEnrichmentOptions.ChunksPerCall"/>: adjacent chunks share one call (the document context sent once
/// per window), one summary per chunk comes back as a JSON array, and a window whose answer is unusable is asked again chunk
/// by chunk.
/// </summary>
public sealed class WindowBatchedContextTests
{
    private const string Document = "# Plant manual\n\nThe plant runs three lines. Each line has its own pump and gauge.";

    private static List<Chunk> Chunks(int n) =>
        Enumerable.Range(1, n).Select(i => new Chunk { Id = $"c{i}", Content = $"Chunk text number {i}." }).ToList();

    private static (ContextualEnrichmentService Service, List<string> Prompts) Service(Func<string, string> answer)
    {
        var prompts = new List<string>();
        var generation = Substitute.For<ITextGenerationService>();
        generation.CompleteAsync(Arg.Any<string>(), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var prompt = call.Arg<string>();
                lock (prompts) prompts.Add(prompt);
                return answer(prompt);
            });
        return (new ContextualEnrichmentService(generation), prompts);
    }

    /// <summary>Answers a window prompt with one summary per «### Chunk n» heading; a single-chunk prompt with one line.</summary>
    private static string Echo(string prompt)
    {
        var count = prompt.Split('\n').Count(l => l.StartsWith("### Chunk ", StringComparison.Ordinal));
        return count == 0
            ? "single summary"
            : System.Text.Json.JsonSerializer.Serialize(Enumerable.Range(1, count).Select(i => $"summary {i}").ToArray());
    }

    [Fact]
    public async Task SevenChunksThreePerCall_MakeThreeCalls_AndEveryChunkGetsItsSummaryInOrder()
    {
        var (service, prompts) = Service(Echo);

        var result = await service.EnrichBatchAsync(Chunks(7), Document,
            new ContextualEnrichmentOptions { ChunksPerCall = 3, EnableParallelProcessing = false }, TestContext.Current.CancellationToken);

        prompts.Should().HaveCount(3);
        result.Select(r => r.Id).Should().Equal("c1", "c2", "c3", "c4", "c5", "c6", "c7");
        result.Select(r => r.ContextSummary).Should().Equal(
            "summary 1", "summary 2", "summary 3", "summary 1", "summary 2", "summary 3", "single summary");
        prompts[0].Should().Contain("Chunk text number 1.").And.Contain("Chunk text number 3.").And.Contain("exactly 3 strings");
        prompts[0].Split("# Plant manual").Should().HaveCount(2, "the document is sent once per window");
    }

    [Fact]
    public async Task AWrongCount_AsksThatWindowAgainChunkByChunk()
    {
        var (service, prompts) = Service(p => p.Contains("### Chunk ", StringComparison.Ordinal) ? "[\"only one\"]" : "single summary");

        var result = await service.EnrichBatchAsync(Chunks(3), Document,
            new ContextualEnrichmentOptions { ChunksPerCall = 3 }, TestContext.Current.CancellationToken);

        prompts.Should().HaveCount(1 + 3);
        result.Should().AllSatisfy(r => r.ContextSummary.Should().Be("single summary"));
    }

    [Fact]
    public async Task AnAnswerThatIsNotAJsonArray_AsksAgainChunkByChunk()
    {
        var (service, _) = Service(p => p.Contains("### Chunk ", StringComparison.Ordinal) ? "Here are the summaries: first, second." : "single summary");

        var result = await service.EnrichBatchAsync(Chunks(2), Document,
            new ContextualEnrichmentOptions { ChunksPerCall = 2 }, TestContext.Current.CancellationToken);

        result.Should().AllSatisfy(r => r.ContextSummary.Should().Be("single summary"));
    }

    [Fact]
    public async Task OnePerCall_IsOneCallPerChunk()
    {
        var (service, prompts) = Service(Echo);

        var result = await service.EnrichBatchAsync(Chunks(4), Document, new ContextualEnrichmentOptions(), TestContext.Current.CancellationToken);

        prompts.Should().HaveCount(4);
        result.Should().AllSatisfy(r => r.ContextSummary.Should().Be("single summary"));
    }

    [Theory]
    [InlineData("```json\n[\"a\", \"b\"]\n```", 2, true)]
    [InlineData("[\"a\"]", 2, false)]
    [InlineData("no array here", 1, false)]
    [InlineData("[1, 2]", 2, false)]
    public void ParseSummaries_TakesOnlyAnArrayOfTheRightCount(string answer, int count, bool parses) =>
        (ContextualEnrichmentService.ParseSummaries(answer, count) is not null).Should().Be(parses);
}

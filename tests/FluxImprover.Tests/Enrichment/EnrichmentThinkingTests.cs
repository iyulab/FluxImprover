namespace FluxImprover.Tests.Enrichment;

using AwesomeAssertions;
using FluxImprover.Enrichment;
using FluxImprover.Options;
using FluxImprover.Services;
using NSubstitute;
using Xunit;

/// <summary>
/// Chunk summaries and keywords are short extractions: a reasoning model left on its template default spends the output
/// budget thinking and the cut-off summary is discarded. Both calls therefore send <see cref="EnrichmentOptions.Thinking"/>,
/// which is off unless the caller asks otherwise.
/// </summary>
public sealed class EnrichmentThinkingTests
{
    private readonly ITextGenerationService _completion = Substitute.For<ITextGenerationService>();

    public EnrichmentThinkingTests()
    {
        _completion.CompleteAsync(Arg.Any<string>(), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns("""{"keywords": ["alpha"]}""");
    }

    [Fact]
    public async Task Summary_AndKeywords_AskForNoThinking_ByDefault()
    {
        await new SummarizationService(_completion).SummarizeAsync("text", cancellationToken: TestContext.Current.CancellationToken);
        await new KeywordExtractionService(_completion).ExtractKeywordsAsync("text", cancellationToken: TestContext.Current.CancellationToken);

        await _completion.Received(2).CompleteAsync(
            Arg.Any<string>(), Arg.Is<CompletionOptions>(o => o.Thinking == ThinkingMode.Off), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ACallerThatWantsReasoning_GetsIt()
    {
        var options = new EnrichmentOptions { Thinking = ThinkingMode.Auto, MaxTokens = 4096 };

        await new SummarizationService(_completion).SummarizeAsync("text", options, TestContext.Current.CancellationToken);

        await _completion.Received(1).CompleteAsync(
            Arg.Any<string>(), Arg.Is<CompletionOptions>(o => o.Thinking == ThinkingMode.Auto && o.MaxTokens == 4096), Arg.Any<CancellationToken>());
    }
}

namespace FluxImprover.Tests.Enrichment;

using FluentAssertions;
using FluxImprover.Services;
using FluxImprover.Options;
using FluxImprover.Enrichment;
using NSubstitute;
using Xunit;

public sealed class SummarizationServiceTests
{
    private readonly ITextCompletionService _completionService;
    private readonly SummarizationService _sut;

    public SummarizationServiceTests()
    {
        _completionService = Substitute.For<ITextCompletionService>();
        _sut = new SummarizationService(_completionService);
    }

    [Fact]
    public async Task SummarizeAsync_WithValidText_ReturnsSummary()
    {
        // Arrange
        var text = "This is a long document that needs to be summarized. It contains important information.";
        var expectedSummary = "A document containing important information.";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedSummary);

        // Act
        var result = await _sut.SummarizeAsync(text);

        // Assert
        result.Should().Be(expectedSummary);
    }

    [Fact]
    public async Task SummarizeAsync_WithOptions_UsesProvidedOptions()
    {
        // Arrange
        var text = "Sample text to summarize.";
        var options = new EnrichmentOptions { MaxSummaryLength = 50, Temperature = 0.3f };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Summary");

        // Act
        await _sut.SummarizeAsync(text, options);

        // Assert
        await _completionService.Received(1).CompleteAsync(
            Arg.Is<string>(s => s.Contains("50")),
            Arg.Is<CompletionOptions>(o => o.Temperature == 0.3f),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SummarizeAsync_WithEmptyText_ReturnsEmpty()
    {
        // Arrange & Act
        var result = await _sut.SummarizeAsync(string.Empty);

        // Assert
        result.Should().BeEmpty();
        await _completionService.DidNotReceive().CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SummarizeAsync_WithCancellation_ThrowsOperationCanceled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.SummarizeAsync("text", cancellationToken: cts.Token));
    }

    [Fact]
    public async Task SummarizeBatchAsync_WithMultipleTexts_ReturnsAllSummaries()
    {
        // Arrange
        var texts = new[] { "Text 1", "Text 2", "Text 3" };
        var summaries = new[] { "Summary 1", "Summary 2", "Summary 3" };

        _completionService.CompleteAsync(
            Arg.Is<string>(s => s.Contains("Text 1")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(summaries[0]);

        _completionService.CompleteAsync(
            Arg.Is<string>(s => s.Contains("Text 2")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(summaries[1]);

        _completionService.CompleteAsync(
            Arg.Is<string>(s => s.Contains("Text 3")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(summaries[2]);

        // Act
        var results = await _sut.SummarizeBatchAsync(texts);

        // Assert
        results.Should().HaveCount(3);
        results.Should().BeEquivalentTo(summaries);
    }
}

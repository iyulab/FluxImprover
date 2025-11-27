namespace FluxImprover.Tests.Enrichment;

using FluentAssertions;
using FluxImprover.Abstractions.Models;
using FluxImprover.Abstractions.Options;
using FluxImprover.Enrichment;
using NSubstitute;
using Xunit;

public sealed class ChunkEnrichmentServiceTests
{
    private readonly ISummarizationService _summarizationService;
    private readonly IKeywordExtractionService _keywordService;
    private readonly ChunkEnrichmentService _sut;

    public ChunkEnrichmentServiceTests()
    {
        _summarizationService = Substitute.For<ISummarizationService>();
        _keywordService = Substitute.For<IKeywordExtractionService>();
        _sut = new ChunkEnrichmentService(_summarizationService, _keywordService);
    }

    [Fact]
    public async Task EnrichAsync_WithValidChunk_ReturnsEnrichedChunk()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "chunk-1",
            Content = "This is a test chunk with important information.",
            Metadata = new Dictionary<string, object> { ["source"] = "test.txt" }
        };

        _summarizationService.SummarizeAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Test summary");

        _keywordService.ExtractKeywordsAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<string> { "test", "chunk", "information" });

        // Act
        var result = await _sut.EnrichAsync(chunk);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("chunk-1");
        result.Text.Should().Be(chunk.Content);
        result.Summary.Should().Be("Test summary");
        result.Keywords.Should().Contain("test");
    }

    [Fact]
    public async Task EnrichAsync_WithOptions_UsesProvidedOptions()
    {
        // Arrange
        var chunk = new Chunk { Id = "test", Content = "Content" };
        var options = new EnrichmentOptions
        {
            MaxSummaryLength = 100,
            MaxKeywords = 5,
            Temperature = 0.2f
        };

        _summarizationService.SummarizeAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Summary");

        _keywordService.ExtractKeywordsAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        // Act
        await _sut.EnrichAsync(chunk, options);

        // Assert
        await _summarizationService.Received(1).SummarizeAsync(
            Arg.Any<string>(),
            Arg.Is<EnrichmentOptions>(o => o.MaxSummaryLength == 100),
            Arg.Any<CancellationToken>());

        await _keywordService.Received(1).ExtractKeywordsAsync(
            Arg.Any<string>(),
            Arg.Is<EnrichmentOptions>(o => o.MaxKeywords == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrichAsync_PreservesOriginalMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["source"] = "document.pdf",
            ["page"] = 5
        };
        var chunk = new Chunk
        {
            Id = "test",
            Content = "Content",
            Metadata = metadata
        };

        _summarizationService.SummarizeAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>())
            .Returns("Summary");
        _keywordService.ExtractKeywordsAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        // Act
        var result = await _sut.EnrichAsync(chunk);

        // Assert
        result.Metadata.Should().ContainKey("source");
        result.Metadata!["source"].Should().Be("document.pdf");
    }

    [Fact]
    public async Task EnrichBatchAsync_WithMultipleChunks_ReturnsAllEnriched()
    {
        // Arrange
        var chunks = new[]
        {
            new Chunk { Id = "1", Content = "Content 1" },
            new Chunk { Id = "2", Content = "Content 2" }
        };

        _summarizationService.SummarizeAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>())
            .Returns("Summary");
        _keywordService.ExtractKeywordsAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "keyword" });

        // Act
        var results = await _sut.EnrichBatchAsync(chunks);

        // Assert
        results.Should().HaveCount(2);
        results.All(r => r.Summary == "Summary").Should().BeTrue();
    }

    [Fact]
    public async Task EnrichAsync_WithEmptyContent_SkipsEnrichment()
    {
        // Arrange
        var chunk = new Chunk { Id = "empty", Content = "" };

        // Act
        var result = await _sut.EnrichAsync(chunk);

        // Assert
        result.Summary.Should().BeNull();
        result.Keywords.Should().BeNull();
    }

    [Fact]
    public async Task EnrichAsync_SetsSourceIdFromChunkId()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "chunk-123",
            Content = "Sample content"
        };

        _summarizationService.SummarizeAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>())
            .Returns("Summary");
        _keywordService.ExtractKeywordsAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        // Act
        var result = await _sut.EnrichAsync(chunk);

        // Assert
        result.SourceId.Should().Be("chunk-123");
    }
}

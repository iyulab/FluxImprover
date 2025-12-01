namespace FluxImprover.Tests.Enrichment;

using FluentAssertions;
using FluxImprover.Enrichment;
using FluxImprover.Models;
using FluxImprover.Options;
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


    #region Conditional Enrichment Tests

    [Fact]
    public async Task EnrichAsync_WithConditionalEnabled_SkipsHighQualityChunks()
    {
        // Arrange - High quality chunk (complete sentence, good structure)
        var chunk = new Chunk
        {
            Id = "high-quality",
            Content = "This is a well-structured paragraph with proper formatting."
        };
        var options = new EnrichmentOptions
        {
            ConditionalOptions = new ConditionalEnrichmentOptions
            {
                EnableConditionalEnrichment = true,
                SkipEnrichmentThreshold = 0.5f // Low threshold to ensure skip
            }
        };

        // Act
        var result = await _sut.EnrichAsync(chunk, options);

        // Assert
        result.Should().NotBeNull();
        // When skipped, LLM services should not be called
        await _summarizationService.DidNotReceive().SummarizeAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrichAsync_WithConditionalEnabled_EnrichesLowQualityChunks()
    {
        // Arrange - Low quality chunk (incomplete, no structure)
        var chunk = new Chunk
        {
            Id = "low-quality",
            Content = new string('x', 600) // Long but repetitive content
        };
        var options = new EnrichmentOptions
        {
            ConditionalOptions = new ConditionalEnrichmentOptions
            {
                EnableConditionalEnrichment = true,
                SkipEnrichmentThreshold = 0.95f // High threshold to ensure enrichment
            }
        };

        _summarizationService.SummarizeAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>())
            .Returns("Summary");
        _keywordService.ExtractKeywordsAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "keyword" });

        // Act
        var result = await _sut.EnrichAsync(chunk, options);

        // Assert
        result.Summary.Should().NotBeNull();
        await _summarizationService.Received(1).SummarizeAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrichAsync_WithConditionalEnabled_IncludesQualityMetrics()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test",
            Content = "This is test content for quality assessment."
        };
        var options = new EnrichmentOptions
        {
            ConditionalOptions = new ConditionalEnrichmentOptions
            {
                EnableConditionalEnrichment = true,
                SkipEnrichmentThreshold = 0.95f,
                IncludeQualityMetrics = true
            }
        };

        _summarizationService.SummarizeAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>())
            .Returns("Summary");
        _keywordService.ExtractKeywordsAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        // Act
        var result = await _sut.EnrichAsync(chunk, options);

        // Assert
        result.Metadata.Should().ContainKey(EnrichmentMetadataKeys.QualityScore);
        result.Metadata.Should().ContainKey(EnrichmentMetadataKeys.WasSkipped);
        result.Metadata![EnrichmentMetadataKeys.QualityScore].Should().BeOfType<float>();
    }

    [Fact]
    public async Task EnrichAsync_ShortContent_SkipsSummarization()
    {
        // Arrange - Content shorter than MinSummarizationLength
        var chunk = new Chunk
        {
            Id = "short",
            Content = "Short content under 500 characters."
        };
        var options = new EnrichmentOptions
        {
            ConditionalOptions = new ConditionalEnrichmentOptions
            {
                EnableConditionalEnrichment = true,
                SkipEnrichmentThreshold = 0.99f,
                MinSummarizationLength = 500
            }
        };

        _keywordService.ExtractKeywordsAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "keyword" });

        // Act
        var result = await _sut.EnrichAsync(chunk, options);

        // Assert
        result.Summary.Should().BeNull();
        await _summarizationService.DidNotReceive().SummarizeAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrichAsync_WithoutConditionalOptions_PerformsFullEnrichment()
    {
        // Arrange
        var chunk = new Chunk { Id = "test", Content = "Test content" };

        _summarizationService.SummarizeAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>())
            .Returns("Summary");
        _keywordService.ExtractKeywordsAsync(Arg.Any<string>(), Arg.Any<EnrichmentOptions>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "test" });

        // Act
        var result = await _sut.EnrichAsync(chunk);

        // Assert - Both services should be called
        await _summarizationService.Received(1).SummarizeAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>());
        await _keywordService.Received(1).ExtractKeywordsAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GetStatistics_CalculatesCorrectly()
    {
        // Arrange
        var enrichedChunks = new List<EnrichedChunk>
        {
            new()
            {
                Id = "1", Text = "Text1", SourceId = "1", Summary = "Summary",
                Metadata = new Dictionary<string, object> { [EnrichmentMetadataKeys.WasSkipped] = false }
            },
            new()
            {
                Id = "2", Text = "Text2", SourceId = "2", Summary = null,
                Metadata = new Dictionary<string, object> { [EnrichmentMetadataKeys.WasSkipped] = true }
            },
            new()
            {
                Id = "3", Text = "Text3", SourceId = "3", Summary = "Summary", Keywords = ["a", "b"],
                Metadata = new Dictionary<string, object> { [EnrichmentMetadataKeys.WasSkipped] = false }
            }
        };

        // Act
        var stats = ChunkEnrichmentService.GetStatistics(enrichedChunks);

        // Assert
        stats.TotalChunks.Should().Be(3);
        stats.SkippedChunks.Should().Be(1);
        stats.SummarizedChunks.Should().Be(2);
        stats.KeywordsExtractedChunks.Should().Be(1);
        stats.EstimatedLlmCallsSaved.Should().Be(2); // 1 skipped * 2 calls
        stats.SkipRate.Should().BeApproximately(1f / 3f, 0.01f);
    }

    #endregion

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

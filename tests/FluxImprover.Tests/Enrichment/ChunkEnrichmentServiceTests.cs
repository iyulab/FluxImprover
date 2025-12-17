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

    #region Multilingual Support Tests

    [Fact]
    public async Task EnrichAsync_WithKoreanContent_ReturnsEnrichedChunk()
    {
        // Arrange - Korean technical document (ClusterPlex HA solution)
        var chunk = new Chunk
        {
            Id = "korean-chunk-1",
            Content = "ClusterPlex는 고가용성(HA) 솔루션으로, 핫빗(Heartbeat) 기반의 페일오버 메커니즘을 제공합니다. " +
                      "DRBD를 통한 실시간 데이터 복제와 Splitbrain 방지 기능을 포함합니다.",
            Metadata = new Dictionary<string, object> { ["source"] = "clusterplex-manual.md" }
        };

        var expectedSummary = "ClusterPlex HA 솔루션의 핵심 기능 설명";
        var expectedKeywords = new List<string> { "ClusterPlex", "고가용성", "핫빗", "페일오버", "DRBD", "Splitbrain" };

        _summarizationService.SummarizeAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedSummary);

        _keywordService.ExtractKeywordsAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedKeywords);

        // Act
        var result = await _sut.EnrichAsync(chunk);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("korean-chunk-1");
        result.Text.Should().Contain("ClusterPlex");
        result.Summary.Should().Be(expectedSummary);
        result.Keywords.Should().Contain("ClusterPlex");
        result.Keywords.Should().Contain("고가용성");
    }

    [Fact]
    public async Task EnrichAsync_WithKoreanTableContent_ProcessesCorrectly()
    {
        // Arrange - Korean table content
        var chunk = new Chunk
        {
            Id = "korean-table-1",
            Content = """
                | 항목 | 설명 | 기본값 |
                |------|------|--------|
                | 핫빗 주기 | 노드 간 상태 확인 주기 | 1초 |
                | 페일오버 시간 | 장애 감지 후 전환 시간 | 3초 |
                | DRBD 동기화 | 데이터 복제 방식 | 동기식 |
                """,
            Metadata = new Dictionary<string, object> { ["contentType"] = "table" }
        };

        _summarizationService.SummarizeAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("ClusterPlex 설정 항목 테이블");

        _keywordService.ExtractKeywordsAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<string> { "핫빗", "페일오버", "DRBD" });

        // Act
        var result = await _sut.EnrichAsync(chunk);

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().NotBeNullOrEmpty();
        result.Keywords.Should().NotBeEmpty();
    }

    [Fact]
    public async Task EnrichAsync_WithMixedLanguageContent_ProcessesCorrectly()
    {
        // Arrange - Mixed Korean and English technical terms
        var chunk = new Chunk
        {
            Id = "mixed-lang-1",
            Content = "Kubernetes 클러스터에서 Pod의 상태를 모니터링합니다. " +
                      "liveness probe와 readiness probe를 설정하여 컨테이너 헬스체크를 수행합니다."
        };

        _summarizationService.SummarizeAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Kubernetes Pod 상태 모니터링 및 헬스체크 설명");

        _keywordService.ExtractKeywordsAsync(
            Arg.Any<string>(),
            Arg.Any<EnrichmentOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<string> { "Kubernetes", "Pod", "liveness probe", "readiness probe", "헬스체크" });

        // Act
        var result = await _sut.EnrichAsync(chunk);

        // Assert
        result.Should().NotBeNull();
        result.Keywords.Should().Contain("Kubernetes");
        result.Keywords.Should().Contain("헬스체크");
    }

    #endregion
}

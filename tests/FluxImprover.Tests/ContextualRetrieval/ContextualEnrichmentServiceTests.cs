namespace FluxImprover.Tests.ContextualRetrieval;

using FluentAssertions;
using FluxImprover.ContextualRetrieval;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.Services;
using NSubstitute;
using Xunit;

public sealed class ContextualEnrichmentServiceTests
{
    private readonly ITextCompletionService _completionService;
    private readonly ContextualEnrichmentService _sut;

    public ContextualEnrichmentServiceTests()
    {
        _completionService = Substitute.For<ITextCompletionService>();
        _sut = new ContextualEnrichmentService(_completionService);
    }

    [Fact]
    public async Task EnrichAsync_WithValidChunkAndDocument_ReturnsContextualChunk()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "chunk-1",
            Content = "The system shall authenticate users within 2 seconds.",
            Metadata = new Dictionary<string, object>
            {
                ["sourceId"] = "doc-1",
                ["headingPath"] = "Requirements > Security"
            }
        };
        var fullDocument = "System Requirements Document. The system shall authenticate users within 2 seconds. Performance is critical.";
        var expectedContext = "This chunk is from the Security Requirements section of a System Requirements Document.";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedContext);

        // Act
        var result = await _sut.EnrichAsync(chunk, fullDocument);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("chunk-1");
        result.Text.Should().Be(chunk.Content);
        result.ContextSummary.Should().Be(expectedContext);
        result.SourceId.Should().Be("doc-1");
        result.HeadingPath.Should().Be("Requirements > Security");
    }

    [Fact]
    public async Task EnrichAsync_WithOptions_UsesProvidedOptions()
    {
        // Arrange
        var chunk = new Chunk { Id = "chunk-1", Content = "Test content" };
        var document = "Full document text";
        var options = new ContextualEnrichmentOptions
        {
            MaxContextLength = 150,
            Temperature = 0.5f
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Context summary");

        // Act
        await _sut.EnrichAsync(chunk, document, options);

        // Assert
        await _completionService.Received(1).CompleteAsync(
            Arg.Is<string>(s => s.Contains("150")),
            Arg.Is<CompletionOptions>(o => o.Temperature == 0.5f),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrichAsync_WithEmptyContent_ReturnsChunkWithNullContext()
    {
        // Arrange
        var chunk = new Chunk { Id = "chunk-1", Content = string.Empty };
        var document = "Full document";

        // Act
        var result = await _sut.EnrichAsync(chunk, document);

        // Assert
        result.ContextSummary.Should().BeNull();
        await _completionService.DidNotReceive().CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrichAsync_WithNullChunk_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.EnrichAsync(null!, "document"));
    }

    [Fact]
    public async Task EnrichAsync_WithNullDocument_ThrowsArgumentNullException()
    {
        // Arrange
        var chunk = new Chunk { Id = "chunk-1", Content = "Content" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.EnrichAsync(chunk, null!));
    }

    [Fact]
    public async Task EnrichBatchAsync_WithMultipleChunks_ReturnsAllEnrichedChunks()
    {
        // Arrange
        var chunks = new[]
        {
            new Chunk { Id = "chunk-1", Content = "Content 1" },
            new Chunk { Id = "chunk-2", Content = "Content 2" }
        };
        var document = "Full document with content 1 and content 2";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Context summary");

        // Act
        var results = await _sut.EnrichBatchAsync(chunks, document);

        // Assert
        results.Should().HaveCount(2);
        results[0].Id.Should().Be("chunk-1");
        results[1].Id.Should().Be("chunk-2");
    }

    [Fact]
    public async Task EnrichBatchAsync_SetsPositionMetadata()
    {
        // Arrange
        var chunks = new[]
        {
            new Chunk { Id = "chunk-1", Content = "Content 1" },
            new Chunk { Id = "chunk-2", Content = "Content 2" },
            new Chunk { Id = "chunk-3", Content = "Content 3" }
        };
        var document = "Full document";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Context");

        // Act
        var results = await _sut.EnrichBatchAsync(chunks, document);

        // Assert
        results[0].Position.Should().Be(0);
        results[0].TotalChunks.Should().Be(3);
        results[2].Position.Should().Be(2);
        results[2].TotalChunks.Should().Be(3);
    }

    [Fact]
    public void GetContextualizedText_WithContextSummary_ReturnsCombinedText()
    {
        // Arrange
        var chunk = new ContextualChunk
        {
            Id = "chunk-1",
            Text = "Original text content",
            SourceId = "doc-1",
            ContextSummary = "This is context about the chunk"
        };

        // Act
        var result = chunk.GetContextualizedText();

        // Assert
        result.Should().Contain("This is context about the chunk");
        result.Should().Contain("Original text content");
        result.Should().StartWith("This is context about the chunk");
    }

    [Fact]
    public void GetContextualizedText_WithoutContextSummary_ReturnsOriginalText()
    {
        // Arrange
        var chunk = new ContextualChunk
        {
            Id = "chunk-1",
            Text = "Original text content",
            SourceId = "doc-1",
            ContextSummary = null
        };

        // Act
        var result = chunk.GetContextualizedText();

        // Assert
        result.Should().Be("Original text content");
    }

    #region Multilingual Support Tests

    [Fact]
    public async Task EnrichAsync_WithKoreanDocument_ReturnsContextualChunk()
    {
        // Arrange - Korean technical document (ClusterPlex HA solution)
        var chunk = new Chunk
        {
            Id = "korean-chunk-1",
            Content = "DRBD를 통한 실시간 데이터 복제와 Splitbrain 방지 기능을 포함합니다.",
            Metadata = new Dictionary<string, object>
            {
                ["sourceId"] = "clusterplex-manual",
                ["headingPath"] = "아키텍처 > 데이터 복제"
            }
        };

        var fullDocument = """
            # ClusterPlex 사용자 매뉴얼

            ## 개요
            ClusterPlex는 고가용성(HA) 솔루션입니다.

            ## 아키텍처
            ### 데이터 복제
            DRBD를 통한 실시간 데이터 복제와 Splitbrain 방지 기능을 포함합니다.

            ### 페일오버
            핫빗 기반의 페일오버 메커니즘을 제공합니다.
            """;

        var expectedContext = "이 청크는 ClusterPlex HA 솔루션 매뉴얼의 '데이터 복제' 섹션에서 DRBD 복제 기능을 설명합니다.";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedContext);

        // Act
        var result = await _sut.EnrichAsync(chunk, fullDocument);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("korean-chunk-1");
        result.ContextSummary.Should().Be(expectedContext);
        result.HeadingPath.Should().Be("아키텍처 > 데이터 복제");
    }

    [Fact]
    public async Task EnrichAsync_WithKoreanContent_PassesCorrectPromptToLLM()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "korean-chunk-1",
            Content = "핫빗 기반의 페일오버 메커니즘을 제공합니다."
        };
        var fullDocument = "ClusterPlex 매뉴얼. 핫빗 기반의 페일오버 메커니즘을 제공합니다.";

        string? capturedPrompt = null;
        _completionService.CompleteAsync(
            Arg.Do<string>(p => capturedPrompt = p),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Context summary");

        // Act
        await _sut.EnrichAsync(chunk, fullDocument);

        // Assert
        capturedPrompt.Should().NotBeNull();
        capturedPrompt.Should().Contain("핫빗");
        capturedPrompt.Should().Contain("페일오버");
        capturedPrompt.Should().Contain("ClusterPlex");
    }

    [Fact]
    public async Task EnrichBatchAsync_WithKoreanChunks_ProcessesAllCorrectly()
    {
        // Arrange - Korean document chunks
        var chunks = new[]
        {
            new Chunk { Id = "k1", Content = "클러스터 구성 방법을 설명합니다." },
            new Chunk { Id = "k2", Content = "페일오버 설정 가이드입니다." },
            new Chunk { Id = "k3", Content = "모니터링 대시보드 사용법입니다." }
        };
        var document = "ClusterPlex 매뉴얼: 클러스터 구성, 페일오버 설정, 모니터링 대시보드";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("한국어 컨텍스트 요약");

        // Act
        var results = await _sut.EnrichBatchAsync(chunks, document);

        // Assert
        results.Should().HaveCount(3);
        results.All(r => r.ContextSummary == "한국어 컨텍스트 요약").Should().BeTrue();
    }

    [Fact]
    public async Task EnrichAsync_WithMixedLanguageDocument_ProcessesCorrectly()
    {
        // Arrange - Mixed Korean/English technical document
        var chunk = new Chunk
        {
            Id = "mixed-1",
            Content = "kubectl apply -f deployment.yaml 명령으로 배포합니다."
        };
        var fullDocument = """
            # Kubernetes 배포 가이드

            kubectl apply -f deployment.yaml 명령으로 배포합니다.

            Pod 상태 확인: kubectl get pods
            """;

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Kubernetes 배포 가이드에서 kubectl 배포 명령어를 설명하는 섹션");

        // Act
        var result = await _sut.EnrichAsync(chunk, fullDocument);

        // Assert
        result.Should().NotBeNull();
        result.ContextSummary.Should().Contain("Kubernetes");
    }

    #endregion
}

namespace FluxImprover.Tests.RelationshipDiscovery;

using FluentAssertions;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.RelationshipDiscovery;
using FluxImprover.Services;
using NSubstitute;
using Xunit;

public sealed class ChunkRelationshipServiceTests
{
    private readonly ITextCompletionService _completionService;
    private readonly ChunkRelationshipService _sut;

    public ChunkRelationshipServiceTests()
    {
        _completionService = Substitute.For<ITextCompletionService>();
        _sut = new ChunkRelationshipService(_completionService);
    }

    [Fact]
    public async Task AnalyzePairAsync_WithRelatedChunks_ReturnsRelationships()
    {
        // Arrange
        var sourceChunk = new Chunk
        {
            Id = "chunk-1",
            Content = "Machine learning is a subset of artificial intelligence."
        };
        var targetChunk = new Chunk
        {
            Id = "chunk-2",
            Content = "Deep learning is a type of machine learning that uses neural networks."
        };

        var jsonResponse = """
            {
                "relationships": [
                    {
                        "type": "SameTopic",
                        "confidence": 0.9,
                        "explanation": "Both discuss machine learning concepts",
                        "bidirectional": true
                    },
                    {
                        "type": "Prerequisite",
                        "confidence": 0.8,
                        "explanation": "Understanding ML helps understand deep learning",
                        "bidirectional": false
                    }
                ]
            }
            """;

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(jsonResponse);

        // Act
        var result = await _sut.AnalyzePairAsync(sourceChunk, targetChunk);

        // Assert
        result.Should().HaveCount(2);
        result[0].RelationshipType.Should().Be(ChunkRelationshipType.SameTopic);
        result[0].Confidence.Should().Be(0.9f);
        result[0].SourceChunkId.Should().Be("chunk-1");
        result[0].TargetChunkId.Should().Be("chunk-2");
    }

    [Fact]
    public async Task AnalyzePairAsync_WithNoRelationships_ReturnsEmptyList()
    {
        // Arrange
        var sourceChunk = new Chunk { Id = "chunk-1", Content = "The weather is nice today." };
        var targetChunk = new Chunk { Id = "chunk-2", Content = "Database optimization techniques." };

        var jsonResponse = """{ "relationships": [] }""";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(jsonResponse);

        // Act
        var result = await _sut.AnalyzePairAsync(sourceChunk, targetChunk);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzePairAsync_WithMinConfidenceFilter_FiltersLowConfidence()
    {
        // Arrange
        var sourceChunk = new Chunk { Id = "chunk-1", Content = "Content 1" };
        var targetChunk = new Chunk { Id = "chunk-2", Content = "Content 2" };
        var options = new ChunkRelationshipOptions { MinConfidence = 0.7f };

        var jsonResponse = """
            {
                "relationships": [
                    { "type": "SameTopic", "confidence": 0.9, "bidirectional": true },
                    { "type": "Complementary", "confidence": 0.5, "bidirectional": true }
                ]
            }
            """;

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(jsonResponse);

        // Act
        var result = await _sut.AnalyzePairAsync(sourceChunk, targetChunk, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].RelationshipType.Should().Be(ChunkRelationshipType.SameTopic);
    }

    [Fact]
    public async Task AnalyzePairAsync_WithInvalidJson_ReturnsEmptyList()
    {
        // Arrange
        var sourceChunk = new Chunk { Id = "chunk-1", Content = "Content 1" };
        var targetChunk = new Chunk { Id = "chunk-2", Content = "Content 2" };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Invalid JSON response");

        // Act
        var result = await _sut.AnalyzePairAsync(sourceChunk, targetChunk);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzePairAsync_WithNullSourceChunk_ThrowsArgumentNullException()
    {
        // Arrange
        var targetChunk = new Chunk { Id = "chunk-2", Content = "Content" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.AnalyzePairAsync(null!, targetChunk));
    }

    [Fact]
    public async Task AnalyzeRelationshipsAsync_WithMultipleCandidates_ReturnsAllRelationships()
    {
        // Arrange
        var sourceChunk = new Chunk { Id = "source", Content = "Source content" };
        var candidates = new[]
        {
            new Chunk { Id = "target-1", Content = "Target 1" },
            new Chunk { Id = "target-2", Content = "Target 2" }
        };
        var options = new ChunkRelationshipOptions { EnableParallelProcessing = false };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("""{ "relationships": [{ "type": "SameTopic", "confidence": 0.8, "bidirectional": true }] }""");

        // Act
        var result = await _sut.AnalyzeRelationshipsAsync(sourceChunk, candidates, options);

        // Assert
        result.Success.Should().BeTrue();
        result.ChunkId.Should().Be("source");
        result.Relationships.Should().HaveCount(2);
    }

    [Fact]
    public async Task DiscoverAllRelationshipsAsync_WithMultipleChunks_AnalyzesAllPairs()
    {
        // Arrange
        var chunks = new[]
        {
            new Chunk { Id = "chunk-1", Content = "Content 1" },
            new Chunk { Id = "chunk-2", Content = "Content 2" },
            new Chunk { Id = "chunk-3", Content = "Content 3" }
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("""{ "relationships": [{ "type": "SameTopic", "confidence": 0.7, "bidirectional": true }] }""");

        // Act
        var result = await _sut.DiscoverAllRelationshipsAsync(chunks);

        // Assert
        // 3 chunks = 3 pairs: (1,2), (1,3), (2,3)
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task AnalyzePairAsync_WithMaxRelationshipsLimit_RespectsLimit()
    {
        // Arrange
        var sourceChunk = new Chunk { Id = "chunk-1", Content = "Content 1" };
        var targetChunk = new Chunk { Id = "chunk-2", Content = "Content 2" };
        var options = new ChunkRelationshipOptions { MaxRelationshipsPerPair = 1 };

        var jsonResponse = """
            {
                "relationships": [
                    { "type": "SameTopic", "confidence": 0.9, "bidirectional": true },
                    { "type": "Complementary", "confidence": 0.8, "bidirectional": true },
                    { "type": "Elaborates", "confidence": 0.7, "bidirectional": false }
                ]
            }
            """;

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(jsonResponse);

        // Act
        var result = await _sut.AnalyzePairAsync(sourceChunk, targetChunk, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].Confidence.Should().Be(0.9f); // Highest confidence should be kept
    }
}

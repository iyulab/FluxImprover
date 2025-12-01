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
}

using Xunit;
namespace FluxImprover.Tests.Services;

using FluentAssertions;
using FluxImprover.Services;
using NSubstitute;

public class IEmbeddingServiceTests
{
    [Fact]
    public async Task EmbedAsync_WithValidText_ReturnsEmbedding()
    {
        // Arrange
        var service = Substitute.For<IEmbeddingService>();
        var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f };

        service.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReadOnlyMemory<float>(expectedEmbedding));

        // Act
        var result = await service.EmbedAsync("Test text");

        // Assert
        result.Length.Should().Be(3);
        result.Span[0].Should().Be(0.1f);
    }

    [Fact]
    public async Task EmbedBatchAsync_WithMultipleTexts_ReturnsMultipleEmbeddings()
    {
        // Arrange
        var service = Substitute.For<IEmbeddingService>();
        var texts = new[] { "text1", "text2", "text3" };
        var embeddings = texts.Select(_ => new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f })).ToList();

        service.EmbedBatchAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(embeddings);

        // Act
        var result = await service.EmbedBatchAsync(texts);

        // Assert
        result.Should().HaveCount(3);
        result.All(e => e.Length == 2).Should().BeTrue();
    }

    [Fact]
    public async Task EmbedAsync_WithEmptyText_StillReturnsEmbedding()
    {
        // Arrange
        var service = Substitute.For<IEmbeddingService>();
        service.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReadOnlyMemory<float>(new float[] { 0.0f }));

        // Act
        var result = await service.EmbedAsync("");

        // Assert
        result.Length.Should().BeGreaterThan(0);
    }
}

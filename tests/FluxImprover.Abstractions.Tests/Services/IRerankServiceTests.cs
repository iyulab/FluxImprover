namespace FluxImprover.Abstractions.Tests.Services;

using FluentAssertions;
using FluxImprover.Abstractions.Services;
using NSubstitute;

public class IRerankServiceTests
{
    [Fact]
    public async Task RerankAsync_WithQueryAndDocuments_ReturnsRankedResults()
    {
        // Arrange
        var service = Substitute.For<IRerankService>();
        var query = "What is RAG?";
        var documents = new[] { "doc1", "doc2", "doc3" };

        var rerankResults = new List<RerankResult>
        {
            new(0, 0.9f, "doc1"),
            new(2, 0.7f, "doc3"),
            new(1, 0.5f, "doc2")
        };

        service.RerankAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(rerankResults);

        // Act
        var result = await service.RerankAsync(query, documents);

        // Assert
        result.Should().HaveCount(3);
        result.First().Score.Should().BeGreaterThan(result.Last().Score);
    }

    [Fact]
    public async Task RerankAsync_WithTopK_ReturnsLimitedResults()
    {
        // Arrange
        var service = Substitute.For<IRerankService>();
        var rerankResults = new List<RerankResult>
        {
            new(0, 0.9f, "doc1"),
            new(2, 0.7f, "doc3")
        };

        service.RerankAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), 2, Arg.Any<CancellationToken>())
            .Returns(rerankResults);

        // Act
        var result = await service.RerankAsync("query", ["doc1", "doc2", "doc3"], topK: 2);

        // Assert
        result.Should().HaveCount(2);
    }
}

public class RerankResultTests
{
    [Fact]
    public void RerankResult_StoresIndexScoreAndDocument()
    {
        // Act
        var result = new RerankResult(5, 0.85f, "document content");

        // Assert
        result.Index.Should().Be(5);
        result.Score.Should().Be(0.85f);
        result.Document.Should().Be("document content");
    }

    [Fact]
    public void RerankResult_ScoreShouldBeInValidRange()
    {
        // Arrange & Act
        var result = new RerankResult(0, 0.5f, "doc");

        // Assert
        result.Score.Should().BeInRange(0f, 1f);
    }
}

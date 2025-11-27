namespace FluxImprover.Tests.Enrichment;

using FluentAssertions;
using FluxImprover.Abstractions.Services;
using FluxImprover.Abstractions.Options;
using FluxImprover.Enrichment;
using NSubstitute;
using Xunit;

public sealed class KeywordExtractionServiceTests
{
    private readonly ITextCompletionService _completionService;
    private readonly KeywordExtractionService _sut;

    public KeywordExtractionServiceTests()
    {
        _completionService = Substitute.For<ITextCompletionService>();
        _sut = new KeywordExtractionService(_completionService);
    }

    [Fact]
    public async Task ExtractKeywordsAsync_WithValidText_ReturnsKeywords()
    {
        // Arrange
        var text = "Machine learning and artificial intelligence are transforming technology.";
        var expectedResponse = @"{""keywords"": [{""keyword"": ""machine learning"", ""category"": ""technical"", ""relevance"": 0.95}, {""keyword"": ""artificial intelligence"", ""category"": ""technical"", ""relevance"": 0.92}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.ExtractKeywordsAsync(text);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("machine learning");
    }

    [Fact]
    public async Task ExtractKeywordsAsync_WithMaxKeywords_LimitsResults()
    {
        // Arrange
        var text = "Sample text for keyword extraction.";
        var options = new EnrichmentOptions { MaxKeywords = 3 };
        var expectedResponse = @"{""keywords"": [{""keyword"": ""keyword1"", ""relevance"": 0.9}, {""keyword"": ""keyword2"", ""relevance"": 0.8}, {""keyword"": ""keyword3"", ""relevance"": 0.7}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.ExtractKeywordsAsync(text, options);

        // Assert
        result.Should().HaveCountLessOrEqualTo(3);
    }

    [Fact]
    public async Task ExtractKeywordsAsync_WithEmptyText_ReturnsEmptyList()
    {
        // Arrange & Act
        var result = await _sut.ExtractKeywordsAsync(string.Empty);

        // Assert
        result.Should().BeEmpty();
        await _completionService.DidNotReceive().CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExtractKeywordsAsync_WithInvalidJsonResponse_ReturnsEmptyList()
    {
        // Arrange
        var text = "Some text to process.";
        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("invalid json response");

        // Act
        var result = await _sut.ExtractKeywordsAsync(text);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractKeywordsWithScoresAsync_ReturnsKeywordScorePairs()
    {
        // Arrange
        var text = "Natural language processing enables AI systems.";
        var expectedResponse = @"{""keywords"": [{""keyword"": ""natural language processing"", ""relevance"": 0.95}, {""keyword"": ""AI systems"", ""relevance"": 0.88}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.ExtractKeywordsWithScoresAsync(text);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().ContainKey("natural language processing");
        result["natural language processing"].Should().BeApproximately(0.95, 0.01);
    }

    [Fact]
    public async Task ExtractKeywordsBatchAsync_WithMultipleTexts_ReturnsAllKeywords()
    {
        // Arrange
        var texts = new[] { "Text about AI", "Text about ML" };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("""{"keywords": [{"keyword": "AI", "relevance": 0.9}]}""",
                     """{"keywords": [{"keyword": "ML", "relevance": 0.9}]}""");

        // Act
        var results = await _sut.ExtractKeywordsBatchAsync(texts);

        // Assert
        results.Should().HaveCount(2);
    }
}

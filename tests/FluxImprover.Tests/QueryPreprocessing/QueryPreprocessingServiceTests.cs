namespace FluxImprover.Tests.QueryPreprocessing;

using FluentAssertions;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.QueryPreprocessing;
using FluxImprover.Services;
using NSubstitute;
using Xunit;

public sealed class QueryPreprocessingServiceTests
{
    private readonly ITextCompletionService _completionService;
    private readonly QueryPreprocessingService _sut;

    public QueryPreprocessingServiceTests()
    {
        _completionService = Substitute.For<ITextCompletionService>();
        _sut = new QueryPreprocessingService(_completionService);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCompletionService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryPreprocessingService(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("completionService");
    }

    #endregion

    #region Normalize Tests

    [Fact]
    public void Normalize_WithUpperCase_ReturnsLowerCase()
    {
        // Arrange
        var query = "How to IMPLEMENT Authentication";

        // Act
        var result = _sut.Normalize(query);

        // Assert
        result.Should().Be("how to implement authentication");
    }

    [Fact]
    public void Normalize_WithExtraWhitespace_RemovesExtraSpaces()
    {
        // Arrange
        var query = "  how   to   implement  ";

        // Act
        var result = _sut.Normalize(query);

        // Assert
        result.Should().Be("how to implement");
    }

    [Fact]
    public void Normalize_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var query = "";

        // Act
        var result = _sut.Normalize(query);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Normalize_WithWhitespaceOnly_ReturnsEmptyString()
    {
        // Arrange
        var query = "   ";

        // Act
        var result = _sut.Normalize(query);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region PreprocessAsync Tests

    [Fact]
    public async Task PreprocessAsync_WithEmptyQuery_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => _sut.PreprocessAsync("");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("query");
    }

    [Fact]
    public async Task PreprocessAsync_WithValidQuery_ReturnsPreprocessedQuery()
    {
        // Arrange
        var query = "How do I implement authentication?";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("""["authentication", "implement", "security"]""");

        // Act
        var result = await _sut.PreprocessAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.OriginalQuery.Should().Be(query);
        result.NormalizedQuery.Should().Be("how do i implement authentication?");
        result.Intent.Should().Be(QueryIntent.HowTo);
    }

    [Fact]
    public async Task PreprocessAsync_WithTechnicalTerms_ExpandsAbbreviations()
    {
        // Arrange
        var query = "auth config for database"; // Using full words >= 3 chars for extraction
        var options = new QueryPreprocessingOptions
        {
            UseLlmExpansion = false, // Disable LLM to test built-in expansion
            ExpandTechnicalTerms = true
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("""["auth", "config", "database"]""");

        // Act
        var result = await _sut.PreprocessAsync(query, options);

        // Assert
        result.ExpandedKeywords.Should().Contain("authentication");
        result.ExpandedKeywords.Should().Contain("configuration");
        result.ExpandedKeywords.Should().Contain("database");
    }

    #endregion

    #region ClassifyIntentAsync Tests

    [Theory]
    [InlineData("What is dependency injection?", QueryIntent.Definition)] // "what is" matches Definition pattern
    [InlineData("When did the release happen?", QueryIntent.Question)] // "when" matches Question pattern
    [InlineData("Where is the config file?", QueryIntent.Search)] // "where is" matches Search pattern
    [InlineData("How do I implement caching?", QueryIntent.HowTo)]
    [InlineData("Define authentication", QueryIntent.Definition)]
    [InlineData("Compare REST vs GraphQL", QueryIntent.Comparison)]
    [InlineData("Error in authentication module", QueryIntent.Troubleshooting)]
    [InlineData("Find all controllers", QueryIntent.Search)]
    public async Task ClassifyIntentAsync_WithHeuristicPatterns_ReturnsExpectedIntent(
        string query, QueryIntent expectedIntent)
    {
        // Arrange
        var options = new QueryPreprocessingOptions
        {
            UseLlmIntentClassification = false // Use heuristic only
        };

        // Act
        var (intent, confidence) = await _sut.ClassifyIntentAsync(query, options);

        // Assert
        intent.Should().Be(expectedIntent);
        confidence.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public async Task ClassifyIntentAsync_WithLlmEnabled_UsesLlmForClassification()
    {
        // Arrange
        var query = "Some complex query";
        var options = new QueryPreprocessingOptions
        {
            UseLlmIntentClassification = true
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("""{"intent": "Code", "confidence": 0.95}""");

        // Act
        var (intent, confidence) = await _sut.ClassifyIntentAsync(query, options);

        // Assert
        intent.Should().Be(QueryIntent.Code);
        confidence.Should().BeApproximately(0.95, 0.01);
    }

    #endregion

    #region ExtractKeywordsAsync Tests

    [Fact]
    public async Task ExtractKeywordsAsync_WithValidQuery_ReturnsKeywords()
    {
        // Arrange
        var query = "How to implement user authentication with JWT";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("""["implement", "user", "authentication", "JWT"]""");

        // Act
        var result = await _sut.ExtractKeywordsAsync(query);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("implement");
        result.Should().Contain("authentication");
        result.Should().Contain("JWT");
    }

    [Fact]
    public async Task ExtractKeywordsAsync_WhenLlmFails_FallsBackToWordExtraction()
    {
        // Arrange
        var query = "How to implement authentication";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns<string>(x => throw new InvalidOperationException("LLM failed"));

        // Act
        var result = await _sut.ExtractKeywordsAsync(query);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("implement");
        result.Should().Contain("authentication");
    }

    #endregion

    #region ExpandWithSynonymsAsync Tests

    [Fact]
    public async Task ExpandWithSynonymsAsync_WithTechnicalTerms_ExpandsBuiltInSynonyms()
    {
        // Arrange
        var query = "auth config";
        var options = new QueryPreprocessingOptions
        {
            UseLlmExpansion = false,
            ExpandTechnicalTerms = true
        };

        // Act
        var result = await _sut.ExpandWithSynonymsAsync(query, options);

        // Assert
        result.Should().Contain("auth");
        result.Should().Contain("authentication");
        result.Should().Contain("authorization");
        result.Should().Contain("config");
        result.Should().Contain("configuration");
    }

    [Fact]
    public async Task ExpandWithSynonymsAsync_WithCustomSynonyms_IncludesCustomExpansions()
    {
        // Arrange
        var query = "custom term";
        var options = new QueryPreprocessingOptions
        {
            UseLlmExpansion = false,
            DomainSynonyms = new Dictionary<string, IReadOnlyList<string>>
            {
                ["custom"] = ["specialized", "specific"]
            }
        };

        // Act
        var result = await _sut.ExpandWithSynonymsAsync(query, options);

        // Assert
        result.Should().Contain("custom");
        result.Should().Contain("specialized");
        result.Should().Contain("specific");
    }

    [Fact]
    public async Task ExpandWithSynonymsAsync_WithLlmExpansion_IncludesLlmSynonyms()
    {
        // Arrange
        var query = "implement caching";
        var options = new QueryPreprocessingOptions
        {
            UseLlmExpansion = true
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("""["cache", "memorization", "storage"]""");

        // Act
        var result = await _sut.ExpandWithSynonymsAsync(query, options);

        // Assert
        result.Should().Contain("implement");
        result.Should().Contain("caching");
        result.Should().Contain("cache");
        result.Should().Contain("memorization");
    }

    #endregion

    #region ExtractEntitiesAsync Tests

    [Fact]
    public async Task ExtractEntitiesAsync_WithValidQuery_ReturnsEntities()
    {
        // Arrange
        var query = "How does UserService handle config.json?";
        var options = new QueryPreprocessingOptions { ExtractEntities = true };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("""{"types": ["UserService"], "files": ["config.json"]}""");

        // Act
        var result = await _sut.ExtractEntitiesAsync(query, options);

        // Assert
        result.Should().ContainKey("types");
        result["types"].Should().Contain("UserService");
        result.Should().ContainKey("files");
        result["files"].Should().Contain("config.json");
    }

    [Fact]
    public async Task ExtractEntitiesAsync_WhenLlmFails_ReturnsEmptyDictionary()
    {
        // Arrange
        var query = "some query";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns<string>(x => throw new InvalidOperationException("LLM failed"));

        // Act
        var result = await _sut.ExtractEntitiesAsync(query);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region PreprocessBatchAsync Tests

    [Fact]
    public async Task PreprocessBatchAsync_WithMultipleQueries_ReturnsAllResults()
    {
        // Arrange
        var queries = new[] { "query one", "query two", "query three" };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("""["query", "one"]""");

        // Act
        var results = await _sut.PreprocessBatchAsync(queries);

        // Assert
        results.Should().HaveCount(3);
        results[0].OriginalQuery.Should().Be("query one");
        results[1].OriginalQuery.Should().Be("query two");
        results[2].OriginalQuery.Should().Be("query three");
    }

    [Fact]
    public async Task PreprocessBatchAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var queries = Array.Empty<string>();

        // Act
        var results = await _sut.PreprocessBatchAsync(queries);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task PreprocessBatchAsync_WithNullQueries_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _sut.PreprocessBatchAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PreprocessBatchAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var queries = new[] { "query one", "query two" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = () => _sut.PreprocessBatchAsync(queries, cancellationToken: cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region SearchStrategy Tests

    [Fact]
    public async Task PreprocessAsync_WithCodeIntent_SuggestsKeywordStrategy()
    {
        // Arrange
        var query = "function implementation syntax example";
        var options = new QueryPreprocessingOptions
        {
            UseLlmExpansion = false,
            UseLlmIntentClassification = false
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("""["function", "implementation"]""");

        // Act
        var result = await _sut.PreprocessAsync(query, options);

        // Assert
        result.Intent.Should().Be(QueryIntent.Code);
        result.SuggestedStrategy.Should().Be(SearchStrategy.Keyword);
    }

    [Fact]
    public async Task PreprocessAsync_WithDefinitionIntent_SuggestsSemanticStrategy()
    {
        // Arrange
        var query = "Explain dependency injection"; // Use "Explain" to trigger definition pattern
        var options = new QueryPreprocessingOptions
        {
            UseLlmExpansion = false,
            UseLlmIntentClassification = false
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("""["dependency", "injection"]""");

        // Act
        var result = await _sut.PreprocessAsync(query, options);

        // Assert
        result.Intent.Should().Be(QueryIntent.Definition);
        result.SuggestedStrategy.Should().Be(SearchStrategy.Semantic);
    }

    #endregion
}

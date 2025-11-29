namespace FluxImprover.Tests.Models;

using FluentAssertions;
using FluxImprover.Models;
using Xunit;

public sealed class PreprocessedQueryTests
{
    [Fact]
    public void PreprocessedQuery_CanBeCreated_WithRequiredProperties()
    {
        // Act
        var query = new PreprocessedQuery
        {
            OriginalQuery = "test query",
            NormalizedQuery = "test query",
            ExpandedQuery = "test query expanded",
            Keywords = ["test", "query"],
            ExpandedKeywords = ["test", "query", "expanded"],
            Intent = QueryIntent.Search,
            IntentConfidence = 0.85,
            Entities = new Dictionary<string, IReadOnlyList<string>>(),
            SuggestedStrategy = SearchStrategy.Hybrid
        };

        // Assert
        query.OriginalQuery.Should().Be("test query");
        query.NormalizedQuery.Should().Be("test query");
        query.ExpandedQuery.Should().Be("test query expanded");
        query.Keywords.Should().HaveCount(2);
        query.ExpandedKeywords.Should().HaveCount(3);
        query.Intent.Should().Be(QueryIntent.Search);
        query.IntentConfidence.Should().BeApproximately(0.85, 0.01);
        query.Entities.Should().BeEmpty();
        query.SuggestedStrategy.Should().Be(SearchStrategy.Hybrid);
        query.Metadata.Should().BeNull();
    }

    [Fact]
    public void PreprocessedQuery_WithMetadata_HasMetadata()
    {
        // Act
        var query = new PreprocessedQuery
        {
            OriginalQuery = "test",
            NormalizedQuery = "test",
            ExpandedQuery = "test",
            Keywords = [],
            ExpandedKeywords = [],
            Intent = QueryIntent.General,
            IntentConfidence = 0.5,
            Entities = new Dictionary<string, IReadOnlyList<string>>(),
            SuggestedStrategy = SearchStrategy.Semantic,
            Metadata = new Dictionary<string, object>
            {
                ["processingTimeMs"] = 123.45,
                ["usedLlmExpansion"] = true
            }
        };

        // Assert
        query.Metadata.Should().NotBeNull();
        query.Metadata.Should().ContainKey("processingTimeMs");
        query.Metadata!["processingTimeMs"].Should().Be(123.45);
    }

    [Fact]
    public void PreprocessedQuery_WithEntities_HasEntities()
    {
        // Act
        var query = new PreprocessedQuery
        {
            OriginalQuery = "UserService config",
            NormalizedQuery = "userservice config",
            ExpandedQuery = "userservice config",
            Keywords = ["userservice", "config"],
            ExpandedKeywords = ["userservice", "config", "configuration"],
            Intent = QueryIntent.Search,
            IntentConfidence = 0.75,
            Entities = new Dictionary<string, IReadOnlyList<string>>
            {
                ["types"] = ["UserService"],
                ["concepts"] = ["configuration"]
            },
            SuggestedStrategy = SearchStrategy.Hybrid
        };

        // Assert
        query.Entities.Should().HaveCount(2);
        query.Entities["types"].Should().Contain("UserService");
        query.Entities["concepts"].Should().Contain("configuration");
    }
}

public sealed class QueryIntentTests
{
    [Theory]
    [InlineData(QueryIntent.General, "General")]
    [InlineData(QueryIntent.Question, "Question")]
    [InlineData(QueryIntent.Search, "Search")]
    [InlineData(QueryIntent.Definition, "Definition")]
    [InlineData(QueryIntent.Comparison, "Comparison")]
    [InlineData(QueryIntent.HowTo, "HowTo")]
    [InlineData(QueryIntent.Troubleshooting, "Troubleshooting")]
    [InlineData(QueryIntent.Code, "Code")]
    [InlineData(QueryIntent.Conceptual, "Conceptual")]
    public void QueryIntent_HasExpectedValues(QueryIntent intent, string expectedName)
    {
        // Assert
        intent.ToString().Should().Be(expectedName);
    }
}

public sealed class SearchStrategyTests
{
    [Theory]
    [InlineData(SearchStrategy.Semantic, "Semantic")]
    [InlineData(SearchStrategy.Keyword, "Keyword")]
    [InlineData(SearchStrategy.Hybrid, "Hybrid")]
    [InlineData(SearchStrategy.MultiQuery, "MultiQuery")]
    public void SearchStrategy_HasExpectedValues(SearchStrategy strategy, string expectedName)
    {
        // Assert
        strategy.ToString().Should().Be(expectedName);
    }
}

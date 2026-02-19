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
            Intent = QueryClassification.Search,
            IntentConfidence = 0.85,
            Entities = new Dictionary<string, IReadOnlyList<string>>(),
            SuggestedStrategy = RecommendedSearchMode.Hybrid
        };

        // Assert
        query.OriginalQuery.Should().Be("test query");
        query.NormalizedQuery.Should().Be("test query");
        query.ExpandedQuery.Should().Be("test query expanded");
        query.Keywords.Should().HaveCount(2);
        query.ExpandedKeywords.Should().HaveCount(3);
        query.Intent.Should().Be(QueryClassification.Search);
        query.IntentConfidence.Should().BeApproximately(0.85, 0.01);
        query.Entities.Should().BeEmpty();
        query.SuggestedStrategy.Should().Be(RecommendedSearchMode.Hybrid);
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
            Intent = QueryClassification.General,
            IntentConfidence = 0.5,
            Entities = new Dictionary<string, IReadOnlyList<string>>(),
            SuggestedStrategy = RecommendedSearchMode.Semantic,
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
            Intent = QueryClassification.Search,
            IntentConfidence = 0.75,
            Entities = new Dictionary<string, IReadOnlyList<string>>
            {
                ["types"] = ["UserService"],
                ["concepts"] = ["configuration"]
            },
            SuggestedStrategy = RecommendedSearchMode.Hybrid
        };

        // Assert
        query.Entities.Should().HaveCount(2);
        query.Entities["types"].Should().Contain("UserService");
        query.Entities["concepts"].Should().Contain("configuration");
    }
}

public sealed class QueryClassificationTests
{
    [Theory]
    [InlineData(QueryClassification.General, "General")]
    [InlineData(QueryClassification.Question, "Question")]
    [InlineData(QueryClassification.Search, "Search")]
    [InlineData(QueryClassification.Definition, "Definition")]
    [InlineData(QueryClassification.Comparison, "Comparison")]
    [InlineData(QueryClassification.HowTo, "HowTo")]
    [InlineData(QueryClassification.Troubleshooting, "Troubleshooting")]
    [InlineData(QueryClassification.Code, "Code")]
    [InlineData(QueryClassification.Conceptual, "Conceptual")]
    public void QueryClassification_HasExpectedValues(QueryClassification intent, string expectedName)
    {
        // Assert
        intent.ToString().Should().Be(expectedName);
    }
}

public sealed class RecommendedSearchModeTests
{
    [Theory]
    [InlineData(RecommendedSearchMode.Semantic, "Semantic")]
    [InlineData(RecommendedSearchMode.Keyword, "Keyword")]
    [InlineData(RecommendedSearchMode.Hybrid, "Hybrid")]
    [InlineData(RecommendedSearchMode.MultiQuery, "MultiQuery")]
    public void RecommendedSearchMode_HasExpectedValues(RecommendedSearchMode strategy, string expectedName)
    {
        // Assert
        strategy.ToString().Should().Be(expectedName);
    }
}

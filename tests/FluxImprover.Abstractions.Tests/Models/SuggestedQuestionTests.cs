namespace FluxImprover.Abstractions.Tests.Models;

using System.Text.Json;
using FluentAssertions;
using FluxImprover.Models;

public class SuggestedQuestionTests
{
    [Fact]
    public void SuggestedQuestion_WithText_StoresCorrectly()
    {
        // Act
        var suggestion = new SuggestedQuestion
        {
            Text = "What are the benefits of RAG?",
            Relevance = 0.9
        };

        // Assert
        suggestion.Text.Should().Be("What are the benefits of RAG?");
        suggestion.Relevance.Should().Be(0.9);
    }

    [Fact]
    public void SuggestedQuestion_WithCategory_StoresCorrectly()
    {
        // Act
        var suggestion = new SuggestedQuestion
        {
            Text = "How does embedding work?",
            Category = QuestionCategory.DeepDive,
            Relevance = 0.85
        };

        // Assert
        suggestion.Category.Should().Be(QuestionCategory.DeepDive);
    }

    [Fact]
    public void SuggestedQuestion_Serialization_RoundTrips()
    {
        // Arrange
        var original = new SuggestedQuestion
        {
            Text = "Follow-up question",
            Category = QuestionCategory.Clarification,
            Relevance = 0.75
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<SuggestedQuestion>(json, options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Text.Should().Be(original.Text);
        deserialized.Category.Should().Be(QuestionCategory.Clarification);
    }

    [Theory]
    [InlineData(QuestionCategory.FollowUp)]
    [InlineData(QuestionCategory.Clarification)]
    [InlineData(QuestionCategory.DeepDive)]
    [InlineData(QuestionCategory.Related)]
    [InlineData(QuestionCategory.Alternative)]
    public void QuestionCategory_AllValues_AreValid(QuestionCategory category)
    {
        // Act
        var suggestion = new SuggestedQuestion
        {
            Text = "Question",
            Category = category
        };

        // Assert
        suggestion.Category.Should().Be(category);
    }
}

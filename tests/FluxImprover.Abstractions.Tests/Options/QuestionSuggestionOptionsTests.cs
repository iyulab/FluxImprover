namespace FluxImprover.Abstractions.Tests.Options;

using FluentAssertions;
using FluxImprover.Models;
using FluxImprover.Options;
using Xunit;

public sealed class QuestionSuggestionOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeReasonable()
    {
        // Arrange & Act
        var options = new QuestionSuggestionOptions();

        // Assert
        options.MaxSuggestions.Should().Be(5);
        options.Temperature.Should().BeNull();
        options.MaxTokens.Should().Be(1024);
        options.IncludeReasoning.Should().BeFalse();
        options.MinRelevanceScore.Should().Be(0.5f);
        options.Categories.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void MaxSuggestions_ValidValues_ShouldBeAccepted(int value)
    {
        // Arrange & Act
        var options = new QuestionSuggestionOptions { MaxSuggestions = value };

        // Assert
        options.MaxSuggestions.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxSuggestions_InvalidValues_ShouldThrow(int value)
    {
        // Arrange & Act
        var act = () => new QuestionSuggestionOptions { MaxSuggestions = value };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(2.0f)]
    public void Temperature_ValidValues_ShouldBeAccepted(float value)
    {
        // Arrange & Act
        var options = new QuestionSuggestionOptions { Temperature = value };

        // Assert
        options.Temperature.Should().Be(value);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(2.1f)]
    public void Temperature_InvalidValues_ShouldThrow(float value)
    {
        // Arrange & Act
        var act = () => new QuestionSuggestionOptions { Temperature = value };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(4096)]
    public void MaxTokens_ValidValues_ShouldBeAccepted(int value)
    {
        // Arrange & Act
        var options = new QuestionSuggestionOptions { MaxTokens = value };

        // Assert
        options.MaxTokens.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxTokens_InvalidValues_ShouldThrow(int value)
    {
        // Arrange & Act
        var act = () => new QuestionSuggestionOptions { MaxTokens = value };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void MinRelevanceScore_ValidValues_ShouldBeAccepted(float value)
    {
        // Arrange & Act
        var options = new QuestionSuggestionOptions { MinRelevanceScore = value };

        // Assert
        options.MinRelevanceScore.Should().Be(value);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void MinRelevanceScore_InvalidValues_ShouldThrow(float value)
    {
        // Arrange & Act
        var act = () => new QuestionSuggestionOptions { MinRelevanceScore = value };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Categories_DefaultValues_ShouldIncludeCommonCategories()
    {
        // Arrange & Act
        var options = new QuestionSuggestionOptions();

        // Assert
        options.Categories.Should().Contain(QuestionCategory.FollowUp);
        options.Categories.Should().Contain(QuestionCategory.Clarification);
    }

    [Fact]
    public void Categories_CustomValues_ShouldBeConfigurable()
    {
        // Arrange & Act
        var categories = new[] { QuestionCategory.DeepDive, QuestionCategory.Alternative };
        var options = new QuestionSuggestionOptions { Categories = categories };

        // Assert
        options.Categories.Should().BeEquivalentTo(categories);
    }

    [Fact]
    public void ContextWindowSize_DefaultValue_ShouldBeReasonable()
    {
        // Arrange & Act
        var options = new QuestionSuggestionOptions();

        // Assert
        options.ContextWindowSize.Should().Be(5);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(20)]
    public void ContextWindowSize_ValidValues_ShouldBeAccepted(int value)
    {
        // Arrange & Act
        var options = new QuestionSuggestionOptions { ContextWindowSize = value };

        // Assert
        options.ContextWindowSize.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ContextWindowSize_InvalidValues_ShouldThrow(int value)
    {
        // Arrange & Act
        var act = () => new QuestionSuggestionOptions { ContextWindowSize = value };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

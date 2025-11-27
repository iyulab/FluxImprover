namespace FluxImprover.Abstractions.Tests.Options;

using FluentAssertions;
using FluxImprover.Abstractions.Models;
using FluxImprover.Abstractions.Options;
using Xunit;

public sealed class QAGenerationOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeReasonable()
    {
        // Arrange & Act
        var options = new QAGenerationOptions();

        // Assert
        options.PairsPerChunk.Should().Be(3);
        options.Temperature.Should().BeNull();
        options.MaxTokens.Should().Be(2048);
        options.IncludeMultiHop.Should().BeFalse();
        options.IncludeReasoning.Should().BeTrue();
        options.MinAnswerLength.Should().Be(10);
        options.MaxAnswerLength.Should().Be(500);
        options.DifficultyDistribution.Should().NotBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void PairsPerChunk_ValidValues_ShouldBeAccepted(int value)
    {
        // Arrange & Act
        var options = new QAGenerationOptions { PairsPerChunk = value };

        // Assert
        options.PairsPerChunk.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PairsPerChunk_InvalidValues_ShouldThrow(int value)
    {
        // Arrange & Act
        var act = () => new QAGenerationOptions { PairsPerChunk = value };

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
        var options = new QAGenerationOptions { Temperature = value };

        // Assert
        options.Temperature.Should().Be(value);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(2.1f)]
    public void Temperature_InvalidValues_ShouldThrow(float value)
    {
        // Arrange & Act
        var act = () => new QAGenerationOptions { Temperature = value };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DifficultyDistribution_DefaultValues_ShouldSumToOne()
    {
        // Arrange & Act
        var options = new QAGenerationOptions();

        // Assert
        var sum = options.DifficultyDistribution.Easy +
                  options.DifficultyDistribution.Medium +
                  options.DifficultyDistribution.Hard;
        sum.Should().BeApproximately(1.0f, 0.001f);
    }

    [Fact]
    public void QuestionTypes_DefaultValues_ShouldIncludeCommonTypes()
    {
        // Arrange & Act
        var options = new QAGenerationOptions();

        // Assert
        options.QuestionTypes.Should().Contain(QuestionType.Factual);
        options.QuestionTypes.Should().Contain(QuestionType.Reasoning);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(4096)]
    public void MaxTokens_ValidValues_ShouldBeAccepted(int value)
    {
        // Arrange & Act
        var options = new QAGenerationOptions { MaxTokens = value };

        // Assert
        options.MaxTokens.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxTokens_InvalidValues_ShouldThrow(int value)
    {
        // Arrange & Act
        var act = () => new QAGenerationOptions { MaxTokens = value };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MinAnswerLength_GreaterThanMax_ShouldThrow()
    {
        // Arrange & Act
        var act = () => new QAGenerationOptions
        {
            MinAnswerLength = 100,
            MaxAnswerLength = 50
        };

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}

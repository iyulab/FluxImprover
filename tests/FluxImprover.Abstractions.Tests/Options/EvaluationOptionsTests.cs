namespace FluxImprover.Abstractions.Tests.Options;

using FluentAssertions;
using FluxImprover.Abstractions.Options;
using Xunit;

public sealed class EvaluationOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeReasonable()
    {
        // Arrange & Act
        var options = new EvaluationOptions();

        // Assert
        options.EnableFaithfulness.Should().BeTrue();
        options.EnableRelevancy.Should().BeTrue();
        options.EnableAnswerability.Should().BeTrue();
        options.Temperature.Should().BeNull();
        options.MaxTokens.Should().Be(1024);
        options.PassThreshold.Should().Be(0.7f);
        options.IncludeDetails.Should().BeTrue();
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void Temperature_ValidValues_ShouldBeAccepted(float value)
    {
        // Arrange & Act
        var options = new EvaluationOptions { Temperature = value };

        // Assert
        options.Temperature.Should().Be(value);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(2.1f)]
    public void Temperature_InvalidValues_ShouldThrow(float value)
    {
        // Arrange & Act
        var act = () => new EvaluationOptions { Temperature = value };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void PassThreshold_ValidValues_ShouldBeAccepted(float value)
    {
        // Arrange & Act
        var options = new EvaluationOptions { PassThreshold = value };

        // Assert
        options.PassThreshold.Should().Be(value);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void PassThreshold_InvalidValues_ShouldThrow(float value)
    {
        // Arrange & Act
        var act = () => new EvaluationOptions { PassThreshold = value };

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
        var options = new EvaluationOptions { MaxTokens = value };

        // Assert
        options.MaxTokens.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxTokens_InvalidValues_ShouldThrow(int value)
    {
        // Arrange & Act
        var act = () => new EvaluationOptions { MaxTokens = value };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AllMetricsDisabled_ShouldBeAllowed()
    {
        // Arrange & Act
        var options = new EvaluationOptions
        {
            EnableFaithfulness = false,
            EnableRelevancy = false,
            EnableAnswerability = false
        };

        // Assert - Should not throw, validation happens at runtime
        options.EnableFaithfulness.Should().BeFalse();
        options.EnableRelevancy.Should().BeFalse();
        options.EnableAnswerability.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void MaxRetries_ValidValues_ShouldBeAccepted(int value)
    {
        // Arrange & Act
        var options = new EvaluationOptions { MaxRetries = value };

        // Assert
        options.MaxRetries.Should().Be(value);
    }

    [Theory]
    [InlineData(-1)]
    public void MaxRetries_InvalidValues_ShouldThrow(int value)
    {
        // Arrange & Act
        var act = () => new EvaluationOptions { MaxRetries = value };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

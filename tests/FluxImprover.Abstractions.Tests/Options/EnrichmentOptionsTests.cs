namespace FluxImprover.Abstractions.Tests.Options;

using FluentAssertions;
using FluxImprover.Abstractions.Options;
using Xunit;

public sealed class EnrichmentOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeReasonable()
    {
        // Arrange & Act
        var options = new EnrichmentOptions();

        // Assert
        options.EnableSummarization.Should().BeTrue();
        options.EnableKeywordExtraction.Should().BeTrue();
        options.EnableEntityExtraction.Should().BeFalse();
        options.Temperature.Should().BeNull();
        options.MaxTokens.Should().Be(512);
        options.MaxKeywords.Should().Be(10);
        options.MaxSummaryLength.Should().Be(200);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(2.0f)]
    public void Temperature_ValidValues_ShouldBeAccepted(float value)
    {
        // Arrange & Act
        var options = new EnrichmentOptions { Temperature = value };

        // Assert
        options.Temperature.Should().Be(value);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(2.1f)]
    public void Temperature_InvalidValues_ShouldThrow(float value)
    {
        // Arrange & Act
        var act = () => new EnrichmentOptions { Temperature = value };

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
        var options = new EnrichmentOptions { MaxTokens = value };

        // Assert
        options.MaxTokens.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxTokens_InvalidValues_ShouldThrow(int value)
    {
        // Arrange & Act
        var act = () => new EnrichmentOptions { MaxTokens = value };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(20)]
    public void MaxKeywords_ValidValues_ShouldBeAccepted(int value)
    {
        // Arrange & Act
        var options = new EnrichmentOptions { MaxKeywords = value };

        // Assert
        options.MaxKeywords.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxKeywords_InvalidValues_ShouldThrow(int value)
    {
        // Arrange & Act
        var act = () => new EnrichmentOptions { MaxKeywords = value };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(500)]
    public void MaxSummaryLength_ValidValues_ShouldBeAccepted(int value)
    {
        // Arrange & Act
        var options = new EnrichmentOptions { MaxSummaryLength = value };

        // Assert
        options.MaxSummaryLength.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxSummaryLength_InvalidValues_ShouldThrow(int value)
    {
        // Arrange & Act
        var act = () => new EnrichmentOptions { MaxSummaryLength = value };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AllEnrichmentsDisabled_ShouldBeAllowed()
    {
        // Arrange & Act
        var options = new EnrichmentOptions
        {
            EnableSummarization = false,
            EnableKeywordExtraction = false,
            EnableEntityExtraction = false
        };

        // Assert - Configuration is allowed, runtime validation separate
        options.EnableSummarization.Should().BeFalse();
        options.EnableKeywordExtraction.Should().BeFalse();
        options.EnableEntityExtraction.Should().BeFalse();
    }

    [Fact]
    public void CustomEntityTypes_ShouldBeConfigurable()
    {
        // Arrange & Act
        var entityTypes = new[] { "Person", "Organization", "Location" };
        var options = new EnrichmentOptions { EntityTypes = entityTypes };

        // Assert
        options.EntityTypes.Should().BeEquivalentTo(entityTypes);
    }
}

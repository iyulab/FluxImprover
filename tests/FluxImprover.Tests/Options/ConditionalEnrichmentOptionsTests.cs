namespace FluxImprover.Tests.Options;

using FluentAssertions;
using FluxImprover.Options;
using Xunit;

public sealed class ConditionalEnrichmentOptionsTests
{
    [Fact]
    public void ConditionalEnrichmentOptions_HasCorrectDefaults()
    {
        // Arrange & Act
        var options = new ConditionalEnrichmentOptions();

        // Assert
        options.EnableConditionalEnrichment.Should().BeFalse();
        options.SkipEnrichmentThreshold.Should().Be(0.8f);
        options.MinSummarizationLength.Should().Be(500);
        options.MinKeywordDensity.Should().Be(0.3f);
        options.IncludeQualityMetrics.Should().BeTrue();
        options.DomainGlossary.Should().BeNull();
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void SkipEnrichmentThreshold_AcceptsValidValues(float threshold)
    {
        // Arrange & Act
        var options = new ConditionalEnrichmentOptions
        {
            SkipEnrichmentThreshold = threshold
        };

        // Assert
        options.SkipEnrichmentThreshold.Should().Be(threshold);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void SkipEnrichmentThreshold_ThrowsForInvalidValues(float threshold)
    {
        // Arrange & Act
        var act = () => new ConditionalEnrichmentOptions
        {
            SkipEnrichmentThreshold = threshold
        };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(1000)]
    public void MinSummarizationLength_AcceptsValidValues(int length)
    {
        // Arrange & Act
        var options = new ConditionalEnrichmentOptions
        {
            MinSummarizationLength = length
        };

        // Assert
        options.MinSummarizationLength.Should().Be(length);
    }

    [Fact]
    public void MinSummarizationLength_ThrowsForNegativeValue()
    {
        // Arrange & Act
        var act = () => new ConditionalEnrichmentOptions
        {
            MinSummarizationLength = -1
        };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void MinKeywordDensity_AcceptsValidValues(float density)
    {
        // Arrange & Act
        var options = new ConditionalEnrichmentOptions
        {
            MinKeywordDensity = density
        };

        // Assert
        options.MinKeywordDensity.Should().Be(density);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void MinKeywordDensity_ThrowsForInvalidValues(float density)
    {
        // Arrange & Act
        var act = () => new ConditionalEnrichmentOptions
        {
            MinKeywordDensity = density
        };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void EnrichmentOptions_CanIncludeConditionalOptions()
    {
        // Arrange & Act
        var options = new EnrichmentOptions
        {
            ConditionalOptions = new ConditionalEnrichmentOptions
            {
                EnableConditionalEnrichment = true,
                SkipEnrichmentThreshold = 0.7f
            }
        };

        // Assert
        options.ConditionalOptions.Should().NotBeNull();
        options.ConditionalOptions!.EnableConditionalEnrichment.Should().BeTrue();
        options.ConditionalOptions.SkipEnrichmentThreshold.Should().Be(0.7f);
    }

    [Fact]
    public void IDomainGlossary_CanBeAssigned()
    {
        // Arrange
        var mockGlossary = new TestDomainGlossary();

        // Act
        var options = new ConditionalEnrichmentOptions
        {
            DomainGlossary = mockGlossary
        };

        // Assert
        options.DomainGlossary.Should().BeSameAs(mockGlossary);
    }

    private sealed class TestDomainGlossary : IDomainGlossary
    {
        public string ExpandTerms(string text) => text;
        public string? GetExpansion(string term) => null;
    }
}

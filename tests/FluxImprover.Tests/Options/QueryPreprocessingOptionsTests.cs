namespace FluxImprover.Tests.Options;

using FluentAssertions;
using FluxImprover.Options;
using Xunit;

public sealed class QueryPreprocessingOptionsTests
{
    [Fact]
    public void DefaultOptions_HasValidDefaults()
    {
        // Act
        var options = new QueryPreprocessingOptions();

        // Assert
        options.UseLlmExpansion.Should().BeTrue();
        options.UseLlmIntentClassification.Should().BeTrue();
        options.ExtractEntities.Should().BeTrue();
        options.MaxSynonymsPerKeyword.Should().Be(3);
        options.MaxKeywords.Should().Be(10);
        options.Temperature.Should().BeApproximately(0.3f, 0.01f);
        options.MaxTokens.Should().Be(500);
        options.Language.Should().Be("en");
        options.ExpandTechnicalTerms.Should().BeTrue();
        options.MinIntentConfidence.Should().BeApproximately(0.5f, 0.01f);
        options.DomainSynonyms.Should().BeNull();
    }

    [Fact]
    public void Options_CanBeCustomized()
    {
        // Act
        var options = new QueryPreprocessingOptions
        {
            UseLlmExpansion = false,
            UseLlmIntentClassification = false,
            ExtractEntities = false,
            MaxSynonymsPerKeyword = 5,
            MaxKeywords = 20,
            Temperature = 0.7f,
            MaxTokens = 1000,
            Language = "ko",
            ExpandTechnicalTerms = false,
            MinIntentConfidence = 0.8f,
            DomainSynonyms = new Dictionary<string, IReadOnlyList<string>>
            {
                ["test"] = ["example", "sample"]
            }
        };

        // Assert
        options.UseLlmExpansion.Should().BeFalse();
        options.UseLlmIntentClassification.Should().BeFalse();
        options.ExtractEntities.Should().BeFalse();
        options.MaxSynonymsPerKeyword.Should().Be(5);
        options.MaxKeywords.Should().Be(20);
        options.Temperature.Should().BeApproximately(0.7f, 0.01f);
        options.MaxTokens.Should().Be(1000);
        options.Language.Should().Be("ko");
        options.ExpandTechnicalTerms.Should().BeFalse();
        options.MinIntentConfidence.Should().BeApproximately(0.8f, 0.01f);
        options.DomainSynonyms.Should().ContainKey("test");
    }

    #region Temperature Validation Tests

    [Theory]
    [InlineData(0f)]
    [InlineData(0.5f)]
    [InlineData(1f)]
    [InlineData(2f)]
    public void Temperature_ValidValues_ShouldBeAccepted(float value)
    {
        // Act
        var options = new QueryPreprocessingOptions { Temperature = value };

        // Assert
        options.Temperature.Should().BeApproximately(value, 0.01f);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(2.1f)]
    public void Temperature_InvalidValues_ShouldThrow(float value)
    {
        // Act & Assert
        var act = () => new QueryPreprocessingOptions { Temperature = value };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region MaxTokens Validation Tests

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(4096)]
    public void MaxTokens_ValidValues_ShouldBeAccepted(int value)
    {
        // Act
        var options = new QueryPreprocessingOptions { MaxTokens = value };

        // Assert
        options.MaxTokens.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxTokens_InvalidValues_ShouldThrow(int value)
    {
        // Act & Assert
        var act = () => new QueryPreprocessingOptions { MaxTokens = value };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region MaxKeywords Validation Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(20)]
    public void MaxKeywords_ValidValues_ShouldBeAccepted(int value)
    {
        // Act
        var options = new QueryPreprocessingOptions { MaxKeywords = value };

        // Assert
        options.MaxKeywords.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxKeywords_InvalidValues_ShouldThrow(int value)
    {
        // Act & Assert
        var act = () => new QueryPreprocessingOptions { MaxKeywords = value };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region MaxSynonymsPerKeyword Validation Tests

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void MaxSynonymsPerKeyword_ValidValues_ShouldBeAccepted(int value)
    {
        // Act
        var options = new QueryPreprocessingOptions { MaxSynonymsPerKeyword = value };

        // Assert
        options.MaxSynonymsPerKeyword.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxSynonymsPerKeyword_InvalidValues_ShouldThrow(int value)
    {
        // Act & Assert
        var act = () => new QueryPreprocessingOptions { MaxSynonymsPerKeyword = value };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region MinIntentConfidence Validation Tests

    [Theory]
    [InlineData(0f)]
    [InlineData(0.5f)]
    [InlineData(1f)]
    public void MinIntentConfidence_ValidValues_ShouldBeAccepted(float value)
    {
        // Act
        var options = new QueryPreprocessingOptions { MinIntentConfidence = value };

        // Assert
        options.MinIntentConfidence.Should().BeApproximately(value, 0.01f);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void MinIntentConfidence_InvalidValues_ShouldThrow(float value)
    {
        // Act & Assert
        var act = () => new QueryPreprocessingOptions { MinIntentConfidence = value };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion
}

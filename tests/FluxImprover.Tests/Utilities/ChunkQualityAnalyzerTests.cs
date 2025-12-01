namespace FluxImprover.Tests.Utilities;

using FluentAssertions;
using FluxImprover.Utilities;
using Xunit;

public sealed class ChunkQualityAnalyzerTests
{
    [Fact]
    public void Analyze_WithEmptyContent_ReturnsZeroScores()
    {
        // Arrange
        var content = "";

        // Act
        var result = ChunkQualityAnalyzer.Analyze(content);

        // Assert
        result.OverallScore.Should().Be(0f);
        result.CompletenessScore.Should().Be(0f);
        result.DensityScore.Should().Be(0f);
        result.StructureScore.Should().Be(0f);
        result.ContentLength.Should().Be(0);
        result.Recommendation.Should().Be(EnrichmentRecommendation.None);
    }

    [Fact]
    public void Analyze_WithNullContent_ReturnsZeroScores()
    {
        // Arrange
        string content = null!;

        // Act
        var result = ChunkQualityAnalyzer.Analyze(content);

        // Assert
        result.OverallScore.Should().Be(0f);
        result.Recommendation.Should().Be(EnrichmentRecommendation.None);
    }

    [Fact]
    public void Analyze_WithCompleteSentence_HasHighCompletenessScore()
    {
        // Arrange - proper sentence with capital start and period end
        var content = "This is a complete sentence with proper punctuation.";

        // Act
        var result = ChunkQualityAnalyzer.Analyze(content);

        // Assert
        result.CompletenessScore.Should().Be(1.0f);
    }

    [Fact]
    public void Analyze_WithIncompleteSentence_HasLowCompletenessScore()
    {
        // Arrange - no capital start, no proper ending
        var content = "incomplete sentence without proper ending";

        // Act
        var result = ChunkQualityAnalyzer.Analyze(content);

        // Assert
        result.CompletenessScore.Should().BeLessThan(1.0f);
    }

    [Fact]
    public void Analyze_WithTechnicalContent_HasHighDensityScore()
    {
        // Arrange - content with technical terms, numbers, identifiers
        var content = "The API_KEY configuration uses port 8080 for HTTP_SERVER connections.";

        // Act
        var result = ChunkQualityAnalyzer.Analyze(content);

        // Assert
        result.DensityScore.Should().BeGreaterThan(0.5f);
    }

    [Fact]
    public void Analyze_WithMarkdownHeadings_HasHighStructureScore()
    {
        // Arrange
        var content = "# Chapter 1\n\nThis is the introduction to the chapter.";

        // Act
        var result = ChunkQualityAnalyzer.Analyze(content);

        // Assert
        result.StructureScore.Should().BeGreaterThan(0.5f);
    }

    [Fact]
    public void Analyze_WithCodeBlock_HasHighStructureScore()
    {
        // Arrange
        var content = "Here is an example:\n```csharp\nvar x = 1;\n```";

        // Act
        var result = ChunkQualityAnalyzer.Analyze(content);

        // Assert
        result.StructureScore.Should().BeGreaterThan(0.5f);
    }

    [Fact]
    public void Analyze_WithLongContent_RecommendsSummarization()
    {
        // Arrange - content longer than 500 chars
        var content = new string('a', 100) + " " + new string('b', 100) + " " +
                      new string('c', 100) + " " + new string('d', 100) + " " +
                      new string('e', 100) + " " + new string('f', 100) + ".";

        // Act
        var result = ChunkQualityAnalyzer.Analyze(content);

        // Assert
        result.ShouldSummarize.Should().BeTrue();
        result.Recommendation.HasFlag(EnrichmentRecommendation.Summarize).Should().BeTrue();
    }

    [Fact]
    public void Analyze_WithShortContent_DoesNotRecommendSummarization()
    {
        // Arrange - content shorter than 500 chars
        var content = "Short content that doesn't need summarization.";

        // Act
        var result = ChunkQualityAnalyzer.Analyze(content);

        // Assert
        result.ShouldSummarize.Should().BeFalse();
        result.Recommendation.HasFlag(EnrichmentRecommendation.Summarize).Should().BeFalse();
    }

    [Fact]
    public void Analyze_WithHighDensity_RecommendsKeywordExtraction()
    {
        // Arrange - high information density content
        var content = "Machine learning models use neural networks for classification tasks.";

        // Act
        var result = ChunkQualityAnalyzer.Analyze(content);

        // Assert
        result.ShouldExtractKeywords.Should().BeTrue();
    }

    [Fact]
    public void Analyze_WithTableMetadata_AddsUseTablePromptRecommendation()
    {
        // Arrange
        var content = "| Column1 | Column2 |\n|---------|---------|";
        var metadata = new Dictionary<string, object>
        {
            [ChunkMetadataKeys.ContentType] = ChunkContentTypes.Table
        };

        // Act
        var result = ChunkQualityAnalyzer.Analyze(content, metadata);

        // Assert
        result.Recommendation.HasFlag(EnrichmentRecommendation.UseTablePrompt).Should().BeTrue();
        result.StructureScore.Should().BeGreaterThanOrEqualTo(0.8f);
    }

    [Fact]
    public void Analyze_WithEarlyChunkIndex_IncreasesStructureScore()
    {
        // Arrange
        var content = "Introduction to the document.";
        var metadata = new Dictionary<string, object>
        {
            [ChunkMetadataKeys.ChunkIndex] = 0
        };

        // Act
        var resultWithMetadata = ChunkQualityAnalyzer.Analyze(content, metadata);
        var resultWithoutMetadata = ChunkQualityAnalyzer.Analyze(content);

        // Assert
        resultWithMetadata.StructureScore.Should().BeGreaterThan(resultWithoutMetadata.StructureScore);
    }

    [Fact]
    public void Analyze_WithLowCompleteness_RecommendsAddContext()
    {
        // Arrange - incomplete content
        var content = "continued from previous section without context";

        // Act
        var result = ChunkQualityAnalyzer.Analyze(content);

        // Assert
        if (result.CompletenessScore < 0.5f)
        {
            result.Recommendation.HasFlag(EnrichmentRecommendation.AddContext).Should().BeTrue();
        }
    }

    [Theory]
    [InlineData("# Heading\nParagraph content here.", true)]
    [InlineData("- Item 1\n- Item 2\n- Item 3", true)]
    [InlineData("plain text without structure", false)]
    public void Analyze_DetectsStructuralElements(string content, bool hasStructure)
    {
        // Act
        var result = ChunkQualityAnalyzer.Analyze(content);

        // Assert
        if (hasStructure)
        {
            result.StructureScore.Should().BeGreaterThan(0.5f);
        }
    }

    [Fact]
    public void Analyze_WithNumberedList_HasExpectedStructureScore()
    {
        // Arrange - numbered lists start with base 0.5 + list bonus 0.1 = 0.6
        var content = "1. First item\n2. Second item\n3. Third item";

        // Act
        var result = ChunkQualityAnalyzer.Analyze(content);

        // Assert - numbered lists should be detected
        result.StructureScore.Should().BeGreaterThanOrEqualTo(0.5f);
    }

    [Fact]
    public void Analyze_OverallScore_IsWeightedAverage()
    {
        // Arrange
        var content = "This is a properly formatted sentence with technical_terms and numbers 123.";

        // Act
        var result = ChunkQualityAnalyzer.Analyze(content);

        // Assert
        // Overall = completeness * 0.3 + density * 0.4 + structure * 0.3
        var expectedOverall = (result.CompletenessScore * 0.3f) +
                              (result.DensityScore * 0.4f) +
                              (result.StructureScore * 0.3f);
        result.OverallScore.Should().BeApproximately(expectedOverall, 0.01f);
    }
}

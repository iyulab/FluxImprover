namespace FluxImprover.Tests.Models;

using FluentAssertions;
using FluxImprover.Models;
using Xunit;

public sealed class ChunkAssessmentTests
{
    [Fact]
    public void ChunkAssessment_DefaultValues_AreSetCorrectly()
    {
        // Act
        var assessment = new ChunkAssessment();

        // Assert
        assessment.InitialScore.Should().Be(0);
        assessment.ReflectionScore.Should().BeNull();
        assessment.CriticScore.Should().BeNull();
        assessment.FinalScore.Should().Be(0);
        assessment.Confidence.Should().Be(0);
        assessment.Factors.Should().BeEmpty();
        assessment.Suggestions.Should().BeEmpty();
        assessment.Reasoning.Should().BeEmpty();
    }

    [Fact]
    public void ChunkAssessment_CanBeInitialized()
    {
        // Arrange
        var factors = new[]
        {
            new AssessmentFactor { Name = "Relevance", Contribution = 0.8, Explanation = "High relevance" }
        };
        var suggestions = new[] { "Consider adding more context" };
        var reasoning = new Dictionary<string, string>
        {
            ["initial"] = "Initial assessment reason"
        };

        // Act
        var assessment = new ChunkAssessment
        {
            InitialScore = 0.75,
            ReflectionScore = 0.80,
            CriticScore = 0.78,
            FinalScore = 0.78,
            Confidence = 0.9,
            Factors = factors,
            Suggestions = suggestions,
            Reasoning = reasoning
        };

        // Assert
        assessment.InitialScore.Should().Be(0.75);
        assessment.ReflectionScore.Should().Be(0.80);
        assessment.CriticScore.Should().Be(0.78);
        assessment.FinalScore.Should().Be(0.78);
        assessment.Confidence.Should().Be(0.9);
        assessment.Factors.Should().HaveCount(1);
        assessment.Suggestions.Should().HaveCount(1);
        assessment.Reasoning.Should().ContainKey("initial");
    }

    [Fact]
    public void ChunkAssessment_IsImmutableRecord()
    {
        // Arrange
        var assessment = new ChunkAssessment
        {
            InitialScore = 0.5,
            FinalScore = 0.6
        };

        // Act - Create modified copy using with expression
        var modified = assessment with { FinalScore = 0.8 };

        // Assert
        assessment.FinalScore.Should().Be(0.6);
        modified.FinalScore.Should().Be(0.8);
        modified.InitialScore.Should().Be(0.5);
    }
}

public sealed class AssessmentFactorTests
{
    [Fact]
    public void AssessmentFactor_RequiresName()
    {
        // Act
        var factor = new AssessmentFactor
        {
            Name = "Test Factor",
            Contribution = 0.5
        };

        // Assert
        factor.Name.Should().Be("Test Factor");
    }

    [Fact]
    public void AssessmentFactor_DefaultExplanation_IsEmpty()
    {
        // Act
        var factor = new AssessmentFactor { Name = "Test" };

        // Assert
        factor.Explanation.Should().BeEmpty();
    }

    [Fact]
    public void AssessmentFactor_Contribution_CanBeNegative()
    {
        // Act
        var factor = new AssessmentFactor
        {
            Name = "Penalty",
            Contribution = -0.3,
            Explanation = "Content too short"
        };

        // Assert
        factor.Contribution.Should().Be(-0.3);
    }

    [Fact]
    public void AssessmentFactor_IsImmutableRecord()
    {
        // Arrange
        var factor = new AssessmentFactor
        {
            Name = "Original",
            Contribution = 0.5,
            Explanation = "Original explanation"
        };

        // Act
        var modified = factor with { Contribution = 0.8 };

        // Assert
        factor.Contribution.Should().Be(0.5);
        modified.Contribution.Should().Be(0.8);
        modified.Name.Should().Be("Original");
    }
}

public sealed class FilteredChunkTests
{
    [Fact]
    public void FilteredChunk_RequiresChunk()
    {
        // Arrange
        var chunk = new Chunk { Id = "test-1", Content = "Test content" };

        // Act
        var filtered = new FilteredChunk { Chunk = chunk };

        // Assert
        filtered.Chunk.Should().NotBeNull();
        filtered.Chunk.Id.Should().Be("test-1");
    }

    [Fact]
    public void FilteredChunk_DefaultValues_AreSetCorrectly()
    {
        // Arrange
        var chunk = new Chunk { Id = "test-1", Content = "Test content" };

        // Act
        var filtered = new FilteredChunk { Chunk = chunk };

        // Assert
        filtered.RelevanceScore.Should().Be(0);
        filtered.QualityScore.Should().Be(0);
        filtered.CombinedScore.Should().Be(0);
        filtered.Passed.Should().BeFalse();
        filtered.Assessment.Should().BeNull();
        filtered.Reason.Should().BeEmpty();
    }

    [Fact]
    public void FilteredChunk_CanBeFullyInitialized()
    {
        // Arrange
        var chunk = new Chunk { Id = "test-1", Content = "High quality content about machine learning." };
        var assessment = new ChunkAssessment
        {
            InitialScore = 0.8,
            FinalScore = 0.85,
            Confidence = 0.9
        };

        // Act
        var filtered = new FilteredChunk
        {
            Chunk = chunk,
            RelevanceScore = 0.85,
            QualityScore = 0.90,
            CombinedScore = 0.87,
            Passed = true,
            Assessment = assessment,
            Reason = "High relevance and quality"
        };

        // Assert
        filtered.RelevanceScore.Should().Be(0.85);
        filtered.QualityScore.Should().Be(0.90);
        filtered.CombinedScore.Should().Be(0.87);
        filtered.Passed.Should().BeTrue();
        filtered.Assessment.Should().NotBeNull();
        filtered.Reason.Should().Be("High relevance and quality");
    }

    [Fact]
    public void FilteredChunk_IsImmutableRecord()
    {
        // Arrange
        var chunk = new Chunk { Id = "test-1", Content = "Test" };
        var filtered = new FilteredChunk
        {
            Chunk = chunk,
            Passed = false,
            CombinedScore = 0.4
        };

        // Act
        var modified = filtered with { Passed = true, CombinedScore = 0.8 };

        // Assert
        filtered.Passed.Should().BeFalse();
        filtered.CombinedScore.Should().Be(0.4);
        modified.Passed.Should().BeTrue();
        modified.CombinedScore.Should().Be(0.8);
    }

    [Fact]
    public void FilteredChunk_PreservesOriginalChunk()
    {
        // Arrange
        var originalContent = "Original content that should not be modified.";
        var chunk = new Chunk
        {
            Id = "preserve-test",
            Content = originalContent,
            Metadata = new Dictionary<string, object> { ["key"] = "value" }
        };

        // Act
        var filtered = new FilteredChunk
        {
            Chunk = chunk,
            Passed = true,
            CombinedScore = 0.9
        };

        // Assert
        filtered.Chunk.Content.Should().Be(originalContent);
        filtered.Chunk.Metadata.Should().ContainKey("key");
    }
}

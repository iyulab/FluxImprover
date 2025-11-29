namespace FluxImprover.Tests.Options;

using FluentAssertions;
using FluxImprover.Options;
using Xunit;

public sealed class ChunkFilteringOptionsTests
{
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var options = new ChunkFilteringOptions();

        // Assert
        options.MinRelevanceScore.Should().Be(0.7);
        options.MaxChunks.Should().BeNull();
        options.UseSelfReflection.Should().BeTrue();
        options.UseCriticValidation.Should().BeTrue();
        options.QualityWeight.Should().Be(0.3);
        options.PreserveOrder.Should().BeFalse();
        options.BatchSize.Should().Be(5);
        options.Criteria.Should().BeEmpty();
    }

    [Fact]
    public void MinRelevanceScore_CanBeCustomized()
    {
        // Act
        var options = new ChunkFilteringOptions { MinRelevanceScore = 0.5 };

        // Assert
        options.MinRelevanceScore.Should().Be(0.5);
    }

    [Fact]
    public void MaxChunks_CanBeSet()
    {
        // Act
        var options = new ChunkFilteringOptions { MaxChunks = 10 };

        // Assert
        options.MaxChunks.Should().Be(10);
    }

    [Fact]
    public void UseSelfReflection_CanBeDisabled()
    {
        // Act
        var options = new ChunkFilteringOptions { UseSelfReflection = false };

        // Assert
        options.UseSelfReflection.Should().BeFalse();
    }

    [Fact]
    public void UseCriticValidation_CanBeDisabled()
    {
        // Act
        var options = new ChunkFilteringOptions { UseCriticValidation = false };

        // Assert
        options.UseCriticValidation.Should().BeFalse();
    }

    [Fact]
    public void QualityWeight_CanBeCustomized()
    {
        // Act
        var options = new ChunkFilteringOptions { QualityWeight = 0.8 };

        // Assert
        options.QualityWeight.Should().Be(0.8);
    }

    [Fact]
    public void PreserveOrder_CanBeEnabled()
    {
        // Act
        var options = new ChunkFilteringOptions { PreserveOrder = true };

        // Assert
        options.PreserveOrder.Should().BeTrue();
    }

    [Fact]
    public void BatchSize_CanBeCustomized()
    {
        // Act
        var options = new ChunkFilteringOptions { BatchSize = 10 };

        // Assert
        options.BatchSize.Should().Be(10);
    }

    [Fact]
    public void Criteria_CanBeSet()
    {
        // Arrange
        var criteria = new[]
        {
            new FilterCriterion { Type = CriterionType.KeywordPresence, Value = "test", Weight = 1.0 },
            new FilterCriterion { Type = CriterionType.InformationDensity, Weight = 0.5 }
        };

        // Act
        var options = new ChunkFilteringOptions { Criteria = criteria };

        // Assert
        options.Criteria.Should().HaveCount(2);
    }
}

public sealed class FilterCriterionTests
{
    [Fact]
    public void DefaultWeight_IsOne()
    {
        // Act
        var criterion = new FilterCriterion { Type = CriterionType.KeywordPresence };

        // Assert
        criterion.Weight.Should().Be(1.0);
    }

    [Fact]
    public void IsMandatory_DefaultsFalse()
    {
        // Act
        var criterion = new FilterCriterion { Type = CriterionType.TopicRelevance };

        // Assert
        criterion.IsMandatory.Should().BeFalse();
    }

    [Fact]
    public void Value_CanBeString()
    {
        // Act
        var criterion = new FilterCriterion
        {
            Type = CriterionType.KeywordPresence,
            Value = "machine learning"
        };

        // Assert
        criterion.Value.Should().Be("machine learning");
    }

    [Fact]
    public void Value_CanBeStringCollection()
    {
        // Arrange
        var keywords = new[] { "AI", "ML", "deep learning" };

        // Act
        var criterion = new FilterCriterion
        {
            Type = CriterionType.KeywordPresence,
            Value = keywords
        };

        // Assert
        criterion.Value.Should().BeEquivalentTo(keywords);
    }

    [Fact]
    public void AllCriterionTypes_AreDefined()
    {
        // Assert
        Enum.GetValues<CriterionType>().Should().Contain(CriterionType.KeywordPresence);
        Enum.GetValues<CriterionType>().Should().Contain(CriterionType.TopicRelevance);
        Enum.GetValues<CriterionType>().Should().Contain(CriterionType.InformationDensity);
        Enum.GetValues<CriterionType>().Should().Contain(CriterionType.FactualContent);
        Enum.GetValues<CriterionType>().Should().Contain(CriterionType.Recency);
        Enum.GetValues<CriterionType>().Should().Contain(CriterionType.SourceCredibility);
        Enum.GetValues<CriterionType>().Should().Contain(CriterionType.Completeness);
    }
}

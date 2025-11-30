namespace FluxImprover.Tests.Options;

using FluentAssertions;
using FluxImprover.Options;
using Xunit;

public sealed class ParentChunkContextTests
{
    [Fact]
    public void ParentChunkContext_WithAllProperties_SetsCorrectly()
    {
        // Arrange & Act
        var context = new ParentChunkContext
        {
            ParentId = "parent-1",
            ParentSummary = "This is the parent summary",
            ParentKeywords = ["keyword1", "keyword2"],
            ParentHeadingPath = "Chapter 1 > Section 1.1",
            HierarchyLevel = 1
        };

        // Assert
        context.ParentId.Should().Be("parent-1");
        context.ParentSummary.Should().Be("This is the parent summary");
        context.ParentKeywords.Should().HaveCount(2);
        context.ParentHeadingPath.Should().Be("Chapter 1 > Section 1.1");
        context.HierarchyLevel.Should().Be(1);
    }

    [Fact]
    public void EnrichmentOptions_WithParentContext_SetsCorrectly()
    {
        // Arrange
        var parentContext = new ParentChunkContext
        {
            ParentId = "parent-1",
            ParentSummary = "Parent content summary"
        };

        // Act
        var options = new EnrichmentOptions
        {
            ParentContext = parentContext
        };

        // Assert
        options.ParentContext.Should().NotBeNull();
        options.ParentContext!.ParentId.Should().Be("parent-1");
        options.ParentContext.ParentSummary.Should().Be("Parent content summary");
    }

    [Fact]
    public void EnrichmentOptions_WithoutParentContext_IsNull()
    {
        // Arrange & Act
        var options = new EnrichmentOptions();

        // Assert
        options.ParentContext.Should().BeNull();
    }
}

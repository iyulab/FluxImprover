namespace FluxImprover.Abstractions.Tests.Models;

using System.Text.Json;
using FluentAssertions;
using FluxImprover.Abstractions.Models;

public class EnrichedChunkTests
{
    [Fact]
    public void EnrichedChunk_WithRequiredProperties_CreatesSuccessfully()
    {
        // Act
        var chunk = new EnrichedChunk
        {
            Id = "chunk-001",
            Text = "This is the chunk content.",
            SourceId = "doc-001"
        };

        // Assert
        chunk.Id.Should().Be("chunk-001");
        chunk.Text.Should().Be("This is the chunk content.");
        chunk.SourceId.Should().Be("doc-001");
    }

    [Fact]
    public void EnrichedChunk_WithOptionalProperties_StoresCorrectly()
    {
        // Act
        var chunk = new EnrichedChunk
        {
            Id = "chunk-001",
            Text = "Content",
            SourceId = "doc-001",
            HeadingPath = "Chapter 1 > Section 1.1",
            Summary = "Brief summary",
            Keywords = ["keyword1", "keyword2"],
            Metadata = new Dictionary<string, object>
            {
                ["page"] = 5,
                ["custom"] = "value"
            }
        };

        // Assert
        chunk.HeadingPath.Should().Be("Chapter 1 > Section 1.1");
        chunk.Summary.Should().Be("Brief summary");
        chunk.Keywords.Should().HaveCount(2);
        chunk.Metadata!["page"].Should().Be(5);
    }

    [Fact]
    public void EnrichedChunk_Serialization_RoundTrips()
    {
        // Arrange
        var original = new EnrichedChunk
        {
            Id = "chunk-001",
            Text = "Test content",
            SourceId = "doc-001",
            HeadingPath = "Test > Path",
            Keywords = ["key1", "key2"]
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<EnrichedChunk>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(original.Id);
        deserialized.Text.Should().Be(original.Text);
        deserialized.Keywords.Should().BeEquivalentTo(original.Keywords);
    }
}

public class IEnrichedChunkInterfaceTests
{
    [Fact]
    public void EnrichedChunk_ImplementsInterface_Correctly()
    {
        // Arrange
        IEnrichedChunk chunk = new EnrichedChunk
        {
            Id = "test",
            Text = "content",
            SourceId = "source"
        };

        // Assert
        chunk.Should().BeAssignableTo<IEnrichedChunk>();
        chunk.Id.Should().Be("test");
        chunk.Text.Should().Be("content");
    }
}

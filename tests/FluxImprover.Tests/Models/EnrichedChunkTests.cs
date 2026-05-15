using Xunit;
namespace FluxImprover.Tests.Models;

using System.Text.Json;
using FluentAssertions;
using FluxImprover.Models;

public class EnrichedChunkTests
{
    [Fact]
    public void EnrichedChunk_WithRequiredProperties_CreatesSuccessfully()
    {
        var chunk = new EnrichedChunk
        {
            ChunkId = "chunk-001",
            Content = "This is the chunk content.",
            SourceId = "doc-001"
        };

        chunk.ChunkId.Should().Be("chunk-001");
        chunk.Content.Should().Be("This is the chunk content.");
        chunk.SourceId.Should().Be("doc-001");
    }

    [Fact]
    public void EnrichedChunk_WithOptionalProperties_StoresCorrectly()
    {
        var chunk = new EnrichedChunk
        {
            ChunkId = "chunk-001",
            Content = "Content",
            SourceId = "doc-001",
            HeadingPath = ["Chapter 1", "Section 1.1"],
            Summary = "Brief summary",
            Keywords = ["keyword1", "keyword2"],
            Metadata = new Dictionary<string, object>
            {
                ["page"] = 5,
                ["custom"] = "value"
            }
        };

        chunk.HeadingPath.Should().HaveCount(2);
        chunk.HeadingPath[0].Should().Be("Chapter 1");
        chunk.Summary.Should().Be("Brief summary");
        chunk.Keywords.Should().HaveCount(2);
        chunk.Metadata!["page"].Should().Be(5);
    }

    [Fact]
    public void EnrichedChunk_Serialization_RoundTrips()
    {
        var original = new EnrichedChunk
        {
            ChunkId = "chunk-001",
            Content = "Test content",
            SourceId = "doc-001",
            HeadingPath = ["Test", "Path"],
            Keywords = ["key1", "key2"]
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<EnrichedChunk>(json);

        deserialized.Should().NotBeNull();
        deserialized!.ChunkId.Should().Be(original.ChunkId);
        deserialized.Content.Should().Be(original.Content);
        deserialized.Keywords.Should().BeEquivalentTo(original.Keywords);
    }
}

public class IEnrichedChunkInterfaceTests
{
    [Fact]
    public void EnrichedChunk_ImplementsInterface_Correctly()
    {
        EnrichedChunk chunk = new EnrichedChunk
        {
            ChunkId = "test",
            Content = "content",
            SourceId = "source"
        };

        chunk.Should().BeAssignableTo<ILlmEnrichedChunk>();
        chunk.Should().BeAssignableTo<Flux.Abstractions.IEnrichedChunk>();
        chunk.ChunkId.Should().Be("test");
        chunk.Content.Should().Be("content");
    }
}

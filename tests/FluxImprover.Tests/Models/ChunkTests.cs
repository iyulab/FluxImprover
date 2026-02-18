namespace FluxImprover.Tests.Models;

using FluentAssertions;
using FluxImprover.Models;
using Xunit;

public sealed class ChunkTests
{
    [Fact]
    public void Chunk_DefaultValues_AreSetCorrectly()
    {
        var chunk = new Chunk();

        chunk.Id.Should().Be(string.Empty);
        chunk.Content.Should().Be(string.Empty);
        chunk.Metadata.Should().BeNull();
    }

    [Fact]
    public void Chunk_CanBeFullyInitialized()
    {
        var metadata = new Dictionary<string, object>
        {
            ["source"] = "document.pdf",
            ["page"] = 5,
            ["confidence"] = 0.95
        };

        var chunk = new Chunk
        {
            Id = "chunk-abc",
            Content = "This is the chunk content.",
            Metadata = metadata
        };

        chunk.Id.Should().Be("chunk-abc");
        chunk.Content.Should().Be("This is the chunk content.");
        chunk.Metadata.Should().HaveCount(3);
        chunk.Metadata!["source"].Should().Be("document.pdf");
    }

    [Fact]
    public void Chunk_WithEmptyMetadata_IsNotNull()
    {
        var chunk = new Chunk
        {
            Id = "test",
            Content = "Content",
            Metadata = new Dictionary<string, object>()
        };

        chunk.Metadata.Should().NotBeNull();
        chunk.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Chunk_MetadataMutableDictionary_CanBeModified()
    {
        var metadata = new Dictionary<string, object> { ["key1"] = "value1" };
        var chunk = new Chunk
        {
            Id = "test",
            Content = "Content",
            Metadata = metadata
        };

        chunk.Metadata!["key2"] = "value2";

        chunk.Metadata.Should().HaveCount(2);
    }
}

public sealed class ChunkRelationshipTests
{
    [Fact]
    public void ChunkRelationship_CanBeFullyInitialized()
    {
        var rel = new ChunkRelationship
        {
            SourceChunkId = "chunk-1",
            TargetChunkId = "chunk-2",
            RelationshipType = ChunkRelationshipType.SameTopic,
            Confidence = 0.85f,
            Explanation = "Both chunks discuss neural networks.",
            IsBidirectional = true
        };

        rel.SourceChunkId.Should().Be("chunk-1");
        rel.TargetChunkId.Should().Be("chunk-2");
        rel.RelationshipType.Should().Be(ChunkRelationshipType.SameTopic);
        rel.Confidence.Should().BeApproximately(0.85f, 0.001f);
        rel.Explanation.Should().Be("Both chunks discuss neural networks.");
        rel.IsBidirectional.Should().BeTrue();
    }

    [Fact]
    public void ChunkRelationship_DefaultValues()
    {
        var rel = new ChunkRelationship
        {
            SourceChunkId = "a",
            TargetChunkId = "b",
            RelationshipType = ChunkRelationshipType.References
        };

        rel.Confidence.Should().Be(0f);
        rel.Explanation.Should().BeNull();
        rel.IsBidirectional.Should().BeFalse();
    }

    [Theory]
    [InlineData(ChunkRelationshipType.SameTopic)]
    [InlineData(ChunkRelationshipType.References)]
    [InlineData(ChunkRelationshipType.Complementary)]
    [InlineData(ChunkRelationshipType.Contradicts)]
    [InlineData(ChunkRelationshipType.Prerequisite)]
    [InlineData(ChunkRelationshipType.Elaborates)]
    [InlineData(ChunkRelationshipType.Summarizes)]
    [InlineData(ChunkRelationshipType.ExampleOf)]
    [InlineData(ChunkRelationshipType.CauseEffect)]
    [InlineData(ChunkRelationshipType.Temporal)]
    public void ChunkRelationshipType_AllValues_CanBeAssigned(ChunkRelationshipType type)
    {
        var rel = new ChunkRelationship
        {
            SourceChunkId = "a",
            TargetChunkId = "b",
            RelationshipType = type
        };

        rel.RelationshipType.Should().Be(type);
    }

    [Fact]
    public void ChunkRelationship_IsImmutableRecord()
    {
        var rel = new ChunkRelationship
        {
            SourceChunkId = "a",
            TargetChunkId = "b",
            RelationshipType = ChunkRelationshipType.References,
            Confidence = 0.5f
        };

        var modified = rel with { Confidence = 0.9f, IsBidirectional = true };

        rel.Confidence.Should().Be(0.5f);
        rel.IsBidirectional.Should().BeFalse();
        modified.Confidence.Should().Be(0.9f);
        modified.IsBidirectional.Should().BeTrue();
    }

    [Fact]
    public void ChunkRelationship_Equality_SameValues_AreEqual()
    {
        var rel1 = new ChunkRelationship
        {
            SourceChunkId = "a",
            TargetChunkId = "b",
            RelationshipType = ChunkRelationshipType.SameTopic,
            Confidence = 0.8f
        };
        var rel2 = new ChunkRelationship
        {
            SourceChunkId = "a",
            TargetChunkId = "b",
            RelationshipType = ChunkRelationshipType.SameTopic,
            Confidence = 0.8f
        };

        rel1.Should().Be(rel2);
    }
}

public sealed class ChunkRelationshipAnalysisTests
{
    [Fact]
    public void ChunkRelationshipAnalysis_Success_DefaultIsTrue()
    {
        var analysis = new ChunkRelationshipAnalysis
        {
            ChunkId = "chunk-1",
            Relationships = []
        };

        analysis.Success.Should().BeTrue();
        analysis.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ChunkRelationshipAnalysis_WithRelationships()
    {
        var relationships = new[]
        {
            new ChunkRelationship
            {
                SourceChunkId = "chunk-1",
                TargetChunkId = "chunk-2",
                RelationshipType = ChunkRelationshipType.SameTopic,
                Confidence = 0.9f
            },
            new ChunkRelationship
            {
                SourceChunkId = "chunk-1",
                TargetChunkId = "chunk-3",
                RelationshipType = ChunkRelationshipType.Elaborates,
                Confidence = 0.7f
            }
        };

        var analysis = new ChunkRelationshipAnalysis
        {
            ChunkId = "chunk-1",
            Relationships = relationships
        };

        analysis.Relationships.Should().HaveCount(2);
        analysis.Relationships[0].RelationshipType.Should().Be(ChunkRelationshipType.SameTopic);
        analysis.Relationships[1].RelationshipType.Should().Be(ChunkRelationshipType.Elaborates);
    }

    [Fact]
    public void ChunkRelationshipAnalysis_Failure()
    {
        var analysis = new ChunkRelationshipAnalysis
        {
            ChunkId = "chunk-1",
            Relationships = [],
            Success = false,
            ErrorMessage = "LLM service unavailable"
        };

        analysis.Success.Should().BeFalse();
        analysis.ErrorMessage.Should().Be("LLM service unavailable");
    }

    [Fact]
    public void ChunkRelationshipAnalysis_IsImmutableRecord()
    {
        var analysis = new ChunkRelationshipAnalysis
        {
            ChunkId = "chunk-1",
            Relationships = [],
            Success = true
        };

        var modified = analysis with { Success = false, ErrorMessage = "Error" };

        analysis.Success.Should().BeTrue();
        modified.Success.Should().BeFalse();
        modified.ChunkId.Should().Be("chunk-1");
    }
}

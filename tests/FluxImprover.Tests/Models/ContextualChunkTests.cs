namespace FluxImprover.Tests.Models;

using FluentAssertions;
using FluxImprover.Models;
using Xunit;

public sealed class ContextualChunkTests
{
    #region GetContextualizedText

    [Fact]
    public void GetContextualizedText_WithContextSummary_CombinesTexts()
    {
        var chunk = new ContextualChunk
        {
            Id = "chunk-1",
            Text = "Machine learning is a subset of AI.",
            SourceId = "doc-1",
            ContextSummary = "This section introduces machine learning concepts."
        };

        var result = chunk.GetContextualizedText();

        result.Should().Be("This section introduces machine learning concepts.\n\nMachine learning is a subset of AI.");
    }

    [Fact]
    public void GetContextualizedText_NullContextSummary_ReturnsOriginalText()
    {
        var chunk = new ContextualChunk
        {
            Id = "chunk-1",
            Text = "Original text only.",
            SourceId = "doc-1",
            ContextSummary = null
        };

        var result = chunk.GetContextualizedText();

        result.Should().Be("Original text only.");
    }

    [Fact]
    public void GetContextualizedText_EmptyContextSummary_ReturnsOriginalText()
    {
        var chunk = new ContextualChunk
        {
            Id = "chunk-1",
            Text = "Original text only.",
            SourceId = "doc-1",
            ContextSummary = ""
        };

        var result = chunk.GetContextualizedText();

        result.Should().Be("Original text only.");
    }

    [Fact]
    public void GetContextualizedText_WhitespaceContextSummary_ReturnsOriginalText()
    {
        var chunk = new ContextualChunk
        {
            Id = "chunk-1",
            Text = "Original text.",
            SourceId = "doc-1",
            ContextSummary = "   \n\t  "
        };

        var result = chunk.GetContextualizedText();

        result.Should().Be("Original text.");
    }

    #endregion

    #region Properties

    [Fact]
    public void ContextualChunk_CanBeFullyInitialized()
    {
        var metadata = new Dictionary<string, object>
        {
            ["source"] = "manual.pdf",
            ["page"] = 42
        };

        var chunk = new ContextualChunk
        {
            Id = "chunk-42",
            Text = "Safety instructions for the device.",
            SourceId = "doc-safety",
            ContextSummary = "This is a safety manual for Device X.",
            HeadingPath = "Chapter 1 > Safety Precautions",
            Position = 5,
            TotalChunks = 20,
            Metadata = metadata
        };

        chunk.Id.Should().Be("chunk-42");
        chunk.Text.Should().Be("Safety instructions for the device.");
        chunk.SourceId.Should().Be("doc-safety");
        chunk.ContextSummary.Should().NotBeNull();
        chunk.HeadingPath.Should().Be("Chapter 1 > Safety Precautions");
        chunk.Position.Should().Be(5);
        chunk.TotalChunks.Should().Be(20);
        chunk.Metadata.Should().HaveCount(2);
    }

    [Fact]
    public void ContextualChunk_DefaultOptionals_AreNull()
    {
        var chunk = new ContextualChunk
        {
            Id = "chunk-1",
            Text = "Minimal chunk.",
            SourceId = "doc-1"
        };

        chunk.ContextSummary.Should().BeNull();
        chunk.HeadingPath.Should().BeNull();
        chunk.Position.Should().BeNull();
        chunk.TotalChunks.Should().BeNull();
        chunk.Metadata.Should().BeNull();
    }

    [Fact]
    public void ContextualChunk_IsImmutableRecord()
    {
        var chunk = new ContextualChunk
        {
            Id = "original",
            Text = "Original text.",
            SourceId = "doc-1",
            Position = 0
        };

        var modified = chunk with { Position = 5, ContextSummary = "Added context" };

        chunk.Position.Should().Be(0);
        chunk.ContextSummary.Should().BeNull();
        modified.Position.Should().Be(5);
        modified.ContextSummary.Should().Be("Added context");
        modified.Id.Should().Be("original");
    }

    #endregion
}

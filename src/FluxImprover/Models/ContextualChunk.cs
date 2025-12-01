namespace FluxImprover.Models;

/// <summary>
/// A chunk enriched with document-level context.
/// Based on Anthropic's Contextual Retrieval pattern.
/// </summary>
public sealed record ContextualChunk
{
    /// <summary>
    /// Chunk unique identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Original chunk text content.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Source document identifier.
    /// </summary>
    public required string SourceId { get; init; }

    /// <summary>
    /// LLM-generated context summary explaining the chunk's role within the document.
    /// This provides document-level context to improve retrieval accuracy.
    /// </summary>
    public string? ContextSummary { get; init; }

    /// <summary>
    /// Document structure path (e.g., "Chapter 1 > Section 1.1").
    /// </summary>
    public string? HeadingPath { get; init; }

    /// <summary>
    /// Chunk position within the document (0-based index).
    /// </summary>
    public int? Position { get; init; }

    /// <summary>
    /// Total number of chunks in the source document.
    /// </summary>
    public int? TotalChunks { get; init; }

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets the contextualized text combining context summary and original text.
    /// This is the recommended format for embedding and indexing.
    /// </summary>
    /// <returns>Contextualized text for embedding.</returns>
    public string GetContextualizedText()
    {
        if (string.IsNullOrWhiteSpace(ContextSummary))
            return Text;

        return $"{ContextSummary}\n\n{Text}";
    }
}

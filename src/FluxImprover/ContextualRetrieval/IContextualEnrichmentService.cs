using FluxImprover.Models;
using FluxImprover.Options;

namespace FluxImprover.ContextualRetrieval;

/// <summary>
/// Service for enriching chunks with document-level context.
/// Implements Anthropic's Contextual Retrieval pattern which reduces
/// failed retrievals by 49% (67% with reranking).
/// </summary>
/// <remarks>
/// Reference: https://www.anthropic.com/news/contextual-retrieval
/// </remarks>
public interface IContextualEnrichmentService
{
    /// <summary>
    /// Enriches a chunk with document-level context.
    /// </summary>
    /// <param name="chunk">The chunk to enrich.</param>
    /// <param name="fullDocumentText">The full document text for context extraction.</param>
    /// <param name="options">Contextual enrichment options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A chunk enriched with document context.</returns>
    Task<ContextualChunk> EnrichAsync(
        Chunk chunk,
        string fullDocumentText,
        ContextualEnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enriches multiple chunks from the same document with document-level context.
    /// </summary>
    /// <param name="chunks">The chunks to enrich (must be from the same document).</param>
    /// <param name="fullDocumentText">The full document text for context extraction.</param>
    /// <param name="options">Contextual enrichment options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Chunks enriched with document context.</returns>
    Task<IReadOnlyList<ContextualChunk>> EnrichBatchAsync(
        IEnumerable<Chunk> chunks,
        string fullDocumentText,
        ContextualEnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);
}

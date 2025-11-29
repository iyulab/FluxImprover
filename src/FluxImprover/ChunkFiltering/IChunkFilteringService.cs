using FluxImprover.Models;
using FluxImprover.Options;

namespace FluxImprover.ChunkFiltering;

/// <summary>
/// LLM-based chunk filtering service with 3-stage assessment.
/// Provides intelligent filtering for RAG retrieval quality improvement.
/// </summary>
public interface IChunkFilteringService
{
    /// <summary>
    /// Filters chunks based on relevance and quality using LLM assessment.
    /// </summary>
    /// <param name="chunks">Chunks to filter.</param>
    /// <param name="query">Query or context for relevance assessment. Can be null for quality-only filtering.</param>
    /// <param name="options">Filtering options. Uses defaults if null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Filtered and scored chunks.</returns>
    Task<IReadOnlyList<FilteredChunk>> FilterAsync(
        IEnumerable<Chunk> chunks,
        string? query,
        ChunkFilteringOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs 3-stage assessment on a single chunk.
    /// </summary>
    /// <param name="chunk">Chunk to assess.</param>
    /// <param name="query">Query context for relevance evaluation.</param>
    /// <param name="options">Assessment options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed assessment result.</returns>
    Task<ChunkAssessment> AssessAsync(
        Chunk chunk,
        string? query,
        ChunkFilteringOptions? options = null,
        CancellationToken cancellationToken = default);
}

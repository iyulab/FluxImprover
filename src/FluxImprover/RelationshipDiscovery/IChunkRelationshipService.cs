using FluxImprover.Models;
using FluxImprover.Options;

namespace FluxImprover.RelationshipDiscovery;

/// <summary>
/// Service for discovering semantic relationships between document chunks.
/// Uses LLM analysis to identify various relationship types.
/// </summary>
public interface IChunkRelationshipService
{
    /// <summary>
    /// Analyzes the relationship between two chunks.
    /// </summary>
    /// <param name="sourceChunk">The source chunk.</param>
    /// <param name="targetChunk">The target chunk to compare with.</param>
    /// <param name="options">Relationship discovery options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Discovered relationships between the chunks.</returns>
    Task<IReadOnlyList<ChunkRelationship>> AnalyzePairAsync(
        Chunk sourceChunk,
        Chunk targetChunk,
        ChunkRelationshipOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes relationships between a chunk and multiple candidate chunks.
    /// </summary>
    /// <param name="sourceChunk">The source chunk.</param>
    /// <param name="candidateChunks">Candidate chunks to analyze relationships with.</param>
    /// <param name="options">Relationship discovery options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis result with all discovered relationships.</returns>
    Task<ChunkRelationshipAnalysis> AnalyzeRelationshipsAsync(
        Chunk sourceChunk,
        IEnumerable<Chunk> candidateChunks,
        ChunkRelationshipOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers all relationships within a collection of chunks.
    /// </summary>
    /// <param name="chunks">Collection of chunks to analyze.</param>
    /// <param name="options">Relationship discovery options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All discovered relationships between chunks.</returns>
    Task<IReadOnlyList<ChunkRelationship>> DiscoverAllRelationshipsAsync(
        IEnumerable<Chunk> chunks,
        ChunkRelationshipOptions? options = null,
        CancellationToken cancellationToken = default);
}

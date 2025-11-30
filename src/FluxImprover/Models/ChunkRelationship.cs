namespace FluxImprover.Models;

/// <summary>
/// Represents a semantic relationship between two chunks.
/// </summary>
public sealed record ChunkRelationship
{
    /// <summary>
    /// Source chunk identifier.
    /// </summary>
    public required string SourceChunkId { get; init; }

    /// <summary>
    /// Target chunk identifier.
    /// </summary>
    public required string TargetChunkId { get; init; }

    /// <summary>
    /// Type of relationship between the chunks.
    /// </summary>
    public required ChunkRelationshipType RelationshipType { get; init; }

    /// <summary>
    /// Confidence score for the relationship (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Explanation of why this relationship exists.
    /// </summary>
    public string? Explanation { get; init; }

    /// <summary>
    /// Whether this is a bidirectional relationship.
    /// </summary>
    public bool IsBidirectional { get; init; }
}

/// <summary>
/// Types of semantic relationships between chunks.
/// </summary>
public enum ChunkRelationshipType
{
    /// <summary>
    /// Chunks discuss the same topic or concept.
    /// </summary>
    SameTopic,

    /// <summary>
    /// One chunk references or cites the other.
    /// </summary>
    References,

    /// <summary>
    /// Chunks contain complementary information on the same subject.
    /// </summary>
    Complementary,

    /// <summary>
    /// Chunks contain contradictory or conflicting information.
    /// </summary>
    Contradicts,

    /// <summary>
    /// Source chunk should be read before target chunk for understanding.
    /// </summary>
    Prerequisite,

    /// <summary>
    /// Target chunk provides more detail on source chunk content.
    /// </summary>
    Elaborates,

    /// <summary>
    /// Target chunk summarizes or abstracts source chunk content.
    /// </summary>
    Summarizes,

    /// <summary>
    /// Chunks provide examples of the same concept.
    /// </summary>
    ExampleOf,

    /// <summary>
    /// Chunks describe cause and effect relationship.
    /// </summary>
    CauseEffect,

    /// <summary>
    /// Chunks show temporal or sequential relationship.
    /// </summary>
    Temporal
}

/// <summary>
/// Result of chunk relationship analysis.
/// </summary>
public sealed record ChunkRelationshipAnalysis
{
    /// <summary>
    /// The chunk that was analyzed.
    /// </summary>
    public required string ChunkId { get; init; }

    /// <summary>
    /// Discovered relationships with other chunks.
    /// </summary>
    public required IReadOnlyList<ChunkRelationship> Relationships { get; init; }

    /// <summary>
    /// Whether analysis completed successfully.
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Error message if analysis failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

namespace FluxImprover.Options;

/// <summary>
/// Options for LLM-based chunk filtering.
/// </summary>
public sealed record ChunkFilteringOptions
{
    /// <summary>
    /// Minimum relevance score to retain chunk (0.0 to 1.0).
    /// Default is 0.7.
    /// </summary>
    public double MinRelevanceScore { get; init; } = 0.7;

    /// <summary>
    /// Maximum number of chunks to return. Null means no limit.
    /// </summary>
    public int? MaxChunks { get; init; }

    /// <summary>
    /// Whether to use self-reflection stage for improved accuracy.
    /// Default is true.
    /// </summary>
    public bool UseSelfReflection { get; init; } = true;

    /// <summary>
    /// Whether to use critic validation stage for final verification.
    /// Default is true.
    /// </summary>
    public bool UseCriticValidation { get; init; } = true;

    /// <summary>
    /// Quality weight vs relevance weight (0.0 to 1.0).
    /// 0 = pure relevance, 1 = pure quality.
    /// Default is 0.3.
    /// </summary>
    public double QualityWeight { get; init; } = 0.3;

    /// <summary>
    /// Whether to preserve document order after filtering.
    /// Default is false (sorted by score).
    /// </summary>
    public bool PreserveOrder { get; init; } = false;

    /// <summary>
    /// Batch size for parallel chunk processing.
    /// Default is 5.
    /// </summary>
    public int BatchSize { get; init; } = 5;

    /// <summary>
    /// Specific filtering criteria to apply.
    /// </summary>
    public IReadOnlyList<FilterCriterion> Criteria { get; init; } = [];
}

/// <summary>
/// Represents a filtering criterion for chunk assessment.
/// </summary>
public sealed record FilterCriterion
{
    /// <summary>
    /// Type of criterion.
    /// </summary>
    public required CriterionType Type { get; init; }

    /// <summary>
    /// Value or threshold for the criterion.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Weight of this criterion (0.0 to 1.0).
    /// </summary>
    public double Weight { get; init; } = 1.0;

    /// <summary>
    /// Whether this criterion is mandatory (chunk fails if not met).
    /// </summary>
    public bool IsMandatory { get; init; }
}

/// <summary>
/// Types of filtering criteria for chunk assessment.
/// </summary>
public enum CriterionType
{
    /// <summary>
    /// Presence of specific keywords in the chunk.
    /// </summary>
    KeywordPresence,

    /// <summary>
    /// Relevance to specific topic or domain.
    /// </summary>
    TopicRelevance,

    /// <summary>
    /// Information density and richness.
    /// </summary>
    InformationDensity,

    /// <summary>
    /// Presence of factual, verifiable content.
    /// </summary>
    FactualContent,

    /// <summary>
    /// Temporal relevance or recency.
    /// </summary>
    Recency,

    /// <summary>
    /// Credibility of the source.
    /// </summary>
    SourceCredibility,

    /// <summary>
    /// Completeness of the information.
    /// </summary>
    Completeness
}

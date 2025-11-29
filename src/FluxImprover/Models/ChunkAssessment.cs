namespace FluxImprover.Models;

/// <summary>
/// Detailed assessment result of a chunk from 3-stage evaluation.
/// </summary>
public sealed record ChunkAssessment
{
    /// <summary>
    /// Initial assessment score from Stage 1.
    /// </summary>
    public double InitialScore { get; init; }

    /// <summary>
    /// Score after self-reflection (Stage 2). Null if reflection was skipped.
    /// </summary>
    public double? ReflectionScore { get; init; }

    /// <summary>
    /// Score after critic validation (Stage 3). Null if validation was skipped.
    /// </summary>
    public double? CriticScore { get; init; }

    /// <summary>
    /// Final consolidated score across all stages.
    /// </summary>
    public double FinalScore { get; init; }

    /// <summary>
    /// Confidence in the assessment (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Key factors contributing to the assessment.
    /// </summary>
    public IReadOnlyList<AssessmentFactor> Factors { get; init; } = [];

    /// <summary>
    /// Suggestions for improving chunk quality.
    /// </summary>
    public IReadOnlyList<string> Suggestions { get; init; } = [];

    /// <summary>
    /// Stage-by-stage reasoning explanations.
    /// </summary>
    public IReadOnlyDictionary<string, string> Reasoning { get; init; } =
        new Dictionary<string, string>();
}

/// <summary>
/// Factor contributing to chunk assessment.
/// </summary>
public sealed record AssessmentFactor
{
    /// <summary>
    /// Name of the factor.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Score contribution (-1.0 to 1.0).
    /// </summary>
    public double Contribution { get; init; }

    /// <summary>
    /// Explanation of the factor's evaluation.
    /// </summary>
    public string Explanation { get; init; } = string.Empty;
}

/// <summary>
/// Result of chunk filtering with scores and decision.
/// </summary>
public sealed record FilteredChunk
{
    /// <summary>
    /// The original chunk that was evaluated.
    /// </summary>
    public required Chunk Chunk { get; init; }

    /// <summary>
    /// Overall relevance score (0.0 to 1.0).
    /// </summary>
    public double RelevanceScore { get; init; }

    /// <summary>
    /// Quality score (0.0 to 1.0).
    /// </summary>
    public double QualityScore { get; init; }

    /// <summary>
    /// Combined score considering relevance and quality weights.
    /// </summary>
    public double CombinedScore { get; init; }

    /// <summary>
    /// Whether the chunk passed the filtering criteria.
    /// </summary>
    public bool Passed { get; init; }

    /// <summary>
    /// Detailed assessment information.
    /// </summary>
    public ChunkAssessment? Assessment { get; init; }

    /// <summary>
    /// Human-readable reason for the filtering decision.
    /// </summary>
    public string Reason { get; init; } = string.Empty;
}

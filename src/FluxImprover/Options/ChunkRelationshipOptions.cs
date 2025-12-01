using FluxImprover.Models;

namespace FluxImprover.Options;

/// <summary>
/// Options for chunk relationship discovery.
/// </summary>
public sealed class ChunkRelationshipOptions
{
    private float? _temperature;
    private int _maxTokens = 1024;
    private float _minConfidence = 0.5f;

    /// <summary>
    /// LLM temperature (0.0 ~ 2.0).
    /// null uses the model's default value.
    /// </summary>
    public float? Temperature
    {
        get => _temperature;
        init
        {
            if (value.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value.Value, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Value, 2.0f);
            }
            _temperature = value;
        }
    }

    /// <summary>
    /// Maximum tokens for LLM response (default: 1024).
    /// </summary>
    public int MaxTokens
    {
        get => _maxTokens;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxTokens = value;
        }
    }

    /// <summary>
    /// Minimum confidence threshold for relationships (default: 0.5).
    /// </summary>
    public float MinConfidence
    {
        get => _minConfidence;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0f);
            _minConfidence = value;
        }
    }

    /// <summary>
    /// Relationship types to discover (default: all types).
    /// </summary>
    public IReadOnlyList<ChunkRelationshipType> RelationshipTypes { get; init; } =
        Enum.GetValues<ChunkRelationshipType>().ToArray();

    /// <summary>
    /// Maximum number of relationships to return per chunk pair (default: 3).
    /// </summary>
    public int MaxRelationshipsPerPair { get; init; } = 3;

    /// <summary>
    /// Enable parallel processing for batch operations (default: true).
    /// </summary>
    public bool EnableParallelProcessing { get; init; } = true;

    /// <summary>
    /// Maximum degree of parallelism for batch operations (default: 4).
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 4;

    /// <summary>
    /// Include explanation for each relationship (default: true).
    /// </summary>
    public bool IncludeExplanations { get; init; } = true;
}

namespace FluxImprover.Options;

/// <summary>
/// Options for contextual enrichment of chunks.
/// Based on Anthropic's Contextual Retrieval pattern (Sep 2024).
/// </summary>
public sealed class ContextualEnrichmentOptions
{
    private float? _temperature;
    private int _maxTokens = 512;
    private int _maxContextLength = 300;

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
    /// Maximum tokens for LLM response (default: 512).
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
    /// Maximum context summary length in characters (default: 300).
    /// </summary>
    public int MaxContextLength
    {
        get => _maxContextLength;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxContextLength = value;
        }
    }

    /// <summary>
    /// Include document structure information (heading path) in context generation.
    /// Default: true.
    /// </summary>
    public bool IncludeStructureInfo { get; init; } = true;

    /// <summary>
    /// Include chunk position information in context generation.
    /// Default: true.
    /// </summary>
    public bool IncludePositionInfo { get; init; } = true;

    /// <summary>
    /// Enable parallel processing for batch operations (default: true).
    /// </summary>
    public bool EnableParallelProcessing { get; init; } = true;

    /// <summary>
    /// Maximum degree of parallelism for batch operations (default: 4).
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 4;
}

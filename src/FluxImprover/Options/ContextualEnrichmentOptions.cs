namespace FluxImprover.Options;

using FluxImprover.Services;

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
    /// Whether the model may reason before answering (default: <see cref="ThinkingMode.Off"/>).
    /// </summary>
    /// <remarks>
    /// The answer is a one-to-three-sentence summary generated once per chunk at ingestion, so reasoning buys no
    /// quality and multiplies the cost. It also shares <see cref="MaxTokens"/> with the answer: a reasoning model left
    /// on its template default can spend the whole budget thinking, and the cut-off summary is then discarded. Set
    /// <see cref="ThinkingMode.Auto"/> to leave the choice to the model's chat template.
    /// </remarks>
    public ThinkingMode Thinking { get; init; } = ThinkingMode.Off;

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

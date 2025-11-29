namespace FluxImprover.Options;

/// <summary>
/// Options for query preprocessing service.
/// </summary>
public sealed class QueryPreprocessingOptions
{
    private float _temperature = 0.3f;
    private int _maxTokens = 500;
    private int _maxSynonymsPerKeyword = 3;
    private int _maxKeywords = 10;
    private float _minIntentConfidence = 0.5f;

    /// <summary>
    /// Whether to use LLM for synonym expansion. Default is true.
    /// </summary>
    public bool UseLlmExpansion { get; init; } = true;

    /// <summary>
    /// Whether to use LLM for intent classification. Default is true.
    /// </summary>
    public bool UseLlmIntentClassification { get; init; } = true;

    /// <summary>
    /// Whether to extract entities from the query. Default is true.
    /// </summary>
    public bool ExtractEntities { get; init; } = true;

    /// <summary>
    /// Maximum number of synonyms per keyword. Default is 3.
    /// </summary>
    public int MaxSynonymsPerKeyword
    {
        get => _maxSynonymsPerKeyword;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxSynonymsPerKeyword = value;
        }
    }

    /// <summary>
    /// Maximum number of keywords to extract. Default is 10.
    /// </summary>
    public int MaxKeywords
    {
        get => _maxKeywords;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxKeywords = value;
        }
    }

    /// <summary>
    /// Temperature for LLM calls (0.0 to 2.0). Default is 0.3 for more deterministic output.
    /// </summary>
    public float Temperature
    {
        get => _temperature;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 2.0f);
            _temperature = value;
        }
    }

    /// <summary>
    /// Maximum tokens for LLM response. Default is 500.
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
    /// Custom domain-specific synonyms to supplement LLM expansion.
    /// Key: original term, Value: list of synonyms.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? DomainSynonyms { get; init; }

    /// <summary>
    /// Language for processing. Default is "en" (English).
    /// </summary>
    public string Language { get; init; } = "en";

    /// <summary>
    /// Whether to include technical term expansions (e.g., "auth" -> "authentication").
    /// Default is true.
    /// </summary>
    public bool ExpandTechnicalTerms { get; init; } = true;

    /// <summary>
    /// Minimum confidence threshold for intent classification (0.0 to 1.0). Default is 0.5.
    /// </summary>
    public float MinIntentConfidence
    {
        get => _minIntentConfidence;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0f);
            _minIntentConfidence = value;
        }
    }
}

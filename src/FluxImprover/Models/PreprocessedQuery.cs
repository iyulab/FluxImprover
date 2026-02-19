namespace FluxImprover.Models;

/// <summary>
/// Represents a preprocessed query with normalized form, expanded terms, and classified intent.
/// </summary>
public sealed record PreprocessedQuery
{
    /// <summary>
    /// The original query text before preprocessing.
    /// </summary>
    public required string OriginalQuery { get; init; }

    /// <summary>
    /// The normalized query text (lowercase, trimmed, cleaned).
    /// </summary>
    public required string NormalizedQuery { get; init; }

    /// <summary>
    /// Expanded query with synonyms and related terms.
    /// </summary>
    public required string ExpandedQuery { get; init; }

    /// <summary>
    /// Keywords extracted from the query.
    /// </summary>
    public required IReadOnlyList<string> Keywords { get; init; }

    /// <summary>
    /// Expanded keywords including synonyms and related terms.
    /// </summary>
    public required IReadOnlyList<string> ExpandedKeywords { get; init; }

    /// <summary>
    /// Classified intent of the query.
    /// </summary>
    public required QueryClassification Intent { get; init; }

    /// <summary>
    /// Confidence score for the intent classification (0.0 to 1.0).
    /// </summary>
    public required double IntentConfidence { get; init; }

    /// <summary>
    /// Detected entities in the query (e.g., file names, class names, method names).
    /// </summary>
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> Entities { get; init; }

    /// <summary>
    /// Suggested search strategy based on query analysis.
    /// </summary>
    public required RecommendedSearchMode SuggestedStrategy { get; init; }

    /// <summary>
    /// Processing metadata including timing and model info.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Recommended search strategy based on query analysis.
/// </summary>
public enum RecommendedSearchMode
{
    /// <summary>
    /// Standard semantic search.
    /// </summary>
    Semantic,

    /// <summary>
    /// Keyword-based search for exact matches.
    /// </summary>
    Keyword,

    /// <summary>
    /// Hybrid search combining semantic and keyword approaches.
    /// </summary>
    Hybrid,

    /// <summary>
    /// Multi-query search with expanded terms.
    /// </summary>
    MultiQuery
}

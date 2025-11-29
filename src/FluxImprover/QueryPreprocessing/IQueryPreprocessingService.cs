using FluxImprover.Models;
using FluxImprover.Options;

namespace FluxImprover.QueryPreprocessing;

/// <summary>
/// Service for preprocessing queries before RAG retrieval.
/// Provides query normalization, synonym expansion, intent classification, and entity extraction.
/// </summary>
public interface IQueryPreprocessingService
{
    /// <summary>
    /// Preprocesses a query with full analysis including normalization, expansion, and classification.
    /// </summary>
    /// <param name="query">The query to preprocess.</param>
    /// <param name="options">Preprocessing options. Uses defaults if null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Preprocessed query with all analysis results.</returns>
    Task<PreprocessedQuery> PreprocessAsync(
        string query,
        QueryPreprocessingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Normalizes a query (lowercase, trim, remove extra whitespace).
    /// </summary>
    /// <param name="query">The query to normalize.</param>
    /// <returns>Normalized query string.</returns>
    string Normalize(string query);

    /// <summary>
    /// Expands a query with synonyms and related terms using LLM.
    /// </summary>
    /// <param name="query">The query to expand.</param>
    /// <param name="options">Preprocessing options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of expanded keywords including synonyms.</returns>
    Task<IReadOnlyList<string>> ExpandWithSynonymsAsync(
        string query,
        QueryPreprocessingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Classifies the intent of a query.
    /// </summary>
    /// <param name="query">The query to classify.</param>
    /// <param name="options">Preprocessing options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of classified intent and confidence score.</returns>
    Task<(QueryIntent Intent, double Confidence)> ClassifyIntentAsync(
        string query,
        QueryPreprocessingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts keywords from a query.
    /// </summary>
    /// <param name="query">The query to extract keywords from.</param>
    /// <param name="options">Preprocessing options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of extracted keywords.</returns>
    Task<IReadOnlyList<string>> ExtractKeywordsAsync(
        string query,
        QueryPreprocessingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts named entities from a query (file names, class names, method names, etc.).
    /// </summary>
    /// <param name="query">The query to extract entities from.</param>
    /// <param name="options">Preprocessing options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of entity types to lists of entities.</returns>
    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ExtractEntitiesAsync(
        string query,
        QueryPreprocessingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preprocesses multiple queries in batch.
    /// </summary>
    /// <param name="queries">The queries to preprocess.</param>
    /// <param name="options">Preprocessing options. Uses defaults if null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of preprocessed queries.</returns>
    Task<IReadOnlyList<PreprocessedQuery>> PreprocessBatchAsync(
        IEnumerable<string> queries,
        QueryPreprocessingOptions? options = null,
        CancellationToken cancellationToken = default);
}

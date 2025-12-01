namespace FluxImprover.Utilities;

/// <summary>
/// Provides heuristic-based quality analysis for chunks without LLM calls.
/// Used for pre-enrichment quality assessment and conditional enrichment decisions.
/// </summary>
public static class ChunkQualityAnalyzer
{
    /// <summary>
    /// Minimum content length to consider for summarization (characters).
    /// Chunks below this threshold skip summarization.
    /// </summary>
    public const int DefaultMinSummarizationLength = 500;

    /// <summary>
    /// Minimum information density to consider for keyword extraction.
    /// Chunks below this threshold may skip keyword extraction.
    /// </summary>
    public const float DefaultMinKeywordDensity = 0.3f;

    /// <summary>
    /// Analyzes chunk quality using heuristics without LLM calls.
    /// </summary>
    /// <param name="content">The chunk content to analyze.</param>
    /// <returns>Quality analysis result with scores and recommendations.</returns>
    public static ChunkQualityResult Analyze(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new ChunkQualityResult
            {
                OverallScore = 0f,
                CompletenessScore = 0f,
                DensityScore = 0f,
                StructureScore = 0f,
                ContentLength = 0,
                Recommendation = EnrichmentRecommendation.None
            };
        }

        var completeness = EvaluateCompleteness(content);
        var density = EvaluateInformationDensity(content);
        var structure = EvaluateStructure(content);

        var overall = (completeness * 0.3f) + (density * 0.4f) + (structure * 0.3f);

        return new ChunkQualityResult
        {
            OverallScore = overall,
            CompletenessScore = completeness,
            DensityScore = density,
            StructureScore = structure,
            ContentLength = content.Length,
            Recommendation = DetermineRecommendation(content, completeness, density, structure)
        };
    }

    /// <summary>
    /// Analyzes chunk quality with metadata context.
    /// </summary>
    /// <param name="content">The chunk content to analyze.</param>
    /// <param name="metadata">Optional metadata dictionary.</param>
    /// <returns>Quality analysis result with scores and recommendations.</returns>
    public static ChunkQualityResult Analyze(string content, IDictionary<string, object>? metadata)
    {
        var result = Analyze(content);

        if (metadata is null)
            return result;

        // Check for table content type
        if (metadata.TryGetValue(ChunkMetadataKeys.ContentType, out var contentTypeObj) &&
            contentTypeObj is string contentType &&
            contentType.Equals(ChunkContentTypes.Table, StringComparison.OrdinalIgnoreCase))
        {
            // Tables have different quality characteristics
            result = result with
            {
                StructureScore = Math.Max(result.StructureScore, 0.8f),
                Recommendation = result.Recommendation | EnrichmentRecommendation.UseTablePrompt
            };
        }

        // Check chunk position for structural importance
        if (metadata.TryGetValue(ChunkMetadataKeys.ChunkIndex, out var indexObj) &&
            indexObj is int index && index < 3)
        {
            result = result with
            {
                StructureScore = Math.Min(1f, result.StructureScore + 0.1f)
            };
        }

        return result;
    }

    /// <summary>
    /// Evaluates sentence completeness (proper start and end).
    /// </summary>
    private static float EvaluateCompleteness(string content)
    {
        var trimmed = content.Trim();
        if (string.IsNullOrEmpty(trimmed)) return 0f;

        var score = 0f;

        // Check for proper sentence start (capital letter or known patterns)
        if (char.IsUpper(trimmed[0]) || trimmed.StartsWith('#') || trimmed.StartsWith('-'))
            score += 0.5f;

        // Check for proper sentence end
        if (trimmed[^1] is '.' or '!' or '?' or ':' or ';')
            score += 0.5f;

        return score;
    }

    /// <summary>
    /// Evaluates information density (unique words, technical terms).
    /// </summary>
    private static float EvaluateInformationDensity(string content)
    {
        var words = content.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return 0f;

        var uniqueWords = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var density = (float)uniqueWords / words.Length;

        // Bonus for technical content indicators
        if (words.Any(w => w.Any(char.IsDigit)))
            density += 0.1f;

        // Bonus for identifiers/technical terms
        if (words.Any(w => w.Contains('_') || w.Contains('-') || w.Contains('.')))
            density += 0.1f;

        return Math.Min(1f, density);
    }

    /// <summary>
    /// Evaluates structural elements (headings, code blocks, tables).
    /// </summary>
    private static float EvaluateStructure(string content)
    {
        var score = 0.5f;

        // Check for markdown headings
        if (content.StartsWith('#') || content.Contains("\n#"))
            score += 0.15f;

        // Check for code blocks
        if (content.Contains("```"))
            score += 0.15f;

        // Check for table markers
        if (content.Contains('|') && content.Contains('\n'))
            score += 0.1f;

        // Check for list items
        if (content.Contains("\n- ") || content.Contains("\n* ") || content.Contains("\n1."))
            score += 0.1f;

        return Math.Min(1f, score);
    }

    /// <summary>
    /// Determines enrichment recommendations based on analysis.
    /// </summary>
    private static EnrichmentRecommendation DetermineRecommendation(
        string content,
        float completeness,
        float density,
        float structure)
    {
        var recommendation = EnrichmentRecommendation.None;

        // Summarization recommendation based on content length
        if (content.Length >= DefaultMinSummarizationLength)
            recommendation |= EnrichmentRecommendation.Summarize;

        // Keyword extraction recommendation based on density
        if (density >= DefaultMinKeywordDensity)
            recommendation |= EnrichmentRecommendation.ExtractKeywords;

        // Context addition for low coherence/completeness
        if (completeness < 0.5f)
            recommendation |= EnrichmentRecommendation.AddContext;

        return recommendation;
    }
}

/// <summary>
/// Result of chunk quality analysis.
/// </summary>
public sealed record ChunkQualityResult
{
    /// <summary>
    /// Overall quality score (0.0 - 1.0).
    /// </summary>
    public required float OverallScore { get; init; }

    /// <summary>
    /// Content completeness score (proper sentences, no truncation).
    /// </summary>
    public required float CompletenessScore { get; init; }

    /// <summary>
    /// Information density score (useful content vs boilerplate).
    /// </summary>
    public required float DensityScore { get; init; }

    /// <summary>
    /// Structure score (headers, lists, code blocks).
    /// </summary>
    public required float StructureScore { get; init; }

    /// <summary>
    /// Content length in characters.
    /// </summary>
    public required int ContentLength { get; init; }

    /// <summary>
    /// Recommended enrichment actions based on analysis.
    /// </summary>
    public required EnrichmentRecommendation Recommendation { get; init; }

    /// <summary>
    /// Indicates whether summarization is recommended.
    /// </summary>
    public bool ShouldSummarize => Recommendation.HasFlag(EnrichmentRecommendation.Summarize);

    /// <summary>
    /// Indicates whether keyword extraction is recommended.
    /// </summary>
    public bool ShouldExtractKeywords => Recommendation.HasFlag(EnrichmentRecommendation.ExtractKeywords);
}

/// <summary>
/// Recommended enrichment actions based on chunk quality analysis.
/// </summary>
[Flags]
public enum EnrichmentRecommendation
{
    /// <summary>
    /// No enrichment recommended.
    /// </summary>
    None = 0,

    /// <summary>
    /// Summarization recommended.
    /// </summary>
    Summarize = 1,

    /// <summary>
    /// Keyword extraction recommended.
    /// </summary>
    ExtractKeywords = 2,

    /// <summary>
    /// Additional context should be added.
    /// </summary>
    AddContext = 4,

    /// <summary>
    /// Use table-specific prompts for enrichment.
    /// </summary>
    UseTablePrompt = 8,

    /// <summary>
    /// Full enrichment (summarize + keywords).
    /// </summary>
    Full = Summarize | ExtractKeywords
}

/// <summary>
/// Well-known metadata keys for chunk processing.
/// </summary>
public static class ChunkMetadataKeys
{
    /// <summary>
    /// Content type of the chunk (e.g., "text", "table", "code").
    /// </summary>
    public const string ContentType = "content_type";

    /// <summary>
    /// Language of the chunk content.
    /// </summary>
    public const string Language = "language";

    /// <summary>
    /// Zero-based index of the chunk in the document.
    /// </summary>
    public const string ChunkIndex = "chunk_index";

    /// <summary>
    /// Source document identifier.
    /// </summary>
    public const string SourceDocument = "source_document";

    /// <summary>
    /// Heading path in the document hierarchy.
    /// </summary>
    public const string HeadingPath = "heading_path";
}

/// <summary>
/// Well-known content type values for chunks.
/// </summary>
public static class ChunkContentTypes
{
    /// <summary>
    /// Plain text content.
    /// </summary>
    public const string Text = "text";

    /// <summary>
    /// Table content (e.g., markdown tables).
    /// </summary>
    public const string Table = "table";

    /// <summary>
    /// Code block content.
    /// </summary>
    public const string Code = "code";

    /// <summary>
    /// List content (bullet points, numbered lists).
    /// </summary>
    public const string List = "list";

    /// <summary>
    /// Heading/title content.
    /// </summary>
    public const string Heading = "heading";
}

using System.Text.Json;
using System.Text.RegularExpressions;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.Services;
using FluxImprover.Utilities;

namespace FluxImprover.QueryPreprocessing;

/// <summary>
/// LLM-powered query preprocessing service for RAG query optimization.
/// </summary>
public sealed partial class QueryPreprocessingService : IQueryPreprocessingService
{
    private readonly ITextCompletionService _completionService;

    // Technical term expansions for common abbreviations
    private static readonly Dictionary<string, string[]> TechnicalTermExpansions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["auth"] = ["authentication", "authorization"],
        ["config"] = ["configuration", "settings"],
        ["db"] = ["database"],
        ["api"] = ["application programming interface", "endpoint"],
        ["ui"] = ["user interface", "frontend"],
        ["ux"] = ["user experience"],
        ["impl"] = ["implementation"],
        ["repo"] = ["repository"],
        ["func"] = ["function", "method"],
        ["param"] = ["parameter", "argument"],
        ["var"] = ["variable"],
        ["const"] = ["constant"],
        ["async"] = ["asynchronous"],
        ["sync"] = ["synchronous"],
        ["err"] = ["error", "exception"],
        ["msg"] = ["message"],
        ["req"] = ["request"],
        ["res"] = ["response"],
        ["docs"] = ["documentation"],
        ["env"] = ["environment"],
        ["deps"] = ["dependencies"],
        ["perf"] = ["performance"],
        ["sec"] = ["security"],
        ["init"] = ["initialization", "initialize"],
        ["exec"] = ["execution", "execute"],
        ["util"] = ["utility", "utilities"],
        ["lib"] = ["library"],
        ["pkg"] = ["package"],
        ["src"] = ["source"],
        ["dest"] = ["destination"],
        ["info"] = ["information"],
        ["val"] = ["validation", "value"],
        ["obj"] = ["object"],
        ["arr"] = ["array"],
        ["str"] = ["string"],
        ["num"] = ["number"],
        ["bool"] = ["boolean"],
        ["int"] = ["integer"],
        ["char"] = ["character"],
        ["prop"] = ["property"],
        ["attr"] = ["attribute"],
        ["elem"] = ["element"],
        ["idx"] = ["index"],
        ["len"] = ["length"],
        ["max"] = ["maximum"],
        ["min"] = ["minimum"],
        ["avg"] = ["average"],
        ["cnt"] = ["count"],
        ["tmp"] = ["temporary"],
        ["prev"] = ["previous"],
        ["curr"] = ["current"],
        ["next"] = ["next"],
    };

    /// <summary>
    /// Initializes a new instance of QueryPreprocessingService.
    /// </summary>
    public QueryPreprocessingService(ITextCompletionService completionService)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
    }

    /// <inheritdoc />
    public async Task<PreprocessedQuery> PreprocessAsync(
        string query,
        QueryPreprocessingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty.", nameof(query));

        options ??= new QueryPreprocessingOptions();
        var startTime = DateTime.UtcNow;

        // Normalize
        var normalized = Normalize(query);

        // Extract keywords
        var keywords = await ExtractKeywordsAsync(query, options, cancellationToken).ConfigureAwait(false);

        // Expand with synonyms
        var expandedKeywords = await ExpandWithSynonymsAsync(query, options, cancellationToken).ConfigureAwait(false);

        // Build expanded query
        var expandedQuery = BuildExpandedQuery(normalized, expandedKeywords);

        // Classify intent
        var (intent, confidence) = await ClassifyIntentAsync(query, options, cancellationToken).ConfigureAwait(false);

        // Extract entities
        var entities = options.ExtractEntities
            ? await ExtractEntitiesAsync(query, options, cancellationToken).ConfigureAwait(false)
            : new Dictionary<string, IReadOnlyList<string>>();

        // Determine search strategy
        var strategy = DetermineSearchStrategy(intent, keywords.Count, expandedKeywords.Count);

        var processingTime = DateTime.UtcNow - startTime;

        return new PreprocessedQuery
        {
            OriginalQuery = query,
            NormalizedQuery = normalized,
            ExpandedQuery = expandedQuery,
            Keywords = keywords,
            ExpandedKeywords = expandedKeywords,
            Intent = intent,
            IntentConfidence = confidence,
            Entities = entities,
            SuggestedStrategy = strategy,
            Metadata = new Dictionary<string, object>
            {
                ["processingTimeMs"] = processingTime.TotalMilliseconds,
                ["usedLlmExpansion"] = options.UseLlmExpansion,
                ["usedLlmIntent"] = options.UseLlmIntentClassification
            }
        };
    }

    /// <inheritdoc />
    public string Normalize(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        // Trim and normalize whitespace
        var normalized = WhitespaceRegex().Replace(query.Trim(), " ");

        // Convert to lowercase for consistent matching
        normalized = normalized.ToLowerInvariant();

        return normalized;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ExpandWithSynonymsAsync(
        string query,
        QueryPreprocessingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new QueryPreprocessingOptions();
        var expandedTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Extract words from query
        var words = ExtractWords(query);
        expandedTerms.UnionWith(words);

        // Add technical term expansions
        if (options.ExpandTechnicalTerms)
        {
            foreach (var word in words)
            {
                if (TechnicalTermExpansions.TryGetValue(word, out var expansions))
                {
                    expandedTerms.UnionWith(expansions);
                }
            }
        }

        // Add domain-specific synonyms
        if (options.DomainSynonyms != null)
        {
            foreach (var word in words)
            {
                if (options.DomainSynonyms.TryGetValue(word, out var synonyms))
                {
                    expandedTerms.UnionWith(synonyms);
                }
            }
        }

        // Use LLM for intelligent expansion
        if (options.UseLlmExpansion)
        {
            var llmExpansions = await GetLlmSynonymsAsync(query, words, options, cancellationToken).ConfigureAwait(false);
            expandedTerms.UnionWith(llmExpansions);
        }

        return expandedTerms.ToList();
    }

    /// <inheritdoc />
    public async Task<(QueryIntent Intent, double Confidence)> ClassifyIntentAsync(
        string query,
        QueryPreprocessingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new QueryPreprocessingOptions();

        // Fast heuristic classification first
        var (heuristicIntent, heuristicConfidence) = ClassifyIntentHeuristic(query);

        // If confidence is high enough or LLM is disabled, use heuristic result
        if (!options.UseLlmIntentClassification || heuristicConfidence >= 0.9)
        {
            return (heuristicIntent, heuristicConfidence);
        }

        // Use LLM for more accurate classification
        try
        {
            return await ClassifyIntentWithLlmAsync(query, options, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Fallback to heuristic on LLM failure
            return (heuristicIntent, heuristicConfidence);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ExtractKeywordsAsync(
        string query,
        QueryPreprocessingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new QueryPreprocessingOptions();

        var prompt = $"""
            Extract the most important keywords from this query for search purposes.
            Return only the keywords as a JSON array of strings.
            Focus on nouns, verbs, and technical terms. Exclude common stop words.
            Maximum {options.MaxKeywords} keywords.

            Query: {query}

            Return format: ["keyword1", "keyword2", ...]
            """;

        try
        {
            var completionOptions = new CompletionOptions
            {
                Temperature = options.Temperature,
                MaxTokens = options.MaxTokens,
                JsonMode = true
            };

            var response = await _completionService.CompleteAsync(prompt, completionOptions, cancellationToken).ConfigureAwait(false);
            return ParseStringArray(response);
        }
        catch
        {
            // Fallback to simple word extraction
            return ExtractWords(query).Take(options.MaxKeywords).ToList();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PreprocessedQuery>> PreprocessBatchAsync(
        IEnumerable<string> queries,
        QueryPreprocessingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var queryList = queries?.ToList() ?? throw new ArgumentNullException(nameof(queries));

        if (queryList.Count == 0)
            return [];

        var results = new List<PreprocessedQuery>(queryList.Count);

        foreach (var query in queryList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await PreprocessAsync(query, options, cancellationToken).ConfigureAwait(false);
            results.Add(result);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ExtractEntitiesAsync(
        string query,
        QueryPreprocessingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new QueryPreprocessingOptions();

        var prompt = $$"""
            Extract named entities from this query. Identify:
            - files: file names or paths (e.g., "config.json", "src/main.ts")
            - types: class names, interface names, type names (e.g., "UserService", "IRepository")
            - methods: function or method names (e.g., "getData", "processRequest")
            - packages: library or package names (e.g., "lodash", "react")
            - variables: variable names mentioned
            - concepts: technical concepts or patterns (e.g., "dependency injection", "singleton")

            Query: {{query}}

            Return as JSON object with entity types as keys and arrays of entities as values.
            Only include entity types that have matches. Example:
            {"files": ["config.json"], "types": ["UserService"]}
            """;

        try
        {
            var completionOptions = new CompletionOptions
            {
                Temperature = options.Temperature,
                MaxTokens = options.MaxTokens,
                JsonMode = true
            };

            var response = await _completionService.CompleteAsync(prompt, completionOptions, cancellationToken).ConfigureAwait(false);
            return ParseEntityDictionary(response);
        }
        catch
        {
            return new Dictionary<string, IReadOnlyList<string>>();
        }
    }

    #region Private Methods

    private async Task<IReadOnlyList<string>> GetLlmSynonymsAsync(
        string query,
        IEnumerable<string> keywords,
        QueryPreprocessingOptions options,
        CancellationToken cancellationToken)
    {
        var keywordList = string.Join(", ", keywords);

        var prompt = $"""
            For the following search query and its keywords, provide synonyms and related terms
            that would help find relevant documents. Focus on technical and domain-specific synonyms.

            Query: {query}
            Keywords: {keywordList}

            Return up to {options.MaxSynonymsPerKeyword} synonyms per keyword as a flat JSON array.
            Only include high-quality, relevant synonyms. Example: ["synonym1", "synonym2"]
            """;

        try
        {
            var completionOptions = new CompletionOptions
            {
                Temperature = options.Temperature,
                MaxTokens = options.MaxTokens,
                JsonMode = true
            };

            var response = await _completionService.CompleteAsync(prompt, completionOptions, cancellationToken).ConfigureAwait(false);
            return ParseStringArray(response);
        }
        catch
        {
            return [];
        }
    }

    private async Task<(QueryIntent Intent, double Confidence)> ClassifyIntentWithLlmAsync(
        string query,
        QueryPreprocessingOptions options,
        CancellationToken cancellationToken)
    {
        var prompt = $$"""
            Classify the intent of this query into one of these categories:
            - General: General information retrieval
            - Question: Direct question requiring an answer
            - Search: Looking for specific information
            - Definition: Asking for definition or explanation
            - Comparison: Comparing multiple items
            - HowTo: How to do something (procedural)
            - Troubleshooting: Problem solving or debugging
            - Code: Code-related query (implementation, syntax)
            - Conceptual: Theoretical or conceptual question

            Query: {{query}}

            Return as JSON: {"intent": "CategoryName", "confidence": 0.0-1.0}
            """;

        var completionOptions = new CompletionOptions
        {
            Temperature = options.Temperature,
            MaxTokens = 100,
            JsonMode = true
        };

        var response = await _completionService.CompleteAsync(prompt, completionOptions, cancellationToken).ConfigureAwait(false);

        try
        {
            var json = JsonHelpers.ExtractJsonFromText(response);
            if (json != null)
            {
                using var doc = JsonDocument.Parse(json);
                var intentStr = doc.RootElement.GetProperty("intent").GetString();
                var confidence = doc.RootElement.GetProperty("confidence").GetDouble();

                if (Enum.TryParse<QueryIntent>(intentStr, true, out var intent))
                {
                    return (intent, Math.Clamp(confidence, 0, 1));
                }
            }
        }
        catch
        {
            // Fallback below
        }

        return ClassifyIntentHeuristic(query);
    }

    private static (QueryIntent Intent, double Confidence) ClassifyIntentHeuristic(string query)
    {
        var lower = query.ToLowerInvariant();

        // More specific patterns first (before general Question pattern)

        // How-to patterns (specific: "how to", "how do I")
        if (HowToPatternRegex().IsMatch(lower))
            return (QueryIntent.HowTo, 0.9);

        // Definition patterns (specific: "define", "explain", "what is")
        if (DefinitionPatternRegex().IsMatch(lower))
            return (QueryIntent.Definition, 0.85);

        // Comparison patterns
        if (ComparisonPatternRegex().IsMatch(lower))
            return (QueryIntent.Comparison, 0.85);

        // Troubleshooting patterns
        if (TroubleshootingPatternRegex().IsMatch(lower))
            return (QueryIntent.Troubleshooting, 0.8);

        // Code patterns
        if (CodePatternRegex().IsMatch(lower))
            return (QueryIntent.Code, 0.75);

        // Search patterns (explicit)
        if (SearchPatternRegex().IsMatch(lower))
            return (QueryIntent.Search, 0.8);

        // General question patterns (less specific: starts with what/where/when/who/which/why/how)
        if (QuestionPatternRegex().IsMatch(lower))
            return (QueryIntent.Question, 0.7);

        // Default to general with lower confidence
        return (QueryIntent.General, 0.5);
    }

    private static string BuildExpandedQuery(string normalizedQuery, IReadOnlyList<string> expandedKeywords)
    {
        var uniqueTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add original query words
        uniqueTerms.UnionWith(ExtractWords(normalizedQuery));

        // Add expanded keywords
        uniqueTerms.UnionWith(expandedKeywords);

        return string.Join(" ", uniqueTerms);
    }

    private static SearchStrategy DetermineSearchStrategy(QueryIntent intent, int keywordCount, int expandedCount)
    {
        // Code queries benefit from keyword search for exact matches
        if (intent == QueryIntent.Code)
            return SearchStrategy.Keyword;

        // Definition queries work well with semantic search
        if (intent == QueryIntent.Definition || intent == QueryIntent.Conceptual)
            return SearchStrategy.Semantic;

        // If we have many expanded terms, use multi-query
        if (expandedCount > keywordCount * 2)
            return SearchStrategy.MultiQuery;

        // Default to hybrid for best coverage
        return SearchStrategy.Hybrid;
    }

    private static List<string> ExtractWords(string text)
    {
        return WordExtractRegex()
            .Matches(text)
            .Select(m => m.Value.ToLowerInvariant())
            .Where(w => w.Length > 2 && !StopWords.Contains(w))
            .Distinct()
            .ToList();
    }

    private static List<string> ParseStringArray(string response)
    {
        try
        {
            var json = JsonHelpers.ExtractJsonFromText(response);
            if (json == null) return [];

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                return doc.RootElement.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }
        }
        catch
        {
            // Fallback
        }
        return [];
    }

    private static Dictionary<string, IReadOnlyList<string>> ParseEntityDictionary(string response)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>();

        try
        {
            var json = JsonHelpers.ExtractJsonFromText(response);
            if (json == null) return result;

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var entities = prop.Value.EnumerateArray()
                            .Where(e => e.ValueKind == JsonValueKind.String)
                            .Select(e => e.GetString()!)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();

                        if (entities.Count > 0)
                        {
                            result[prop.Name] = entities;
                        }
                    }
                }
            }
        }
        catch
        {
            // Return empty on parse failure
        }

        return result;
    }

    // Common English stop words
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "as", "is", "was", "are", "were", "been",
        "be", "have", "has", "had", "do", "does", "did", "will", "would",
        "could", "should", "may", "might", "must", "shall", "can", "this",
        "that", "these", "those", "it", "its", "they", "them", "their",
        "we", "our", "you", "your", "he", "she", "him", "her", "his",
        "not", "no", "yes", "all", "any", "some", "most", "more", "less",
        "very", "just", "only", "also", "even", "still", "already", "yet"
    };

    #endregion

    #region Generated Regex

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\b\w+\b")]
    private static partial Regex WordExtractRegex();

    [GeneratedRegex(@"^(what|who|where|when|why|which|whose|whom)\b|(\?$)")]
    private static partial Regex QuestionPatternRegex();

    [GeneratedRegex(@"^how (do|can|to|should|would|could|does|did|is|are)\b")]
    private static partial Regex HowToPatternRegex();

    [GeneratedRegex(@"^(what is|define|explain|describe|meaning of)\b")]
    private static partial Regex DefinitionPatternRegex();

    [GeneratedRegex(@"\b(vs|versus|compare|comparison|difference|between|or)\b")]
    private static partial Regex ComparisonPatternRegex();

    [GeneratedRegex(@"\b(error|bug|fix|issue|problem|not working|failed|crash|exception)\b")]
    private static partial Regex TroubleshootingPatternRegex();

    [GeneratedRegex(@"\b(code|implement|function|class|method|syntax|example|snippet)\b")]
    private static partial Regex CodePatternRegex();

    [GeneratedRegex(@"^(find|search|look for|locate|where is)\b")]
    private static partial Regex SearchPatternRegex();

    #endregion
}

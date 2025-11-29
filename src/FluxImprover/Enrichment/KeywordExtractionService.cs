namespace FluxImprover.Enrichment;

using System.Text.Json;
using FluxImprover.Options;
using FluxImprover.Services;
using FluxImprover.Utilities;

/// <summary>
/// LLM 기반 키워드 추출 서비스
/// </summary>
public sealed class KeywordExtractionService : IKeywordExtractionService
{
    private readonly ITextCompletionService _completionService;

    public KeywordExtractionService(ITextCompletionService completionService)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ExtractKeywordsAsync(
        string text,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var response = await GetKeywordResponseAsync(text, options, cancellationToken);
        return ParseKeywords(response);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, double>> ExtractKeywordsWithScoresAsync(
        string text,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new Dictionary<string, double>();

        var response = await GetKeywordResponseAsync(text, options, cancellationToken);
        return ParseKeywordsWithScores(response);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IReadOnlyList<string>>> ExtractKeywordsBatchAsync(
        IEnumerable<string> texts,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<IReadOnlyList<string>>();

        foreach (var text in texts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var keywords = await ExtractKeywordsAsync(text, options, cancellationToken);
            results.Add(keywords);
        }

        return results;
    }

    private async Task<string> GetKeywordResponseAsync(
        string text,
        EnrichmentOptions? options,
        CancellationToken cancellationToken)
    {
        options ??= new EnrichmentOptions();

        var prompt = BuildPrompt(text, options);
        var completionOptions = new CompletionOptions
        {
            SystemPrompt = GetSystemPrompt(),
            Temperature = options.Temperature,
            MaxTokens = options.MaxTokens,
            JsonMode = true
        };

        return await _completionService.CompleteAsync(prompt, completionOptions, cancellationToken);
    }

    private static string GetSystemPrompt()
    {
        return "You are an expert at extracting relevant keywords and key phrases from text. " +
               "Always return results in valid JSON format.";
    }

    private static string BuildPrompt(string text, EnrichmentOptions options)
    {
        var maxKeywords = options.MaxKeywords > 0 ? options.MaxKeywords : 10;

        return $$"""
            Extract the top {{maxKeywords}} most relevant keywords from the following text.
            Return the results as a JSON object with this structure:
            {
                "keywords": [
                    {"keyword": "term", "relevance": 0.0-1.0}
                ]
            }

            Text to analyze:
            {{text}}
            """;
    }

    private static IReadOnlyList<string> ParseKeywords(string response)
    {
        try
        {
            var json = JsonHelpers.ExtractJsonFromText(response);
            if (json is null)
                return [];

            using var doc = JsonDocument.Parse(json);
            var keywords = new List<string>();

            // Handle both object with "keywords" property and direct array
            var keywordsArray = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement
                : doc.RootElement.TryGetProperty("keywords", out var kw) ? kw : default;

            if (keywordsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in keywordsArray.EnumerateArray())
                {
                    var keyword = ExtractKeywordFromElement(item);
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        keywords.Add(keyword);
                    }
                }
            }

            return keywords;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Extracts keyword string from a JSON element, handling various LLM response formats.
    /// Supports: {"keyword": "term"}, {"term": "word"}, {"name": "word"}, {"text": "word"},
    /// {"value": "word"}, {"word": "term"}, or plain string "term"
    /// </summary>
    private static string? ExtractKeywordFromElement(JsonElement item)
    {
        // Handle plain string array: ["keyword1", "keyword2"]
        if (item.ValueKind == JsonValueKind.String)
        {
            return item.GetString();
        }

        // Handle object with various property names (in order of likelihood)
        if (item.ValueKind == JsonValueKind.Object)
        {
            // Common property names for keyword values
            string[] keywordPropertyNames = ["keyword", "term", "name", "text", "value", "word", "key"];

            foreach (var propName in keywordPropertyNames)
            {
                if (item.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString();
                }
            }

            // Fallback: try to get the first string property
            foreach (var property in item.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    var value = property.Value.GetString();
                    // Skip score/relevance-like properties
                    if (!string.IsNullOrEmpty(value) &&
                        !property.Name.Contains("score", StringComparison.OrdinalIgnoreCase) &&
                        !property.Name.Contains("relevance", StringComparison.OrdinalIgnoreCase) &&
                        !property.Name.Contains("weight", StringComparison.OrdinalIgnoreCase) &&
                        !property.Name.Contains("confidence", StringComparison.OrdinalIgnoreCase))
                    {
                        return value;
                    }
                }
            }
        }

        return null;
    }

    private static IReadOnlyDictionary<string, double> ParseKeywordsWithScores(string response)
    {
        try
        {
            var json = JsonHelpers.ExtractJsonFromText(response);
            if (json is null)
                return new Dictionary<string, double>();

            using var doc = JsonDocument.Parse(json);
            var keywords = new Dictionary<string, double>();

            // Handle both object with "keywords" property and direct array
            var keywordsArray = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement
                : doc.RootElement.TryGetProperty("keywords", out var kw) ? kw : default;

            if (keywordsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in keywordsArray.EnumerateArray())
                {
                    var keyword = ExtractKeywordFromElement(item);
                    var relevance = ExtractScoreFromElement(item);

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        keywords[keyword] = relevance;
                    }
                }
            }

            return keywords;
        }
        catch
        {
            return new Dictionary<string, double>();
        }
    }

    /// <summary>
    /// Extracts score/relevance value from a JSON element, handling various LLM response formats.
    /// Supports: {"relevance": 0.9}, {"score": 0.9}, {"weight": 0.9}, {"confidence": 0.9}
    /// </summary>
    private static double ExtractScoreFromElement(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
            return 0.0;

        // Common property names for score values
        string[] scorePropertyNames = ["relevance", "score", "weight", "confidence", "importance", "rank"];

        foreach (var propName in scorePropertyNames)
        {
            if (item.TryGetProperty(propName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                {
                    return prop.GetDouble();
                }
                // Handle string numbers like "0.9"
                if (prop.ValueKind == JsonValueKind.String &&
                    double.TryParse(prop.GetString(), out var score))
                {
                    return score;
                }
            }
        }

        return 0.0;
    }
}

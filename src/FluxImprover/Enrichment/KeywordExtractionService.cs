namespace FluxImprover.Enrichment;

using System.Text.Json;
using FluxImprover.Abstractions.Options;
using FluxImprover.Abstractions.Services;
using FluxImprover.Abstractions.Utilities;

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
                    if (item.TryGetProperty("keyword", out var keyword))
                    {
                        keywords.Add(keyword.GetString() ?? string.Empty);
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
                    var keyword = item.TryGetProperty("keyword", out var k) ? k.GetString() : null;
                    var relevance = item.TryGetProperty("relevance", out var r) ? r.GetDouble() : 0.0;

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
}

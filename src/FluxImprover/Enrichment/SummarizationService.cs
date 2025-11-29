namespace FluxImprover.Enrichment;

using FluxImprover.Options;
using FluxImprover.Services;

/// <summary>
/// LLM 기반 텍스트 요약 서비스
/// </summary>
public sealed class SummarizationService : ISummarizationService
{
    private readonly ITextCompletionService _completionService;

    public SummarizationService(ITextCompletionService completionService)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
    }

    /// <inheritdoc />
    public async Task<string> SummarizeAsync(
        string text,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        options ??= new EnrichmentOptions();

        var prompt = BuildPrompt(text, options);
        var completionOptions = new CompletionOptions
        {
            SystemPrompt = GetSystemPrompt(),
            Temperature = options.Temperature,
            MaxTokens = options.MaxTokens
        };

        return await _completionService.CompleteAsync(prompt, completionOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> SummarizeBatchAsync(
        IEnumerable<string> texts,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        var results = new List<string>(textList.Count);

        foreach (var text in textList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var summary = await SummarizeAsync(text, options, cancellationToken);
            results.Add(summary);
        }

        return results;
    }

    private static string GetSystemPrompt()
    {
        return "You are an expert at creating clear, accurate summaries of documents and text. " +
               "Always preserve key information while being concise.";
    }

    private static string BuildPrompt(string text, EnrichmentOptions options)
    {
        var prompt = $"Please summarize the following text";

        if (options.MaxSummaryLength > 0)
        {
            prompt += $" in approximately {options.MaxSummaryLength} words or less";
        }

        prompt += ":\n\n" + text;

        return prompt;
    }
}

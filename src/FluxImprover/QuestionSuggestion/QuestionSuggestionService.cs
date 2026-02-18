namespace FluxImprover.QuestionSuggestion;

using System.Text.Json;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.Services;
using FluxImprover.Utilities;

/// <summary>
/// LLM 기반 질문 추천 서비스
/// </summary>
public class QuestionSuggestionService
{
    private readonly ITextCompletionService _completionService;

    public QuestionSuggestionService(ITextCompletionService completionService)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
    }

    /// <summary>
    /// 컨텍스트 기반으로 후속 질문을 추천합니다.
    /// </summary>
    /// <param name="context">문서 또는 대화 컨텍스트</param>
    /// <param name="options">추천 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추천 질문 목록</returns>
    public virtual async Task<IReadOnlyList<SuggestedQuestion>> SuggestAsync(
        string context,
        QuestionSuggestionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context))
            return [];

        options ??= new QuestionSuggestionOptions();

        var prompt = BuildPrompt(context, options);
        var completionOptions = new CompletionOptions
        {
            SystemPrompt = GetSystemPrompt(),
            Temperature = options.Temperature,
            MaxTokens = options.MaxTokens,
            JsonMode = true
        };

        var response = await _completionService.CompleteAsync(prompt, completionOptions, cancellationToken);
        return ParseAndFilterResponse(response, options);
    }

    /// <summary>
    /// QA 쌍을 기반으로 후속 질문을 추천합니다.
    /// </summary>
    /// <param name="qaPair">기준 QA 쌍</param>
    /// <param name="options">추천 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추천 질문 목록</returns>
    public virtual async Task<IReadOnlyList<SuggestedQuestion>> SuggestFromQAAsync(
        QAPair qaPair,
        QuestionSuggestionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(qaPair);

        var context = $"""
            Question: {qaPair.Question}
            Answer: {qaPair.Answer}
            """;

        return await SuggestAsync(context, options, cancellationToken);
    }

    /// <summary>
    /// 대화 기록을 기반으로 후속 질문을 추천합니다.
    /// </summary>
    /// <param name="history">대화 기록</param>
    /// <param name="options">추천 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추천 질문 목록</returns>
    public virtual async Task<IReadOnlyList<SuggestedQuestion>> SuggestFromConversationAsync(
        IEnumerable<ConversationMessage> history,
        QuestionSuggestionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);

        options ??= new QuestionSuggestionOptions();

        var messages = history
            .TakeLast(options.ContextWindowSize)
            .Select(m => $"{m.Role}: {m.Content}");

        var context = string.Join("\n\n", messages);

        return await SuggestAsync(context, options, cancellationToken);
    }

    private static string GetSystemPrompt()
    {
        return """
            You are an expert at generating insightful follow-up questions.
            Generate questions that help users explore topics more deeply.
            Questions should be relevant, clear, and encourage further exploration.
            Always return results in valid JSON format.
            """;
    }

    private static string BuildPrompt(string context, QuestionSuggestionOptions options)
    {
        var categories = string.Join(", ", options.Categories.Select(c => c.ToString()));
        var reasoningInstruction = options.IncludeReasoning
            ? "Include a 'reasoning' field explaining why each question is relevant."
            : "";

        return $$"""
            Based on the following context, suggest {{options.MaxSuggestions}} follow-up questions.

            Question categories to include: {{categories}}
            {{reasoningInstruction}}

            Context:
            {{context}}

            Return a JSON object with this structure:
            {
                "suggestions": [
                    {
                        "text": "question text",
                        "category": "FollowUp|Clarification|DeepDive|Related|Alternative",
                        "relevance": 0.0-1.0,
                        "reasoning": "optional explanation"
                    }
                ]
            }

            Category definitions:
            - FollowUp: Questions that naturally continue from the context
            - Clarification: Questions that clarify ambiguous points
            - DeepDive: Questions that explore a topic more deeply
            - Related: Questions about related topics
            - Alternative: Questions from different perspectives
            """;
    }

    private static List<SuggestedQuestion> ParseAndFilterResponse(
        string response,
        QuestionSuggestionOptions options)
    {
        try
        {
            var json = JsonHelpers.ExtractJsonFromText(response);
            if (json is null)
                return [];

            using var doc = JsonDocument.Parse(json);
            var suggestions = new List<SuggestedQuestion>();

            if (doc.RootElement.TryGetProperty("suggestions", out var suggestionsArray) &&
                suggestionsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in suggestionsArray.EnumerateArray())
                {
                    var text = item.TryGetProperty("text", out var t) ? t.GetString() : null;
                    var categoryStr = item.TryGetProperty("category", out var c) ? c.GetString() : "FollowUp";
                    var relevance = item.TryGetProperty("relevance", out var r) ? r.GetDouble() : 1.0;
                    var reasoning = item.TryGetProperty("reasoning", out var re) ? re.GetString() : null;

                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    // Filter by minimum relevance score
                    if (relevance < options.MinRelevanceScore)
                        continue;

                    var category = Enum.TryParse<QuestionCategory>(categoryStr, ignoreCase: true, out var cat)
                        ? cat
                        : QuestionCategory.FollowUp;

                    suggestions.Add(new SuggestedQuestion
                    {
                        Text = text,
                        Category = category,
                        Relevance = Math.Clamp(relevance, 0.0, 1.0),
                        Reasoning = reasoning
                    });
                }
            }

            // Limit to max suggestions, sorted by relevance
            return suggestions
                .OrderByDescending(s => s.Relevance)
                .Take(options.MaxSuggestions)
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}

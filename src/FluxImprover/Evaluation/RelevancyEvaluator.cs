namespace FluxImprover.Evaluation;

using System.Text.Json;
using FluxImprover.Abstractions.Options;
using FluxImprover.Abstractions.Services;
using FluxImprover.Abstractions.Utilities;

/// <summary>
/// LLM 기반 관련성(Relevancy) 평가기.
/// 답변이 질문에 관련되어 있는지 평가합니다.
/// </summary>
public sealed class RelevancyEvaluator
{
    private const string MetricName = "Relevancy";
    private readonly ITextCompletionService _completionService;

    public RelevancyEvaluator(ITextCompletionService completionService)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
    }

    /// <summary>
    /// 질문에 대한 답변의 관련성을 평가합니다.
    /// </summary>
    /// <param name="question">질문</param>
    /// <param name="answer">평가할 답변</param>
    /// <param name="options">평가 옵션</param>
    /// <param name="context">선택적 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>관련성 평가 결과</returns>
    public async Task<MetricResult> EvaluateAsync(
        string question,
        string answer,
        EvaluationOptions? options = null,
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(answer))
            return MetricResult.Failed(MetricName, "Empty question or answer");

        options ??= new EvaluationOptions();

        var prompt = BuildPrompt(question, answer, context);
        var completionOptions = new CompletionOptions
        {
            SystemPrompt = GetSystemPrompt(),
            Temperature = options.Temperature,
            MaxTokens = options.MaxTokens,
            JsonMode = true
        };

        var response = await _completionService.CompleteAsync(prompt, completionOptions, cancellationToken);
        return ParseResponse(response);
    }

    /// <summary>
    /// 여러 쌍의 질문-답변을 일괄 평가합니다.
    /// </summary>
    public async Task<IReadOnlyList<MetricResult>> EvaluateBatchAsync(
        IEnumerable<(string Question, string Answer)> pairs,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<MetricResult>();

        foreach (var (question, answer) in pairs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await EvaluateAsync(question, answer, options, cancellationToken: cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private static string GetSystemPrompt()
    {
        return """
            You are an expert at evaluating the relevancy of AI-generated answers.
            Relevancy measures how well the answer addresses the question asked.
            A relevant answer directly responds to what was asked.
            Always return results in valid JSON format.
            """;
    }

    private static string BuildPrompt(string question, string answer, string? context)
    {
        var contextSection = string.IsNullOrWhiteSpace(context)
            ? string.Empty
            : $"""

            Context (for reference):
            {context}
            """;

        return $$"""
            Evaluate the relevancy of the following answer to the given question.
            Consider whether the answer directly addresses what was asked.

            Question:
            {{question}}

            Answer:
            {{answer}}{{contextSection}}

            Return a JSON object with this structure:
            {
                "score": 0.0-1.0,
                "reasoning": "explanation of the score"
            }

            Score guidelines:
            - 1.0: Answer completely and directly addresses the question
            - 0.7-0.9: Answer mostly addresses the question with minor gaps
            - 0.4-0.6: Answer partially addresses the question
            - 0.1-0.3: Answer tangentially related to the question
            - 0.0: Answer completely unrelated to the question
            """;
    }

    private static MetricResult ParseResponse(string response)
    {
        try
        {
            var json = JsonHelpers.ExtractJsonFromText(response);
            if (json is null)
                return MetricResult.Failed(MetricName, "Failed to extract JSON from response");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var score = root.TryGetProperty("score", out var s) ? s.GetDouble() : 0.0;
            var reasoning = root.TryGetProperty("reasoning", out var r) ? r.GetString() : null;

            var details = new Dictionary<string, object?>
            {
                ["reasoning"] = reasoning
            };

            return new MetricResult
            {
                MetricName = MetricName,
                Score = Math.Clamp(score, 0.0, 1.0),
                Details = details
            };
        }
        catch
        {
            return MetricResult.Failed(MetricName, "Failed to parse evaluation response");
        }
    }
}

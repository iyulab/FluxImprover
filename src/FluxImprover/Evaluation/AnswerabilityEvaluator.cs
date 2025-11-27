namespace FluxImprover.Evaluation;

using System.Text.Json;
using FluxImprover.Abstractions.Options;
using FluxImprover.Abstractions.Services;
using FluxImprover.Abstractions.Utilities;

/// <summary>
/// LLM 기반 답변 가능성(Answerability) 평가기.
/// 주어진 컨텍스트로 질문에 답변할 수 있는지 평가합니다.
/// </summary>
public sealed class AnswerabilityEvaluator
{
    private const string MetricName = "Answerability";
    private readonly ITextCompletionService _completionService;

    public AnswerabilityEvaluator(ITextCompletionService completionService)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
    }

    /// <summary>
    /// 컨텍스트를 기반으로 질문의 답변 가능성을 평가합니다.
    /// </summary>
    /// <param name="context">참조 컨텍스트</param>
    /// <param name="question">평가할 질문</param>
    /// <param name="options">평가 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>답변 가능성 평가 결과</returns>
    public async Task<MetricResult> EvaluateAsync(
        string context,
        string question,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context) || string.IsNullOrWhiteSpace(question))
            return MetricResult.Failed(MetricName, "Empty context or question");

        options ??= new EvaluationOptions();

        var prompt = BuildPrompt(context, question);
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
    /// 여러 컨텍스트를 결합하여 질문의 답변 가능성을 평가합니다.
    /// </summary>
    public async Task<MetricResult> EvaluateWithMultipleContextsAsync(
        IEnumerable<string> contexts,
        string question,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var combinedContext = string.Join("\n\n", contexts.Where(c => !string.IsNullOrWhiteSpace(c)));
        return await EvaluateAsync(combinedContext, question, options, cancellationToken);
    }

    /// <summary>
    /// 여러 쌍의 컨텍스트-질문을 일괄 평가합니다.
    /// </summary>
    public async Task<IReadOnlyList<MetricResult>> EvaluateBatchAsync(
        IEnumerable<(string Context, string Question)> pairs,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<MetricResult>();

        foreach (var (context, question) in pairs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await EvaluateAsync(context, question, options, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private static string GetSystemPrompt()
    {
        return """
            You are an expert at evaluating whether questions can be answered from given contexts.
            Answerability measures whether the context contains sufficient information to answer the question.
            Always return results in valid JSON format.
            """;
    }

    private static string BuildPrompt(string context, string question)
    {
        return $$"""
            Evaluate whether the following question can be answered using the provided context.
            Consider if the context contains all necessary information to fully answer the question.

            Context:
            {{context}}

            Question:
            {{question}}

            Return a JSON object with this structure:
            {
                "score": 0.0-1.0,
                "reasoning": "explanation of the score",
                "answerable": true/false,
                "evidence": "relevant evidence from context if answerable"
            }

            Score guidelines (aligned with NVIDIA A-D grading):
            - 1.0 (Grade A): Context fully answers the question with all required information
            - 0.7-0.9 (Grade B): Context provides most information, minor details missing
            - 0.3-0.6 (Grade C): Question is related but context cannot answer it adequately
            - 0.0-0.2 (Grade D): Question is unrelated to the context
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
            var answerable = root.TryGetProperty("answerable", out var a) && a.GetBoolean();
            var evidence = root.TryGetProperty("evidence", out var e) ? e.GetString() : null;

            var details = new Dictionary<string, object?>
            {
                ["reasoning"] = reasoning,
                ["answerable"] = answerable,
                ["evidence"] = evidence
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

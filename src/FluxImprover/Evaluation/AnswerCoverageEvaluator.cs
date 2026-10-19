namespace FluxImprover.Evaluation;

using System.Text.Json;
using FluxImprover.Options;
using FluxImprover.Services;
using FluxImprover.Utilities;

/// <summary>
/// LLM 기반 정답 커버리지(Answer coverage) 평가기.
/// 답변이 정답(reference)의 사실을 얼마나 담았는지 평가합니다 — 정답을 원자 사실로 나누고(또는 호출자가 사실 목록을 주고)
/// 각 사실이 답변에 담겼는지 <c>supported</c> / <c>partial</c> / <c>missing</c> 으로 판정한 뒤,
/// 점수 = (supported + 0.5 × partial) / 사실 수. TREC 2024 RAG Track 의 nugget 평가와 같은 형태다.
/// 충실도(<see cref="FaithfulnessEvaluator"/>)가 «답이 컨텍스트를 벗어나지 않는가» 를 본다면, 이것은 «답이 필요한 것을 말했는가» 를 본다.
/// </summary>
public sealed class AnswerCoverageEvaluator
{
    private const string MetricName = "AnswerCoverage";
    private readonly ITextGenerationService _completionService;

    public AnswerCoverageEvaluator(ITextGenerationService completionService)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
    }

    /// <summary>
    /// 정답 문장에서 사실을 뽑아, 답변이 각 사실을 담았는지 평가합니다.
    /// </summary>
    /// <param name="answer">평가할 답변</param>
    /// <param name="referenceAnswer">정답(사람이 쓴 기대 답변)</param>
    /// <param name="options">평가 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>커버리지 평가 결과 — <see cref="MetricResult.Details"/> 의 <c>facts</c> 에 사실별 판정</returns>
    public Task<MetricResult> EvaluateAsync(
        string answer,
        string referenceAnswer,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(answer) || string.IsNullOrWhiteSpace(referenceAnswer))
            return Task.FromResult(MetricResult.Failed(MetricName, "Empty answer or reference answer"));

        return RunAsync(BuildPrompt(answer, $"Reference answer:\n{referenceAnswer}\n\nFirst split the reference answer into atomic facts, then judge each."),
            options, cancellationToken);
    }

    /// <summary>
    /// 호출자가 준 사실 목록에 대해, 답변이 각 사실을 담았는지 평가합니다.
    /// </summary>
    /// <param name="answer">평가할 답변</param>
    /// <param name="facts">답변이 담아야 할 사실들</param>
    /// <param name="options">평가 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>커버리지 평가 결과 — <see cref="MetricResult.Details"/> 의 <c>facts</c> 에 사실별 판정</returns>
    public Task<MetricResult> EvaluateAsync(
        string answer,
        IReadOnlyList<string> facts,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (string.IsNullOrWhiteSpace(answer) || facts.Count == 0)
            return Task.FromResult(MetricResult.Failed(MetricName, "Empty answer or no facts"));

        var list = string.Join("\n", facts.Select((f, i) => $"{i + 1}. {f}"));
        return RunAsync(BuildPrompt(answer, $"Facts:\n{list}\n\nJudge exactly these facts, in this order."), options, cancellationToken);
    }

    private async Task<MetricResult> RunAsync(string prompt, EvaluationOptions? options, CancellationToken cancellationToken)
    {
        options ??= new EvaluationOptions();
        var completionOptions = new CompletionOptions
        {
            SystemPrompt = GetSystemPrompt(),
            Temperature = options.Temperature,
            MaxTokens = options.MaxTokens,
            JsonMode = true
        };

        var response = await _completionService.CompleteAsync(prompt, completionOptions, cancellationToken);
        return ParseResponse(response, options);
    }

    private static string GetSystemPrompt()
    {
        return """
            You are an expert at judging whether an answer contains the facts a correct answer needs.
            A fact is supported when the answer states it (in any wording), partial when the answer states part of it or states
            it vaguely, and missing when the answer does not state it or contradicts it.
            Always return results in valid JSON format.
            """;
    }

    private static string BuildPrompt(string answer, string factsSection)
    {
        return $$"""
            Judge which facts the following answer contains.

            {{factsSection}}

            Answer:
            {{answer}}

            Return a JSON object with this structure:
            {
                "facts": [
                    {"fact": "one atomic fact", "verdict": "supported" | "partial" | "missing"}
                ]
            }
            """;
    }

    private static MetricResult ParseResponse(string response, EvaluationOptions options)
    {
        try
        {
            var json = JsonHelpers.ExtractJsonFromText(response);
            if (json is null)
                return MetricResult.Failed(MetricName, "Failed to extract JSON from response");

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("facts", out var facts) || facts.ValueKind != JsonValueKind.Array)
                return MetricResult.Failed(MetricName, "The response has no facts array");

            var judged = new List<(string? Fact, string Verdict)>();
            foreach (var item in facts.EnumerateArray())
            {
                var verdict = item.TryGetProperty("verdict", out var v) && v.ValueKind == JsonValueKind.String
                    ? v.GetString()!.Trim().ToLowerInvariant()
                    : "missing";
                judged.Add((item.TryGetProperty("fact", out var f) ? f.GetString() : null,
                    verdict is "supported" or "partial" ? verdict : "missing"));
            }

            if (judged.Count == 0)
                return MetricResult.Failed(MetricName, "The response judged no facts");

            var supported = judged.Count(j => j.Verdict == "supported");
            var partial = judged.Count(j => j.Verdict == "partial");
            var details = new Dictionary<string, object?>();
            if (options.IncludeDetails)
            {
                details["supportedFacts"] = supported;
                details["partialFacts"] = partial;
                details["totalFacts"] = judged.Count;
                details["facts"] = judged.Select(j => new Dictionary<string, object?> { ["fact"] = j.Fact, ["verdict"] = j.Verdict }).ToList();
            }

            return new MetricResult
            {
                MetricName = MetricName,
                Score = (supported + 0.5 * partial) / judged.Count,
                PassThreshold = options.PassThreshold,
                Details = details
            };
        }
        catch (JsonException)
        {
            return MetricResult.Failed(MetricName, "Failed to parse evaluation response");
        }
    }
}

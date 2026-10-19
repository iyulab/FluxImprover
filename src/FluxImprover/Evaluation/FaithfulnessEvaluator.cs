namespace FluxImprover.Evaluation;

using System.Text.Json;
using FluxImprover.Options;
using FluxImprover.Services;
using FluxImprover.Utilities;

/// <summary>
/// LLM 기반 충실도(Faithfulness) 평가기.
/// 답변이 컨텍스트에 기반하는지 평가합니다. 모델은 답변을 주장으로 나누고 각 주장이 컨텍스트로 뒷받침되는지 판정하며,
/// 점수는 그 판정에서 계산한다 — 지지된 주장 수 / 전체 주장 수. 주장이 하나도 없을 때만 모델이 쓴 점수를 쓴다
/// (<see cref="MetricResult.Details"/> 의 <c>scoreSource</c> 가 <c>claims</c> / <c>model</c> 로 출처를 말한다).
/// </summary>
public sealed class FaithfulnessEvaluator
{
    private const string MetricName = "Faithfulness";
    private readonly ITextGenerationService _completionService;

    public FaithfulnessEvaluator(ITextGenerationService completionService)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
    }

    /// <summary>
    /// 컨텍스트에 대한 답변의 충실도를 평가합니다.
    /// </summary>
    /// <param name="context">참조 컨텍스트</param>
    /// <param name="answer">평가할 답변</param>
    /// <param name="options">평가 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>충실도 평가 결과</returns>
    public async Task<MetricResult> EvaluateAsync(
        string context,
        string answer,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context) || string.IsNullOrWhiteSpace(answer))
            return MetricResult.Failed(MetricName, "Empty context or answer");

        options ??= new EvaluationOptions();

        var prompt = BuildPrompt(context, answer);
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

    /// <summary>
    /// 여러 쌍의 컨텍스트-답변을 일괄 평가합니다. <see cref="EvaluationOptions.EnableParallelProcessing"/> 가 켜져 있으면
    /// <see cref="EvaluationOptions.MaxDegreeOfParallelism"/> 개까지 동시에 평가하며, 결과 순서는 입력 순서를 따른다.
    /// </summary>
    public async Task<IReadOnlyList<MetricResult>> EvaluateBatchAsync(
        IEnumerable<(string Context, string Answer)> pairs,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new EvaluationOptions();

        return await BoundedBatch.RunAsync(
            pairs,
            BoundedBatch.Parallelism(options.EnableParallelProcessing, options.MaxDegreeOfParallelism),
            (pair, ct) => EvaluateAsync(pair.Context, pair.Answer, options, ct),
            cancellationToken).ConfigureAwait(false);
    }

    private static string GetSystemPrompt()
    {
        return """
            You are an expert at evaluating the faithfulness of AI-generated answers.
            Faithfulness measures whether the answer is grounded in the provided context.
            A faithful answer only contains information that can be verified from the context.
            Always return results in valid JSON format.
            """;
    }

    private static string BuildPrompt(string context, string answer)
    {
        return $$"""
            Evaluate the faithfulness of the following answer based on the provided context.
            Extract claims from the answer and verify each against the context.

            Context:
            {{context}}

            Answer:
            {{answer}}

            Return a JSON object with this structure:
            {
                "score": 0.0-1.0,
                "reasoning": "explanation of the score",
                "claims": [
                    {"claim": "extracted claim", "supported": true/false}
                ]
            }

            Score guidelines:
            - 1.0: All claims are fully supported by the context
            - 0.5-0.9: Most claims are supported, some minor unsupported details
            - 0.1-0.4: Some claims are supported, significant unsupported content
            - 0.0: No claims are supported or answer contradicts context
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
            var root = doc.RootElement;

            var modelScore = root.TryGetProperty("score", out var s) ? s.GetDouble() : 0.0;
            var details = new Dictionary<string, object?>();

            // The score comes from the claim verdicts when there are any: the model's own number can disagree with its
            // verdicts (one of four supported, «0.9»), and two runs with the same verdicts must score the same.
            var verdicts = new List<bool>();
            if (root.TryGetProperty("claims", out var claimArray) && claimArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var claim in claimArray.EnumerateArray())
                    verdicts.Add(claim.TryGetProperty("supported", out var sup) && sup.ValueKind == JsonValueKind.True);
            }

            var score = verdicts.Count > 0 ? (double)verdicts.Count(v => v) / verdicts.Count : modelScore;
            if (options.IncludeDetails)
            {
                details["scoreSource"] = verdicts.Count > 0 ? "claims" : "model";
                details["supportedClaims"] = verdicts.Count(v => v);
                details["totalClaims"] = verdicts.Count;
            }

            if (options.IncludeDetails)
            {
                details["reasoning"] = root.TryGetProperty("reasoning", out var r) ? r.GetString() : null;
            }

            if (options.IncludeDetails && root.TryGetProperty("claims", out var claims) && claims.ValueKind == JsonValueKind.Array)
            {
                var claimsList = new List<Dictionary<string, object?>>();
                foreach (var claim in claims.EnumerateArray())
                {
                    claimsList.Add(new Dictionary<string, object?>
                    {
                        ["claim"] = claim.TryGetProperty("claim", out var c) ? c.GetString() : null,
                        ["supported"] = claim.TryGetProperty("supported", out var sup) && sup.ValueKind == JsonValueKind.True
                    });
                }
                details["claims"] = claimsList;
            }

            return new MetricResult
            {
                MetricName = MetricName,
                Score = Math.Clamp(score, 0.0, 1.0),
                PassThreshold = options.PassThreshold,
                Details = details
            };
        }
        catch
        {
            return MetricResult.Failed(MetricName, "Failed to parse evaluation response");
        }
    }
}

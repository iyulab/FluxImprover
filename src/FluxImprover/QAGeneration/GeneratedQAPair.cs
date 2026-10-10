namespace FluxImprover.QAGeneration;

using FluxImprover.Models;

/// <summary>
/// 생성된 QA 쌍 (평가 결과 포함)
/// </summary>
public sealed record GeneratedQAPair
{
    /// <summary>
    /// QA 쌍 고유 식별자
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 질문 텍스트
    /// </summary>
    public required string Question { get; init; }

    /// <summary>
    /// 답변 텍스트
    /// </summary>
    public required string Answer { get; init; }

    /// <summary>
    /// 원본 컨텍스트
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// 소스 식별자
    /// </summary>
    public string? SourceId { get; init; }

    /// <summary>
    /// 평가 결과
    /// </summary>
    public QAPairEvaluation? Evaluation { get; init; }

    /// <summary>
    /// 표준 QAPair로 변환
    /// </summary>
    public QAPair ToQAPair()
    {
        var contexts = string.IsNullOrEmpty(Context)
            ? []
            : new List<ContextReference>
              {
                  new(SourceId ?? Id, Context, IsGold: true)
              };

        return new QAPair
        {
            Id = Id,
            Question = Question,
            Answer = Answer,
            Contexts = contexts,
            FaithfulnessScore = Evaluation?.Faithfulness
        };
    }
}

/// <summary>
/// QA 쌍 평가 결과
/// </summary>
public sealed record QAPairEvaluation
{
    /// <summary>
    /// 충실도 점수 (0.0 ~ 1.0)
    /// </summary>
    public double? Faithfulness { get; init; }

    /// <summary>
    /// 관련성 점수 (0.0 ~ 1.0)
    /// </summary>
    public double? Relevancy { get; init; }

    /// <summary>
    /// 답변 가능성 점수 (0.0 ~ 1.0)
    /// </summary>
    public double? Answerability { get; init; }

    /// <summary>
    /// 종합 점수 — 평가된(null 이 아닌) 메트릭의 평균. 평가된 메트릭이 없으면 null.
    /// </summary>
    public double? OverallScore
    {
        get
        {
            double[] scores = [.. new[] { Faithfulness, Relevancy, Answerability }.OfType<double>()];
            return scores.Length == 0 ? null : scores.Average();
        }
    }

    /// <summary>
    /// 평가된 모든 기준 통과 여부. 평가되지 않은(null) 메트릭은 기준에서 빠진다 —
    /// <see cref="Options.EvaluationOptions.EnableFaithfulness"/> 등으로 끈 메트릭이 쌍을 떨어뜨리지 않는다.
    /// 아무 메트릭도 평가되지 않았으면 통과하지 않는다.
    /// </summary>
    public bool PassesThresholds(double minFaithfulness = 0.5, double minRelevancy = 0.5, double minAnswerability = 0.5)
    {
        if (Faithfulness is null && Relevancy is null && Answerability is null)
            return false;

        return (Faithfulness is not { } f || f >= minFaithfulness) &&
               (Relevancy is not { } r || r >= minRelevancy) &&
               (Answerability is not { } a || a >= minAnswerability);
    }
}

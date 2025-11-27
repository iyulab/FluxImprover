namespace FluxImprover.QAGeneration;

using FluxImprover.Abstractions.Models;

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
    /// 종합 점수 (평균)
    /// </summary>
    public double? OverallScore => (Faithfulness + Relevancy + Answerability) / 3.0;

    /// <summary>
    /// 모든 기준 통과 여부
    /// </summary>
    public bool PassesThresholds(double minFaithfulness = 0.5, double minRelevancy = 0.5, double minAnswerability = 0.5)
    {
        return (Faithfulness ?? 0) >= minFaithfulness &&
               (Relevancy ?? 0) >= minRelevancy &&
               (Answerability ?? 0) >= minAnswerability;
    }
}

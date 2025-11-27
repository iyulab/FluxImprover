namespace FluxImprover.Abstractions.Models;

using System.Text.Json.Serialization;

/// <summary>
/// 품질 평가 결과
/// </summary>
public sealed record EvaluationResult
{
    /// <summary>
    /// 충실도 점수 (0.0 ~ 1.0).
    /// 답변이 컨텍스트에 기반한 정도. 환각 탐지에 사용.
    /// </summary>
    public double? Faithfulness { get; init; }

    /// <summary>
    /// 관련성 점수 (0.0 ~ 1.0).
    /// 답변이 질문과 관련된 정도.
    /// </summary>
    public double? Relevancy { get; init; }

    /// <summary>
    /// 답변 가능성 등급 (A: 완전 답변 가능 ~ D: 무관)
    /// </summary>
    public AnswerabilityGrade? Answerability { get; init; }

    /// <summary>
    /// 종합 점수 (0.0 ~ 1.0)
    /// </summary>
    public double? OverallScore { get; init; }

    /// <summary>
    /// 평가 상세 정보
    /// </summary>
    public EvaluationDetails? Details { get; init; }

    /// <summary>
    /// 품질 기준 통과 여부.
    /// Answerability가 A 또는 B인 경우 통과.
    /// </summary>
    [JsonIgnore]
    public bool IsPassed => Answerability is AnswerabilityGrade.A or AnswerabilityGrade.B;
}

/// <summary>
/// 평가 상세 정보
/// </summary>
public sealed record EvaluationDetails
{
    /// <summary>
    /// 충실도 검증에 사용된 claim 목록
    /// </summary>
    public IReadOnlyList<ClaimVerification>? FaithfulnessClaims { get; init; }

    /// <summary>
    /// 관련성 평가 근거
    /// </summary>
    public string? RelevancyReasoning { get; init; }

    /// <summary>
    /// 답변 가능성 평가 근거
    /// </summary>
    public string? AnswerabilityReasoning { get; init; }
}

/// <summary>
/// Claim 검증 결과
/// </summary>
/// <param name="Claim">검증할 주장</param>
/// <param name="IsSupported">컨텍스트에서 지원되는지 여부</param>
/// <param name="Reasoning">검증 근거</param>
public sealed record ClaimVerification(
    string Claim,
    bool IsSupported,
    string? Reasoning = null);

/// <summary>
/// 평가 입력
/// </summary>
public sealed record EvaluationInput
{
    /// <summary>
    /// 질문
    /// </summary>
    public required string Question { get; init; }

    /// <summary>
    /// RAG 시스템이 생성한 답변
    /// </summary>
    public required string Answer { get; init; }

    /// <summary>
    /// 검색된 컨텍스트 목록
    /// </summary>
    public IReadOnlyList<ContextReference> Contexts { get; init; } = [];

    /// <summary>
    /// 정답 (Ground Truth). 선택적.
    /// </summary>
    public string? GroundTruth { get; init; }
}

/// <summary>
/// 답변 가능성 등급 (NVIDIA A-D 체계)
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnswerabilityGrade
{
    /// <summary>A: 컨텍스트가 질문에 완전히 답변</summary>
    A,

    /// <summary>B: 핵심 정보 일부 누락되었으나 부분 답변 가능</summary>
    B,

    /// <summary>C: 질문은 관련있으나 컨텍스트로 답변 불가</summary>
    C,

    /// <summary>D: 질문이 컨텍스트와 무관</summary>
    D
}

namespace FluxImprover.Abstractions.Models;

using System.Text.Json.Serialization;

/// <summary>
/// 추천 질문
/// </summary>
public sealed record SuggestedQuestion
{
    /// <summary>
    /// 질문 텍스트
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// 질문 카테고리
    /// </summary>
    public QuestionCategory Category { get; init; } = QuestionCategory.FollowUp;

    /// <summary>
    /// 관련성 점수 (0.0 ~ 1.0)
    /// </summary>
    public double Relevance { get; init; } = 1.0;

    /// <summary>
    /// 질문 생성 근거 (선택적)
    /// </summary>
    public string? Reasoning { get; init; }
}

/// <summary>
/// 추천 질문 카테고리
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QuestionCategory
{
    /// <summary>후속 질문 - 이전 답변에서 자연스럽게 이어지는 질문</summary>
    FollowUp,

    /// <summary>명확화 질문 - 답변 내용을 더 명확하게 하기 위한 질문</summary>
    Clarification,

    /// <summary>심층 분석 질문 - 주제를 더 깊이 탐구하는 질문</summary>
    DeepDive,

    /// <summary>관련 주제 질문 - 관련된 다른 주제로 확장하는 질문</summary>
    Related,

    /// <summary>대안 질문 - 다른 관점에서 접근하는 질문</summary>
    Alternative
}

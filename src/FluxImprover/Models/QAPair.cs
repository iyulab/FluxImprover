namespace FluxImprover.Models;

using System.Text.Json.Serialization;

/// <summary>
/// 질문-답변 쌍
/// </summary>
public sealed record QAPair
{
    /// <summary>
    /// QA 쌍 고유 식별자
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// 질문 텍스트
    /// </summary>
    public required string Question { get; init; }

    /// <summary>
    /// 답변 텍스트 (Ground Truth)
    /// </summary>
    public required string Answer { get; init; }

    /// <summary>
    /// 답변 도출에 사용된 컨텍스트 목록 (골드 컨텍스트 포함)
    /// </summary>
    public IReadOnlyList<ContextReference> Contexts { get; init; } = [];

    /// <summary>
    /// 답변을 뒷받침하는 구체적인 문장 참조 (HotpotQA 스타일)
    /// </summary>
    public IReadOnlyList<SupportingFact>? SupportingFacts { get; init; }

    /// <summary>
    /// 질문 유형 및 난이도 분류
    /// </summary>
    public QAClassification? Classification { get; init; }

    /// <summary>
    /// 충실도 점수 (0.0 ~ 1.0). 답변이 컨텍스트에 기반한 정도.
    /// </summary>
    public double? FaithfulnessScore { get; init; }

    /// <summary>
    /// 답변의 대체 표현 목록 (평가 시 유연한 매칭용)
    /// </summary>
    public IReadOnlyList<string>? AnswerAliases { get; init; }
}

/// <summary>
/// 컨텍스트 참조
/// </summary>
/// <param name="ChunkId">청크 식별자</param>
/// <param name="Text">컨텍스트 텍스트</param>
/// <param name="IsGold">골드 컨텍스트 여부 (답변 도출에 필수적인 컨텍스트)</param>
/// <param name="SourceDocument">원본 문서 식별자</param>
public sealed record ContextReference(
    string ChunkId,
    string Text,
    bool IsGold = false,
    string? SourceDocument = null);

/// <summary>
/// 뒷받침 사실 (Supporting Fact)
/// </summary>
/// <param name="ChunkId">청크 식별자</param>
/// <param name="SentenceIndex">문장 인덱스 (0-based)</param>
public sealed record SupportingFact(string ChunkId, int SentenceIndex);

/// <summary>
/// QA 분류 정보
/// </summary>
/// <param name="Type">질문 유형</param>
/// <param name="Difficulty">난이도</param>
/// <param name="RequiredContextCount">필요한 컨텍스트 수 (Multi-hop의 경우 2 이상)</param>
public sealed record QAClassification(
    QuestionType Type,
    Difficulty Difficulty,
    int RequiredContextCount = 1);

/// <summary>
/// 질문 유형
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QuestionType
{
    /// <summary>단순 사실 기반 질문</summary>
    Factual,

    /// <summary>추론이 필요한 질문</summary>
    Reasoning,

    /// <summary>비교 질문</summary>
    Comparative,

    /// <summary>여러 컨텍스트를 연결해야 하는 질문</summary>
    MultiHop,

    /// <summary>조건부 질문</summary>
    Conditional
}

/// <summary>
/// 난이도 수준
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Difficulty
{
    /// <summary>쉬움</summary>
    Easy,

    /// <summary>보통</summary>
    Medium,

    /// <summary>어려움</summary>
    Hard
}

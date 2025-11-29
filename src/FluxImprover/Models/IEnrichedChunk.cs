namespace FluxImprover.Models;

/// <summary>
/// 강화된 청크 인터페이스.
/// FileFlux/WebFlux의 청크와 호환되는 공통 인터페이스.
/// </summary>
public interface IEnrichedChunk
{
    /// <summary>
    /// 청크 고유 식별자
    /// </summary>
    string Id { get; }

    /// <summary>
    /// 청크 텍스트 내용
    /// </summary>
    string Text { get; }

    /// <summary>
    /// 원본 문서 식별자
    /// </summary>
    string SourceId { get; }

    /// <summary>
    /// 문서 구조 경로 (예: "Chapter 1 > Section 1.1")
    /// </summary>
    string? HeadingPath { get; }

    /// <summary>
    /// LLM 생성 요약
    /// </summary>
    string? Summary { get; }

    /// <summary>
    /// 추출된 키워드 목록
    /// </summary>
    IReadOnlyList<string>? Keywords { get; }

    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    IReadOnlyDictionary<string, object>? Metadata { get; }
}

/// <summary>
/// 강화된 청크 기본 구현
/// </summary>
public sealed record EnrichedChunk : IEnrichedChunk
{
    /// <inheritdoc />
    public required string Id { get; init; }

    /// <inheritdoc />
    public required string Text { get; init; }

    /// <inheritdoc />
    public required string SourceId { get; init; }

    /// <inheritdoc />
    public string? HeadingPath { get; init; }

    /// <inheritdoc />
    public string? Summary { get; init; }

    /// <inheritdoc />
    public IReadOnlyList<string>? Keywords { get; init; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

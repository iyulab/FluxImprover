namespace FluxImprover.Abstractions.Models;

/// <summary>
/// 원본 문서 청크
/// </summary>
public sealed class Chunk
{
    /// <summary>
    /// 청크 고유 식별자
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 청크 내용
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 메타데이터 (출처, 페이지 번호 등)
    /// </summary>
    public IDictionary<string, object>? Metadata { get; init; }
}

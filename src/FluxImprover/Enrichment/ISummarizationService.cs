namespace FluxImprover.Enrichment;

using FluxImprover.Abstractions.Options;

/// <summary>
/// 텍스트 요약 서비스 인터페이스
/// </summary>
public interface ISummarizationService
{
    /// <summary>
    /// 텍스트를 요약합니다.
    /// </summary>
    /// <param name="text">요약할 텍스트</param>
    /// <param name="options">요약 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>요약된 텍스트</returns>
    Task<string> SummarizeAsync(
        string text,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 텍스트를 일괄 요약합니다.
    /// </summary>
    /// <param name="texts">요약할 텍스트 목록</param>
    /// <param name="options">요약 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>요약된 텍스트 목록</returns>
    Task<IReadOnlyList<string>> SummarizeBatchAsync(
        IEnumerable<string> texts,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);
}

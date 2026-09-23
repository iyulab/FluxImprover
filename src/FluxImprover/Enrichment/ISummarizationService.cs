namespace FluxImprover.Enrichment;

using FluxImprover.Options;

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
    /// <exception cref="Flux.Abstractions.TextCompletionTruncatedException">
    /// 요약이 <see cref="Options.EnrichmentOptions.MaxTokens"/> 에서 잘렸을 때(완료 사유를 관찰할 수 있는 생성 서비스에서) —
    /// 잘린 요약을 돌려주는 대신 던진다.
    /// </exception>
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

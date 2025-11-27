namespace FluxImprover.Enrichment;

using FluxImprover.Abstractions.Options;

/// <summary>
/// 키워드 추출 서비스 인터페이스
/// </summary>
public interface IKeywordExtractionService
{
    /// <summary>
    /// 텍스트에서 키워드를 추출합니다.
    /// </summary>
    /// <param name="text">키워드를 추출할 텍스트</param>
    /// <param name="options">추출 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 키워드 목록</returns>
    Task<IReadOnlyList<string>> ExtractKeywordsAsync(
        string text,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 텍스트에서 키워드와 관련도 점수를 추출합니다.
    /// </summary>
    /// <param name="text">키워드를 추출할 텍스트</param>
    /// <param name="options">추출 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>키워드와 관련도 점수 딕셔너리</returns>
    Task<IReadOnlyDictionary<string, double>> ExtractKeywordsWithScoresAsync(
        string text,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 텍스트에서 키워드를 일괄 추출합니다.
    /// </summary>
    /// <param name="texts">텍스트 목록</param>
    /// <param name="options">추출 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>각 텍스트의 키워드 목록</returns>
    Task<IReadOnlyList<IReadOnlyList<string>>> ExtractKeywordsBatchAsync(
        IEnumerable<string> texts,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);
}

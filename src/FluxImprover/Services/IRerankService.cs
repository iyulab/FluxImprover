namespace FluxImprover.Services;

/// <summary>
/// 문서 재순위 서비스 인터페이스.
/// 소비 애플리케이션에서 선택적으로 구현합니다.
/// </summary>
public interface IRerankService
{
    /// <summary>
    /// 쿼리에 대해 문서들을 관련성 점수로 재순위화합니다.
    /// </summary>
    /// <param name="query">검색 쿼리</param>
    /// <param name="documents">재순위할 문서들</param>
    /// <param name="topK">반환할 상위 결과 수</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>관련성 점수로 정렬된 결과</returns>
    Task<IReadOnlyList<RerankResult>> RerankAsync(
        string query,
        IEnumerable<string> documents,
        int topK = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 재순위 결과
/// </summary>
/// <param name="Index">원본 문서 인덱스</param>
/// <param name="Score">관련성 점수 (0.0 ~ 1.0)</param>
/// <param name="Document">문서 내용</param>
public sealed record RerankResult(int Index, float Score, string Document);

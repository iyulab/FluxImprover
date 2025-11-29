namespace FluxImprover.Services;

/// <summary>
/// 텍스트 임베딩 생성 서비스 인터페이스.
/// 소비 애플리케이션에서 선택적으로 구현합니다.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// 단일 텍스트의 임베딩 벡터를 생성합니다.
    /// </summary>
    /// <param name="text">입력 텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>임베딩 벡터</returns>
    Task<ReadOnlyMemory<float>> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 텍스트의 임베딩 벡터를 일괄 생성합니다.
    /// </summary>
    /// <param name="texts">입력 텍스트들</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>임베딩 벡터 목록</returns>
    Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default);
}

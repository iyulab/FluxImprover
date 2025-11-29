namespace FluxImprover.Services;

/// <summary>
/// 토크나이저 인터페이스.
/// 소비 애플리케이션에서 선택적으로 구현합니다.
/// </summary>
public interface ITokenizer
{
    /// <summary>
    /// 텍스트의 토큰 수를 계산합니다.
    /// </summary>
    /// <param name="text">입력 텍스트</param>
    /// <returns>토큰 수</returns>
    int CountTokens(string text);

    /// <summary>
    /// 텍스트를 토큰 ID 시퀀스로 인코딩합니다.
    /// </summary>
    /// <param name="text">입력 텍스트</param>
    /// <returns>토큰 ID 목록</returns>
    IReadOnlyList<int> Encode(string text);

    /// <summary>
    /// 토큰 ID 시퀀스를 텍스트로 디코딩합니다.
    /// </summary>
    /// <param name="tokens">토큰 ID 목록</param>
    /// <returns>디코딩된 텍스트</returns>
    string Decode(IReadOnlyList<int> tokens);
}

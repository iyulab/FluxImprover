namespace FluxImprover.Prompts;

/// <summary>
/// 빌드된 프롬프트 결과
/// </summary>
public sealed class Prompt
{
    /// <summary>
    /// 시스템 프롬프트
    /// </summary>
    public string? System { get; init; }

    /// <summary>
    /// 사용자 프롬프트
    /// </summary>
    public string? User { get; init; }

    /// <summary>
    /// 컨텍스트 (RAG 검색 결과 등)
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// Few-shot 예시 목록
    /// </summary>
    public IReadOnlyList<string> Examples { get; init; } = [];

    /// <summary>
    /// JSON 모드 활성화 여부
    /// </summary>
    public bool JsonMode { get; init; }

    /// <summary>
    /// 최대 토큰 수
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Temperature 설정
    /// </summary>
    public float? Temperature { get; init; }
}

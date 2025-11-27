namespace FluxImprover.Abstractions.Services;

/// <summary>
/// LLM 텍스트 생성 서비스 인터페이스.
/// 소비 애플리케이션에서 구현해야 합니다.
/// </summary>
public interface ITextCompletionService
{
    /// <summary>
    /// 주어진 프롬프트에 대한 텍스트 완성을 생성합니다.
    /// </summary>
    /// <param name="prompt">입력 프롬프트</param>
    /// <param name="options">완성 옵션 (선택)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>생성된 텍스트</returns>
    Task<string> CompleteAsync(
        string prompt,
        CompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 주어진 프롬프트에 대한 텍스트 완성을 스트리밍으로 생성합니다.
    /// </summary>
    /// <param name="prompt">입력 프롬프트</param>
    /// <param name="options">완성 옵션 (선택)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>생성된 토큰들의 비동기 스트림</returns>
    IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        CompletionOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 텍스트 완성 옵션
/// </summary>
public sealed record CompletionOptions
{
    /// <summary>
    /// 생성 온도 (0.0 ~ 2.0). 높을수록 창의적, 낮을수록 결정적.
    /// null이면 모델 기본값 사용 (일부 모델은 기본값 외 temperature를 지원하지 않음).
    /// </summary>
    public float? Temperature { get; init; }

    /// <summary>
    /// 최대 생성 토큰 수. null이면 모델 기본값 사용.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// 시스템 프롬프트 (역할 정의)
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// 이전 대화 메시지들 (컨텍스트 유지용)
    /// </summary>
    public IReadOnlyList<ChatMessage>? Messages { get; init; }

    /// <summary>
    /// JSON 모드 활성화 여부
    /// </summary>
    public bool JsonMode { get; init; }

    /// <summary>
    /// 응답 형식 스키마 (JSON 모드에서 사용)
    /// </summary>
    public string? ResponseSchema { get; init; }
}

/// <summary>
/// 채팅 메시지
/// </summary>
/// <param name="Role">메시지 역할 (system, user, assistant)</param>
/// <param name="Content">메시지 내용</param>
public sealed record ChatMessage(string Role, string Content);

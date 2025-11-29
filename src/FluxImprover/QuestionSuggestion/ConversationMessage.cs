namespace FluxImprover.QuestionSuggestion;

/// <summary>
/// 대화 메시지
/// </summary>
public sealed record ConversationMessage
{
    /// <summary>
    /// 역할 (user, assistant, system)
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// 메시지 내용
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// 타임스탬프 (선택적)
    /// </summary>
    public DateTimeOffset? Timestamp { get; init; }
}

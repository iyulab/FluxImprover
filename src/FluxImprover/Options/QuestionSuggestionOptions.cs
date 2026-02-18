namespace FluxImprover.Options;

using FluxImprover.Models;

/// <summary>
/// 질문 추천 옵션
/// </summary>
public sealed class QuestionSuggestionOptions
{
    private int _maxSuggestions = 5;
    private float? _temperature;
    private int _maxTokens = 1024;
    private float _minRelevanceScore = 0.5f;
    private int _contextWindowSize = 5;

    /// <summary>
    /// 최대 추천 질문 수 (기본값: 5)
    /// </summary>
    public int MaxSuggestions
    {
        get => _maxSuggestions;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxSuggestions = value;
        }
    }

    /// <summary>
    /// LLM 온도 (0.0 ~ 2.0).
    /// null이면 모델 기본값 사용 (일부 모델은 기본값 외 temperature를 지원하지 않음).
    /// </summary>
    public float? Temperature
    {
        get => _temperature;
        init
        {
            if (value.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value.Value, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Value, 2.0f);
            }
            _temperature = value;
        }
    }

    /// <summary>
    /// 최대 토큰 수 (기본값: 1024)
    /// </summary>
    public int MaxTokens
    {
        get => _maxTokens;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxTokens = value;
        }
    }

    /// <summary>
    /// 추천 근거 포함 여부 (기본값: false)
    /// </summary>
    public bool IncludeReasoning { get; init; }

    /// <summary>
    /// 최소 관련성 점수 (0.0 ~ 1.0, 기본값: 0.5)
    /// </summary>
    public float MinRelevanceScore
    {
        get => _minRelevanceScore;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0f);
            _minRelevanceScore = value;
        }
    }

    /// <summary>
    /// 포함할 질문 카테고리 목록
    /// </summary>
    public IReadOnlyList<QuestionCategory> Categories { get; init; } =
    [
        QuestionCategory.FollowUp,
        QuestionCategory.Clarification,
        QuestionCategory.DeepDive,
        QuestionCategory.Related
    ];

    /// <summary>
    /// 컨텍스트 윈도우 크기 (최근 N개 메시지 참조, 기본값: 5)
    /// </summary>
    public int ContextWindowSize
    {
        get => _contextWindowSize;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _contextWindowSize = value;
        }
    }

    /// <summary>
    /// 문서 컨텍스트 사용 여부 (기본값: true)
    /// </summary>
    public bool UseDocumentContext { get; init; } = true;

    /// <summary>
    /// 대화 기록 사용 여부 (기본값: true)
    /// </summary>
    public bool UseConversationHistory { get; init; } = true;
}

namespace FluxImprover.Options;

using FluxImprover.Models;

/// <summary>
/// QA 생성 옵션
/// </summary>
public sealed class QAGenerationOptions
{
    private int _pairsPerChunk = 3;
    private float? _temperature;
    private int _maxTokens = 2048;
    private int _minAnswerLength = 10;
    private int _maxAnswerLength = 500;

    /// <summary>
    /// 청크당 생성할 QA 쌍 수 (기본값: 3)
    /// </summary>
    public int PairsPerChunk
    {
        get => _pairsPerChunk;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _pairsPerChunk = value;
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
    /// 최대 토큰 수 (기본값: 2048)
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
    /// Multi-hop 질문 포함 여부 (기본값: false)
    /// </summary>
    public bool IncludeMultiHop { get; init; } = false;

    /// <summary>
    /// 추론 질문 포함 여부 (기본값: true)
    /// </summary>
    public bool IncludeReasoning { get; init; } = true;

    /// <summary>
    /// 최소 답변 길이 (기본값: 10)
    /// </summary>
    public int MinAnswerLength
    {
        get => _minAnswerLength;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _minAnswerLength = value;
            ValidateAnswerLengthRange();
        }
    }

    /// <summary>
    /// 최대 답변 길이 (기본값: 500)
    /// </summary>
    public int MaxAnswerLength
    {
        get => _maxAnswerLength;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxAnswerLength = value;
            ValidateAnswerLengthRange();
        }
    }

    /// <summary>
    /// 난이도 분포 설정
    /// </summary>
    public DifficultyDistribution DifficultyDistribution { get; init; } = new();

    /// <summary>
    /// 생성할 질문 유형 목록
    /// </summary>
    public IReadOnlyList<QuestionType> QuestionTypes { get; init; } =
        [QuestionType.Factual, QuestionType.Reasoning, QuestionType.Comparative];

    private void ValidateAnswerLengthRange()
    {
        if (_minAnswerLength > _maxAnswerLength)
        {
            throw new ArgumentException(
                $"MinAnswerLength ({_minAnswerLength}) cannot be greater than MaxAnswerLength ({_maxAnswerLength})");
        }
    }
}

/// <summary>
/// 난이도 분포 설정
/// </summary>
public sealed class DifficultyDistribution
{
    private float _easy = 0.3f;
    private float _medium = 0.5f;
    private float _hard = 0.2f;

    /// <summary>
    /// 쉬운 난이도 비율 (기본값: 0.3)
    /// </summary>
    public float Easy
    {
        get => _easy;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0f);
            _easy = value;
        }
    }

    /// <summary>
    /// 보통 난이도 비율 (기본값: 0.5)
    /// </summary>
    public float Medium
    {
        get => _medium;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0f);
            _medium = value;
        }
    }

    /// <summary>
    /// 어려운 난이도 비율 (기본값: 0.2)
    /// </summary>
    public float Hard
    {
        get => _hard;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0f);
            _hard = value;
        }
    }
}

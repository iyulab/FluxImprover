namespace FluxImprover.Abstractions.Options;

/// <summary>
/// 품질 평가 옵션
/// </summary>
public sealed class EvaluationOptions
{
    private float _temperature = 0.0f;
    private int _maxTokens = 1024;
    private float _passThreshold = 0.7f;
    private int _maxRetries = 3;

    /// <summary>
    /// 충실도(Faithfulness) 평가 활성화 여부 (기본값: true)
    /// </summary>
    public bool EnableFaithfulness { get; init; } = true;

    /// <summary>
    /// 관련성(Relevancy) 평가 활성화 여부 (기본값: true)
    /// </summary>
    public bool EnableRelevancy { get; init; } = true;

    /// <summary>
    /// 답변 가능성(Answerability) 평가 활성화 여부 (기본값: true)
    /// </summary>
    public bool EnableAnswerability { get; init; } = true;

    /// <summary>
    /// LLM 온도 (0.0 ~ 2.0, 기본값: 0.0 - 평가는 결정적이어야 함)
    /// </summary>
    public float Temperature
    {
        get => _temperature;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 2.0f);
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
    /// 품질 통과 임계값 (0.0 ~ 1.0, 기본값: 0.7)
    /// </summary>
    public float PassThreshold
    {
        get => _passThreshold;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0f);
            _passThreshold = value;
        }
    }

    /// <summary>
    /// 상세 평가 정보 포함 여부 (기본값: true)
    /// </summary>
    public bool IncludeDetails { get; init; } = true;

    /// <summary>
    /// 최대 재시도 횟수 (기본값: 3)
    /// </summary>
    public int MaxRetries
    {
        get => _maxRetries;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
            _maxRetries = value;
        }
    }

    /// <summary>
    /// 병렬 처리 활성화 여부 (기본값: true)
    /// </summary>
    public bool EnableParallelProcessing { get; init; } = true;

    /// <summary>
    /// 병렬 처리 시 최대 동시 작업 수 (기본값: 4)
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 4;
}

namespace FluxImprover.Abstractions.Options;

/// <summary>
/// 문서 강화 옵션
/// </summary>
public sealed class EnrichmentOptions
{
    private float _temperature = 0.3f;
    private int _maxTokens = 512;
    private int _maxKeywords = 10;
    private int _maxSummaryLength = 200;

    /// <summary>
    /// 요약 생성 활성화 여부 (기본값: true)
    /// </summary>
    public bool EnableSummarization { get; init; } = true;

    /// <summary>
    /// 키워드 추출 활성화 여부 (기본값: true)
    /// </summary>
    public bool EnableKeywordExtraction { get; init; } = true;

    /// <summary>
    /// 개체 추출 활성화 여부 (기본값: false)
    /// </summary>
    public bool EnableEntityExtraction { get; init; } = false;

    /// <summary>
    /// LLM 온도 (0.0 ~ 2.0, 기본값: 0.3)
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
    /// 최대 토큰 수 (기본값: 512)
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
    /// 최대 키워드 수 (기본값: 10)
    /// </summary>
    public int MaxKeywords
    {
        get => _maxKeywords;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxKeywords = value;
        }
    }

    /// <summary>
    /// 최대 요약 길이 (문자 수, 기본값: 200)
    /// </summary>
    public int MaxSummaryLength
    {
        get => _maxSummaryLength;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxSummaryLength = value;
        }
    }

    /// <summary>
    /// 추출할 개체 유형 목록
    /// </summary>
    public IReadOnlyList<string> EntityTypes { get; init; } =
        ["Person", "Organization", "Location", "Date", "Product"];

    /// <summary>
    /// 병렬 처리 활성화 여부 (기본값: true)
    /// </summary>
    public bool EnableParallelProcessing { get; init; } = true;

    /// <summary>
    /// 병렬 처리 시 최대 동시 작업 수 (기본값: 4)
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 4;
}

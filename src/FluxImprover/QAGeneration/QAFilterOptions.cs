namespace FluxImprover.QAGeneration;

/// <summary>
/// QA 필터링 옵션
/// </summary>
public sealed class QAFilterOptions
{
    private double _minFaithfulness = 0.5;
    private double _minRelevancy = 0.5;
    private double _minAnswerability = 0.5;

    /// <summary>
    /// 최소 충실도 점수 (기본값: 0.5)
    /// </summary>
    public double MinFaithfulness
    {
        get => _minFaithfulness;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0);
            _minFaithfulness = value;
        }
    }

    /// <summary>
    /// 최소 관련성 점수 (기본값: 0.5)
    /// </summary>
    public double MinRelevancy
    {
        get => _minRelevancy;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0);
            _minRelevancy = value;
        }
    }

    /// <summary>
    /// 최소 답변 가능성 점수 (기본값: 0.5)
    /// </summary>
    public double MinAnswerability
    {
        get => _minAnswerability;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0);
            _minAnswerability = value;
        }
    }
}

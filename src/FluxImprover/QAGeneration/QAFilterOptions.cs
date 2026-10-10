namespace FluxImprover.QAGeneration;

using FluxImprover.Options;

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
    /// 필터가 각 QA 쌍을 평가할 때 쓰는 평가 옵션 (기본값: null = <see cref="EvaluationOptions"/> 기본값).
    /// 어떤 메트릭을 요청할지(<see cref="EvaluationOptions.EnableFaithfulness"/> 등), 모델 호출 설정, 쌍 단위 병렬도를 정한다.
    /// 꺼진 메트릭은 요청되지 않고 그 최소 점수 기준도 적용되지 않는다.
    /// </summary>
    public EvaluationOptions? Evaluation { get; init; }

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

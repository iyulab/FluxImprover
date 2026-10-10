namespace FluxImprover.Evaluation;

/// <summary>
/// 개별 메트릭 평가 결과
/// </summary>
public sealed record MetricResult
{
    /// <summary>
    /// 메트릭 이름
    /// </summary>
    public required string MetricName { get; init; }

    /// <summary>
    /// 점수 (0.0 ~ 1.0)
    /// </summary>
    public required double Score { get; init; }

    /// <summary>
    /// 평가 상세 정보
    /// </summary>
    public IReadOnlyDictionary<string, object?> Details { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// <see cref="IsPassed"/> 의 기준 점수 (기본값: 0.5). 평가기는 <see cref="Options.EvaluationOptions.PassThreshold"/> 를 싣는다.
    /// </summary>
    public double PassThreshold { get; init; } = 0.5;

    /// <summary>
    /// 품질 기준 통과 여부 (<see cref="Score"/> 가 <see cref="PassThreshold"/> 이상)
    /// </summary>
    public bool IsPassed => Score >= PassThreshold;

    /// <summary>
    /// 실패한 기본 결과 생성 (점수 0, 기본 <see cref="PassThreshold"/> — 통과하지 않는다)
    /// </summary>
    public static MetricResult Failed(string metricName, string? reason = null)
    {
        var details = new Dictionary<string, object?>();
        if (reason is not null)
            details["reason"] = reason;

        return new MetricResult
        {
            MetricName = metricName,
            Score = 0.0,
            Details = details
        };
    }
}

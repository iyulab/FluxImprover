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
    /// 품질 기준 통과 여부 (0.5 이상)
    /// </summary>
    public bool IsPassed => Score >= 0.5;

    /// <summary>
    /// 실패한 기본 결과 생성
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

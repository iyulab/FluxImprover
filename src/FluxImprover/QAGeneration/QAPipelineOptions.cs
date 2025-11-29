namespace FluxImprover.QAGeneration;

using FluxImprover.Options;

/// <summary>
/// QA 파이프라인 옵션
/// </summary>
public sealed record QAPipelineOptions
{
    /// <summary>
    /// QA 생성 옵션
    /// </summary>
    public QAGenerationOptions? GenerationOptions { get; init; }

    /// <summary>
    /// QA 필터링 옵션
    /// </summary>
    public QAFilterOptions? FilterOptions { get; init; }

    /// <summary>
    /// 필터링 건너뛰기 여부 (기본값: false)
    /// </summary>
    public bool SkipFiltering { get; init; } = false;

    /// <summary>
    /// 소스 ID (옵션)
    /// </summary>
    public string? SourceId { get; init; }
}

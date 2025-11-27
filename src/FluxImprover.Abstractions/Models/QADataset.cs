namespace FluxImprover.Abstractions.Models;

/// <summary>
/// QA 데이터셋 (RAGAS EvaluationDataset 호환)
/// </summary>
public sealed record QADataset
{
    /// <summary>
    /// 데이터셋 버전
    /// </summary>
    public string Version { get; init; } = "1.0";

    /// <summary>
    /// QA 샘플 목록
    /// </summary>
    public required IReadOnlyList<QAPair> Samples { get; init; }

    /// <summary>
    /// 데이터셋 메타데이터
    /// </summary>
    public DatasetMetadata? Metadata { get; init; }
}

/// <summary>
/// 데이터셋 메타데이터
/// </summary>
public sealed record DatasetMetadata
{
    /// <summary>
    /// 생성 시각
    /// </summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>
    /// 생성기 정보 (예: "FluxImprover v1.0")
    /// </summary>
    public string? Generator { get; init; }

    /// <summary>
    /// 원본 문서 수
    /// </summary>
    public int? SourceDocuments { get; init; }

    /// <summary>
    /// 총 샘플 수
    /// </summary>
    public int? TotalSamples { get; init; }

    /// <summary>
    /// 생성 설정 (재현성용)
    /// </summary>
    public IReadOnlyDictionary<string, object>? Configuration { get; init; }
}

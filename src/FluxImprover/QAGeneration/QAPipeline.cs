namespace FluxImprover.QAGeneration;

using FluxImprover.Abstractions.Models;

/// <summary>
/// QA 생성 및 필터링 파이프라인
/// </summary>
public class QAPipeline
{
    private readonly QAGeneratorService _generator;
    private readonly QAFilterService _filter;

    public QAPipeline(QAGeneratorService generator, QAFilterService filter)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    /// <summary>
    /// 컨텍스트에서 QA 쌍을 생성하고 필터링합니다.
    /// </summary>
    /// <param name="context">원본 컨텍스트</param>
    /// <param name="options">파이프라인 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>파이프라인 실행 결과</returns>
    public async Task<QAPipelineResult> ExecuteAsync(
        string context,
        QAPipelineOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new QAPipelineOptions();

        // Generate QA pairs
        var generated = await _generator.GenerateAsync(
            context,
            options.GenerationOptions,
            options.SourceId,
            cancellationToken);

        if (generated.Count == 0)
        {
            return QAPipelineResult.Empty;
        }

        // Filter if not skipped
        IReadOnlyList<GeneratedQAPair> filtered;
        if (options.SkipFiltering)
        {
            filtered = generated;
        }
        else
        {
            filtered = await _filter.FilterAsync(
                generated,
                options.FilterOptions,
                cancellationToken);
        }

        return new QAPipelineResult
        {
            QAPairs = filtered,
            GeneratedCount = generated.Count,
            FilteredCount = filtered.Count
        };
    }

    /// <summary>
    /// 청크에서 QA 쌍을 생성하고 필터링합니다.
    /// </summary>
    public async Task<QAPipelineResult> ExecuteFromChunkAsync(
        Chunk chunk,
        QAPipelineOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        options ??= new QAPipelineOptions();
        var pipelineOptions = options with { SourceId = chunk.Id };

        return await ExecuteAsync(chunk.Content, pipelineOptions, cancellationToken);
    }

    /// <summary>
    /// 여러 컨텍스트에서 QA 쌍을 일괄 생성하고 필터링합니다.
    /// </summary>
    public async Task<IReadOnlyList<QAPipelineResult>> ExecuteBatchAsync(
        IEnumerable<string> contexts,
        QAPipelineOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<QAPipelineResult>();

        foreach (var context in contexts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await ExecuteAsync(context, options, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// 여러 청크에서 QA 쌍을 일괄 생성하고 필터링합니다.
    /// </summary>
    public async Task<IReadOnlyList<QAPipelineResult>> ExecuteFromChunksBatchAsync(
        IEnumerable<Chunk> chunks,
        QAPipelineOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<QAPipelineResult>();

        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await ExecuteFromChunkAsync(chunk, options, cancellationToken);
            results.Add(result);
        }

        return results;
    }
}

/// <summary>
/// QA 파이프라인 실행 결과
/// </summary>
public sealed record QAPipelineResult
{
    /// <summary>
    /// 빈 결과
    /// </summary>
    public static QAPipelineResult Empty { get; } = new();

    /// <summary>
    /// 최종 QA 쌍 목록
    /// </summary>
    public IReadOnlyList<GeneratedQAPair> QAPairs { get; init; } = [];

    /// <summary>
    /// 생성된 QA 쌍 수
    /// </summary>
    public int GeneratedCount { get; init; }

    /// <summary>
    /// 필터링 통과한 QA 쌍 수
    /// </summary>
    public int FilteredCount { get; init; }

    /// <summary>
    /// 필터링으로 제외된 QA 쌍 수
    /// </summary>
    public int FilteredOutCount => GeneratedCount - FilteredCount;

    /// <summary>
    /// 필터링 통과율
    /// </summary>
    public double PassRate => GeneratedCount > 0 ? (double)FilteredCount / GeneratedCount : 0.0;
}

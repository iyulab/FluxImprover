namespace FluxImprover.Enrichment;

using FluxImprover.Models;
using FluxImprover.Options;

/// <summary>
/// 청크 강화 서비스 - 요약과 키워드 추출을 통해 청크를 강화합니다.
/// </summary>
public sealed class ChunkEnrichmentService
{
    private readonly ISummarizationService _summarizationService;
    private readonly IKeywordExtractionService _keywordExtractionService;

    public ChunkEnrichmentService(
        ISummarizationService summarizationService,
        IKeywordExtractionService keywordExtractionService)
    {
        _summarizationService = summarizationService ?? throw new ArgumentNullException(nameof(summarizationService));
        _keywordExtractionService = keywordExtractionService ?? throw new ArgumentNullException(nameof(keywordExtractionService));
    }

    /// <summary>
    /// 청크를 강화합니다.
    /// </summary>
    /// <param name="chunk">강화할 청크</param>
    /// <param name="options">강화 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>강화된 청크</returns>
    public async Task<EnrichedChunk> EnrichAsync(
        Chunk chunk,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        options ??= new EnrichmentOptions();

        string? summary = null;
        IReadOnlyList<string>? keywords = null;

        if (!string.IsNullOrWhiteSpace(chunk.Content))
        {
            var summarizeTask = _summarizationService.SummarizeAsync(chunk.Content, options, cancellationToken);
            var keywordsTask = _keywordExtractionService.ExtractKeywordsAsync(chunk.Content, options, cancellationToken);

            await Task.WhenAll(summarizeTask, keywordsTask);

            summary = await summarizeTask;
            keywords = await keywordsTask;
        }

        return new EnrichedChunk
        {
            Id = chunk.Id,
            Text = chunk.Content,
            SourceId = chunk.Id, // Use chunk Id as source for now
            Summary = summary,
            Keywords = keywords,
            Metadata = chunk.Metadata is not null
                ? new Dictionary<string, object>(chunk.Metadata)
                : null
        };
    }

    /// <summary>
    /// 여러 청크를 일괄 강화합니다.
    /// </summary>
    /// <param name="chunks">강화할 청크 목록</param>
    /// <param name="options">강화 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>강화된 청크 목록</returns>
    public async Task<IReadOnlyList<EnrichedChunk>> EnrichBatchAsync(
        IEnumerable<Chunk> chunks,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EnrichedChunk>();

        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var enriched = await EnrichAsync(chunk, options, cancellationToken);
            results.Add(enriched);
        }

        return results;
    }
}

namespace FluxImprover.Enrichment;

using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.Utilities;

/// <summary>
/// Chunk enrichment service that adds summaries and keywords to chunks.
/// Supports conditional enrichment based on pre-assessment quality scores.
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
    /// Enriches a chunk with summary and keywords.
    /// When conditional enrichment is enabled, chunks are pre-assessed and may skip
    /// unnecessary operations based on quality thresholds.
    /// </summary>
    /// <param name="chunk">Chunk to enrich.</param>
    /// <param name="options">Enrichment options including conditional settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Enriched chunk with optional quality metrics.</returns>
    public async Task<EnrichedChunk> EnrichAsync(
        Chunk chunk,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        options ??= new EnrichmentOptions();
        var conditionalOptions = options.ConditionalOptions;

        // Pre-assess quality if conditional enrichment is enabled
        ChunkQualityResult? qualityResult = null;
        if (conditionalOptions?.EnableConditionalEnrichment == true)
        {
            qualityResult = ChunkQualityAnalyzer.Analyze(chunk.Content, chunk.Metadata);

            // Skip enrichment entirely if quality is above threshold
            if (qualityResult.OverallScore >= conditionalOptions.SkipEnrichmentThreshold)
            {
                return CreateEnrichedChunk(chunk, null, null, qualityResult, conditionalOptions, wasSkipped: true);
            }
        }

        string? summary = null;
        IReadOnlyList<string>? keywords = null;

        if (!string.IsNullOrWhiteSpace(chunk.Content))
        {
            // Determine which enrichments to perform
            var shouldSummarize = ShouldPerformSummarization(chunk, options, qualityResult, conditionalOptions);
            var shouldExtractKeywords = ShouldPerformKeywordExtraction(options, qualityResult, conditionalOptions);

            // Execute selected enrichments in parallel
            var tasks = new List<Task>();
            Task<string>? summarizeTask = null;
            Task<IReadOnlyList<string>>? keywordsTask = null;

            if (shouldSummarize)
            {
                summarizeTask = _summarizationService.SummarizeAsync(chunk.Content, options, cancellationToken);
                tasks.Add(summarizeTask);
            }

            if (shouldExtractKeywords)
            {
                keywordsTask = _keywordExtractionService.ExtractKeywordsAsync(chunk.Content, options, cancellationToken);
                tasks.Add(keywordsTask);
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
                summary = summarizeTask is not null ? await summarizeTask : null;
                keywords = keywordsTask is not null ? await keywordsTask : null;
            }
        }

        return CreateEnrichedChunk(chunk, summary, keywords, qualityResult, conditionalOptions, wasSkipped: false);
    }

    /// <summary>
    /// Enriches multiple chunks in batch.
    /// When conditional enrichment is enabled, each chunk is independently assessed.
    /// </summary>
    /// <param name="chunks">Chunks to enrich.</param>
    /// <param name="options">Enrichment options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of enriched chunks.</returns>
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

    /// <summary>
    /// Gets batch enrichment statistics when conditional enrichment is used.
    /// </summary>
    /// <param name="enrichedChunks">Previously enriched chunks.</param>
    /// <returns>Statistics about enrichment operations performed.</returns>
    public static EnrichmentStatistics GetStatistics(IEnumerable<EnrichedChunk> enrichedChunks)
    {
        var chunks = enrichedChunks.ToList();
        var skipped = 0;
        var summarized = 0;
        var keywordsExtracted = 0;

        foreach (var chunk in chunks)
        {
            if (chunk.Metadata?.TryGetValue(EnrichmentMetadataKeys.WasSkipped, out var wasSkippedObj) == true &&
                wasSkippedObj is true)
            {
                skipped++;
            }
            else
            {
                if (chunk.Summary is not null) summarized++;
                if (chunk.Keywords is not null && chunk.Keywords.Count > 0) keywordsExtracted++;
            }
        }

        return new EnrichmentStatistics
        {
            TotalChunks = chunks.Count,
            SkippedChunks = skipped,
            SummarizedChunks = summarized,
            KeywordsExtractedChunks = keywordsExtracted,
            EstimatedLlmCallsSaved = skipped * 2 // Each skipped chunk saves ~2 LLM calls
        };
    }

    private static bool ShouldPerformSummarization(
        Chunk chunk,
        EnrichmentOptions options,
        ChunkQualityResult? qualityResult,
        ConditionalEnrichmentOptions? conditionalOptions)
    {
        if (!options.EnableSummarization)
            return false;

        if (conditionalOptions?.EnableConditionalEnrichment != true)
            return true;

        // Check content length threshold
        if (chunk.Content.Length < conditionalOptions.MinSummarizationLength)
            return false;

        // Check quality-based recommendation
        if (qualityResult is not null && !qualityResult.ShouldSummarize)
            return false;

        return true;
    }

    private static bool ShouldPerformKeywordExtraction(
        EnrichmentOptions options,
        ChunkQualityResult? qualityResult,
        ConditionalEnrichmentOptions? conditionalOptions)
    {
        if (!options.EnableKeywordExtraction)
            return false;

        if (conditionalOptions?.EnableConditionalEnrichment != true)
            return true;

        // Check density threshold
        if (qualityResult is not null && qualityResult.DensityScore < conditionalOptions.MinKeywordDensity)
            return false;

        // Check quality-based recommendation
        if (qualityResult is not null && !qualityResult.ShouldExtractKeywords)
            return false;

        return true;
    }

    private static EnrichedChunk CreateEnrichedChunk(
        Chunk chunk,
        string? summary,
        IReadOnlyList<string>? keywords,
        ChunkQualityResult? qualityResult,
        ConditionalEnrichmentOptions? conditionalOptions,
        bool wasSkipped)
    {
        var metadata = chunk.Metadata is not null
            ? new Dictionary<string, object>(chunk.Metadata)
            : new Dictionary<string, object>();

        // Add quality metrics if enabled
        if (conditionalOptions?.IncludeQualityMetrics == true && qualityResult is not null)
        {
            metadata[EnrichmentMetadataKeys.QualityScore] = qualityResult.OverallScore;
            metadata[EnrichmentMetadataKeys.CompletenessScore] = qualityResult.CompletenessScore;
            metadata[EnrichmentMetadataKeys.DensityScore] = qualityResult.DensityScore;
            metadata[EnrichmentMetadataKeys.StructureScore] = qualityResult.StructureScore;
            metadata[EnrichmentMetadataKeys.WasSkipped] = wasSkipped;
        }

        return new EnrichedChunk
        {
            Id = chunk.Id,
            Text = chunk.Content,
            SourceId = chunk.Id,
            Summary = summary,
            Keywords = keywords,
            Metadata = metadata.Count > 0 ? metadata : null
        };
    }
}

/// <summary>
/// Statistics about batch enrichment operations.
/// </summary>
public sealed record EnrichmentStatistics
{
    /// <summary>Total number of chunks processed.</summary>
    public required int TotalChunks { get; init; }

    /// <summary>Number of chunks that skipped enrichment due to high quality.</summary>
    public required int SkippedChunks { get; init; }

    /// <summary>Number of chunks that received summarization.</summary>
    public required int SummarizedChunks { get; init; }

    /// <summary>Number of chunks that received keyword extraction.</summary>
    public required int KeywordsExtractedChunks { get; init; }

    /// <summary>Estimated number of LLM API calls saved by conditional enrichment.</summary>
    public required int EstimatedLlmCallsSaved { get; init; }

    /// <summary>Percentage of chunks that were skipped.</summary>
    public float SkipRate => TotalChunks > 0 ? (float)SkippedChunks / TotalChunks : 0f;
}

/// <summary>
/// Well-known metadata keys for enrichment results.
/// </summary>
public static class EnrichmentMetadataKeys
{
    /// <summary>Overall quality score from pre-assessment (float, 0.0-1.0).</summary>
    public const string QualityScore = "enrichment_quality_score";

    /// <summary>Completeness score from pre-assessment (float, 0.0-1.0).</summary>
    public const string CompletenessScore = "enrichment_completeness_score";

    /// <summary>Information density score from pre-assessment (float, 0.0-1.0).</summary>
    public const string DensityScore = "enrichment_density_score";

    /// <summary>Structure score from pre-assessment (float, 0.0-1.0).</summary>
    public const string StructureScore = "enrichment_structure_score";

    /// <summary>Whether enrichment was skipped for this chunk (bool).</summary>
    public const string WasSkipped = "enrichment_was_skipped";
}

using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.Services;

namespace FluxImprover.ContextualRetrieval;

/// <summary>
/// Service for enriching chunks with document-level context.
/// Implements Anthropic's Contextual Retrieval pattern.
/// </summary>
public sealed class ContextualEnrichmentService : IContextualEnrichmentService
{
    private readonly ITextCompletionService _completionService;

    public ContextualEnrichmentService(ITextCompletionService completionService)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
    }

    /// <inheritdoc />
    public async Task<ContextualChunk> EnrichAsync(
        Chunk chunk,
        string fullDocumentText,
        ContextualEnrichmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullDocumentText);

        options ??= new ContextualEnrichmentOptions();

        string? contextSummary = null;

        if (!string.IsNullOrWhiteSpace(chunk.Content))
        {
            var prompt = BuildPrompt(chunk, fullDocumentText, options);
            var completionOptions = new CompletionOptions
            {
                SystemPrompt = GetSystemPrompt(),
                Temperature = options.Temperature,
                MaxTokens = options.MaxTokens
            };

            contextSummary = await _completionService.CompleteAsync(prompt, completionOptions, cancellationToken);
            contextSummary = contextSummary?.Trim();
        }

        return new ContextualChunk
        {
            Id = chunk.Id,
            Text = chunk.Content,
            SourceId = GetSourceId(chunk),
            ContextSummary = contextSummary,
            HeadingPath = GetHeadingPath(chunk),
            Position = GetPosition(chunk),
            TotalChunks = GetTotalChunks(chunk),
            Metadata = chunk.Metadata is not null
                ? new Dictionary<string, object>(chunk.Metadata)
                : null
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContextualChunk>> EnrichBatchAsync(
        IEnumerable<Chunk> chunks,
        string fullDocumentText,
        ContextualEnrichmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullDocumentText);

        options ??= new ContextualEnrichmentOptions();
        var chunkList = chunks.ToList();
        var totalChunks = chunkList.Count;

        // Set total chunks for position information
        for (int i = 0; i < chunkList.Count; i++)
        {
            var chunk = chunkList[i];
            if (chunk.Metadata is null || !chunk.Metadata.ContainsKey("position"))
            {
                var metadata = chunk.Metadata is not null
                    ? new Dictionary<string, object>(chunk.Metadata)
                    : new Dictionary<string, object>();
                metadata["position"] = i;
                metadata["totalChunks"] = totalChunks;
                chunkList[i] = new Chunk
                {
                    Id = chunk.Id,
                    Content = chunk.Content,
                    Metadata = metadata
                };
            }
        }

        if (options.EnableParallelProcessing && chunkList.Count > 1)
        {
            return await EnrichBatchParallelAsync(chunkList, fullDocumentText, totalChunks, options, cancellationToken);
        }

        var results = new List<ContextualChunk>(chunkList.Count);
        foreach (var chunk in chunkList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var enriched = await EnrichAsync(chunk, fullDocumentText, options, cancellationToken);
            results.Add(enriched);
        }

        return results;
    }

    private async Task<IReadOnlyList<ContextualChunk>> EnrichBatchParallelAsync(
        List<Chunk> chunks,
        string fullDocumentText,
        int totalChunks,
        ContextualEnrichmentOptions options,
        CancellationToken cancellationToken)
    {
        var semaphore = new SemaphoreSlim(options.MaxDegreeOfParallelism);
        var tasks = chunks.Select(async chunk =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await EnrichAsync(chunk, fullDocumentText, options, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        return results;
    }

    private static string GetSystemPrompt() =>
        "You are an expert at providing context for document chunks to improve search and retrieval. " +
        "Generate concise, informative context summaries that explain the chunk's role within its source document.";

    private static string BuildPrompt(Chunk chunk, string fullDocumentText, ContextualEnrichmentOptions options)
    {
        var parts = new List<string>
        {
            "## Full Document",
            fullDocumentText,
            "",
            "## Chunk to Contextualize",
            chunk.Content
        };

        if (options.IncludePositionInfo)
        {
            var position = GetPosition(chunk);
            var total = GetTotalChunks(chunk);
            if (position.HasValue && total.HasValue)
            {
                parts.Add("");
                parts.Add("## Chunk Position");
                parts.Add($"Position: {position.Value + 1} of {total.Value}");
            }
        }

        if (options.IncludeStructureInfo)
        {
            var headingPath = GetHeadingPath(chunk);
            if (!string.IsNullOrWhiteSpace(headingPath))
            {
                parts.Add("");
                parts.Add("## Document Structure");
                parts.Add($"Section: {headingPath}");
            }
        }

        parts.Add("");
        parts.Add("## Instructions");
        parts.Add("Generate a brief contextual summary (1-3 sentences) that explains:");
        parts.Add("1. What document this chunk comes from and its overall purpose");
        parts.Add("2. Where this chunk fits within the document's structure");
        parts.Add("3. What specific topic or concept this chunk addresses");
        parts.Add("");
        parts.Add($"Maximum length: {options.MaxContextLength} characters");
        parts.Add("");
        parts.Add("Provide only the contextual summary, no additional formatting.");

        return string.Join("\n", parts);
    }

    private static string GetSourceId(Chunk chunk)
    {
        if (chunk.Metadata?.TryGetValue("sourceId", out var sourceId) == true)
            return sourceId?.ToString() ?? chunk.Id;
        return chunk.Id;
    }

    private static string? GetHeadingPath(Chunk chunk)
    {
        if (chunk.Metadata?.TryGetValue("headingPath", out var headingPath) == true)
            return headingPath?.ToString();
        return null;
    }

    private static int? GetPosition(Chunk chunk)
    {
        if (chunk.Metadata?.TryGetValue("position", out var position) == true)
        {
            if (position is int pos) return pos;
            if (int.TryParse(position?.ToString(), out var parsed)) return parsed;
        }
        return null;
    }

    private static int? GetTotalChunks(Chunk chunk)
    {
        if (chunk.Metadata?.TryGetValue("totalChunks", out var total) == true)
        {
            if (total is int t) return t;
            if (int.TryParse(total?.ToString(), out var parsed)) return parsed;
        }
        return null;
    }
}

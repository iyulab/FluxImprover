using Flux.Abstractions;
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
    private readonly ITextGenerationService _completionService;

    public ContextualEnrichmentService(ITextGenerationService completionService)
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
        return await EnrichAsync(chunk, DocumentContext.Prepare(fullDocumentText, options.MaxDocumentContextLength), options, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ContextualChunk> EnrichAsync(
        Chunk chunk,
        DocumentContext.Prepared document,
        ContextualEnrichmentOptions options,
        CancellationToken cancellationToken)
    {
        string? contextSummary = null;

        if (!string.IsNullOrWhiteSpace(chunk.Content))
        {
            var prompt = BuildPrompt(chunk, document, options);
            var completionOptions = new CompletionOptions
            {
                SystemPrompt = GetSystemPrompt(),
                Temperature = options.Temperature,
                MaxTokens = options.MaxTokens,
                Thinking = options.Thinking,
                // The summary is prepended to the chunk for retrieval; a cut-off sentence there is worse than none.
                ThrowOnTruncation = true
            };

            try
            {
                contextSummary = await _completionService.CompleteAsync(prompt, completionOptions, cancellationToken);
                contextSummary = contextSummary?.Trim();
            }
            catch (TextCompletionTruncatedException)
            {
                // The chunk stays without context, exactly as a chunk with no content does.
                contextSummary = null;
            }
        }

        return ToContextualChunk(chunk, contextSummary);
    }

    private static ContextualChunk ToContextualChunk(Chunk chunk, string? contextSummary) =>
        new()
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

        // The document's profile is the same for every chunk: computed once here, not once per chunk.
        var document = DocumentContext.Prepare(fullDocumentText, options.MaxDocumentContextLength);

        // Windows of adjacent chunks, each one model call (ChunksPerCall = 1: one chunk per call, as before).
        var windows = chunkList.Chunk(options.ChunksPerCall).ToList();
        var parallelism = options.EnableParallelProcessing ? options.MaxDegreeOfParallelism : 1;
        using var semaphore = new SemaphoreSlim(Math.Max(1, parallelism));
        var tasks = windows.Select(async window =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return window.Length == 1
                    ? [await EnrichAsync(window[0], document, options, cancellationToken).ConfigureAwait(false)]
                    : await EnrichWindowAsync(window, document, options, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return (await Task.WhenAll(tasks).ConfigureAwait(false)).SelectMany(r => r).ToList();
    }

    /// <summary>
    /// One call for a window of adjacent chunks: the document context once, the chunks numbered, one summary per chunk back
    /// as a JSON array. Anything but exactly one usable summary per chunk — no array, the wrong count, an answer cut off —
    /// asks again chunk by chunk.
    /// </summary>
    private async Task<IReadOnlyList<ContextualChunk>> EnrichWindowAsync(
        Chunk[] window,
        DocumentContext.Prepared document,
        ContextualEnrichmentOptions options,
        CancellationToken cancellationToken)
    {
        var completionOptions = new CompletionOptions
        {
            SystemPrompt = GetSystemPrompt(),
            Temperature = options.Temperature,
            MaxTokens = options.MaxTokens * window.Length,
            Thinking = options.Thinking,
            ThrowOnTruncation = true
        };

        string? answer;
        try
        {
            answer = await _completionService.CompleteAsync(BuildWindowPrompt(window, document, options), completionOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TextCompletionTruncatedException)
        {
            answer = null;
        }

        if (ParseSummaries(answer, window.Length) is { } summaries)
        {
            return window.Select((chunk, i) => ToContextualChunk(chunk, string.IsNullOrWhiteSpace(summaries[i]) ? null : summaries[i].Trim())).ToList();
        }

        var fallback = new List<ContextualChunk>(window.Length);
        foreach (var chunk in window)
        {
            fallback.Add(await EnrichAsync(chunk, document, options, cancellationToken).ConfigureAwait(false));
        }

        return fallback;
    }

    /// <summary>The JSON array of strings in <paramref name="answer"/>, when it has exactly <paramref name="count"/> of them.</summary>
    internal static IReadOnlyList<string>? ParseSummaries(string? answer, int count)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return null;

        var start = answer.IndexOf('[', StringComparison.Ordinal);
        var end = answer.LastIndexOf(']');
        if (start < 0 || end <= start)
            return null;

        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<string?[]>(answer[start..(end + 1)]);
            return parsed is { Length: var n } && n == count ? parsed.Select(p => p ?? string.Empty).ToList() : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static string GetSystemPrompt() =>
        "You are an expert at providing context for document chunks to improve search and retrieval. " +
        "Generate concise, informative context summaries that explain the chunk's role within its source document.";

    private static string BuildWindowPrompt(Chunk[] window, DocumentContext.Prepared document, ContextualEnrichmentOptions options)
    {
        // Centred on the middle of the window, so the excerpt (for a long document) covers its chunks.
        var middle = window[window.Length / 2];
        var (heading, documentText) = document.For(middle.Content, GetPosition(middle), GetTotalChunks(middle));
        var parts = new List<string> { heading, documentText, "", "## Chunks to Contextualize" };
        for (var i = 0; i < window.Length; i++)
        {
            parts.Add("");
            var position = GetPosition(window[i]);
            var total = GetTotalChunks(window[i]);
            parts.Add(options.IncludePositionInfo && position.HasValue && total.HasValue
                ? $"### Chunk {i + 1} (position {position.Value + 1} of {total.Value})"
                : $"### Chunk {i + 1}");
            if (options.IncludeStructureInfo && GetHeadingPath(window[i]) is { Length: > 0 } section)
                parts.Add($"Section: {section}");
            parts.Add(window[i].Content);
        }

        parts.Add("");
        parts.Add("## Instructions");
        parts.Add($"For each of the {window.Length} chunks above, write a brief contextual summary (1-3 sentences) that explains");
        parts.Add("what document it comes from, where it fits in the document's structure, and what topic it addresses.");
        parts.Add($"Maximum length per summary: {options.MaxContextLength} characters.");
        parts.Add("");
        parts.Add($"Answer with only a JSON array of exactly {window.Length} strings, one summary per chunk, in chunk order.");

        return string.Join("\n", parts);
    }

    private static string BuildPrompt(Chunk chunk, DocumentContext.Prepared document, ContextualEnrichmentOptions options)
    {
        var (heading, documentText) = document.For(chunk.Content, GetPosition(chunk), GetTotalChunks(chunk));
        var parts = new List<string>
        {
            heading,
            documentText,
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

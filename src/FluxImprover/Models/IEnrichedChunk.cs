namespace FluxImprover.Models;

/// <summary>
/// LLM-enriched chunk implementation. Implements <see cref="ILlmEnrichedChunk"/>
/// which extends <see cref="Flux.Abstractions.IEnrichedChunk"/> with LLM-generated metadata.
/// </summary>
public sealed record EnrichedChunk : ILlmEnrichedChunk
{
    /// <summary>Unique chunk identifier.</summary>
    public required string ChunkId { get; init; }

    /// <summary>Chunk content text.</summary>
    public required string Content { get; init; }

    /// <summary>Original document identifier used to build <see cref="Flux.Abstractions.IEnrichedChunk.Source"/>.</summary>
    public required string SourceId { get; init; }

    /// <summary>Zero-based chunk index within the document.</summary>
    public int ChunkIndex { get; init; }

    /// <summary>Hierarchical heading path from document root.</summary>
    public IReadOnlyList<string> HeadingPath { get; init; } = [];

    /// <summary>Current section title.</summary>
    public string? SectionTitle { get; init; }

    /// <summary>Start page (1-based).</summary>
    public int? StartPage { get; init; }

    /// <summary>End page.</summary>
    public int? EndPage { get; init; }

    /// <summary>Overall chunk quality score (0.0–1.0).</summary>
    public double Quality { get; init; }

    /// <summary>Context dependency score (0.0–1.0).</summary>
    public double ContextDependency { get; init; }

    /// <summary>Estimated token count.</summary>
    public int? TokenCount { get; init; }

    // ILlmEnrichedChunk additions

    /// <summary>LLM-generated summary.</summary>
    public string? Summary { get; init; }

    /// <summary>LLM-extracted keywords.</summary>
    public IReadOnlyList<string>? Keywords { get; init; }

    /// <summary>Additional enrichment metadata.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    // Flux.Abstractions.IEnrichedChunk.Source — computed from SourceId, not serialized
    Flux.Abstractions.ISourceMetadata Flux.Abstractions.IEnrichedChunk.Source
        => new LlmEnrichedChunkSource { SourceId = SourceId };
}

file sealed record LlmEnrichedChunkSource : Flux.Abstractions.ISourceMetadata
{
    public required string SourceId { get; init; }
    public string SourceType => "llm-enriched";
    public string Title => SourceId;
    public string? FilePath => null;
    public string? Url => null;
    public DateTime CreatedAt => default;
    public string Language => string.Empty;
    public double? LanguageConfidence => null;
    public int WordCount => 0;
    public int ChunkCount => 0;
    public int? PageCount => null;
    public DateTime? PublishedAt => null;
    public string? Author => null;
    public IReadOnlyList<string>? Keywords => null;
}

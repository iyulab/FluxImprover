namespace FluxImprover.Models;

/// <summary>
/// LLM-enriched chunk. Extends the canonical <see cref="Flux.Abstractions.IEnrichedChunk"/>
/// with LLM-generated summary, keywords, and additional metadata.
/// </summary>
public interface ILlmEnrichedChunk : Flux.Abstractions.IEnrichedChunk
{
    /// <summary>
    /// LLM-generated summary of the chunk.
    /// </summary>
    string? Summary { get; }

    /// <summary>
    /// Keywords extracted by LLM.
    /// </summary>
    IReadOnlyList<string>? Keywords { get; }

    /// <summary>
    /// Additional metadata produced during enrichment.
    /// </summary>
    IReadOnlyDictionary<string, object>? Metadata { get; }
}

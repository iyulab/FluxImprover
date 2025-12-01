using System.Text.Json;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.Services;
using FluxImprover.Utilities;

namespace FluxImprover.RelationshipDiscovery;

/// <summary>
/// LLM-based service for discovering semantic relationships between document chunks.
/// </summary>
public sealed class ChunkRelationshipService : IChunkRelationshipService
{
    private readonly ITextCompletionService _completionService;

    public ChunkRelationshipService(ITextCompletionService completionService)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChunkRelationship>> AnalyzePairAsync(
        Chunk sourceChunk,
        Chunk targetChunk,
        ChunkRelationshipOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceChunk);
        ArgumentNullException.ThrowIfNull(targetChunk);

        options ??= new ChunkRelationshipOptions();

        var prompt = BuildPairAnalysisPrompt(sourceChunk, targetChunk, options);
        var completionOptions = new CompletionOptions
        {
            SystemPrompt = GetSystemPrompt(),
            Temperature = options.Temperature,
            MaxTokens = options.MaxTokens,
            JsonMode = true
        };

        var response = await _completionService.CompleteAsync(prompt, completionOptions, cancellationToken);
        return ParseRelationships(response, sourceChunk.Id, targetChunk.Id, options);
    }

    /// <inheritdoc />
    public async Task<ChunkRelationshipAnalysis> AnalyzeRelationshipsAsync(
        Chunk sourceChunk,
        IEnumerable<Chunk> candidateChunks,
        ChunkRelationshipOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceChunk);
        ArgumentNullException.ThrowIfNull(candidateChunks);

        options ??= new ChunkRelationshipOptions();
        var candidates = candidateChunks.ToList();
        var allRelationships = new List<ChunkRelationship>();

        try
        {
            if (options.EnableParallelProcessing && candidates.Count > 1)
            {
                var semaphore = new SemaphoreSlim(options.MaxDegreeOfParallelism);
                var tasks = candidates.Select(async candidate =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        return await AnalyzePairAsync(sourceChunk, candidate, options, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var results = await Task.WhenAll(tasks);
                allRelationships.AddRange(results.SelectMany(r => r));
            }
            else
            {
                foreach (var candidate in candidates)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var relationships = await AnalyzePairAsync(sourceChunk, candidate, options, cancellationToken);
                    allRelationships.AddRange(relationships);
                }
            }

            return new ChunkRelationshipAnalysis
            {
                ChunkId = sourceChunk.Id,
                Relationships = allRelationships,
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new ChunkRelationshipAnalysis
            {
                ChunkId = sourceChunk.Id,
                Relationships = allRelationships,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChunkRelationship>> DiscoverAllRelationshipsAsync(
        IEnumerable<Chunk> chunks,
        ChunkRelationshipOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        options ??= new ChunkRelationshipOptions();
        var chunkList = chunks.ToList();
        var allRelationships = new List<ChunkRelationship>();
        var processedPairs = new HashSet<string>();

        for (int i = 0; i < chunkList.Count; i++)
        {
            for (int j = i + 1; j < chunkList.Count; j++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pairKey = $"{chunkList[i].Id}:{chunkList[j].Id}";
                if (processedPairs.Contains(pairKey))
                    continue;

                processedPairs.Add(pairKey);
                var relationships = await AnalyzePairAsync(chunkList[i], chunkList[j], options, cancellationToken);
                allRelationships.AddRange(relationships);
            }
        }

        return allRelationships;
    }

    private static string GetSystemPrompt() =>
        """
        You are an expert at analyzing semantic relationships between document chunks.
        Your task is to identify meaningful relationships that would help in understanding
        how different parts of a document or document collection relate to each other.
        Always provide accurate, well-reasoned relationship classifications with confidence scores.
        """;

    private static string BuildPairAnalysisPrompt(Chunk source, Chunk target, ChunkRelationshipOptions options)
    {
        var relationshipTypes = string.Join(", ", options.RelationshipTypes.Select(r => r.ToString()));

        return $$"""
            Analyze the relationship between these two text chunks.

            ## Chunk A (Source)
            ID: {{source.Id}}
            Content: {{source.Content}}

            ## Chunk B (Target)
            ID: {{target.Id}}
            Content: {{target.Content}}

            ## Instructions
            Identify semantic relationships between Chunk A and Chunk B.
            Consider these relationship types: {{relationshipTypes}}

            Relationship type definitions:
            - SameTopic: Both chunks discuss the same topic or concept
            - References: One chunk references or cites the other
            - Complementary: Chunks contain complementary information on the same subject
            - Contradicts: Chunks contain contradictory or conflicting information
            - Prerequisite: Chunk A should be read before Chunk B for understanding
            - Elaborates: Chunk B provides more detail on Chunk A's content
            - Summarizes: Chunk B summarizes or abstracts Chunk A's content
            - ExampleOf: Chunks provide examples of the same concept
            - CauseEffect: Chunks describe cause and effect relationship
            - Temporal: Chunks show temporal or sequential relationship

            ## Output Format
            Return a JSON array of relationships found:
            ```json
            {
                "relationships": [
                    {
                        "type": "RelationshipType",
                        "confidence": 0.0-1.0,
                        "explanation": "Brief explanation",
                        "bidirectional": true/false
                    }
                ]
            }
            ```

            Only include relationships with confidence >= {{options.MinConfidence}}.
            Maximum {{options.MaxRelationshipsPerPair}} relationships.
            If no meaningful relationship exists, return empty array.
            {{(options.IncludeExplanations ? "Include brief explanations for each relationship." : "Omit explanations.")}}
            """;
    }

    private static IReadOnlyList<ChunkRelationship> ParseRelationships(
        string response,
        string sourceId,
        string targetId,
        ChunkRelationshipOptions options)
    {
        var relationships = new List<ChunkRelationship>();

        try
        {
            var json = JsonHelpers.ExtractJsonFromText(response);
            if (string.IsNullOrWhiteSpace(json))
                return relationships;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            JsonElement relArray;
            if (root.TryGetProperty("relationships", out relArray) && relArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var rel in relArray.EnumerateArray())
                {
                    var typeStr = rel.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
                    if (string.IsNullOrWhiteSpace(typeStr))
                        continue;

                    if (!Enum.TryParse<ChunkRelationshipType>(typeStr, true, out var relType))
                        continue;

                    if (!options.RelationshipTypes.Contains(relType))
                        continue;

                    var confidence = rel.TryGetProperty("confidence", out var confProp)
                        ? (float)confProp.GetDouble()
                        : 0.5f;

                    if (confidence < options.MinConfidence)
                        continue;

                    var explanation = rel.TryGetProperty("explanation", out var expProp)
                        ? expProp.GetString()
                        : null;

                    var bidirectional = rel.TryGetProperty("bidirectional", out var bidirProp)
                        && bidirProp.GetBoolean();

                    relationships.Add(new ChunkRelationship
                    {
                        SourceChunkId = sourceId,
                        TargetChunkId = targetId,
                        RelationshipType = relType,
                        Confidence = confidence,
                        Explanation = explanation,
                        IsBidirectional = bidirectional
                    });
                }
            }
        }
        catch (JsonException)
        {
            // Return empty list if parsing fails
        }

        return relationships
            .OrderByDescending(r => r.Confidence)
            .Take(options.MaxRelationshipsPerPair)
            .ToList();
    }
}

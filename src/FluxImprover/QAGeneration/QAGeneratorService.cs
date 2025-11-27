namespace FluxImprover.QAGeneration;

using System.Text.Json;
using FluxImprover.Abstractions.Models;
using FluxImprover.Abstractions.Options;
using FluxImprover.Abstractions.Services;
using FluxImprover.Abstractions.Utilities;

/// <summary>
/// LLM 기반 QA 쌍 생성 서비스
/// </summary>
public class QAGeneratorService
{
    private readonly ITextCompletionService _completionService;

    public QAGeneratorService(ITextCompletionService completionService)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
    }

    /// <summary>
    /// 컨텍스트에서 QA 쌍을 생성합니다.
    /// </summary>
    /// <param name="context">원본 컨텍스트</param>
    /// <param name="options">생성 옵션</param>
    /// <param name="sourceId">소스 식별자</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>생성된 QA 쌍 목록</returns>
    public virtual async Task<IReadOnlyList<GeneratedQAPair>> GenerateAsync(
        string context,
        QAGenerationOptions? options = null,
        string? sourceId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context))
            return [];

        options ??= new QAGenerationOptions();

        var prompt = BuildPrompt(context, options);
        var completionOptions = new CompletionOptions
        {
            SystemPrompt = GetSystemPrompt(),
            Temperature = options.Temperature,
            MaxTokens = options.MaxTokens,
            JsonMode = true
        };

        var response = await _completionService.CompleteAsync(prompt, completionOptions, cancellationToken);
        return ParseResponse(response, context, sourceId);
    }

    /// <summary>
    /// 청크에서 QA 쌍을 생성합니다.
    /// </summary>
    public virtual async Task<IReadOnlyList<GeneratedQAPair>> GenerateFromChunkAsync(
        Chunk chunk,
        QAGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        return await GenerateAsync(chunk.Content, options, chunk.Id, cancellationToken);
    }

    /// <summary>
    /// 여러 컨텍스트에서 QA 쌍을 일괄 생성합니다.
    /// </summary>
    public virtual async Task<IReadOnlyList<IReadOnlyList<GeneratedQAPair>>> GenerateBatchAsync(
        IEnumerable<string> contexts,
        QAGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<IReadOnlyList<GeneratedQAPair>>();

        foreach (var context in contexts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pairs = await GenerateAsync(context, options, cancellationToken: cancellationToken);
            results.Add(pairs);
        }

        return results;
    }

    private static string GetSystemPrompt()
    {
        return """
            You are an expert at generating high-quality question-answer pairs from text.
            Generate questions that test comprehension and understanding of the content.
            Ensure answers are accurate and directly supported by the provided context.
            Always return results in valid JSON format.
            """;
    }

    private static string BuildPrompt(string context, QAGenerationOptions options)
    {
        var questionTypes = string.Join(", ", options.QuestionTypes.Select(t => t.ToString().ToLowerInvariant()));
        var multiHopInstruction = options.IncludeMultiHop
            ? "Include multi-hop questions that require connecting information from different parts of the text."
            : "";
        var reasoningInstruction = options.IncludeReasoning
            ? "Include reasoning questions that require inference."
            : "";

        return $$"""
            Generate {{options.PairsPerChunk}} question-answer pairs from the following context.

            Requirements:
            - Questions should be clear and specific
            - Answers should be directly supported by the context
            - Answer length should be between {{options.MinAnswerLength}} and {{options.MaxAnswerLength}} characters
            - Question types to include: {{questionTypes}}
            {{multiHopInstruction}}
            {{reasoningInstruction}}

            Context:
            {{context}}

            Return a JSON object with this structure:
            {
                "qa_pairs": [
                    {"question": "...", "answer": "..."}
                ]
            }
            """;
    }

    private static IReadOnlyList<GeneratedQAPair> ParseResponse(string response, string context, string? sourceId)
    {
        try
        {
            var json = JsonHelpers.ExtractJsonFromText(response);
            if (json is null)
                return [];

            using var doc = JsonDocument.Parse(json);
            var pairs = new List<GeneratedQAPair>();

            if (doc.RootElement.TryGetProperty("qa_pairs", out var qaPairs) && qaPairs.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in qaPairs.EnumerateArray())
                {
                    var question = item.TryGetProperty("question", out var q) ? q.GetString() : null;
                    var answer = item.TryGetProperty("answer", out var a) ? a.GetString() : null;

                    if (!string.IsNullOrWhiteSpace(question) && !string.IsNullOrWhiteSpace(answer))
                    {
                        pairs.Add(new GeneratedQAPair
                        {
                            Question = question,
                            Answer = answer,
                            Context = context,
                            SourceId = sourceId
                        });
                    }
                }
            }

            return pairs;
        }
        catch
        {
            return [];
        }
    }
}

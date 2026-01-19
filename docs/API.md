# FluxImprover API Reference

> Complete API documentation for FluxImprover v0.6.0

---

## Table of Contents

1. [FluxImproverBuilder](#fluximproverbuilder)
2. [Services](#services)
   - [Summarization](#isummarizationservice)
   - [Keyword Extraction](#ikeywordextractionservice)
   - [Chunk Enrichment](#chunkenrichmentservice)
   - [Chunk Filtering](#ichunkfilteringservice)
   - [Query Preprocessing](#iquerypreprocessingservice)
   - [QA Generation](#qageneratorservice)
   - [Evaluators](#evaluators)
   - [Question Suggestion](#questionsuggestionservice)
   - [QA Pipeline](#qapipeline)
   - [Contextual Enrichment](#icontextualenrichmentservice)
   - [Chunk Relationship](#ichunkrelationshipservice)
3. [Models](#models)
4. [Options](#options)
5. [Interfaces](#interfaces)

---

## FluxImproverBuilder

Entry point for configuring and creating all FluxImprover services.

```csharp
public class FluxImproverBuilder
{
    public FluxImproverBuilder WithCompletionService(ITextCompletionService service);
    public FluxImproverServices Build();
}
```

### Usage

```csharp
var services = new FluxImproverBuilder()
    .WithCompletionService(completionService)
    .Build();
```

### FluxImproverServices

```csharp
public sealed record FluxImproverServices(
    SummarizationService Summarization,
    KeywordExtractionService KeywordExtraction,
    ChunkEnrichmentService ChunkEnrichment,
    FaithfulnessEvaluator Faithfulness,
    RelevancyEvaluator Relevancy,
    AnswerabilityEvaluator Answerability,
    QAGeneratorService QAGenerator,
    QAFilterService QAFilter,
    QAPipeline QAPipeline,
    QuestionSuggestionService QuestionSuggestion,
    IChunkFilteringService ChunkFiltering,
    IQueryPreprocessingService QueryPreprocessing,
    IContextualEnrichmentService ContextualEnrichment,
    IChunkRelationshipService ChunkRelationship
);
```

---

## Services

### ISummarizationService

Generates concise summaries from text content.

```csharp
public interface ISummarizationService
{
    Task<string> SummarizeAsync(
        string content,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### Example

```csharp
var summary = await services.Summarization.SummarizeAsync(
    "Long text content here...",
    new EnrichmentOptions { MaxSummaryLength = 100 });
```

---

### IKeywordExtractionService

Extracts relevant keywords from text content.

```csharp
public interface IKeywordExtractionService
{
    Task<IReadOnlyList<string>> ExtractKeywordsAsync(
        string content,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### Example

```csharp
var keywords = await services.KeywordExtraction.ExtractKeywordsAsync(
    "Machine learning is a subset of artificial intelligence...",
    new EnrichmentOptions { MaxKeywords = 5 });
// Returns: ["machine learning", "artificial intelligence", ...]
```

---

### ChunkEnrichmentService

Combines summarization and keyword extraction to enrich document chunks.

```csharp
public class ChunkEnrichmentService
{
    Task<IEnrichedChunk> EnrichAsync(
        Chunk chunk,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<IEnrichedChunk> EnrichBatchAsync(
        IEnumerable<Chunk> chunks,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### Example

```csharp
var chunk = new Chunk { Id = "1", Content = "Document content..." };
var enriched = await services.ChunkEnrichment.EnrichAsync(chunk);

Console.WriteLine($"Summary: {enriched.Summary}");
Console.WriteLine($"Keywords: {string.Join(", ", enriched.Keywords ?? [])}");
```

---

### IChunkFilteringService

3-stage LLM-based chunk assessment with self-reflection and critic validation.

```csharp
public interface IChunkFilteringService
{
    Task<ChunkAssessment> AssessAsync(
        Chunk chunk,
        ChunkFilteringOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChunkAssessment> AssessBatchAsync(
        IEnumerable<Chunk> chunks,
        ChunkFilteringOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### ChunkAssessment

```csharp
public sealed record ChunkAssessment
{
    public required string ChunkId { get; init; }
    public required double InitialScore { get; init; }
    public double? ReflectedScore { get; init; }
    public double? CriticScore { get; init; }
    public required double FinalScore { get; init; }
    public required bool ShouldInclude { get; init; }
    public string? Reasoning { get; init; }
}
```

#### Example

```csharp
var assessment = await services.ChunkFiltering.AssessAsync(chunk, new ChunkFilteringOptions
{
    MinimumScore = 0.6,
    EnableSelfReflection = true,
    EnableCriticValidation = true
});

Console.WriteLine($"Final Score: {assessment.FinalScore:P0}");
Console.WriteLine($"Should Include: {assessment.ShouldInclude}");
```

---

### IQueryPreprocessingService

Normalizes, expands, and classifies queries for optimal retrieval.

```csharp
public interface IQueryPreprocessingService
{
    Task<PreprocessedQuery> PreprocessAsync(
        string query,
        QueryPreprocessingOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### PreprocessedQuery

```csharp
public sealed record PreprocessedQuery
{
    public required string OriginalQuery { get; init; }
    public required string NormalizedQuery { get; init; }
    public required string ExpandedQuery { get; init; }
    public required QueryIntent Intent { get; init; }
    public required double IntentConfidence { get; init; }
    public required SearchStrategy SuggestedStrategy { get; init; }
    public required IReadOnlyList<string> Keywords { get; init; }
    public required IReadOnlyList<string> ExpandedKeywords { get; init; }
}
```

#### Example

```csharp
var result = await services.QueryPreprocessing.PreprocessAsync(
    "How do I implement auth config?",
    new QueryPreprocessingOptions { UseLlmExpansion = true });

Console.WriteLine($"Expanded: {result.ExpandedQuery}");
Console.WriteLine($"Intent: {result.Intent}");
```

---

### QAGeneratorService

Generates question-answer pairs from content.

```csharp
public class QAGeneratorService
{
    Task<IReadOnlyList<GeneratedQAPair>> GenerateAsync(
        string context,
        QAGenerationOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### GeneratedQAPair

```csharp
public sealed record GeneratedQAPair
{
    public required string Question { get; init; }
    public required string Answer { get; init; }
    public QuestionType Type { get; init; }
}
```

#### Example

```csharp
var qaPairs = await services.QAGenerator.GenerateAsync(
    "The solar system has eight planets...",
    new QAGenerationOptions
    {
        PairsPerChunk = 3,
        QuestionTypes = [QuestionType.Factual]
    });
```

---

### Evaluators

#### FaithfulnessEvaluator

Evaluates if an answer is grounded in the provided context.

```csharp
public class FaithfulnessEvaluator
{
    Task<MetricResult> EvaluateAsync(
        string context,
        string answer,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### RelevancyEvaluator

Evaluates if an answer addresses the question.

```csharp
public class RelevancyEvaluator
{
    Task<MetricResult> EvaluateAsync(
        string question,
        string answer,
        string? context = null,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### AnswerabilityEvaluator

Evaluates if a question can be answered from the context.

```csharp
public class AnswerabilityEvaluator
{
    Task<MetricResult> EvaluateAsync(
        string context,
        string question,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### MetricResult

```csharp
public sealed record MetricResult
{
    public required double Score { get; init; }  // 0.0 to 1.0
    public IReadOnlyDictionary<string, string> Details { get; init; }
}
```

#### Example

```csharp
var faithfulness = await services.Faithfulness.EvaluateAsync(context, answer);
var relevancy = await services.Relevancy.EvaluateAsync(question, answer, context);
var answerability = await services.Answerability.EvaluateAsync(context, question);

Console.WriteLine($"Faithfulness: {faithfulness.Score:P0}");
Console.WriteLine($"Relevancy: {relevancy.Score:P0}");
Console.WriteLine($"Answerability: {answerability.Score:P0}");
```

---

### QuestionSuggestionService

Suggests follow-up questions based on content or conversation.

```csharp
public class QuestionSuggestionService
{
    Task<IReadOnlyList<SuggestedQuestion>> SuggestFromContentAsync(
        string content,
        QuestionSuggestionOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SuggestedQuestion>> SuggestFromConversationAsync(
        IEnumerable<ConversationMessage> history,
        QuestionSuggestionOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### SuggestedQuestion

```csharp
public sealed record SuggestedQuestion
{
    public required string Text { get; init; }
    public required QuestionCategory Category { get; init; }
    public required double Relevance { get; init; }
}
```

#### Example

```csharp
var suggestions = await services.QuestionSuggestion.SuggestFromConversationAsync(
    [
        new ConversationMessage { Role = "user", Content = "What is AI?" },
        new ConversationMessage { Role = "assistant", Content = "AI is..." }
    ],
    new QuestionSuggestionOptions { MaxSuggestions = 3 });
```

---

### QAPipeline

End-to-end QA generation with quality filtering.

```csharp
public class QAPipeline
{
    Task<IReadOnlyList<QAPipelineResult>> ExecuteFromChunksBatchAsync(
        IEnumerable<Chunk> chunks,
        QAPipelineOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### QAPipelineResult

```csharp
public sealed record QAPipelineResult
{
    public required string ChunkId { get; init; }
    public required int GeneratedCount { get; init; }
    public required int FilteredCount { get; init; }
    public required IReadOnlyList<QAPair> QAPairs { get; init; }
}
```

#### Example

```csharp
var results = await services.QAPipeline.ExecuteFromChunksBatchAsync(
    chunks,
    new QAPipelineOptions
    {
        GenerationOptions = new QAGenerationOptions { PairsPerChunk = 2 },
        FilterOptions = new QAFilterOptions
        {
            MinFaithfulness = 0.7,
            MinRelevancy = 0.7
        }
    });

var totalGenerated = results.Sum(r => r.GeneratedCount);
var totalPassed = results.Sum(r => r.FilteredCount);
```

---

### IContextualEnrichmentService

Document-level contextual retrieval (Anthropic pattern).

```csharp
public interface IContextualEnrichmentService
{
    Task<ContextualChunk> EnrichAsync(
        Chunk chunk,
        string fullDocument,
        ContextualEnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ContextualChunk> EnrichBatchAsync(
        IEnumerable<Chunk> chunks,
        string fullDocument,
        ContextualEnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

---

### IChunkRelationshipService

Discovers relationships between chunks.

```csharp
public interface IChunkRelationshipService
{
    Task<IReadOnlyList<ChunkRelationship>> DiscoverRelationshipsAsync(
        IEnumerable<Chunk> chunks,
        ChunkRelationshipOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

---

## Models

### Chunk

Input model for document chunks.

```csharp
public sealed record Chunk
{
    public required string Id { get; init; }
    public required string Content { get; init; }
    public string? SourceDocument { get; init; }
    public IDictionary<string, string>? Metadata { get; init; }
}
```

### ConversationMessage

Represents a message in a conversation.

```csharp
public sealed record ConversationMessage
{
    public required string Role { get; init; }  // "user" or "assistant"
    public required string Content { get; init; }
}
```

### QuestionType

```csharp
public enum QuestionType
{
    Factual,
    Reasoning,
    Comparative,
    MultiHop,
    Conditional
}
```

### QuestionCategory

```csharp
public enum QuestionCategory
{
    DeepDive,
    Related,
    Clarification,
    Application
}
```

### QueryIntent

```csharp
public enum QueryIntent
{
    Unknown,
    HowTo,
    Definition,
    Code,
    Search,
    Troubleshoot,
    Compare
}
```

### SearchStrategy

```csharp
public enum SearchStrategy
{
    Semantic,
    Keyword,
    Hybrid,
    MultiQuery
}
```

---

## Options

### EnrichmentOptions

```csharp
public sealed record EnrichmentOptions
{
    public bool GenerateSummary { get; init; } = true;
    public bool ExtractKeywords { get; init; } = true;
    public int MaxKeywords { get; init; } = 5;
    public int MaxSummaryLength { get; init; } = 200;
}
```

### QAGenerationOptions

```csharp
public sealed record QAGenerationOptions
{
    public int PairsPerChunk { get; init; } = 3;
    public IReadOnlyList<QuestionType>? QuestionTypes { get; init; }
    public float Temperature { get; init; } = 0.7f;
}
```

### QAFilterOptions

```csharp
public sealed record QAFilterOptions
{
    public double MinFaithfulness { get; init; } = 0.7;
    public double MinRelevancy { get; init; } = 0.7;
    public double MinAnswerability { get; init; } = 0.6;
}
```

### QAPipelineOptions

```csharp
public sealed record QAPipelineOptions
{
    public QAGenerationOptions? GenerationOptions { get; init; }
    public QAFilterOptions? FilterOptions { get; init; }
}
```

### QuestionSuggestionOptions

```csharp
public sealed record QuestionSuggestionOptions
{
    public int MaxSuggestions { get; init; } = 3;
    public IReadOnlyList<QuestionCategory>? Categories { get; init; }
}
```

### ChunkFilteringOptions

```csharp
public sealed record ChunkFilteringOptions
{
    public double MinimumScore { get; init; } = 0.6;
    public bool EnableSelfReflection { get; init; } = true;
    public bool EnableCriticValidation { get; init; } = true;
}
```

### QueryPreprocessingOptions

```csharp
public sealed record QueryPreprocessingOptions
{
    public bool UseLlmExpansion { get; init; } = true;
    public bool ExpandTechnicalTerms { get; init; } = true;
    public int MaxSynonymsPerKeyword { get; init; } = 3;
}
```

---

## Interfaces

### ITextCompletionService

The core interface you must implement to connect FluxImprover to your LLM provider.

```csharp
public interface ITextCompletionService
{
    Task<string> CompleteAsync(
        string prompt,
        CompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        CompletionOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

### CompletionOptions

```csharp
public sealed record CompletionOptions
{
    public string? SystemPrompt { get; init; }
    public float? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public bool JsonMode { get; init; } = false;
    public string? ResponseSchema { get; init; }
    public IReadOnlyList<ChatMessage>? Messages { get; init; }
}
```

### ChatMessage

```csharp
public sealed record ChatMessage
{
    public required string Role { get; init; }
    public required string Content { get; init; }
}
```

---

## Dependency Injection

### Registration Methods

```csharp
// With factory
services.AddFluxImprover(sp => new OpenAICompletionService(apiKey));

// With pre-registered ITextCompletionService
services.AddSingleton<ITextCompletionService, OpenAICompletionService>();
services.AddFluxImprover();
```

---

## Error Handling

All services may throw the following exceptions:

- `ArgumentNullException` - When required parameters are null
- `ArgumentException` - When parameters are invalid
- `InvalidOperationException` - When service is not properly configured
- `OperationCanceledException` - When cancellation is requested
- `HttpRequestException` - When LLM API calls fail (from your implementation)

---

*API Reference for FluxImprover v0.6.0*
*Last updated: 2025-01-19*

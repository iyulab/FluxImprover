# FluxImprover API Reference

> Complete API documentation for FluxImprover v0.1.0

---

## Table of Contents

1. [FluxImproverBuilder](#fluximproverbuilder)
2. [Services](#services)
   - [Summarization](#isummarizationservice)
   - [Keyword Extraction](#ikeywordextractionservice)
   - [Chunk Enrichment](#ichunkenrichmentservice)
   - [QA Generation](#iqageneratorservice)
   - [Evaluators](#evaluators)
   - [Question Suggestion](#iquestionsuggestservice)
   - [QA Pipeline](#iqapipelineservice)
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
public class FluxImproverServices
{
    public ISummarizationService Summarization { get; }
    public IKeywordExtractionService KeywordExtraction { get; }
    public IChunkEnrichmentService ChunkEnrichment { get; }
    public IQAGeneratorService QAGenerator { get; }
    public IFaithfulnessEvaluator Faithfulness { get; }
    public IRelevancyEvaluator Relevancy { get; }
    public IAnswerabilityEvaluator Answerability { get; }
    public IQuestionSuggestService QuestionSuggestion { get; }
    public IQAPipelineService QAPipeline { get; }
}
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
        SummarizationOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### Example

```csharp
var summary = await services.Summarization.SummarizeAsync(
    "Long text content here...",
    new SummarizationOptions { MaxLength = 100 });
```

---

### IKeywordExtractionService

Extracts relevant keywords from text content.

```csharp
public interface IKeywordExtractionService
{
    Task<IReadOnlyList<string>> ExtractAsync(
        string content,
        KeywordExtractionOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### Example

```csharp
var keywords = await services.KeywordExtraction.ExtractAsync(
    "Machine learning is a subset of artificial intelligence...",
    new KeywordExtractionOptions { MaxKeywords = 5 });
// Returns: ["machine learning", "artificial intelligence", ...]
```

---

### IChunkEnrichmentService

Combines summarization and keyword extraction to enrich document chunks.

```csharp
public interface IChunkEnrichmentService
{
    Task<EnrichedChunk> EnrichAsync(
        Chunk chunk,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### EnrichedChunk

```csharp
public class EnrichedChunk
{
    public string Id { get; }
    public string Content { get; }
    public string? Summary { get; }
    public IReadOnlyList<string>? Keywords { get; }
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

### IQAGeneratorService

Generates question-answer pairs from content.

```csharp
public interface IQAGeneratorService
{
    Task<IReadOnlyList<QAPair>> GenerateAsync(
        string context,
        QAGenerationOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### QAPair

```csharp
public class QAPair
{
    public string Question { get; }
    public string Answer { get; }
    public string? Context { get; }
    public QuestionType? Type { get; }
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

#### IFaithfulnessEvaluator

Evaluates if an answer is grounded in the provided context.

```csharp
public interface IFaithfulnessEvaluator
{
    Task<MetricResult> EvaluateAsync(
        string context,
        string answer,
        CancellationToken cancellationToken = default);
}
```

#### IRelevancyEvaluator

Evaluates if an answer addresses the question.

```csharp
public interface IRelevancyEvaluator
{
    Task<MetricResult> EvaluateAsync(
        string question,
        string answer,
        string? context = null,
        CancellationToken cancellationToken = default);
}
```

#### IAnswerabilityEvaluator

Evaluates if a question can be answered from the context.

```csharp
public interface IAnswerabilityEvaluator
{
    Task<MetricResult> EvaluateAsync(
        string context,
        string question,
        CancellationToken cancellationToken = default);
}
```

#### MetricResult

```csharp
public class MetricResult
{
    public double Score { get; }  // 0.0 to 1.0
    public IReadOnlyDictionary<string, string> Details { get; }
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

### IQuestionSuggestService

Suggests follow-up questions based on content or conversation.

```csharp
public interface IQuestionSuggestService
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
public class SuggestedQuestion
{
    public string Text { get; }
    public QuestionCategory Category { get; }
    public double Relevance { get; }
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

### IQAPipelineService

End-to-end QA generation with quality filtering.

```csharp
public interface IQAPipelineService
{
    Task<IReadOnlyList<QAPipelineResult>> ExecuteFromChunksBatchAsync(
        IEnumerable<Chunk> chunks,
        QAPipelineOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

#### QAPipelineResult

```csharp
public class QAPipelineResult
{
    public string ChunkId { get; }
    public int GeneratedCount { get; }
    public int FilteredCount { get; }
    public IReadOnlyList<QAPair> QAPairs { get; }
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

## Models

### Chunk

Input model for document chunks.

```csharp
public class Chunk
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
public class ConversationMessage
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

---

## Options

### QAGenerationOptions

```csharp
public class QAGenerationOptions
{
    public int PairsPerChunk { get; init; } = 3;
    public IReadOnlyList<QuestionType>? QuestionTypes { get; init; }
    public float Temperature { get; init; } = 0.7f;
}
```

### QAFilterOptions

```csharp
public class QAFilterOptions
{
    public double MinFaithfulness { get; init; } = 0.7;
    public double MinRelevancy { get; init; } = 0.7;
    public double MinAnswerability { get; init; } = 0.6;
}
```

### QAPipelineOptions

```csharp
public class QAPipelineOptions
{
    public QAGenerationOptions? GenerationOptions { get; init; }
    public QAFilterOptions? FilterOptions { get; init; }
}
```

### QuestionSuggestionOptions

```csharp
public class QuestionSuggestionOptions
{
    public int MaxSuggestions { get; init; } = 3;
    public IReadOnlyList<QuestionCategory>? Categories { get; init; }
}
```

### EnrichmentOptions

```csharp
public class EnrichmentOptions
{
    public bool GenerateSummary { get; init; } = true;
    public bool ExtractKeywords { get; init; } = true;
    public int MaxKeywords { get; init; } = 5;
    public int MaxSummaryLength { get; init; } = 200;
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
public record CompletionOptions
{
    public string? SystemPrompt { get; init; }
    public float Temperature { get; init; } = 0.7f;
    public int? MaxTokens { get; init; }
    public bool JsonMode { get; init; } = false;
    public IReadOnlyList<Message>? Messages { get; init; }
}
```

### Message

```csharp
public record Message
{
    public required string Role { get; init; }
    public required string Content { get; init; }
}
```

---

## Error Handling

All services may throw the following exceptions:

- `ArgumentNullException` - When required parameters are null
- `ArgumentException` - When parameters are invalid
- `OperationCanceledException` - When cancellation is requested
- `HttpRequestException` - When LLM API calls fail (from your implementation)

---

*API Reference for FluxImprover v0.1.0*
*Last updated: 2025-11-27*

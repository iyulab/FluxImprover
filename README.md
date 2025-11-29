# FluxImprover

> **The Quality Layer for RAG Data Pipelines.**
> **LLM-powered enrichment and quality assessment for document chunks.**

[![NuGet](https://img.shields.io/nuget/v/FluxImprover.svg)](https://www.nuget.org/packages/FluxImprover)
[![Downloads](https://img.shields.io/nuget/dt/FluxImprover.svg)](https://www.nuget.org/packages/FluxImprover)
[![CI](https://github.com/iyulab/FluxImprover/actions/workflows/ci.yml/badge.svg)](https://github.com/iyulab/FluxImprover/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

## Overview

**FluxImprover** is a specialized .NET library designed to enhance and validate the quality of document chunks before they are indexed into a RAG (Retrieval-Augmented Generation) system.

It acts as the **quality assurance and value-add layer**, leveraging Large Language Models (LLMs) to transform raw chunks into highly optimized assets for superior search and answer generation.

### Key Capabilities

* **Chunk Enrichment**: Uses LLMs to create concise **summaries** and relevant **keywords** for each chunk
* **Chunk Filtering**: 3-stage LLM-based assessment with **self-reflection** and **critic validation** for intelligent retrieval filtering
* **QA Pair Generation**: Automatically generates **Golden QA datasets** from document chunks for RAG benchmarking
* **Quality Assessment**: Provides **Faithfulness**, **Relevancy**, and **Answerability** evaluators
* **Question Suggestion**: Generates contextual follow-up questions from content or conversations
* **Decoupled Design**: Works with any LLM through the `ITextCompletionService` abstraction

---

## Installation

Install the main package via NuGet:

```bash
dotnet add package FluxImprover
```

---

## Quick Start

### 1. Implement ITextCompletionService

FluxImprover requires you to provide an LLM implementation:

```csharp
public class OpenAICompletionService : ITextCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OpenAICompletionService(string apiKey, string model = "gpt-4o-mini")
    {
        _model = model;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.openai.com/v1/")
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public async Task<string> CompleteAsync(
        string prompt,
        CompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Your OpenAI API implementation
    }

    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        CompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Your streaming implementation
    }
}
```

### 2. Configure and Build Services

Use the `FluxImproverBuilder` to create all services with a single LLM provider:

```csharp
using FluxImprover;
using FluxImprover.Services;

// Your ITextCompletionService implementation
ITextCompletionService completionService = new OpenAICompletionService(apiKey);

// Build all FluxImprover services
var services = new FluxImproverBuilder()
    .WithCompletionService(completionService)
    .Build();
```

### 3. Enrich Chunks

Add summaries and keywords to your document chunks:

```csharp
using FluxImprover.Models;

var chunk = new Chunk
{
    Id = "chunk-1",
    Content = "Paris is the capital of France. It is known for the Eiffel Tower."
};

// Enrich with summary and keywords
var enrichedChunk = await services.ChunkEnrichment.EnrichAsync(chunk);

Console.WriteLine($"Summary: {enrichedChunk.Summary}");
Console.WriteLine($"Keywords: {string.Join(", ", enrichedChunk.Keywords ?? [])}");
```

### 4. Generate QA Pairs

Create question-answer pairs for RAG testing:

```csharp
using FluxImprover.Options;

var context = "The solar system has eight planets. Earth is the third planet from the sun.";

var options = new QAGenerationOptions
{
    PairsPerChunk = 3,
    QuestionTypes = [QuestionType.Factual, QuestionType.Reasoning]
};

var qaPairs = await services.QAGenerator.GenerateAsync(context, options);

foreach (var qa in qaPairs)
{
    Console.WriteLine($"Q: {qa.Question}");
    Console.WriteLine($"A: {qa.Answer}");
}
```

### 5. Evaluate Quality

Assess answer quality with multiple metrics:

```csharp
var context = "France is in Europe. Paris is the capital of France.";
var question = "What is the capital of France?";
var answer = "Paris is the capital of France.";

// Faithfulness: Is the answer grounded in the context?
var faithfulness = await services.Faithfulness.EvaluateAsync(context, answer);

// Relevancy: Does the answer address the question?
var relevancy = await services.Relevancy.EvaluateAsync(question, answer, context: context);

// Answerability: Can the question be answered from the context?
var answerability = await services.Answerability.EvaluateAsync(context, question);

Console.WriteLine($"Faithfulness: {faithfulness.Score:P0}");
Console.WriteLine($"Relevancy: {relevancy.Score:P0}");
Console.WriteLine($"Answerability: {answerability.Score:P0}");

// Access detailed information
foreach (var detail in faithfulness.Details)
{
    Console.WriteLine($"  {detail.Key}: {detail.Value}");
}
```

### 6. Filter QA Pairs by Quality

Use the QA Pipeline to generate and automatically filter low-quality pairs:

```csharp
using FluxImprover.QAGeneration;

var chunks = new[]
{
    new Chunk { Id = "1", Content = "Machine learning is a subset of AI..." },
    new Chunk { Id = "2", Content = "Neural networks mimic the human brain..." }
};

var pipelineOptions = new QAPipelineOptions
{
    GenerationOptions = new QAGenerationOptions { PairsPerChunk = 2 },
    FilterOptions = new QAFilterOptions
    {
        MinFaithfulness = 0.7,
        MinRelevancy = 0.7,
        MinAnswerability = 0.6
    }
};

var results = await services.QAPipeline.ExecuteFromChunksBatchAsync(chunks, pipelineOptions);

var totalGenerated = results.Sum(r => r.GeneratedCount);
var totalFiltered = results.Sum(r => r.FilteredCount);
var allQAPairs = results.SelectMany(r => r.QAPairs).ToList();

Console.WriteLine($"Generated: {totalGenerated}, Passed Filter: {totalFiltered}");
```

### 7. Filter Chunks with 3-Stage Assessment

Use intelligent chunk filtering with self-reflection and critic validation:

```csharp
using FluxImprover.ChunkFiltering;
using FluxImprover.Options;

var chunk = new Chunk
{
    Id = "chunk-1",
    Content = "This is a detailed technical document about machine learning algorithms..."
};

var filterOptions = new ChunkFilteringOptions
{
    MinimumScore = 0.6,
    EnableSelfReflection = true,
    EnableCriticValidation = true
};

// Assess chunk quality with 3-stage evaluation
var assessment = await services.ChunkFiltering.AssessAsync(chunk, filterOptions);

Console.WriteLine($"Initial Score: {assessment.InitialScore:P0}");
Console.WriteLine($"Reflected Score: {assessment.ReflectedScore:P0}");
Console.WriteLine($"Final Score: {assessment.FinalScore:P0}");
Console.WriteLine($"Should Include: {assessment.ShouldInclude}");
Console.WriteLine($"Reasoning: {assessment.Reasoning}");
```

The 3-stage assessment process:
1. **Initial Assessment**: LLM evaluates chunk quality and relevance
2. **Self-Reflection**: LLM reviews its initial assessment for consistency
3. **Critic Validation**: Independent LLM evaluation validates the assessment

### 8. Suggest Follow-up Questions

Generate contextual questions from content or conversations:

```csharp
using FluxImprover.QuestionSuggestion;
using FluxImprover.Options;

// From a conversation
var history = new[]
{
    new ConversationMessage { Role = "user", Content = "What is machine learning?" },
    new ConversationMessage { Role = "assistant", Content = "Machine learning is a subset of AI..." }
};

var options = new QuestionSuggestionOptions
{
    MaxSuggestions = 3,
    Categories = [QuestionCategory.DeepDive, QuestionCategory.Related]
};

var suggestions = await services.QuestionSuggestion.SuggestFromConversationAsync(history, options);

foreach (var suggestion in suggestions)
{
    Console.WriteLine($"[{suggestion.Category}] {suggestion.Text} (relevance: {suggestion.Relevance:P0})");
}
```

---

## Available Services

| Service | Description |
|---------|-------------|
| `Summarization` | Generates concise summaries from text |
| `KeywordExtraction` | Extracts relevant keywords |
| `ChunkEnrichment` | Combines summarization and keyword extraction |
| `ChunkFiltering` | 3-stage LLM-based chunk assessment with self-reflection and critic validation |
| `Faithfulness` | Evaluates if answers are grounded in context |
| `Relevancy` | Evaluates if answers address the question |
| `Answerability` | Evaluates if questions can be answered from context |
| `QAGenerator` | Generates question-answer pairs from content |
| `QAFilter` | Filters QA pairs by quality thresholds |
| `QAPipeline` | End-to-end QA generation with quality filtering |
| `QuestionSuggestion` | Suggests contextual follow-up questions |

---

## ITextCompletionService Interface

FluxImprover requires an implementation of `ITextCompletionService` to communicate with LLMs:

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

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     FluxImproverBuilder                         │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                 ITextCompletionService                      ││
│  └─────────────────────────────────────────────────────────────┘│
│         │                    │                    │             │
│  ┌──────▼──────┐     ┌──────▼──────┐     ┌──────▼──────┐       │
│  │ Enrichment  │     │ Evaluation  │     │    QA       │       │
│  │  Services   │     │  Metrics    │     │ Generation  │       │
│  └─────────────┘     └─────────────┘     └─────────────┘       │
│                                                                 │
│  ┌──────────────────────────┐  ┌───────────────────────────────┐│
│  │     Chunk Filtering      │  │   Question Suggestion Service ││
│  │  (3-Stage Assessment)    │  │                               ││
│  └──────────────────────────┘  └───────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

---

## Sample Project

Check out the [Console Demo](samples/FluxImprover.ConsoleDemo) for a complete example showing all features with OpenAI integration.

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

## License

MIT License - See [LICENSE](LICENSE) file

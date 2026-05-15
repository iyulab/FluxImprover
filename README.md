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
* **Query Preprocessing**: Normalizes, expands, and classifies queries with **synonym expansion** and **intent classification** for optimal retrieval
* **QA Pair Generation**: Automatically generates **Golden QA datasets** from document chunks for RAG benchmarking
* **Quality Assessment**: Provides **Faithfulness**, **Relevancy**, and **Answerability** evaluators
* **Question Suggestion**: Generates contextual follow-up questions from content or conversations
* **Decoupled Design**: Works with any LLM through the `ITextGenerationService` abstraction

---

## Installation

Install the main package via NuGet:

```bash
dotnet add package FluxImprover
```

For the built-in OpenAI-compatible provider (OpenAI, Azure OpenAI, Ollama, etc.), no additional package is required — it ships with the core package.

For the built-in local model provider using LMSupply:

```bash
dotnet add package FluxImprover.LMSupply
```

---

## Quick Start

### 1. Configure and Build Services

FluxImprover ships with two built-in providers. Choose the one that matches your setup.

#### Option A: OpenAI-compatible API (OpenAI, Azure OpenAI, Ollama, etc.)

```csharp
// Using dependency injection (ASP.NET Core, Worker Service, etc.)
services.AddFluxImproverWithOpenAI(
    endpoint: "https://api.openai.com/v1",
    apiKey: "<your-key>",
    model: "gpt-4o-mini");

// Or using the builder pattern (no DI container required)
using var completionService = new OpenAICompatibleCompletionService(
    endpoint: "https://api.openai.com/v1",
    apiKey: "<your-key>",
    model: "gpt-4o-mini",
    logger: loggerFactory.CreateLogger<OpenAICompatibleCompletionService>());

var services = new FluxImproverBuilder()
    .WithCompletionService(completionService)
    .Build();
```

`AddFluxImproverWithOpenAI` is defined in `FluxImprover.Services.Providers` and is included in the core `FluxImprover` package. It works with any OpenAI-compatible endpoint (Ollama, Azure OpenAI, Fireworks, etc.) by adjusting the `endpoint` parameter.

#### Option B: Local model via LMSupply (offline, GGUF/ONNX)

Requires `dotnet add package FluxImprover.LMSupply`.

```csharp
// Assuming IGeneratorModel is registered by LMSupply
services.AddFluxImproverWithLMSupply(
    modelFactory: sp => sp.GetRequiredService<IGeneratorModel>(),
    defaultTemperature: 0.3f,
    defaultMaxTokens: 512);

// Shorthand when IGeneratorModel is already in the container
services.AddFluxImproverWithLMSupply();
```

#### Option C: Custom ITextGenerationService

For any other provider, implement `ITextGenerationService` and register it:

```csharp
// Register your own implementation
services.AddSingleton<ITextGenerationService, MyCompletionService>();

// Then add FluxImprover (resolves ITextGenerationService from the container)
services.AddFluxImprover();

// Or pass a factory directly
services.AddFluxImprover(sp => new MyCompletionService(sp.GetRequiredService<...>()));
```

#### Service Lifetime

All FluxImprover services default to `Scoped`, compatible with the standard ASP.NET Core
`IServiceScopeFactory.CreateScope()` pattern. Use `ServiceLifetime.Singleton` only when the
`ITextGenerationService` and all its dependencies are also singletons:

```csharp
services.AddFluxImproverWithOpenAI(endpoint, apiKey, model, ServiceLifetime.Singleton);
services.AddFluxImprover(_ => new MyCompletionService(apiKey), ServiceLifetime.Singleton);
```

> **Breaking Change (v0.8.0 — Scoped default)**: Prior to v0.8.0, `AddFluxImprover()` registered all services as
> `Singleton`. Services are now `Scoped` by default. Consumers that resolve FluxImprover services
> directly from the root provider (e.g. `app.Services.GetRequiredService<...>()`) must either:
> - Wrap the call in a scope: `using var scope = services.CreateScope(); scope.ServiceProvider.GetRequiredService<...>()`
> - Or opt back into Singleton explicitly: `services.AddFluxImprover(_ => ..., ServiceLifetime.Singleton)`

### 2. Enrich Chunks

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

### 3. Generate QA Pairs

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

### 4. Evaluate Quality

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

### 5. Filter QA Pairs by Quality

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

### 6. Filter Chunks with 3-Stage Assessment

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

### 7. Preprocess Queries for Better Retrieval

Optimize queries before RAG retrieval with normalization, synonym expansion, and intent classification:

```csharp
using FluxImprover.QueryPreprocessing;
using FluxImprover.Options;

var query = "How do I implement auth config?";

var options = new QueryPreprocessingOptions
{
    UseLlmExpansion = true,
    ExpandTechnicalTerms = true,
    MaxSynonymsPerKeyword = 3
};

var result = await services.QueryPreprocessing.PreprocessAsync(query, options);

Console.WriteLine($"Original: {result.OriginalQuery}");
Console.WriteLine($"Normalized: {result.NormalizedQuery}");
Console.WriteLine($"Expanded: {result.ExpandedQuery}");
Console.WriteLine($"Intent: {result.Intent} (confidence: {result.IntentConfidence:P0})");
Console.WriteLine($"Strategy: {result.SuggestedStrategy}");
Console.WriteLine($"Keywords: {string.Join(", ", result.Keywords)}");
Console.WriteLine($"Expanded Keywords: {string.Join(", ", result.ExpandedKeywords)}");
```

Features:
- **Query Normalization**: Lowercase, trim, remove extra whitespace
- **Synonym Expansion**: LLM-based and built-in technical term expansion (e.g., "auth" -> "authentication")
- **Intent Classification**: Classifies queries into types (HowTo, Definition, Code, Search, etc.)
- **Entity Extraction**: Identifies file names, class names, method names in queries
- **Search Strategy**: Recommends optimal search strategy (Semantic, Keyword, Hybrid, MultiQuery)

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

## Language Support

FluxImprover is designed to be **language-agnostic**. The underlying LLM automatically detects the input language and responds accordingly.

### Supported Languages

Any language supported by your LLM provider works with FluxImprover:
- **English** - Primary development and testing language
- **Korean** - Tested with technical documentation (e.g., ClusterPlex HA solution manuals)
- **Other languages** - Japanese, Chinese, German, French, etc. (depends on LLM capability)

### Best Practices for Non-English Documents

1. **Use a capable LLM**: Modern LLMs (GPT-4, Claude, Phi-4) have excellent multilingual support
2. **Domain terminology**: The LLM will recognize domain-specific terms in any language
3. **Mixed content**: Documents with mixed languages (e.g., Korean text with English technical terms) are handled naturally

### Example: Korean Document Enrichment

```csharp
var chunk = new Chunk
{
    Id = "korean-1",
    Content = "ClusterPlex는 고가용성(HA) 솔루션으로, 핫빗 기반의 페일오버 메커니즘을 제공합니다."
};

var enriched = await services.ChunkEnrichment.EnrichAsync(chunk);
// Summary and keywords will be generated in Korean
```

---

## Available Services

| Service | Description |
|---------|-------------|
| `Summarization` | Generates concise summaries from text |
| `KeywordExtraction` | Extracts relevant keywords |
| `ChunkEnrichment` | Combines summarization and keyword extraction |
| `ChunkFiltering` | 3-stage LLM-based chunk assessment with self-reflection and critic validation |
| `QueryPreprocessing` | Normalizes, expands, and classifies queries for optimal retrieval |
| `Faithfulness` | Evaluates if answers are grounded in context |
| `Relevancy` | Evaluates if answers address the question |
| `Answerability` | Evaluates if questions can be answered from context |
| `QAGenerator` | Generates question-answer pairs from content |
| `QAFilter` | Filters QA pairs by quality thresholds |
| `QAPipeline` | End-to-end QA generation with quality filtering |
| `QuestionSuggestion` | Suggests contextual follow-up questions |
| `ContextualEnrichment` | Document-level contextual retrieval (Anthropic pattern) |
| `ChunkRelationship` | Discovers relationships between chunks |

---

## ITextGenerationService Interface

FluxImprover's core abstraction is `ITextGenerationService`. The built-in providers implement this interface. To use a custom LLM provider, implement this interface and register it with the DI container (see Quick Start Option C).

```csharp
public interface ITextGenerationService
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

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                       FluxImproverBuilder                           │
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │               ITextGenerationService                             ││
│  │   (Built-in: OpenAICompatibleCompletionService,                  ││
│  │    LMSupplyCompletionService — or custom implementation)         ││
│  │   ┌────────────────────────────────────────────────────────┐    ││
│  │   │  OpenAI, Azure, Ollama, LMSupply local models, etc.    │    ││
│  │   └────────────────────────────────────────────────────────┘    ││
│  └─────────────────────────────────────────────────────────────────┘│
│         │                    │                    │                 │
│  ┌──────▼──────┐     ┌──────▼──────┐     ┌──────▼──────┐           │
│  │ Enrichment  │     │ Evaluation  │     │    QA       │           │
│  │  Services   │     │  Metrics    │     │ Generation  │           │
│  └─────────────┘     └─────────────┘     └─────────────┘           │
│                                                                     │
│  ┌────────────────────┐  ┌─────────────────┐  ┌───────────────────┐│
│  │   Chunk Filtering  │  │ Query Preproc.  │  │ Question Suggest. ││
│  │  (3-Stage Assess.) │  │ (Expand/Intent) │  │                   ││
│  └────────────────────┘  └─────────────────┘  └───────────────────┘│
│                                                                     │
│  ┌────────────────────┐  ┌─────────────────┐                       │
│  │ Contextual Enrich. │  │ Chunk Relations │                       │
│  │ (Anthropic pattern)│  │   Discovery     │                       │
│  └────────────────────┘  └─────────────────┘                       │
└─────────────────────────────────────────────────────────────────────┘
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

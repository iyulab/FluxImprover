# FluxImprover Architecture

> **The Quality Layer for RAG Data Pipelines**
> LLM-powered chunk quality enhancement and evaluation library

---

## 1. Core Values

### Quality First
RAG system performance directly depends on data quality. FluxImprover acts as a **gateway** ensuring chunk data quality before indexing.

### Minimal Dependencies
- Core library depends only on:
  - `Microsoft.Extensions.DependencyInjection.Abstractions`
  - `Microsoft.Extensions.Options`
  - `Polly.Core` (for resilience)
- No LLM SDK dependencies - consumers provide their own `ITextCompletionService` implementation

### Self-Contained
- All prompt templates embedded as resources
- No external configuration files required
- Works independently without DI container

### Flexible Extension
- Interface-based design (DIP principle)
- Connect to any LLM provider
- Consumer application provides service implementations

---

## 2. Role & Scope

### Position in Data Flow
```
┌──────────────┐     ┌───────────────┐     ┌────────────┐
│  FileFlux    │     │               │     │            │
│  WebFlux     │────▶│ FluxImprover  │────▶│ FluxIndex  │
│  (Chunking)  │     │ (Quality)     │     │ (Indexing) │
└──────────────┘     └───────────────┘     └────────────┘
```

### In Scope
| Feature | Description |
|---------|-------------|
| **QA Generation** | Generate Question-Answer pairs for RAG evaluation |
| **Quality Evaluation** | Faithfulness, Relevancy, Answerability metrics |
| **Chunk Enrichment** | Add summaries, keywords, metadata |
| **Chunk Filtering** | 3-stage assessment with self-reflection |
| **Query Preprocessing** | Normalize, expand, classify queries |
| **Question Suggestion** | Generate follow-up questions from context |
| **Contextual Enrichment** | Document-level context (Anthropic pattern) |
| **Relationship Discovery** | Discover chunk relationships |

### Out of Scope
| Feature | Responsibility |
|---------|----------------|
| Document Chunking | FileFlux, WebFlux |
| Embedding Generation | FluxIndex or consumer app |
| Vector Search | FluxIndex |
| LLM API Calls | Consumer app (interface implementation) |

---

## 3. Architecture Overview

### Layer Structure
```
┌─────────────────────────────────────────────────────────┐
│                  Consumer Application                    │
│         (OpenAI, Azure AI, Anthropic, etc.)             │
└─────────────────────────┬───────────────────────────────┘
                          │ ITextCompletionService
┌─────────────────────────▼───────────────────────────────┐
│                    FluxImprover                          │
│  ┌────────────┬────────────┬────────────┬────────────┐  │
│  │    QA      │  Quality   │   Chunk    │  Question  │  │
│  │ Generation │ Evaluation │ Enrichment │ Suggestion │  │
│  └────────────┴────────────┴────────────┴────────────┘  │
│  ┌────────────┬────────────┬────────────┬────────────┐  │
│  │   Chunk    │   Query    │ Contextual │   Chunk    │  │
│  │ Filtering  │ Preprocess │ Enrichment │ Relations  │  │
│  └────────────┴────────────┴────────────┴────────────┘  │
│  ┌──────────────────────────────────────────────────┐   │
│  │           Prompts & Templates (Embedded)          │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

### Project Structure
```
FluxImprover/
├── src/
│   └── FluxImprover/
│       ├── FluxImprover.csproj
│       ├── FluxImproverBuilder.cs          # Service builder
│       ├── ServiceCollectionExtensions.cs  # DI extensions
│       │
│       ├── Services/                       # Core abstractions
│       │   ├── ITextCompletionService.cs
│       │   ├── IEmbeddingService.cs
│       │   ├── IRerankService.cs
│       │   └── ITokenizer.cs
│       │
│       ├── Models/                         # Domain models
│       │   ├── Chunk.cs
│       │   ├── IEnrichedChunk.cs
│       │   ├── QAPair.cs
│       │   ├── ChunkAssessment.cs
│       │   ├── PreprocessedQuery.cs
│       │   └── ...
│       │
│       ├── Options/                        # Configuration
│       │   ├── EnrichmentOptions.cs
│       │   ├── QAGenerationOptions.cs
│       │   ├── ChunkFilteringOptions.cs
│       │   └── ...
│       │
│       ├── Enrichment/                     # Enrichment services
│       │   ├── SummarizationService.cs
│       │   ├── KeywordExtractionService.cs
│       │   └── ChunkEnrichmentService.cs
│       │
│       ├── Evaluation/                     # Quality evaluators
│       │   ├── FaithfulnessEvaluator.cs
│       │   ├── RelevancyEvaluator.cs
│       │   ├── AnswerabilityEvaluator.cs
│       │   └── MetricResult.cs
│       │
│       ├── QAGeneration/                   # QA generation
│       │   ├── QAGeneratorService.cs
│       │   ├── QAFilterService.cs
│       │   └── QAPipeline.cs
│       │
│       ├── ChunkFiltering/                 # 3-stage filtering
│       │   ├── IChunkFilteringService.cs
│       │   └── ChunkFilteringService.cs
│       │
│       ├── QueryPreprocessing/             # Query optimization
│       │   ├── IQueryPreprocessingService.cs
│       │   └── QueryPreprocessingService.cs
│       │
│       ├── ContextualRetrieval/            # Anthropic pattern
│       │   ├── IContextualEnrichmentService.cs
│       │   └── ContextualEnrichmentService.cs
│       │
│       ├── RelationshipDiscovery/          # Chunk relationships
│       │   ├── IChunkRelationshipService.cs
│       │   └── ChunkRelationshipService.cs
│       │
│       ├── QuestionSuggestion/             # Follow-up questions
│       │   └── QuestionSuggestionService.cs
│       │
│       ├── Prompts/                        # Prompt system
│       │   ├── EmbeddedPrompts.cs
│       │   ├── PromptTemplate.cs
│       │   ├── PromptBuilder.cs
│       │   └── Templates/*.txt
│       │
│       └── Utilities/                      # Helpers
│           ├── JsonHelpers.cs
│           └── StringExtensions.cs
│
├── tests/
│   └── FluxImprover.Tests/
│
├── samples/
│   └── FluxImprover.ConsoleDemo/
│
└── docs/
    ├── API.md
    └── ARCHITECTURE.md
```

---

## 4. Core Interface

### ITextCompletionService (Consumer Implements)

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

---

## 5. Service Categories

### Enrichment Services
| Service | Purpose |
|---------|---------|
| `SummarizationService` | Generate concise summaries |
| `KeywordExtractionService` | Extract relevant keywords |
| `ChunkEnrichmentService` | Combine summary + keywords |

### Evaluation Services
| Service | Purpose |
|---------|---------|
| `FaithfulnessEvaluator` | Is answer grounded in context? |
| `RelevancyEvaluator` | Does answer address the question? |
| `AnswerabilityEvaluator` | Can question be answered from context? |

### QA Generation Services
| Service | Purpose |
|---------|---------|
| `QAGeneratorService` | Generate QA pairs from content |
| `QAFilterService` | Filter by quality thresholds |
| `QAPipeline` | End-to-end generation + filtering |

### Filtering & Preprocessing
| Service | Purpose |
|---------|---------|
| `ChunkFilteringService` | 3-stage chunk assessment |
| `QueryPreprocessingService` | Normalize, expand, classify queries |

### Advanced Services
| Service | Purpose |
|---------|---------|
| `ContextualEnrichmentService` | Document-level context (Anthropic pattern) |
| `ChunkRelationshipService` | Discover chunk relationships |
| `QuestionSuggestionService` | Generate follow-up questions |

---

## 6. Chunk Filtering Pipeline

3-stage assessment with self-reflection and critic validation:

```
┌─────────────────────────────────────────────────────────────┐
│                  Chunk Filtering Pipeline                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐   │
│  │   Stage 1    │───▶│   Stage 2    │───▶│   Stage 3    │   │
│  │   Initial    │    │    Self      │    │   Critic     │   │
│  │  Assessment  │    │  Reflection  │    │  Validation  │   │
│  └──────────────┘    └──────────────┘    └──────────────┘   │
│         │                   │                   │            │
│         ▼                   ▼                   ▼            │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐   │
│  │ Evaluate     │    │ Review own   │    │ Independent  │   │
│  │ quality &    │    │ assessment   │    │ validation   │   │
│  │ relevance    │    │ for errors   │    │ of scores    │   │
│  └──────────────┘    └──────────────┘    └──────────────┘   │
│                                                              │
│                      Final Score & Decision                  │
└─────────────────────────────────────────────────────────────┘
```

---

## 7. Prompt Template System

### Embedded Resources
All prompts are embedded in `src/FluxImprover/Prompts/Templates/*.txt`:

- `Summarization.txt`
- `KeywordExtraction.txt`
- `QAGeneration.txt`
- `FaithfulnessEvaluation.txt`
- `RelevancyEvaluation.txt`
- `AnswerabilityEvaluation.txt`
- `ContextualEnrichment.txt`
- `QuestionSuggestion.txt`

### PromptTemplate Usage

```csharp
var template = EmbeddedPrompts.Get("Summarization");
var prompt = template.Render(new Dictionary<string, string>
{
    ["content"] = chunkContent,
    ["maxLength"] = "200"
});
```

---

## 8. Usage Patterns

### Builder Pattern

```csharp
// Create services
var services = new FluxImproverBuilder()
    .WithCompletionService(myCompletionService)
    .Build();

// Use services
var enriched = await services.ChunkEnrichment.EnrichAsync(chunk);
var score = await services.Faithfulness.EvaluateAsync(context, answer);
```

### Dependency Injection

```csharp
// Register
services.AddSingleton<ITextCompletionService, MyCompletionService>();
services.AddFluxImprover();

// Inject
public class MyService(ChunkEnrichmentService enrichment)
{
    public Task ProcessAsync(Chunk chunk)
        => enrichment.EnrichAsync(chunk);
}
```

---

## 9. Dependencies

### Allowed
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  <PackageReference Include="Microsoft.Extensions.Options" />
  <PackageReference Include="Polly.Core" />
</ItemGroup>
```

### Not Allowed
- LLM SDK packages (OpenAI, Azure.AI, etc.)
- JSON libraries (use System.Text.Json)
- Any other external NuGet packages

---

## 10. Version History

| Version | Changes |
|---------|---------|
| 0.1.0 | Initial release with core services |
| 0.2.0 | Added QA generation and evaluation |
| 0.3.0 | Added chunk filtering and query preprocessing |
| 0.4.0 | Added contextual enrichment and relationships |
| 0.5.0 | Added built-in LMSupply integration |
| 0.6.0 | Removed LMSupply dependency - pure abstraction model |

---

## 11. Resources

- **GitHub Repository**: https://github.com/iyulab/FluxImprover
- **NuGet Package**: https://www.nuget.org/packages/FluxImprover
- **API Reference**: [docs/API.md](API.md)
- **Sample Project**: [samples/FluxImprover.ConsoleDemo](../samples/FluxImprover.ConsoleDemo)

---

*Document created: 2025-01-19*
*Target framework: .NET 10*

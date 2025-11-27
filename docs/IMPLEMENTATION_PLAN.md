# FluxImprover 구현 계획서

> **TDD 기반 다단계 구현 계획**
> 총 7 Phase, ~35 Tasks, ~200 Tests

---

## 1. 종속성 정책

### 1.1 허용 종속성

| 패키지 | 버전 | 용도 | 레이어 |
|--------|------|------|--------|
| **Core (최소)** ||||
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 9.0+ | DI 추상화 | Abstractions |
| `Microsoft.Extensions.Options` | 9.0+ | 옵션 패턴 | Abstractions |
| `System.Text.Json` | BCL | JSON 직렬화 | All |
| **선택적** ||||
| `Polly.Core` | 8.0+ | 재시도/서킷브레이커 | Core |
| **테스트 전용** ||||
| `xunit` | 2.9+ | 테스트 프레임워크 | Tests |
| `xunit.runner.visualstudio` | 2.8+ | VS 통합 | Tests |
| `FluentAssertions` | 7.0+ | 가독성 어설션 | Tests |
| `NSubstitute` | 5.3+ | Mock 프레임워크 | Tests |
| `Microsoft.NET.Test.Sdk` | 17.12+ | 테스트 SDK | Tests |
| `coverlet.collector` | 6.0+ | 커버리지 수집 | Tests |

### 1.2 배제 종속성

| 패키지 | 사유 |
|--------|------|
| ❌ Microsoft.SemanticKernel | 과도한 종속성 체인 |
| ❌ Newtonsoft.Json | System.Text.Json으로 충분 |
| ❌ AutoMapper | 수동 매핑으로 명시성 확보 |
| ❌ MediatR | 단순 파이프라인으로 대체 |

---

## 2. TDD 원칙

### 2.1 Red-Green-Refactor 사이클

```
┌─────────────────────────────────────────────────────┐
│                    TDD Cycle                         │
├─────────────────────────────────────────────────────┤
│                                                      │
│    ┌───────┐     ┌───────┐     ┌──────────┐        │
│    │  RED  │────▶│ GREEN │────▶│ REFACTOR │        │
│    │       │     │       │     │          │        │
│    │ Write │     │ Write │     │ Improve  │        │
│    │ Test  │     │ Code  │     │ Code     │        │
│    │ First │     │ Pass  │     │ Quality  │        │
│    └───────┘     └───────┘     └──────────┘        │
│        ▲                            │               │
│        └────────────────────────────┘               │
│                                                      │
└─────────────────────────────────────────────────────┘
```

### 2.2 테스트 명명 규칙

```csharp
// 패턴: [Method]_[Scenario]_[ExpectedBehavior]
[Fact]
public async Task CompleteAsync_WithValidPrompt_ReturnsCompletion()

[Theory]
[InlineData("", false)]
[InlineData("valid", true)]
public void Validate_WithInput_ReturnsExpected(string input, bool expected)
```

### 2.3 품질 게이트

| 게이트 | 기준 | 시점 |
|--------|------|------|
| Task 완료 | 관련 테스트 100% 통과 | PR 전 |
| Phase 완료 | 커버리지 80% 이상 | Phase 종료 |
| 릴리스 | 커버리지 85% 이상 + 0 실패 | 릴리스 전 |

---

## 3. Phase 상세

### Phase 1: Foundation (기반 구축)

**목표**: 프로젝트 구조 + 인터페이스 + 모델
**예상 소요**: 3일
**테스트 수**: ~25개

#### Task 1.1: 솔루션 구조 생성

```
FluxImprover/
├── FluxImprover.sln
├── global.json                    # .NET 10 SDK 지정
├── Directory.Build.props          # 공통 빌드 속성
├── Directory.Packages.props       # 중앙 패키지 관리
├── src/
│   ├── FluxImprover.Abstractions/
│   │   └── FluxImprover.Abstractions.csproj
│   └── FluxImprover/
│       └── FluxImprover.csproj
├── tests/
│   ├── FluxImprover.Abstractions.Tests/
│   │   └── FluxImprover.Abstractions.Tests.csproj
│   └── FluxImprover.Tests/
│       └── FluxImprover.Tests.csproj
└── docs/
```

**산출물**:
- [ ] FluxImprover.sln
- [ ] Directory.Build.props
- [ ] Directory.Packages.props
- [ ] .editorconfig

#### Task 1.2: 서비스 인터페이스 정의

**테스트 먼저**:
```csharp
// ITextCompletionServiceTests.cs
public class ITextCompletionServiceTests
{
    [Fact]
    public async Task CompleteAsync_WithValidPrompt_ReturnsNonEmptyString()
    {
        // Arrange
        var service = Substitute.For<ITextCompletionService>();
        service.CompleteAsync(Arg.Any<string>(), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns("Generated response");

        // Act
        var result = await service.CompleteAsync("Test prompt");

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CompleteAsync_WithCancellation_ThrowsOperationCanceled()
    {
        // Arrange
        var service = Substitute.For<ITextCompletionService>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        service.CompleteAsync(Arg.Any<string>(), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await service.Invoking(s => s.CompleteAsync("prompt", null, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }
}
```

**구현**:
```csharp
// ITextCompletionService.cs
namespace FluxImprover.Abstractions.Services;

public interface ITextCompletionService
{
    Task<string> CompleteAsync(
        string prompt,
        CompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        CompletionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default);
}

public sealed record CompletionOptions
{
    public float Temperature { get; init; } = 0.7f;
    public int? MaxTokens { get; init; }
    public string? SystemPrompt { get; init; }
    public IReadOnlyList<ChatMessage>? Messages { get; init; }
}

public sealed record ChatMessage(string Role, string Content);
```

**산출물**:
- [ ] ITextCompletionService.cs
- [ ] IEmbeddingService.cs
- [ ] IRerankService.cs
- [ ] ITokenizer.cs
- [ ] CompletionOptions.cs

#### Task 1.3: 핵심 모델 정의

**테스트 먼저**:
```csharp
// QAPairTests.cs
public class QAPairTests
{
    [Fact]
    public void QAPair_Serialization_RoundTrips()
    {
        // Arrange
        var original = new QAPair
        {
            Id = "qa-001",
            Question = "What is RAG?",
            Answer = "Retrieval Augmented Generation",
            Contexts = [new ContextReference("chunk-1", "RAG is...", IsGold: true)],
            Classification = new QAClassification(QuestionType.Factual, Difficulty.Medium)
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<QAPair>(json);

        // Assert
        deserialized.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void QAPair_WithEmptyQuestion_ThrowsValidationException()
    {
        // Act & Assert
        var act = () => new QAPair { Id = "1", Question = "", Answer = "ans" };
        act.Should().Throw<ArgumentException>();
    }
}
```

**구현**:
```csharp
// QAPair.cs
namespace FluxImprover.Abstractions.Models;

public sealed record QAPair
{
    public required string Id { get; init; }

    private string _question = string.Empty;
    public required string Question
    {
        get => _question;
        init => _question = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Question cannot be empty")
            : value;
    }

    public required string Answer { get; init; }
    public IReadOnlyList<ContextReference> Contexts { get; init; } = [];
    public IReadOnlyList<SupportingFact>? SupportingFacts { get; init; }
    public QAClassification? Classification { get; init; }
    public double? FaithfulnessScore { get; init; }
}

public sealed record ContextReference(
    string ChunkId,
    string Text,
    bool IsGold = false,
    string? SourceDocument = null
);

public sealed record QAClassification(
    QuestionType Type,
    Difficulty Difficulty,
    int RequiredContextCount = 1
);

public enum QuestionType { Factual, Reasoning, Comparative, MultiHop, Conditional }
public enum Difficulty { Easy, Medium, Hard }
```

**산출물**:
- [ ] IEnrichedChunk.cs
- [ ] QAPair.cs
- [ ] QADataset.cs
- [ ] EvaluationResult.cs
- [ ] SuggestedQuestion.cs
- [ ] ContextReference.cs

#### Task 1.4: 옵션 클래스 정의

**테스트 먼저**:
```csharp
// QAGenerationOptionsTests.cs
public class QAGenerationOptionsTests
{
    [Fact]
    public void DefaultOptions_HasValidDefaults()
    {
        var options = new QAGenerationOptions();

        options.PairsPerChunk.Should().Be(3);
        options.Temperature.Should().BeInRange(0f, 1f);
        options.EnableEvolution.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PairsPerChunk_WithInvalidValue_ThrowsValidation(int invalid)
    {
        var act = () => new QAGenerationOptions { PairsPerChunk = invalid };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
```

**산출물**:
- [ ] QAGenerationOptions.cs
- [ ] EvaluationOptions.cs
- [ ] EnrichmentOptions.cs
- [ ] QuestionSuggestionOptions.cs

#### Task 1.5: 공통 유틸리티

**테스트 먼저**:
```csharp
// ResultTests.cs
public class ResultTests
{
    [Fact]
    public void Success_ContainsValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_ContainsError()
    {
        var result = Result<int>.Failure("Error occurred");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Error occurred");
    }

    [Fact]
    public void Match_ExecutesCorrectBranch()
    {
        var success = Result<int>.Success(10);
        var failure = Result<int>.Failure("err");

        var successResult = success.Match(v => v * 2, e => 0);
        var failureResult = failure.Match(v => v * 2, e => -1);

        successResult.Should().Be(20);
        failureResult.Should().Be(-1);
    }
}
```

**구현**:
```csharp
// Result.cs
namespace FluxImprover.Abstractions;

public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}
```

**산출물**:
- [ ] Result.cs
- [ ] Guard.cs
- [ ] FluxImproverException.cs

---

### Phase 2: Prompts & Pipeline (프롬프트 시스템)

**목표**: 재사용 가능한 프롬프트 템플릿 + 파이프라인 기반
**예상 소요**: 4일
**테스트 수**: ~35개

#### Task 2.1: 프롬프트 템플릿 엔진

**테스트**:
```csharp
public class PromptTemplateTests
{
    [Fact]
    public void Render_WithVariables_ReplacesPlaceholders()
    {
        var template = new PromptTemplate("Hello, {{name}}!");
        var result = template.Render(new { name = "World" });
        result.Should().Be("Hello, World!");
    }

    [Fact]
    public void Render_WithConditional_IncludesWhenTrue()
    {
        var template = new PromptTemplate("{{#if includeDetail}}Details here{{/if}}");
        var result = template.Render(new { includeDetail = true });
        result.Should().Contain("Details here");
    }

    [Fact]
    public void Render_WithEach_IteratesCollection()
    {
        var template = new PromptTemplate("Items: {{#each items}}{{this}}, {{/each}}");
        var result = template.Render(new { items = new[] { "a", "b", "c" } });
        result.Should().Contain("a,").And.Contain("b,").And.Contain("c,");
    }
}
```

**산출물**:
- [ ] PromptTemplate.cs
- [ ] PromptRenderer.cs

#### Task 2.2: 프롬프트 빌더

**테스트**:
```csharp
public class PromptBuilderTests
{
    [Fact]
    public void Build_WithSystemAndUser_CreatesCorrectStructure()
    {
        var builder = new PromptBuilder()
            .WithSystemPrompt("You are a helpful assistant.")
            .WithUserPrompt("Generate a question about: {{context}}")
            .WithVariable("context", "RAG systems");

        var prompt = builder.Build();

        prompt.SystemPrompt.Should().Contain("helpful assistant");
        prompt.UserPrompt.Should().Contain("RAG systems");
    }

    [Fact]
    public void Build_WithFewShot_IncludesExamples()
    {
        var builder = new PromptBuilder()
            .WithFewShotExample("Q: What is AI?", "AI is artificial intelligence.")
            .WithFewShotExample("Q: What is ML?", "ML is machine learning.");

        var prompt = builder.Build();

        prompt.Examples.Should().HaveCount(2);
    }
}
```

**산출물**:
- [ ] PromptBuilder.cs
- [ ] BuiltPrompt.cs

#### Task 2.3: 내장 프롬프트 템플릿

**구조**:
```csharp
// Templates/QAGenerationPrompts.cs
namespace FluxImprover.Prompts.Templates;

public static class QAGenerationPrompts
{
    public static string SeedQuestion => """
        주어진 컨텍스트에서 완전히 답변할 수 있는 {{questionType}} 질문을 생성하세요.

        <context>
        {{context}}
        </context>

        요구사항:
        - 질문은 컨텍스트만으로 답변 가능해야 함
        - 질문은 15단어 이하로 간결하게
        - "제공된 컨텍스트에 따르면" 등의 표현 금지

        출력 (JSON만):
        {"question": "생성된 질문"}
        """;

    public static string ReasoningEvolution => """
        다음 질문을 다단계 추론이 필요한 질문으로 복잡화하세요.

        원본 질문: {{originalQuestion}}
        컨텍스트: {{context}}

        출력: {"evolved_question": "복잡화된 질문", "reasoning_steps": ["단계1", "단계2"]}
        """;
}
```

**산출물**:
- [ ] Templates/QAGenerationPrompts.cs
- [ ] Templates/EvaluationPrompts.cs
- [ ] Templates/EnrichmentPrompts.cs
- [ ] Templates/SuggestionPrompts.cs

#### Task 2.4: 파이프라인 기반

**테스트**:
```csharp
public class PipelineTests
{
    [Fact]
    public async Task ExecuteAsync_WithSteps_ExecutesInOrder()
    {
        var executionOrder = new List<string>();

        var pipeline = new Pipeline<string, string>()
            .AddStep("Step1", async (input, ct) =>
            {
                executionOrder.Add("Step1");
                return input + "-1";
            })
            .AddStep("Step2", async (input, ct) =>
            {
                executionOrder.Add("Step2");
                return input + "-2";
            });

        var result = await pipeline.ExecuteAsync("Start");

        executionOrder.Should().ContainInOrder("Step1", "Step2");
        result.Should().Be("Start-1-2");
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingStep_StopsExecution()
    {
        var pipeline = new Pipeline<string, string>()
            .AddStep("Failing", (_, _) => throw new InvalidOperationException("Test error"));

        var act = () => pipeline.ExecuteAsync("input");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
```

**산출물**:
- [ ] Pipeline.cs
- [ ] PipelineStep.cs
- [ ] PipelineContext.cs

#### Task 2.5: JSON 응답 파서

**테스트**:
```csharp
public class LLMResponseParserTests
{
    [Fact]
    public void Parse_WithValidJson_ReturnsObject()
    {
        var json = """{"question": "What is RAG?"}""";
        var result = LLMResponseParser.Parse<QuestionOutput>(json);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Question.Should().Be("What is RAG?");
    }

    [Fact]
    public void Parse_WithMarkdownCodeBlock_ExtractsJson()
    {
        var response = """
            Here's the output:
            ```json
            {"question": "What is RAG?"}
            ```
            """;
        var result = LLMResponseParser.Parse<QuestionOutput>(response);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Parse_WithInvalidJson_ReturnsFailure()
    {
        var invalid = "not json at all";
        var result = LLMResponseParser.Parse<QuestionOutput>(invalid);
        result.IsSuccess.Should().BeFalse();
    }
}
```

**산출물**:
- [ ] LLMResponseParser.cs

---

### Phase 3: Enrichment (청크 강화)

**목표**: 요약/키워드 추출 기능
**예상 소요**: 3일
**테스트 수**: ~25개

#### Task 3.1: KeywordExtractor

**테스트**:
```csharp
public class KeywordExtractorTests
{
    [Fact]
    public async Task ExtractAsync_WithValidChunk_ReturnsKeywords()
    {
        // Arrange
        var mockLLM = Substitute.For<ITextCompletionService>();
        mockLLM.CompleteAsync(Arg.Any<string>(), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns("""{"keywords": ["RAG", "LLM", "retrieval"]}""");

        var extractor = new KeywordExtractor(mockLLM);
        var chunk = new TestChunk("RAG combines retrieval with LLM generation.");

        // Act
        var result = await extractor.ExtractAsync(chunk, new EnrichmentOptions { KeywordCount = 3 });

        // Assert
        result.Keywords.Should().HaveCount(3);
        result.Keywords.Should().Contain("RAG");
    }

    [Fact]
    public async Task ExtractAsync_WithMaxKeywords_RespectsLimit()
    {
        // ... 키워드 개수 제한 테스트
    }
}
```

**산출물**:
- [ ] KeywordExtractor.cs

#### Task 3.2: Summarizer

**산출물**:
- [ ] Summarizer.cs

#### Task 3.3: ChunkEnricher 통합

**테스트**:
```csharp
public class ChunkEnricherTests
{
    [Fact]
    public async Task EnrichAsync_WithAllOptions_EnrichesChunk()
    {
        // Arrange
        var enricher = CreateEnricherWithMocks();
        var chunk = new TestChunk("Original content");
        var options = new EnrichmentOptions
        {
            GenerateSummary = true,
            ExtractKeywords = true
        };

        // Act
        var result = await enricher.EnrichAsync(chunk, options);

        // Assert
        result.Summary.Should().NotBeNullOrEmpty();
        result.Keywords.Should().NotBeEmpty();
    }

    [Fact]
    public async Task EnrichBatchAsync_WithMultipleChunks_ProcessesAll()
    {
        // Arrange
        var enricher = CreateEnricherWithMocks();
        var chunks = Enumerable.Range(1, 5).Select(i => new TestChunk($"Content {i}"));

        // Act
        var results = await enricher.EnrichBatchAsync(chunks).ToListAsync();

        // Assert
        results.Should().HaveCount(5);
    }
}
```

**산출물**:
- [ ] ChunkEnricher.cs
- [ ] EnrichedChunkResult.cs

---

### Phase 4: Evaluation (품질 평가)

**목표**: Faithfulness, Relevancy, Answerability 평가
**예상 소요**: 4일
**테스트 수**: ~30개

#### Task 4.1: FaithfulnessMetric

**테스트**:
```csharp
public class FaithfulnessMetricTests
{
    [Fact]
    public async Task EvaluateAsync_WithGroundedAnswer_ReturnsHighScore()
    {
        // Arrange
        var mockLLM = CreateMockLLM("""
            {"claims": [
                {"claim": "RAG uses retrieval", "supported": true},
                {"claim": "RAG uses LLM", "supported": true}
            ]}
            """);

        var metric = new FaithfulnessMetric(mockLLM);
        var input = new EvaluationInput
        {
            Question = "What is RAG?",
            Answer = "RAG uses retrieval and LLM.",
            Contexts = [new("ctx1", "RAG combines retrieval with LLM generation.")]
        };

        // Act
        var score = await metric.EvaluateAsync(input);

        // Assert
        score.Should().BeGreaterOrEqualTo(0.9);
    }

    [Fact]
    public async Task EvaluateAsync_WithHallucination_ReturnsLowScore()
    {
        // 환각이 포함된 답변 → 낮은 점수
    }
}
```

**산출물**:
- [ ] Metrics/FaithfulnessMetric.cs
- [ ] Metrics/IMetric.cs

#### Task 4.2: RelevancyMetric

**산출물**:
- [ ] Metrics/RelevancyMetric.cs

#### Task 4.3: AnswerabilityMetric

**테스트**:
```csharp
public class AnswerabilityMetricTests
{
    [Theory]
    [InlineData("A", true)]   // 완전 답변 가능
    [InlineData("B", true)]   // 부분 답변 가능
    [InlineData("C", false)]  // 관련있지만 답변 불가
    [InlineData("D", false)]  // 무관
    public async Task EvaluateAsync_WithGrade_ReturnsCorrectPassFail(string grade, bool shouldPass)
    {
        var mockLLM = CreateMockLLM($$$"""{"score": "{{{grade}}}", "reasoning": "test"}""");
        var metric = new AnswerabilityMetric(mockLLM);

        var result = await metric.EvaluateAsync(new EvaluationInput { /* ... */ });

        result.Passed.Should().Be(shouldPass);
    }
}
```

**산출물**:
- [ ] Metrics/AnswerabilityMetric.cs

#### Task 4.4: QualityEvaluator 통합

**산출물**:
- [ ] QualityEvaluator.cs
- [ ] EvaluationResult.cs (확장)

---

### Phase 5: QA Generation (QA 생성)

**목표**: 핵심 QA 생성 파이프라인
**예상 소요**: 6일
**테스트 수**: ~50개

#### Task 5.1: ContextSelector (Planning)

**테스트**:
```csharp
public class ContextSelectorTests
{
    [Fact]
    public async Task SelectAsync_ForSingleHop_ReturnsSingleChunk()
    {
        var selector = new ContextSelector();
        var chunks = CreateTestChunks(10);

        var selected = await selector.SelectAsync(chunks, new ContextSelectionOptions
        {
            Strategy = SelectionStrategy.SingleHop,
            MaxContexts = 1
        });

        selected.Should().HaveCount(1);
    }

    [Fact]
    public async Task SelectAsync_ForMultiHop_ReturnsMultipleRelatedChunks()
    {
        // Multi-hop 질문을 위한 관련 청크 2-3개 선택
    }
}
```

**산출물**:
- [ ] Planning/ContextSelector.cs
- [ ] Planning/SelectionStrategy.cs

#### Task 5.2-5.3: QuestionGenerator & AnswerGenerator

**산출물**:
- [ ] Synthesis/QuestionGenerator.cs
- [ ] Synthesis/AnswerGenerator.cs

#### Task 5.4: QuestionEvolver

**테스트**:
```csharp
public class QuestionEvolverTests
{
    [Fact]
    public async Task EvolveAsync_ToReasoning_AddsComplexity()
    {
        var evolver = CreateEvolver();
        var simple = new QAPair { Question = "What is RAG?", /* ... */ };

        var evolved = await evolver.EvolveAsync(simple, EvolutionType.Reasoning);

        evolved.Classification!.Type.Should().Be(QuestionType.Reasoning);
        evolved.Question.Should().NotBe(simple.Question);
    }

    [Theory]
    [InlineData(EvolutionType.Reasoning)]
    [InlineData(EvolutionType.MultiContext)]
    [InlineData(EvolutionType.Conditional)]
    public async Task EvolveAsync_WithType_PreservesAnswerability(EvolutionType type)
    {
        // 진화 후에도 답변 가능성 유지
    }
}
```

**산출물**:
- [ ] Synthesis/QuestionEvolver.cs
- [ ] Synthesis/EvolutionType.cs

#### Task 5.5: Validators

**산출물**:
- [ ] Validation/AnswerabilityValidator.cs
- [ ] Validation/FaithfulnessValidator.cs
- [ ] Validation/IValidator.cs

#### Task 5.6: BenchmarkGenerator 통합

**테스트**:
```csharp
public class BenchmarkGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_WithChunks_ReturnsQADataset()
    {
        var generator = CreateGeneratorWithMocks();
        var chunks = CreateTestChunks(5);

        var dataset = await generator.GenerateAsync(chunks, new QAGenerationOptions
        {
            PairsPerChunk = 2,
            EnableEvolution = true,
            EnableValidation = true
        });

        dataset.Samples.Should().HaveCountGreaterOrEqualTo(5); // 일부 필터링됨
        dataset.Samples.Should().OnlyContain(qa => qa.FaithfulnessScore >= 0.7);
    }

    [Fact]
    public async Task GenerateStreamingAsync_ReportsProgress()
    {
        var generator = CreateGeneratorWithMocks();
        var progress = new List<QAPair>();

        await foreach (var qa in generator.GenerateStreamingAsync(CreateTestChunks(3)))
        {
            progress.Add(qa);
        }

        progress.Should().NotBeEmpty();
    }
}
```

**산출물**:
- [ ] BenchmarkGenerator.cs

---

### Phase 6: Question Suggestion (질문 추천)

**목표**: 대화형 AI 후속 질문 생성
**예상 소요**: 2일
**테스트 수**: ~15개

#### Task 6.1-6.2: QuestionSuggester

**테스트**:
```csharp
public class QuestionSuggesterTests
{
    [Fact]
    public async Task SuggestAsync_WithConversation_ReturnsSuggestions()
    {
        var suggester = CreateSuggesterWithMock();

        var suggestions = await suggester.SuggestAsync(
            conversationContext: "User: What is RAG?\nAI: RAG is...",
            currentAnswer: "RAG combines retrieval with generation.",
            new QuestionSuggestionOptions { Count = 3 }
        );

        suggestions.Should().HaveCount(3);
        suggestions.Should().OnlyContain(s => !string.IsNullOrEmpty(s.Text));
    }

    [Fact]
    public async Task SuggestAsync_AvoidsDuplicates()
    {
        // 중복 질문 제거 검증
    }
}
```

**산출물**:
- [ ] QuestionSuggestion/QuestionSuggester.cs
- [ ] QuestionSuggestion/SuggestedQuestion.cs

---

### Phase 7: Integration & Polish (통합 및 마무리)

**목표**: DI 통합, Export, 문서화
**예상 소요**: 3일
**테스트 수**: ~20개

#### Task 7.1: DI 확장 메서드

**테스트**:
```csharp
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFluxImprover_RegistersAllServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<ITextCompletionService>());

        services.AddFluxImprover();

        var provider = services.BuildServiceProvider();
        provider.GetService<IBenchmarkGenerator>().Should().NotBeNull();
        provider.GetService<IQualityEvaluator>().Should().NotBeNull();
        provider.GetService<IChunkEnricher>().Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithOptions_AppliesConfiguration()
    {
        var services = new ServiceCollection();
        services.AddFluxImprover(options =>
        {
            options.DefaultPairsPerChunk = 5;
            options.EnableValidation = false;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<FluxImproverOptions>>();

        options.Value.DefaultPairsPerChunk.Should().Be(5);
    }
}
```

**산출물**:
- [ ] Extensions/ServiceCollectionExtensions.cs
- [ ] FluxImproverOptions.cs

#### Task 7.2: Export 기능

**테스트**:
```csharp
public class DatasetExporterTests
{
    [Fact]
    public async Task ExportToJsonAsync_CreatesValidJson()
    {
        var dataset = CreateTestDataset();
        var exporter = new DatasetExporter();

        using var stream = new MemoryStream();
        await exporter.ExportToJsonAsync(dataset, stream);

        stream.Position = 0;
        var json = await JsonDocument.ParseAsync(stream);
        json.RootElement.GetProperty("samples").GetArrayLength().Should().Be(dataset.Samples.Count);
    }

    [Fact]
    public async Task ExportToRagasFormat_CreatesCompatibleOutput()
    {
        // RAGAS EvaluationDataset 호환 형식
    }
}
```

**산출물**:
- [ ] Export/DatasetExporter.cs
- [ ] Export/RagasFormatConverter.cs
- [ ] Export/SQuADFormatConverter.cs

#### Task 7.3: 통합 테스트

**산출물**:
- [ ] IntegrationTests/EndToEndTests.cs
- [ ] IntegrationTests/MockTextCompletionService.cs

#### Task 7.4: 문서화

**산출물**:
- [ ] README.md 업데이트
- [ ] API 문서 (XML Comments)
- [ ] 사용 예제

---

## 4. 일정 요약

```
┌─────────────────────────────────────────────────────────────────┐
│                    Implementation Timeline                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Phase 1: Foundation          ████████░░░░░░░░░░░░░░  3일       │
│  Phase 2: Prompts & Pipeline  ░░░░░░░░████████████░░░  4일       │
│  Phase 3: Enrichment          ░░░░░░░░░░░░░░░░████████  3일       │
│  Phase 4: Evaluation          ░░░░░░░░░░░░░░░░░░░░████  4일       │
│  Phase 5: QA Generation       ░░░░░░░░░░░░░░░░░░░░░░░░  6일       │
│  Phase 6: Question Suggestion ░░░░░░░░░░░░░░░░░░░░░░░░  2일       │
│  Phase 7: Integration         ░░░░░░░░░░░░░░░░░░░░░░░░  3일       │
│                                                                  │
│  Total: ~25 working days (5 weeks)                              │
│  Tests: ~200 unit tests                                          │
│  Coverage Target: 85%+                                           │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 5. 품질 체크리스트

### Phase 완료 조건

- [ ] 모든 테스트 통과 (0 failures)
- [ ] 코드 커버리지 80% 이상
- [ ] 빌드 경고 0개
- [ ] XML 문서 주석 완성
- [ ] 코드 리뷰 완료

### 릴리스 조건

- [ ] 전체 테스트 통과
- [ ] 코드 커버리지 85% 이상
- [ ] 통합 테스트 통과
- [ ] README.md 완성
- [ ] NuGet 패키지 메타데이터 완성
- [ ] 샘플 프로젝트 동작 확인

---

*문서 작성일: 2025-11-27*
*예상 완료: 5주 후*

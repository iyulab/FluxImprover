# FluxImprover 아키텍처 문서

> **The Quality Layer for RAG Data Pipelines**
> LLM 기반 청크 품질 향상 및 평가 라이브러리

---

## 1. 핵심 가치 (Core Values)

### 🎯 품질 우선 (Quality First)
RAG 시스템의 성능은 데이터 품질에 직결됨. FluxImprover는 청크 데이터가 인덱싱되기 전 품질을 보장하는 **게이트웨이** 역할 수행.

### 📦 최소 종속성 (Minimal Dependencies)
- **BCL(Base Class Library)만 사용**
- 외부 NuGet 패키지 배제
- .NET 10 표준 라이브러리만 활용

### 🔒 자기 완결성 (Self-Contained)
- 모든 프롬프트 템플릿 내장
- 외부 설정 파일 불필요
- DI 컨테이너 없이 독립 실행 가능

### 🔌 유연한 확장 (Flexible Extension)
- 인터페이스 기반 설계 (DIP 원칙)
- 어떤 LLM 제공자든 연결 가능
- 소비앱이 서비스 구현체 제공

---

## 2. 역할과 범위 (Role & Scope)

### 데이터 흐름에서의 위치
```
┌──────────────┐     ┌───────────────┐     ┌────────────┐
│  FileFlux    │     │               │     │            │
│  WebFlux     │────▶│ FluxImprover  │────▶│ FluxIndex  │
│  (청크 생성)  │     │ (품질 향상)    │     │ (인덱싱)    │
└──────────────┘     └───────────────┘     └────────────┘
```

### ✅ 범위 내 (In Scope)
| 기능 | 설명 |
|------|------|
| **QA 생성** | RAG 평가용 Question-Answer 쌍 생성 |
| **품질 평가** | Faithfulness, Relevancy, Answerability 평가 |
| **청크 강화** | 요약, 키워드, 메타데이터 추가 |
| **질문 추천** | 대화 컨텍스트 기반 후속 질문 생성 |

### ❌ 범위 외 (Out of Scope)
| 기능 | 담당 |
|------|------|
| 문서 청킹 | FileFlux, WebFlux |
| 임베딩 생성/저장 | FluxIndex |
| 벡터 검색 | FluxIndex |
| LLM API 호출 | 소비앱 (인터페이스 구현) |

---

## 3. 사용 시나리오

### 3.1 RAG 평가용 QA 생성
```csharp
// 소비앱에서 ITextCompletionService 구현 제공
var generator = new BenchmarkGenerator(textCompletionService);

var dataset = await generator.GenerateAsync(chunks, new QAGenerationOptions
{
    PairsPerChunk = 3,
    QuestionTypes = [QuestionType.Factual, QuestionType.Reasoning],
    IncludeFaithfulnessScore = true
});
```

### 3.2 대화형 AI 다음 질문 추천
```csharp
var suggester = new QuestionSuggester(textCompletionService);

var suggestions = await suggester.SuggestAsync(
    conversationContext: "사용자와 AI의 이전 대화",
    currentAnswer: "현재 AI 응답",
    count: 3
);
```

### 3.3 품질 평가
```csharp
var evaluator = new QualityEvaluator(textCompletionService);

var result = await evaluator.EvaluateAsync(new EvaluationInput
{
    Question = "질문",
    Answer = "RAG 시스템 답변",
    Contexts = retrievedChunks
});
// result.Faithfulness, result.Relevancy, result.Answerability
```

### 3.4 청크 강화 (요약/키워드)
```csharp
var enricher = new ChunkEnricher(textCompletionService);

var enrichedChunk = await enricher.EnrichAsync(chunk, new EnrichmentOptions
{
    GenerateSummary = true,
    ExtractKeywords = true,
    KeywordCount = 5
});
```

---

## 4. 아키텍처 개요

### 4.1 레이어 구조
```
┌─────────────────────────────────────────────────────────┐
│                  Consumer Application                    │
│         (OpenAI, Azure AI, Anthropic 구현체 제공)         │
└─────────────────────────┬───────────────────────────────┘
                          │ ITextCompletionService
                          │ IEmbeddingService (선택)
                          │ IRerankService (선택)
┌─────────────────────────▼───────────────────────────────┐
│                    FluxImprover                          │
│  ┌────────────┬────────────┬────────────┬────────────┐  │
│  │    QA      │  Quality   │   Chunk    │  Question  │  │
│  │ Generation │ Evaluation │ Enrichment │ Suggestion │  │
│  └────────────┴────────────┴────────────┴────────────┘  │
│  ┌──────────────────────────────────────────────────┐   │
│  │              Pipeline & Prompts                   │   │
│  │         (내장 프롬프트 템플릿 시스템)               │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│              FluxImprover.Abstractions                   │
│  ┌──────────────────────────────────────────────────┐   │
│  │  Interfaces: ITextCompletionService, IEnrichedChunk│  │
│  │  Models: QAPair, EvaluationResult, EnrichedChunk  │   │
│  └──────────────────────────────────────────────────┘   │
│                    (종속성 없음)                          │
└─────────────────────────────────────────────────────────┘
```

### 4.2 프로젝트 구조
```
FluxImprover/
├── src/
│   ├── FluxImprover.Abstractions/     # 인터페이스 & 모델
│   │   ├── FluxImprover.Abstractions.csproj
│   │   ├── Services/
│   │   │   ├── ITextCompletionService.cs
│   │   │   ├── IEmbeddingService.cs
│   │   │   ├── IRerankService.cs
│   │   │   └── ITokenizer.cs
│   │   ├── Models/
│   │   │   ├── IEnrichedChunk.cs
│   │   │   ├── QAPair.cs
│   │   │   ├── QADataset.cs
│   │   │   ├── EvaluationResult.cs
│   │   │   └── SuggestedQuestion.cs
│   │   └── Options/
│   │       ├── QAGenerationOptions.cs
│   │       ├── EvaluationOptions.cs
│   │       └── EnrichmentOptions.cs
│   │
│   └── FluxImprover/                  # 핵심 구현
│       ├── FluxImprover.csproj
│       ├── QAGeneration/
│       │   ├── BenchmarkGenerator.cs
│       │   ├── Planning/
│       │   │   └── ContextSelector.cs
│       │   ├── Synthesis/
│       │   │   ├── QuestionGenerator.cs
│       │   │   ├── AnswerGenerator.cs
│       │   │   └── QuestionEvolver.cs
│       │   └── Validation/
│       │       ├── AnswerabilityValidator.cs
│       │       └── FaithfulnessValidator.cs
│       ├── Evaluation/
│       │   ├── QualityEvaluator.cs
│       │   ├── Metrics/
│       │   │   ├── FaithfulnessMetric.cs
│       │   │   ├── RelevancyMetric.cs
│       │   │   └── AnswerabilityMetric.cs
│       │   └── Judges/
│       │       └── LLMJudge.cs
│       ├── Enrichment/
│       │   ├── ChunkEnricher.cs
│       │   ├── Summarizer.cs
│       │   └── KeywordExtractor.cs
│       ├── QuestionSuggestion/
│       │   └── QuestionSuggester.cs
│       ├── Prompts/
│       │   ├── PromptTemplate.cs
│       │   ├── PromptBuilder.cs
│       │   └── Templates/
│       │       ├── QAGenerationPrompts.cs
│       │       ├── EvaluationPrompts.cs
│       │       └── EnrichmentPrompts.cs
│       └── Extensions/
│           └── ServiceCollectionExtensions.cs
│
├── tests/
│   └── FluxImprover.Tests/
│
└── docs/
    └── ARCHITECTURE.md
```

---

## 5. 핵심 인터페이스 설계

### 5.1 서비스 인터페이스 (소비앱 구현)

```csharp
namespace FluxImprover.Abstractions.Services;

/// <summary>
/// LLM 텍스트 생성 서비스 (필수)
/// </summary>
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

/// <summary>
/// 임베딩 생성 서비스 (선택)
/// </summary>
public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 재순위 서비스 (선택)
/// </summary>
public interface IRerankService
{
    Task<IReadOnlyList<RerankResult>> RerankAsync(
        string query,
        IEnumerable<string> documents,
        int topK = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 토크나이저 (선택)
/// </summary>
public interface ITokenizer
{
    int CountTokens(string text);
    IReadOnlyList<int> Encode(string text);
    string Decode(IReadOnlyList<int> tokens);
}
```

### 5.2 핵심 서비스 인터페이스

```csharp
namespace FluxImprover.Abstractions;

/// <summary>
/// RAG 평가용 QA 데이터셋 생성기
/// </summary>
public interface IBenchmarkGenerator
{
    Task<QADataset> GenerateAsync(
        IEnumerable<IEnrichedChunk> chunks,
        QAGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<QAPair> GenerateStreamingAsync(
        IEnumerable<IEnrichedChunk> chunks,
        QAGenerationOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// RAG 응답 품질 평가기
/// </summary>
public interface IQualityEvaluator
{
    Task<EvaluationResult> EvaluateAsync(
        EvaluationInput input,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 청크 메타데이터 강화기
/// </summary>
public interface IChunkEnricher
{
    Task<EnrichedChunkResult> EnrichAsync(
        IEnrichedChunk chunk,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<EnrichedChunkResult> EnrichBatchAsync(
        IEnumerable<IEnrichedChunk> chunks,
        EnrichmentOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 후속 질문 추천기
/// </summary>
public interface IQuestionSuggester
{
    Task<IReadOnlyList<SuggestedQuestion>> SuggestAsync(
        string conversationContext,
        string currentAnswer,
        QuestionSuggestionOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

---

## 6. 연구 자료 학습점 적용

### 6.1 research-01.md 핵심 학습

| 학습점 | 적용 |
|--------|------|
| **3모듈 파이프라인** | QAGeneration 내부: Planning → Synthesis → Validation |
| **골드 컨텍스트 선택** | `ContextSelector` 클래스로 구현 |
| **충실도 검증** | `FaithfulnessValidator` + LLM-as-Judge |
| **복잡도 메타데이터** | `QAPair.Classification` 속성 |

### 6.2 research-02.md 핵심 학습

| 학습점 | 적용 |
|--------|------|
| **RAGAS Evol-Instruct** | `QuestionEvolver` 클래스로 질문 복잡화 |
| **12개 프롬프트 체계** | `Prompts/Templates/` 내장 템플릿 |
| **A-D 점수 체계** | `AnswerabilityValidator` 4단계 평가 |
| **Generator-Critic 분리** | 단일 서비스 + 프롬프트로 역할 전환 |

### 6.3 QA 생성 파이프라인 (research-01 적용)

```
┌─────────────────────────────────────────────────────────────┐
│                    QA Generation Pipeline                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐   │
│  │   Planning   │───▶│  Synthesis   │───▶│  Validation  │   │
│  │   (Module A) │    │   (Module B) │    │   (Module C) │   │
│  └──────────────┘    └──────────────┘    └──────────────┘   │
│         │                   │                   │            │
│         ▼                   ▼                   ▼            │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐   │
│  │ Context      │    │ Question     │    │ Answerability│   │
│  │ Selection    │    │ Generation   │    │ Validation   │   │
│  │              │    │              │    │              │   │
│  │ Complexity   │    │ Answer       │    │ Faithfulness │   │
│  │ Planning     │    │ Generation   │    │ Validation   │   │
│  │              │    │              │    │              │   │
│  │ Entity       │    │ Evolution    │    │ Relevancy    │   │
│  │ Extraction   │    │ (Evol-Inst)  │    │ Check        │   │
│  └──────────────┘    └──────────────┘    └──────────────┘   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 7. 구현 계획

### Phase 1: 기반 구축 (Abstractions)
- [ ] 프로젝트 구조 생성
- [ ] 서비스 인터페이스 정의 (ITextCompletionService 등)
- [ ] 핵심 모델 정의 (QAPair, EvaluationResult 등)
- [ ] 옵션 클래스 정의

### Phase 2: 청크 강화 (Enrichment)
- [ ] ChunkEnricher 구현
- [ ] Summarizer 프롬프트 및 로직
- [ ] KeywordExtractor 프롬프트 및 로직

### Phase 3: 품질 평가 (Evaluation)
- [ ] QualityEvaluator 구현
- [ ] Faithfulness 메트릭 (LLM-as-Judge)
- [ ] Relevancy 메트릭
- [ ] Answerability 메트릭 (A-D 점수)

### Phase 4: QA 생성 (Generation)
- [ ] BenchmarkGenerator 구현
- [ ] Planning 모듈 (ContextSelector)
- [ ] Synthesis 모듈 (Question/Answer Generator)
- [ ] Validation 모듈 (Validators)
- [ ] QuestionEvolver (Evol-Instruct)

### Phase 5: 질문 추천 (Suggestion)
- [ ] QuestionSuggester 구현
- [ ] 대화 컨텍스트 분석 프롬프트

### Phase 6: 테스트 및 문서화
- [ ] 단위 테스트 작성
- [ ] 통합 테스트 작성
- [ ] API 문서 생성
- [ ] 사용 예제 작성

---

## 8. 종속성 정책

### 허용
```xml
<!-- FluxImprover.Abstractions.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <!-- 외부 종속성 없음 -->
  </PropertyGroup>
</Project>

<!-- FluxImprover.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\FluxImprover.Abstractions\FluxImprover.Abstractions.csproj" />
    <!-- 외부 종속성 없음 -->
  </ItemGroup>
</Project>
```

### 금지
- ❌ Microsoft.SemanticKernel
- ❌ Azure.AI.OpenAI
- ❌ Newtonsoft.Json (System.Text.Json 사용)
- ❌ 기타 모든 외부 NuGet 패키지

---

## 9. 확장 포인트

### 소비앱 구현 예시 (FluxImprover.Community)
```csharp
// OpenAI 구현체 (별도 패키지)
public class OpenAITextCompletionService : ITextCompletionService
{
    private readonly OpenAIClient _client;

    public async Task<string> CompleteAsync(string prompt, ...)
    {
        var response = await _client.GetChatCompletionsAsync(...);
        return response.Value.Choices[0].Message.Content;
    }
}

// Azure OpenAI 구현체 (별도 패키지)
public class AzureOpenAITextCompletionService : ITextCompletionService { ... }

// Anthropic 구현체 (별도 패키지)
public class AnthropicTextCompletionService : ITextCompletionService { ... }
```

---

## 10. 버전 관리

| 버전 | 내용 |
|------|------|
| 0.1.0 | Abstractions + Enrichment + Evaluation + QAGeneration + QuestionSuggestion (초기 릴리스) |
| 0.2.0 | 성능 최적화 + 버그 수정 |
| 1.0.0 | 안정화 릴리스 |

---

## 11. 추가 리소스

- **GitHub Repository**: https://github.com/iyulab/FluxImprover
- **NuGet Package**: https://www.nuget.org/packages/FluxImprover
- **API Reference**: [docs/API.md](API.md)
- **Sample Project**: [samples/FluxImprover.ConsoleDemo](../samples/FluxImprover.ConsoleDemo)

---

*문서 작성일: 2025-11-27*
*최종 수정일: 2025-11-27*
*타겟 프레임워크: .NET 10*

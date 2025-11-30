namespace FluxImprover;

using FluxImprover.ChunkFiltering;
using FluxImprover.ContextualRetrieval;
using FluxImprover.Enrichment;
using FluxImprover.Evaluation;
using FluxImprover.QAGeneration;
using FluxImprover.QueryPreprocessing;
using FluxImprover.QuestionSuggestion;
using FluxImprover.RelationshipDiscovery;
using FluxImprover.Services;

/// <summary>
/// FluxImprover 서비스 빌더 - 모든 서비스의 중앙 구성 및 생성 지점
/// </summary>
public sealed class FluxImproverBuilder
{
    private ITextCompletionService? _completionService;

    /// <summary>
    /// LLM 완성 서비스를 설정합니다.
    /// </summary>
    public FluxImproverBuilder WithCompletionService(ITextCompletionService completionService)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
        return this;
    }

    /// <summary>
    /// FluxImprover 인스턴스를 빌드합니다.
    /// </summary>
    public FluxImproverServices Build()
    {
        if (_completionService is null)
            throw new InvalidOperationException("CompletionService must be configured before building.");

        // Enrichment Services
        var summarizationService = new SummarizationService(_completionService);
        var keywordExtractionService = new KeywordExtractionService(_completionService);
        var chunkEnrichmentService = new ChunkEnrichmentService(summarizationService, keywordExtractionService);

        // Evaluation Services
        var faithfulnessEvaluator = new FaithfulnessEvaluator(_completionService);
        var relevancyEvaluator = new RelevancyEvaluator(_completionService);
        var answerabilityEvaluator = new AnswerabilityEvaluator(_completionService);

        // QA Generation Services
        var qaGenerator = new QAGeneratorService(_completionService);
        var qaFilter = new QAFilterService(faithfulnessEvaluator, relevancyEvaluator, answerabilityEvaluator);
        var qaPipeline = new QAPipeline(qaGenerator, qaFilter);

        // Question Suggestion
        var questionSuggestion = new QuestionSuggestionService(_completionService);

        // Chunk Filtering
        var chunkFiltering = new ChunkFilteringService(_completionService);

        // Query Preprocessing
        var queryPreprocessing = new QueryPreprocessingService(_completionService);

        // Contextual Retrieval (Anthropic pattern)
        var contextualEnrichment = new ContextualEnrichmentService(_completionService);

        // Chunk Relationship Discovery
        var chunkRelationship = new ChunkRelationshipService(_completionService);

        return new FluxImproverServices(
            Summarization: summarizationService,
            KeywordExtraction: keywordExtractionService,
            ChunkEnrichment: chunkEnrichmentService,
            Faithfulness: faithfulnessEvaluator,
            Relevancy: relevancyEvaluator,
            Answerability: answerabilityEvaluator,
            QAGenerator: qaGenerator,
            QAFilter: qaFilter,
            QAPipeline: qaPipeline,
            QuestionSuggestion: questionSuggestion,
            ChunkFiltering: chunkFiltering,
            QueryPreprocessing: queryPreprocessing,
            ContextualEnrichment: contextualEnrichment,
            ChunkRelationship: chunkRelationship
        );
    }
}

/// <summary>
/// FluxImprover 서비스 컨테이너
/// </summary>
/// <param name="Summarization">요약 서비스</param>
/// <param name="KeywordExtraction">키워드 추출 서비스</param>
/// <param name="ChunkEnrichment">청크 강화 서비스</param>
/// <param name="Faithfulness">충실도 평가기</param>
/// <param name="Relevancy">관련성 평가기</param>
/// <param name="Answerability">답변 가능성 평가기</param>
/// <param name="QAGenerator">QA 생성기</param>
/// <param name="QAFilter">QA 필터</param>
/// <param name="QAPipeline">QA 파이프라인</param>
/// <param name="QuestionSuggestion">질문 추천 서비스</param>
/// <param name="ChunkFiltering">청크 필터링 서비스</param>
/// <param name="QueryPreprocessing">쿼리 전처리 서비스</param>
/// <param name="ContextualEnrichment">Contextual Retrieval 서비스 (Anthropic pattern)</param>
/// <param name="ChunkRelationship">청크 관계 발견 서비스</param>
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

namespace FluxImprover;

using FluxImprover.ChunkFiltering;
using FluxImprover.ContextualRetrieval;
using FluxImprover.Enrichment;
using FluxImprover.Evaluation;
using FluxImprover.LMSupply;
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
    private bool _useLMSupply;
    private Action<LMSupplyCompletionOptions>? _LMSupplyOptionsAction;

    /// <summary>
    /// LLM 완성 서비스를 설정합니다.
    /// </summary>
    public FluxImproverBuilder WithCompletionService(ITextCompletionService completionService)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
        _useLMSupply = false;
        return this;
    }

    /// <summary>
    /// LMSupply.Generator를 기본 LLM 서비스로 사용합니다.
    /// 기본 모델 프리셋(Default)을 사용합니다.
    /// </summary>
    public FluxImproverBuilder WithLMSupply()
    {
        _useLMSupply = true;
        _LMSupplyOptionsAction = null;
        _completionService = null;
        return this;
    }

    /// <summary>
    /// LMSupply.Generator를 기본 LLM 서비스로 사용합니다.
    /// </summary>
    /// <param name="configure">LMSupply 옵션 구성 액션</param>
    public FluxImproverBuilder WithLMSupply(Action<LMSupplyCompletionOptions> configure)
    {
        _useLMSupply = true;
        _LMSupplyOptionsAction = configure ?? throw new ArgumentNullException(nameof(configure));
        _completionService = null;
        return this;
    }

    /// <summary>
    /// FluxImprover 인스턴스를 빌드합니다.
    /// LMSupply 사용 시에는 BuildAsync()를 사용하세요.
    /// </summary>
    /// <exception cref="InvalidOperationException">LMSupply 사용 시 BuildAsync()를 호출해야 합니다.</exception>
    public FluxImproverServices Build()
    {
        if (_useLMSupply)
            throw new InvalidOperationException(
                "LMSupply requires async initialization. Use BuildAsync() instead of Build().");

        if (_completionService is null)
            throw new InvalidOperationException("CompletionService must be configured before building.");

        return BuildServices(_completionService);
    }

    /// <summary>
    /// FluxImprover 인스턴스를 비동기로 빌드합니다.
    /// LMSupply 사용 시 필수입니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>빌드된 서비스 컨테이너</returns>
    public async Task<FluxImproverServices> BuildAsync(CancellationToken cancellationToken = default)
    {
        if (_useLMSupply)
        {
            var options = new LMSupplyCompletionOptions();
            _LMSupplyOptionsAction?.Invoke(options);

            _completionService = await LMSupplyCompletionServiceBuilder
                .BuildAsync(options, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        if (_completionService is null)
            throw new InvalidOperationException(
                "CompletionService must be configured. Use WithCompletionService() or WithLMSupply().");

        return BuildServices(_completionService);
    }

    private static FluxImproverServices BuildServices(ITextCompletionService completionService)
    {
        // Enrichment Services
        var summarizationService = new SummarizationService(completionService);
        var keywordExtractionService = new KeywordExtractionService(completionService);
        var chunkEnrichmentService = new ChunkEnrichmentService(summarizationService, keywordExtractionService);

        // Evaluation Services
        var faithfulnessEvaluator = new FaithfulnessEvaluator(completionService);
        var relevancyEvaluator = new RelevancyEvaluator(completionService);
        var answerabilityEvaluator = new AnswerabilityEvaluator(completionService);

        // QA Generation Services
        var qaGenerator = new QAGeneratorService(completionService);
        var qaFilter = new QAFilterService(faithfulnessEvaluator, relevancyEvaluator, answerabilityEvaluator);
        var qaPipeline = new QAPipeline(qaGenerator, qaFilter);

        // Question Suggestion
        var questionSuggestion = new QuestionSuggestionService(completionService);

        // Chunk Filtering
        var chunkFiltering = new ChunkFilteringService(completionService);

        // Query Preprocessing
        var queryPreprocessing = new QueryPreprocessingService(completionService);

        // Contextual Retrieval (Anthropic pattern)
        var contextualEnrichment = new ContextualEnrichmentService(completionService);

        // Chunk Relationship Discovery
        var chunkRelationship = new ChunkRelationshipService(completionService);

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

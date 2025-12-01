namespace FluxImprover.Options;

/// <summary>
/// 문서 강화 옵션
/// </summary>
public sealed class EnrichmentOptions
{
    private float? _temperature;
    private int _maxTokens = 512;
    private int _maxKeywords = 10;
    private int _maxSummaryLength = 200;

    /// <summary>
    /// 요약 생성 활성화 여부 (기본값: true)
    /// </summary>
    public bool EnableSummarization { get; init; } = true;

    /// <summary>
    /// 키워드 추출 활성화 여부 (기본값: true)
    /// </summary>
    public bool EnableKeywordExtraction { get; init; } = true;

    /// <summary>
    /// 개체 추출 활성화 여부 (기본값: false)
    /// </summary>
    public bool EnableEntityExtraction { get; init; } = false;

    /// <summary>
    /// LLM 온도 (0.0 ~ 2.0).
    /// null이면 모델 기본값 사용 (일부 모델은 기본값 외 temperature를 지원하지 않음).
    /// </summary>
    public float? Temperature
    {
        get => _temperature;
        init
        {
            if (value.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value.Value, 0.0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Value, 2.0f);
            }
            _temperature = value;
        }
    }

    /// <summary>
    /// 최대 토큰 수 (기본값: 512)
    /// </summary>
    public int MaxTokens
    {
        get => _maxTokens;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxTokens = value;
        }
    }

    /// <summary>
    /// 최대 키워드 수 (기본값: 10)
    /// </summary>
    public int MaxKeywords
    {
        get => _maxKeywords;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxKeywords = value;
        }
    }

    /// <summary>
    /// 최대 요약 길이 (문자 수, 기본값: 200)
    /// </summary>
    public int MaxSummaryLength
    {
        get => _maxSummaryLength;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxSummaryLength = value;
        }
    }

    /// <summary>
    /// 추출할 개체 유형 목록
    /// </summary>
    public IReadOnlyList<string> EntityTypes { get; init; } =
        ["Person", "Organization", "Location", "Date", "Product"];

    /// <summary>
    /// 병렬 처리 활성화 여부 (기본값: true)
    /// </summary>
    public bool EnableParallelProcessing { get; init; } = true;

    /// <summary>
    /// 병렬 처리 시 최대 동시 작업 수 (기본값: 4)
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 4;

    /// <summary>
    /// Parent chunk context for hierarchical enrichment.
    /// When provided, this context is used to inform the enrichment of child chunks.
    /// Supports hierarchical document structures where parent context improves child understanding.
    /// </summary>
    public ParentChunkContext? ParentContext { get; init; }

    /// <summary>
    /// Conditional enrichment options for cost optimization.
    /// When configured, chunks are pre-assessed and may skip unnecessary enrichment operations.
    /// </summary>
    public ConditionalEnrichmentOptions? ConditionalOptions { get; init; }
}

/// <summary>
/// Context information from a parent chunk for hierarchical enrichment.
/// </summary>
public sealed class ParentChunkContext
{
    /// <summary>
    /// Parent chunk identifier.
    /// </summary>
    public string? ParentId { get; init; }

    /// <summary>
    /// Summary of the parent chunk.
    /// </summary>
    public string? ParentSummary { get; init; }

    /// <summary>
    /// Keywords from the parent chunk.
    /// </summary>
    public IReadOnlyList<string>? ParentKeywords { get; init; }

    /// <summary>
    /// Document structure path of the parent (e.g., "Chapter 1").
    /// </summary>
    public string? ParentHeadingPath { get; init; }

    /// <summary>
    /// Hierarchy level (0 = root, 1 = first child level, etc.).
    /// </summary>
    public int HierarchyLevel { get; init; }
}


/// <summary>
/// Options for conditional enrichment based on chunk quality assessment.
/// Enables cost optimization by skipping enrichment for high-quality chunks.
/// </summary>
public sealed class ConditionalEnrichmentOptions
{
    private float _skipEnrichmentThreshold = 0.8f;
    private int _minSummarizationLength = 500;
    private float _minKeywordDensity = 0.3f;

    /// <summary>
    /// Enable conditional enrichment based on quality assessment.
    /// When enabled, chunks are analyzed before enrichment and may skip
    /// unnecessary operations. Default: false (all chunks are enriched).
    /// </summary>
    public bool EnableConditionalEnrichment { get; init; }

    /// <summary>
    /// Quality score threshold above which enrichment is skipped (0.0 - 1.0).
    /// Chunks with OverallScore >= this value skip enrichment.
    /// Default: 0.8
    /// </summary>
    public float SkipEnrichmentThreshold
    {
        get => _skipEnrichmentThreshold;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1f);
            _skipEnrichmentThreshold = value;
        }
    }

    /// <summary>
    /// Minimum content length (characters) to enable summarization.
    /// Chunks shorter than this skip summarization even if otherwise recommended.
    /// Default: 500
    /// </summary>
    public int MinSummarizationLength
    {
        get => _minSummarizationLength;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
            _minSummarizationLength = value;
        }
    }

    /// <summary>
    /// Minimum information density score to enable keyword extraction.
    /// Chunks with lower density skip keyword extraction.
    /// Default: 0.3
    /// </summary>
    public float MinKeywordDensity
    {
        get => _minKeywordDensity;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1f);
            _minKeywordDensity = value;
        }
    }

    /// <summary>
    /// Include quality assessment results in enriched chunk metadata.
    /// Default: true
    /// </summary>
    public bool IncludeQualityMetrics { get; init; } = true;

    /// <summary>
    /// Optional domain glossary for term expansion.
    /// When provided, acronyms and technical terms are expanded using domain knowledge.
    /// Implementation is provided by the consumer.
    /// </summary>
    public IDomainGlossary? DomainGlossary { get; init; }
}

/// <summary>
/// Interface for domain-specific glossary that expands acronyms and technical terms.
/// Consumers implement this interface to provide domain knowledge for their use case.
/// </summary>
public interface IDomainGlossary
{
    /// <summary>
    /// Expands known acronyms and technical terms in the text.
    /// </summary>
    /// <param name="text">The text containing terms to expand.</param>
    /// <returns>Text with expanded terms, or original text if no expansions apply.</returns>
    string ExpandTerms(string text);

    /// <summary>
    /// Gets expanded form of a specific term if known.
    /// </summary>
    /// <param name="term">The term or acronym to expand.</param>
    /// <returns>The expanded form, or null if not in glossary.</returns>
    string? GetExpansion(string term);
}

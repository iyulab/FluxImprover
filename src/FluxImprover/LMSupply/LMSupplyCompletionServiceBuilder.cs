namespace FluxImprover.LMSupply;

using global::LMSupply.Generator;

/// <summary>
/// LMSupplyCompletionService 빌더
/// </summary>
public sealed class LMSupplyCompletionServiceBuilder
{
    private readonly LMSupplyCompletionOptions _options = new();

    private LMSupplyCompletionServiceBuilder()
    {
    }

    /// <summary>
    /// 새 빌더 인스턴스 생성
    /// </summary>
    public static LMSupplyCompletionServiceBuilder Create() => new();

    /// <summary>
    /// 모델 프리셋 지정 (Default, Fast, Quality)
    /// </summary>
    public LMSupplyCompletionServiceBuilder WithModelPreset(GeneratorModelPreset preset)
    {
        _options.ModelPreset = preset;
        return this;
    }

    /// <summary>
    /// 로컬 모델 경로 지정
    /// </summary>
    public LMSupplyCompletionServiceBuilder WithModelPath(string modelPath)
    {
        _options.ModelPath = modelPath;
        return this;
    }

    /// <summary>
    /// HuggingFace 모델 ID 지정
    /// </summary>
    public LMSupplyCompletionServiceBuilder WithHuggingFaceModel(string modelId)
    {
        _options.HuggingFaceModelId = modelId;
        return this;
    }

    /// <summary>
    /// 모델 캐시 디렉토리 지정
    /// </summary>
    public LMSupplyCompletionServiceBuilder WithCacheDirectory(string cacheDirectory)
    {
        _options.CacheDirectory = cacheDirectory;
        return this;
    }

    /// <summary>
    /// 최대 컨텍스트 길이 지정
    /// </summary>
    public LMSupplyCompletionServiceBuilder WithMaxContextLength(int maxContextLength)
    {
        _options.MaxContextLength = maxContextLength;
        return this;
    }

    /// <summary>
    /// 상세 로깅 활성화
    /// </summary>
    public LMSupplyCompletionServiceBuilder WithVerboseLogging(bool enabled = true)
    {
        _options.VerboseLogging = enabled;
        return this;
    }

    /// <summary>
    /// 기본 생성 옵션 지정
    /// </summary>
    public LMSupplyCompletionServiceBuilder WithGenerationDefaults(Action<LMSupplyGenerationDefaults> configure)
    {
        _options.GenerationDefaults ??= new LMSupplyGenerationDefaults();
        configure(_options.GenerationDefaults);
        return this;
    }

    /// <summary>
    /// 기본 생성 온도 지정
    /// </summary>
    public LMSupplyCompletionServiceBuilder WithTemperature(float temperature)
    {
        _options.GenerationDefaults ??= new LMSupplyGenerationDefaults();
        _options.GenerationDefaults.Temperature = temperature;
        return this;
    }

    /// <summary>
    /// 기본 최대 토큰 수 지정
    /// </summary>
    public LMSupplyCompletionServiceBuilder WithMaxTokens(int maxTokens)
    {
        _options.GenerationDefaults ??= new LMSupplyGenerationDefaults();
        _options.GenerationDefaults.MaxTokens = maxTokens;
        return this;
    }

    /// <summary>
    /// LMSupplyCompletionService 빌드 (비동기)
    /// </summary>
    public Task<LMSupplyCompletionService> BuildAsync(CancellationToken cancellationToken = default)
        => BuildAsync(_options, cancellationToken);

    /// <summary>
    /// 옵션을 사용하여 LMSupplyCompletionService 빌드 (정적)
    /// </summary>
    public static async Task<LMSupplyCompletionService> BuildAsync(
        LMSupplyCompletionOptions? options,
        CancellationToken cancellationToken = default)
    {
        options ??= new LMSupplyCompletionOptions();

        var builder = TextGeneratorBuilder.Create();

        // 모델 소스 결정 (ModelPath > HuggingFaceModelId > ModelPreset > Default)
        if (!string.IsNullOrEmpty(options.ModelPath))
        {
            builder.WithModelPath(options.ModelPath);
        }
        else if (!string.IsNullOrEmpty(options.HuggingFaceModelId))
        {
            builder.WithHuggingFaceModel(options.HuggingFaceModelId);
        }
        else if (options.ModelPreset.HasValue)
        {
            builder.WithModel(options.ModelPreset.Value);
        }
        else
        {
            builder.WithDefaultModel();
        }

        // 추가 설정
        if (!string.IsNullOrEmpty(options.CacheDirectory))
        {
            builder.WithCacheDirectory(options.CacheDirectory);
        }

        if (options.MaxContextLength.HasValue)
        {
            builder.WithMaxContextLength(options.MaxContextLength.Value);
        }

        if (options.VerboseLogging)
        {
            builder.WithVerboseLogging();
        }

        // 모델 로드
        var generator = await builder.BuildAsync(cancellationToken)
            .ConfigureAwait(false);

        return new LMSupplyCompletionService(generator, options.GenerationDefaults);
    }
}

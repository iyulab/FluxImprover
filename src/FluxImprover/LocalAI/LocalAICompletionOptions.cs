namespace FluxImprover.LocalAI;

using global::LocalAI.Generator;

/// <summary>
/// LocalAI.Generator 기반 ITextCompletionService의 설정 옵션
/// </summary>
public sealed class LocalAICompletionOptions
{
    /// <summary>
    /// 사용할 모델 프리셋 (Default, Fast, Quality 등)
    /// ModelPath 또는 HuggingFaceModelId가 지정되면 무시됩니다.
    /// </summary>
    public GeneratorModelPreset? ModelPreset { get; set; }

    /// <summary>
    /// 사용자 지정 모델 경로 (ONNX 모델 파일 또는 디렉토리)
    /// 지정 시 ModelPreset보다 우선합니다.
    /// </summary>
    public string? ModelPath { get; set; }

    /// <summary>
    /// HuggingFace 모델 ID (예: "microsoft/phi-4-onnx")
    /// ModelPath가 지정되면 무시됩니다.
    /// </summary>
    public string? HuggingFaceModelId { get; set; }

    /// <summary>
    /// 모델 캐시 디렉토리 경로
    /// </summary>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// 최대 컨텍스트 길이 (기본값: 모델 기본값)
    /// </summary>
    public int? MaxContextLength { get; set; }

    /// <summary>
    /// 상세 로깅 활성화
    /// </summary>
    public bool VerboseLogging { get; set; }

    /// <summary>
    /// 기본 생성 옵션 (Temperature, MaxTokens 등)
    /// </summary>
    public LocalAIGenerationDefaults? GenerationDefaults { get; set; }
}

/// <summary>
/// 기본 생성 옵션
/// </summary>
public sealed class LocalAIGenerationDefaults
{
    /// <summary>
    /// 생성 온도 (0.0 ~ 2.0). 높을수록 창의적, 낮을수록 결정적.
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// 최대 생성 토큰 수
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Top-P (nucleus sampling) 값
    /// </summary>
    public float? TopP { get; set; }

    /// <summary>
    /// Top-K 샘플링 값
    /// </summary>
    public int? TopK { get; set; }

    /// <summary>
    /// 반복 패널티 값
    /// </summary>
    public float? RepetitionPenalty { get; set; }
}

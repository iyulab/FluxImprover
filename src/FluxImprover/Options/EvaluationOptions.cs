namespace FluxImprover.Options;

/// <summary>
/// 품질 평가 옵션
/// </summary>
public sealed class EvaluationOptions
{
    private float? _temperature;
    private int _maxTokens = 1024;
    private float _passThreshold = 0.5f;

    /// <summary>
    /// 충실도(Faithfulness) 평가 활성화 여부 (기본값: true).
    /// 여러 메트릭을 함께 평가하는 <see cref="QAGeneration.QAFilterService"/> 가 읽는다 — 끄면 그 메트릭은 요청되지 않고
    /// 점수는 <c>null</c> 로 남아 필터 기준에서 빠진다. 개별 평가기(<see cref="Evaluation.FaithfulnessEvaluator"/>)는 호출 자체가
    /// 그 메트릭을 고르는 것이므로 이 값을 보지 않는다.
    /// </summary>
    public bool EnableFaithfulness { get; init; } = true;

    /// <summary>
    /// 관련성(Relevancy) 평가 활성화 여부 (기본값: true).
    /// <see cref="EnableFaithfulness"/> 와 같이 <see cref="QAGeneration.QAFilterService"/> 가 읽는다.
    /// </summary>
    public bool EnableRelevancy { get; init; } = true;

    /// <summary>
    /// 답변 가능성(Answerability) 평가 활성화 여부 (기본값: true).
    /// <see cref="EnableFaithfulness"/> 와 같이 <see cref="QAGeneration.QAFilterService"/> 가 읽는다.
    /// </summary>
    public bool EnableAnswerability { get; init; } = true;

    /// <summary>
    /// LLM 온도 (0.0 ~ 2.0).
    /// null이면 모델 기본값 사용 (일부 모델은 기본값 외 temperature를 지원하지 않음).
    /// 평가 작업에는 낮은 temperature가 권장됩니다.
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
    /// 최대 토큰 수 (기본값: 1024)
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
    /// 품질 통과 임계값 (0.0 ~ 1.0, 기본값: 0.5) — 평가기가 돌려주는 <see cref="Evaluation.MetricResult.PassThreshold"/> 가 되고,
    /// <see cref="Evaluation.MetricResult.IsPassed"/> 는 점수가 이 값 이상일 때 참이다.
    /// </summary>
    public float PassThreshold
    {
        get => _passThreshold;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0f);
            _passThreshold = value;
        }
    }

    /// <summary>
    /// 상세 평가 정보 포함 여부 (기본값: true). false 이면 평가기는 모델이 돌려준 근거·claim 등을
    /// <see cref="Evaluation.MetricResult.Details"/> 에 싣지 않는다(점수만). 평가 실패 사유(<c>reason</c>)는 그대로 실린다.
    /// </summary>
    public bool IncludeDetails { get; init; } = true;

    /// <summary>
    /// 평가기의 판정 호출에서 모델의 추론(thinking) 모드 (기본값: <see cref="Services.ThinkingMode.Off"/>).
    /// </summary>
    /// <remarks>
    /// 판정 호출은 짧은 구조화 답(JSON)을 받는다. 추론 모델은 <see cref="Services.ThinkingMode.Auto"/> 에서 <see cref="MaxTokens"/> 예산을
    /// 숨은 추론에 써 버리고 판정 없이 끝날 수 있다 — 그러면 점수가 답이 아니라 파싱 실패를 말한다. 그래서 <see cref="EnrichmentOptions.Thinking"/>
    /// 과 같은 판단으로 기본을 끈다. 추론을 원하면 <c>Auto</c>/<c>On</c> 과 함께 <see cref="MaxTokens"/> 를 늘린다. 이 값을 전달하지 못하는
    /// 생성 서비스는 모델 기본을 쓴다.
    /// </remarks>
    public Services.ThinkingMode Thinking { get; init; } = Services.ThinkingMode.Off;

    /// <summary>
    /// 일괄 평가(<c>EvaluateBatchAsync</c>, <see cref="QAGeneration.QAFilterService.FilterAsync"/>)를 병렬로 처리할지 여부 (기본값: true).
    /// false 이면 항목을 하나씩 평가한다. 결과 순서는 입력 순서를 따른다.
    /// </summary>
    public bool EnableParallelProcessing { get; init; } = true;

    /// <summary>
    /// 병렬 처리 시 동시에 평가하는 최대 항목 수 (기본값: 4). 1 미만은 1 로 취급한다.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 4;
}

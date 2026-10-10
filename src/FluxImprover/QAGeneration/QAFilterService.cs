namespace FluxImprover.QAGeneration;

using FluxImprover.Evaluation;
using FluxImprover.Options;
using FluxImprover.Utilities;

/// <summary>
/// QA 쌍 품질 평가 및 필터링 서비스
/// </summary>
public class QAFilterService
{
    private readonly FaithfulnessEvaluator _faithfulnessEvaluator;
    private readonly RelevancyEvaluator _relevancyEvaluator;
    private readonly AnswerabilityEvaluator _answerabilityEvaluator;

    public QAFilterService(
        FaithfulnessEvaluator faithfulnessEvaluator,
        RelevancyEvaluator relevancyEvaluator,
        AnswerabilityEvaluator answerabilityEvaluator)
    {
        _faithfulnessEvaluator = faithfulnessEvaluator ?? throw new ArgumentNullException(nameof(faithfulnessEvaluator));
        _relevancyEvaluator = relevancyEvaluator ?? throw new ArgumentNullException(nameof(relevancyEvaluator));
        _answerabilityEvaluator = answerabilityEvaluator ?? throw new ArgumentNullException(nameof(answerabilityEvaluator));
    }

    /// <summary>
    /// QA 쌍을 평가하고 필터링합니다.
    /// </summary>
    /// <param name="pairs">평가할 QA 쌍 목록</param>
    /// <param name="options">필터링 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>품질 기준을 통과한 QA 쌍 목록</returns>
    public virtual async Task<IReadOnlyList<GeneratedQAPair>> FilterAsync(
        IReadOnlyList<GeneratedQAPair> pairs,
        QAFilterOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (pairs.Count == 0)
            return [];

        options ??= new QAFilterOptions();
        var evaluation = options.Evaluation ?? new EvaluationOptions();

        // Pairs without context are skipped, as before.
        var withContext = pairs.Where(p => !string.IsNullOrWhiteSpace(p.Context)).ToList();
        var evaluated = await BoundedBatch.RunAsync(
            withContext,
            BoundedBatch.Parallelism(evaluation.EnableParallelProcessing, evaluation.MaxDegreeOfParallelism),
            (pair, ct) => EvaluateAsync(pair, evaluation, ct),
            cancellationToken).ConfigureAwait(false);

        return evaluated
            .Where(p => p.Evaluation?.PassesThresholds(
                options.MinFaithfulness,
                options.MinRelevancy,
                options.MinAnswerability) == true)
            .ToList();
    }

    /// <summary>
    /// QA 쌍을 평가합니다 (필터링 없음).
    /// </summary>
    /// <param name="pair">평가할 QA 쌍</param>
    /// <param name="options">
    /// 평가 옵션 (기본값: null = <see cref="EvaluationOptions"/> 기본값). 꺼진 메트릭(<see cref="EvaluationOptions.EnableFaithfulness"/> 등)은
    /// 요청되지 않고 그 점수는 <c>null</c> 로 남는다.
    /// </param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>평가 결과가 포함된 QA 쌍</returns>
    public virtual async Task<GeneratedQAPair> EvaluateAsync(
        GeneratedQAPair pair,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pair);

        options ??= new EvaluationOptions();

        if (string.IsNullOrWhiteSpace(pair.Context))
        {
            return pair with
            {
                Evaluation = new QAPairEvaluation
                {
                    Faithfulness = options.EnableFaithfulness ? 0 : null,
                    Relevancy = options.EnableRelevancy ? 0 : null,
                    Answerability = options.EnableAnswerability ? 0 : null
                }
            };
        }

        // The enabled metrics run in parallel; a disabled one is never requested.
        var faithfulnessTask = options.EnableFaithfulness
            ? _faithfulnessEvaluator.EvaluateAsync(pair.Context, pair.Answer, options, cancellationToken)
            : null;
        var relevancyTask = options.EnableRelevancy
            ? _relevancyEvaluator.EvaluateAsync(pair.Question, pair.Answer, options, pair.Context, cancellationToken)
            : null;
        var answerabilityTask = options.EnableAnswerability
            ? _answerabilityEvaluator.EvaluateAsync(pair.Context, pair.Question, options, cancellationToken)
            : null;

        await Task.WhenAll(new[] { faithfulnessTask, relevancyTask, answerabilityTask }.OfType<Task<MetricResult>>())
            .ConfigureAwait(false);

        return pair with
        {
            Evaluation = new QAPairEvaluation
            {
                Faithfulness = faithfulnessTask is null ? null : (await faithfulnessTask.ConfigureAwait(false)).Score,
                Relevancy = relevancyTask is null ? null : (await relevancyTask.ConfigureAwait(false)).Score,
                Answerability = answerabilityTask is null ? null : (await answerabilityTask.ConfigureAwait(false)).Score
            }
        };
    }

    /// <summary>
    /// 여러 QA 쌍을 일괄 평가합니다. <see cref="EvaluationOptions.EnableParallelProcessing"/> 가 켜져 있으면
    /// <see cref="EvaluationOptions.MaxDegreeOfParallelism"/> 개까지 동시에 평가하며, 결과 순서는 입력 순서를 따른다.
    /// </summary>
    public virtual async Task<IReadOnlyList<GeneratedQAPair>> EvaluateBatchAsync(
        IReadOnlyList<GeneratedQAPair> pairs,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new EvaluationOptions();

        return await BoundedBatch.RunAsync(
            pairs,
            BoundedBatch.Parallelism(options.EnableParallelProcessing, options.MaxDegreeOfParallelism),
            (pair, ct) => EvaluateAsync(pair, options, ct),
            cancellationToken).ConfigureAwait(false);
    }
}

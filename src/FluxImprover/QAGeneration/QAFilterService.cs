namespace FluxImprover.QAGeneration;

using FluxImprover.Evaluation;

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
        var results = new List<GeneratedQAPair>();

        foreach (var pair in pairs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip pairs without context
            if (string.IsNullOrWhiteSpace(pair.Context))
                continue;

            var evaluated = await EvaluateAsync(pair, cancellationToken);

            if (evaluated.Evaluation?.PassesThresholds(
                options.MinFaithfulness,
                options.MinRelevancy,
                options.MinAnswerability) == true)
            {
                results.Add(evaluated);
            }
        }

        return results;
    }

    /// <summary>
    /// QA 쌍을 평가합니다 (필터링 없음).
    /// </summary>
    /// <param name="pair">평가할 QA 쌍</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>평가 결과가 포함된 QA 쌍</returns>
    public virtual async Task<GeneratedQAPair> EvaluateAsync(
        GeneratedQAPair pair,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pair);

        if (string.IsNullOrWhiteSpace(pair.Context))
        {
            return pair with
            {
                Evaluation = new QAPairEvaluation
                {
                    Faithfulness = 0,
                    Relevancy = 0,
                    Answerability = 0
                }
            };
        }

        // Evaluate all metrics in parallel
        var faithfulnessTask = _faithfulnessEvaluator.EvaluateAsync(pair.Context, pair.Answer, cancellationToken: cancellationToken);
        var relevancyTask = _relevancyEvaluator.EvaluateAsync(pair.Question, pair.Answer, context: pair.Context, cancellationToken: cancellationToken);
        var answerabilityTask = _answerabilityEvaluator.EvaluateAsync(pair.Context, pair.Question, cancellationToken: cancellationToken);

        await Task.WhenAll(faithfulnessTask, relevancyTask, answerabilityTask);

        var faithfulness = await faithfulnessTask;
        var relevancy = await relevancyTask;
        var answerability = await answerabilityTask;

        return pair with
        {
            Evaluation = new QAPairEvaluation
            {
                Faithfulness = faithfulness.Score,
                Relevancy = relevancy.Score,
                Answerability = answerability.Score
            }
        };
    }

    /// <summary>
    /// 여러 QA 쌍을 일괄 평가합니다.
    /// </summary>
    public virtual async Task<IReadOnlyList<GeneratedQAPair>> EvaluateBatchAsync(
        IReadOnlyList<GeneratedQAPair> pairs,
        CancellationToken cancellationToken = default)
    {
        var results = new List<GeneratedQAPair>();

        foreach (var pair in pairs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var evaluated = await EvaluateAsync(pair, cancellationToken);
            results.Add(evaluated);
        }

        return results;
    }
}

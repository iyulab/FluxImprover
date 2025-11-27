namespace FluxImprover.Prompts;

using System.Reflection;

/// <summary>
/// 내장된 프롬프트 템플릿 모음
/// </summary>
public static class EmbeddedPrompts
{
    private static readonly Assembly ResourceAssembly = typeof(EmbeddedPrompts).Assembly;
    private static readonly string ResourcePrefix = "FluxImprover.Prompts.Templates.";

    private static readonly Lazy<PromptTemplate> _qaGeneration = new(() =>
        LoadTemplate("QAGeneration.txt"));

    private static readonly Lazy<PromptTemplate> _faithfulnessEvaluation = new(() =>
        LoadTemplate("FaithfulnessEvaluation.txt"));

    private static readonly Lazy<PromptTemplate> _relevancyEvaluation = new(() =>
        LoadTemplate("RelevancyEvaluation.txt"));

    private static readonly Lazy<PromptTemplate> _answerabilityEvaluation = new(() =>
        LoadTemplate("AnswerabilityEvaluation.txt"));

    private static readonly Lazy<PromptTemplate> _questionSuggestion = new(() =>
        LoadTemplate("QuestionSuggestion.txt"));

    private static readonly Lazy<PromptTemplate> _summarization = new(() =>
        LoadTemplate("Summarization.txt"));

    private static readonly Lazy<PromptTemplate> _keywordExtraction = new(() =>
        LoadTemplate("KeywordExtraction.txt"));

    /// <summary>
    /// QA 쌍 생성용 프롬프트 템플릿
    /// </summary>
    public static PromptTemplate QAGeneration => _qaGeneration.Value;

    /// <summary>
    /// 충실도(Faithfulness) 평가용 프롬프트 템플릿
    /// </summary>
    public static PromptTemplate FaithfulnessEvaluation => _faithfulnessEvaluation.Value;

    /// <summary>
    /// 관련성(Relevancy) 평가용 프롬프트 템플릿
    /// </summary>
    public static PromptTemplate RelevancyEvaluation => _relevancyEvaluation.Value;

    /// <summary>
    /// 답변 가능성(Answerability) 평가용 프롬프트 템플릿
    /// </summary>
    public static PromptTemplate AnswerabilityEvaluation => _answerabilityEvaluation.Value;

    /// <summary>
    /// 질문 제안용 프롬프트 템플릿
    /// </summary>
    public static PromptTemplate QuestionSuggestion => _questionSuggestion.Value;

    /// <summary>
    /// 문서 요약용 프롬프트 템플릿
    /// </summary>
    public static PromptTemplate Summarization => _summarization.Value;

    /// <summary>
    /// 키워드 추출용 프롬프트 템플릿
    /// </summary>
    public static PromptTemplate KeywordExtraction => _keywordExtraction.Value;

    /// <summary>
    /// 모든 내장 템플릿을 반환합니다.
    /// </summary>
    /// <returns>템플릿 이름과 템플릿 쌍의 딕셔너리</returns>
    public static IReadOnlyDictionary<string, PromptTemplate> GetAll()
    {
        return new Dictionary<string, PromptTemplate>
        {
            [nameof(QAGeneration)] = QAGeneration,
            [nameof(FaithfulnessEvaluation)] = FaithfulnessEvaluation,
            [nameof(RelevancyEvaluation)] = RelevancyEvaluation,
            [nameof(AnswerabilityEvaluation)] = AnswerabilityEvaluation,
            [nameof(QuestionSuggestion)] = QuestionSuggestion,
            [nameof(Summarization)] = Summarization,
            [nameof(KeywordExtraction)] = KeywordExtraction
        };
    }

    private static PromptTemplate LoadTemplate(string fileName)
    {
        var resourceName = ResourcePrefix + fileName;

        using var stream = ResourceAssembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found. " +
                $"Available resources: {string.Join(", ", ResourceAssembly.GetManifestResourceNames())}");
        }

        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        return new PromptTemplate(content);
    }
}

namespace FluxImprover.Prompts;

/// <summary>
/// 프롬프트를 구성하기 위한 Fluent Builder
/// </summary>
public sealed class PromptBuilder
{
    private string? _systemPrompt;
    private string? _userPrompt;
    private PromptTemplate? _template;
    private readonly List<string> _contexts = [];
    private readonly List<string> _examples = [];
    private readonly Dictionary<string, object> _variables = new(StringComparer.OrdinalIgnoreCase);
    private bool _jsonMode;
    private int? _maxTokens;
    private float? _temperature;

    /// <summary>
    /// 시스템 프롬프트를 설정합니다.
    /// </summary>
    /// <param name="prompt">시스템 프롬프트</param>
    /// <returns>Builder 인스턴스</returns>
    public PromptBuilder WithSystemPrompt(string prompt)
    {
        _systemPrompt = prompt;
        return this;
    }

    /// <summary>
    /// 사용자 프롬프트를 설정합니다.
    /// </summary>
    /// <param name="prompt">사용자 프롬프트</param>
    /// <returns>Builder 인스턴스</returns>
    public PromptBuilder WithUserPrompt(string prompt)
    {
        _userPrompt = prompt;
        return this;
    }

    /// <summary>
    /// 프롬프트 템플릿을 설정합니다.
    /// </summary>
    /// <param name="template">프롬프트 템플릿</param>
    /// <returns>Builder 인스턴스</returns>
    public PromptBuilder WithTemplate(PromptTemplate template)
    {
        _template = template;
        return this;
    }

    /// <summary>
    /// 컨텍스트를 추가합니다 (RAG 검색 결과 등).
    /// </summary>
    /// <param name="context">컨텍스트 내용</param>
    /// <returns>Builder 인스턴스</returns>
    public PromptBuilder WithContext(string context)
    {
        _contexts.Add(context);
        return this;
    }

    /// <summary>
    /// Few-shot 예시를 추가합니다.
    /// </summary>
    /// <param name="example">예시 내용</param>
    /// <returns>Builder 인스턴스</returns>
    public PromptBuilder WithExample(string example)
    {
        _examples.Add(example);
        return this;
    }

    /// <summary>
    /// 템플릿 변수를 설정합니다.
    /// </summary>
    /// <param name="name">변수 이름</param>
    /// <param name="value">변수 값</param>
    /// <returns>Builder 인스턴스</returns>
    public PromptBuilder WithVariable(string name, object value)
    {
        _variables[name] = value;
        return this;
    }

    /// <summary>
    /// JSON 모드를 활성화합니다.
    /// </summary>
    /// <returns>Builder 인스턴스</returns>
    public PromptBuilder WithJsonMode()
    {
        _jsonMode = true;
        return this;
    }

    /// <summary>
    /// 최대 토큰 수를 설정합니다.
    /// </summary>
    /// <param name="maxTokens">최대 토큰 수</param>
    /// <returns>Builder 인스턴스</returns>
    public PromptBuilder WithMaxTokens(int maxTokens)
    {
        _maxTokens = maxTokens;
        return this;
    }

    /// <summary>
    /// Temperature를 설정합니다.
    /// </summary>
    /// <param name="temperature">Temperature 값 (0.0 ~ 2.0)</param>
    /// <returns>Builder 인스턴스</returns>
    public PromptBuilder WithTemperature(float temperature)
    {
        _temperature = temperature;
        return this;
    }

    /// <summary>
    /// 프롬프트를 빌드합니다.
    /// </summary>
    /// <returns>빌드된 프롬프트</returns>
    public Prompt Build()
    {
        var systemPrompt = InterpolateVariables(_systemPrompt);
        var userPrompt = _template is not null
            ? _template.Render(_variables)
            : InterpolateVariables(_userPrompt);

        var context = _contexts.Count switch
        {
            0 => null,
            1 => _contexts[0],
            _ => string.Join("\n\n", _contexts)
        };

        return new Prompt
        {
            System = systemPrompt,
            User = userPrompt,
            Context = context,
            Examples = _examples.ToList(),
            JsonMode = _jsonMode,
            MaxTokens = _maxTokens,
            Temperature = _temperature
        };
    }

    private string? InterpolateVariables(string? text)
    {
        if (string.IsNullOrEmpty(text) || _variables.Count == 0)
            return text;

        var template = new PromptTemplate(text);
        return template.Render(_variables);
    }
}

namespace FluxImprover.Prompts;

using System.Collections;
using System.Text.RegularExpressions;

/// <summary>
/// 변수 보간을 지원하는 프롬프트 템플릿
/// </summary>
public sealed partial class PromptTemplate
{
    private static readonly Regex VariablePattern = new(@"\{\{(\w+(?:\.\w+)*)\}\}", RegexOptions.Compiled);

    /// <summary>
    /// 템플릿 원본 내용
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// 새 프롬프트 템플릿을 생성합니다.
    /// </summary>
    /// <param name="content">템플릿 내용 ({{variable}} 형식의 변수 포함 가능)</param>
    public PromptTemplate(string content)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    /// <summary>
    /// 템플릿에 변수를 적용하여 렌더링합니다.
    /// </summary>
    /// <param name="variables">변수 값을 포함하는 객체 (익명 객체 또는 Dictionary)</param>
    /// <returns>렌더링된 문자열</returns>
    public string Render(object variables)
    {
        var variableDict = ConvertToVariableDictionary(variables);

        return VariablePattern.Replace(Content, match =>
        {
            var variableName = match.Groups[1].Value;

            // Case-insensitive 매칭
            var key = variableDict.Keys.FirstOrDefault(k =>
                string.Equals(k, variableName, StringComparison.OrdinalIgnoreCase));

            if (key is not null && variableDict.TryGetValue(key, out var value))
            {
                return FormatValue(value);
            }

            // 변수가 없으면 원본 플레이스홀더 유지
            return match.Value;
        });
    }

    /// <summary>
    /// 템플릿에 정의된 모든 변수 이름을 반환합니다.
    /// </summary>
    /// <returns>변수 이름 목록</returns>
    public IReadOnlyList<string> GetVariables()
    {
        return VariablePattern.Matches(Content)
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// 제공된 변수가 템플릿의 모든 변수를 충족하는지 검증합니다.
    /// </summary>
    /// <param name="variables">검증할 변수 객체</param>
    /// <param name="missingVariables">누락된 변수 목록</param>
    /// <returns>모든 변수가 제공되었으면 true</returns>
    public bool Validate(object variables, out IReadOnlyList<string> missingVariables)
    {
        var variableDict = ConvertToVariableDictionary(variables);
        var templateVariables = GetVariables();

        var missing = templateVariables
            .Where(v => !variableDict.Keys.Any(k =>
                string.Equals(k, v, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        missingVariables = missing;
        return missing.Count == 0;
    }

    private static Dictionary<string, object?> ConvertToVariableDictionary(object variables)
    {
        if (variables is IDictionary<string, object> dict)
        {
            return new Dictionary<string, object?>(dict!, StringComparer.OrdinalIgnoreCase);
        }

        if (variables is IDictionary genericDict)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in genericDict)
            {
                result[entry.Key.ToString()!] = entry.Value;
            }
            return result;
        }

        // 익명 객체 또는 일반 객체의 프로퍼티를 딕셔너리로 변환
        var properties = variables.GetType().GetProperties();
        var propDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in properties)
        {
            propDict[prop.Name] = prop.GetValue(variables);
        }

        return propDict;
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
            return string.Empty;

        if (value is string str)
            return str;

        if (value is IEnumerable<string> stringEnumerable)
            return string.Join(", ", stringEnumerable);

        if (value is IEnumerable enumerable and not string)
            return string.Join(", ", enumerable.Cast<object>().Select(x => x?.ToString() ?? string.Empty));

        return value.ToString() ?? string.Empty;
    }
}

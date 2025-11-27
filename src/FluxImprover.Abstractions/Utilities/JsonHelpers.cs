namespace FluxImprover.Abstractions.Utilities;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

/// <summary>
/// JSON 직렬화/역직렬화 도우미
/// </summary>
public static partial class JsonHelpers
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// 객체를 JSON 문자열로 직렬화합니다.
    /// </summary>
    /// <typeparam name="T">객체 타입</typeparam>
    /// <param name="obj">직렬화할 객체</param>
    /// <param name="indented">들여쓰기 여부</param>
    /// <returns>JSON 문자열</returns>
    public static string Serialize<T>(T obj, bool indented = false)
    {
        return JsonSerializer.Serialize(obj, indented ? IndentedOptions : DefaultOptions);
    }

    /// <summary>
    /// JSON 문자열을 객체로 역직렬화합니다.
    /// </summary>
    /// <typeparam name="T">대상 타입</typeparam>
    /// <param name="json">JSON 문자열</param>
    /// <returns>역직렬화된 객체 또는 실패 시 null</returns>
    public static T? Deserialize<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// JSON 문자열을 객체로 역직렬화를 시도합니다.
    /// </summary>
    /// <typeparam name="T">대상 타입</typeparam>
    /// <param name="json">JSON 문자열</param>
    /// <param name="result">역직렬화된 객체</param>
    /// <returns>성공 여부</returns>
    public static bool TryDeserialize<T>(string json, out T? result) where T : class
    {
        result = Deserialize<T>(json);
        return result is not null;
    }

    /// <summary>
    /// 텍스트에서 JSON을 추출합니다.
    /// LLM 응답에서 JSON만 추출하는 데 유용합니다.
    /// </summary>
    /// <param name="text">JSON을 포함한 텍스트</param>
    /// <returns>추출된 JSON 또는 null</returns>
    public static string? ExtractJsonFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Markdown 코드 블록에서 JSON 추출
        var codeBlockMatch = CodeBlockRegex().Match(text);
        if (codeBlockMatch.Success)
        {
            return codeBlockMatch.Groups[1].Value.Trim();
        }

        // JSON 객체 추출 먼저 시도 (중괄호로 시작하고 끝나는 부분)
        // 대부분의 LLM 응답이 객체 형태이므로 객체를 먼저 시도
        var objectStart = text.IndexOf('{');
        var objectEnd = text.LastIndexOf('}');
        if (objectStart >= 0 && objectEnd > objectStart)
        {
            var extracted = text[objectStart..(objectEnd + 1)];
            if (IsValidJson(extracted))
                return extracted;
        }

        // JSON 배열 추출 (대괄호로 시작하고 끝나는 부분)
        var arrayStart = text.IndexOf('[');
        var arrayEnd = text.LastIndexOf(']');
        if (arrayStart >= 0 && arrayEnd > arrayStart)
        {
            var extracted = text[arrayStart..(arrayEnd + 1)];
            if (IsValidJson(extracted))
                return extracted;
        }

        return null;
    }

    private static bool IsValidJson(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [GeneratedRegex(@"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.Multiline)]
    private static partial Regex CodeBlockRegex();

    [GeneratedRegex(@"\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}", RegexOptions.Singleline)]
    private static partial Regex JsonObjectRegex();

    [GeneratedRegex(@"\[[^\[\]]*(?:\[[^\[\]]*\][^\[\]]*)*\]", RegexOptions.Singleline)]
    private static partial Regex JsonArrayRegex();
}

namespace FluxImprover.Abstractions.Utilities;

using System.Text.RegularExpressions;

/// <summary>
/// 문자열 확장 메서드
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// 문자열이 null이거나 빈 문자열인지 확인합니다.
    /// </summary>
    public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);

    /// <summary>
    /// 문자열이 null이거나 공백 문자열인지 확인합니다.
    /// </summary>
    public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// 문자열을 지정된 길이로 자릅니다.
    /// </summary>
    /// <param name="value">원본 문자열</param>
    /// <param name="maxLength">최대 길이</param>
    /// <param name="addEllipsis">말줄임표 추가 여부</param>
    /// <returns>잘린 문자열</returns>
    public static string Truncate(this string value, int maxLength, bool addEllipsis = false)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        if (addEllipsis && maxLength > 3)
        {
            return value[..(maxLength - 3)] + "...";
        }

        return value[..maxLength];
    }

    /// <summary>
    /// 지정된 수의 단어만 반환합니다.
    /// </summary>
    /// <param name="value">원본 문자열</param>
    /// <param name="count">반환할 단어 수</param>
    /// <returns>지정된 수의 단어</returns>
    public static string TakeWords(this string value, int count)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Take(count));
    }

    /// <summary>
    /// 문자열의 단어 수를 반환합니다.
    /// </summary>
    public static int WordCount(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// 문자열을 문장 단위로 분리합니다.
    /// </summary>
    public static IReadOnlyList<string> SplitIntoSentences(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        // 약어 패턴 (Mr., Dr., etc.)을 임시로 대체
        var text = AbbreviationRegex().Replace(value, m => m.Value.Replace(".", "{{DOT}}"));

        // 문장 분리
        var sentences = SentenceEndRegex()
            .Split(text)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Replace("{{DOT}}", ".").Trim())
            .ToList();

        return sentences;
    }

    /// <summary>
    /// CamelCase/PascalCase 문자열을 공백으로 분리합니다.
    /// </summary>
    public static string SplitCamelCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return CamelCaseRegex().Replace(value, " ").Trim();
    }

    /// <summary>
    /// HTML 태그를 제거합니다.
    /// </summary>
    public static string RemoveHtmlTags(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return HtmlTagRegex().Replace(value, string.Empty);
    }

    /// <summary>
    /// 연속된 공백과 줄바꿈을 정규화합니다.
    /// </summary>
    public static string NormalizeWhitespace(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // 연속된 줄바꿈을 하나로
        var result = MultipleNewlinesRegex().Replace(value, "\n");

        // 연속된 공백을 하나로
        result = MultipleSpacesRegex().Replace(result, " ");

        return result.Trim();
    }

    /// <summary>
    /// 이메일 주소가 포함되어 있는지 확인합니다.
    /// </summary>
    public static bool ContainsEmail(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return EmailRegex().IsMatch(value);
    }

    [GeneratedRegex(@"(?<!\w)(?:Mr|Mrs|Ms|Dr|Prof|Inc|Ltd|Corp|etc)\.", RegexOptions.IgnoreCase)]
    private static partial Regex AbbreviationRegex();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceEndRegex();

    [GeneratedRegex(@"(?<!^)(?=[A-Z][a-z])|(?<=[a-z])(?=[A-Z])")]
    private static partial Regex CamelCaseRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\n{2,}")]
    private static partial Regex MultipleNewlinesRegex();

    [GeneratedRegex(@" {2,}")]
    private static partial Regex MultipleSpacesRegex();

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex EmailRegex();
}

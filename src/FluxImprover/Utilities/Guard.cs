namespace FluxImprover.Utilities;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
/// 인수 검증을 위한 Guard 클래스
/// </summary>
public static class Guard
{
    /// <summary>
    /// 값이 null이 아닌지 확인합니다.
    /// </summary>
    /// <typeparam name="T">값 타입</typeparam>
    /// <param name="value">확인할 값</param>
    /// <param name="parameterName">매개변수 이름</param>
    /// <returns>null이 아닌 값</returns>
    /// <exception cref="ArgumentNullException">값이 null인 경우</exception>
    public static T NotNull<T>(
        [NotNull] T? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }
        return value;
    }

    /// <summary>
    /// 문자열이 null이거나 빈 문자열이 아닌지 확인합니다.
    /// </summary>
    /// <param name="value">확인할 문자열</param>
    /// <param name="parameterName">매개변수 이름</param>
    /// <returns>유효한 문자열</returns>
    /// <exception cref="ArgumentException">문자열이 null이거나 빈 경우</exception>
    public static string NotNullOrEmpty(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Value cannot be null or empty.", parameterName);
        }
        return value;
    }

    /// <summary>
    /// 문자열이 null이거나 공백 문자열이 아닌지 확인합니다.
    /// </summary>
    /// <param name="value">확인할 문자열</param>
    /// <param name="parameterName">매개변수 이름</param>
    /// <returns>유효한 문자열</returns>
    /// <exception cref="ArgumentException">문자열이 null이거나 공백인 경우</exception>
    public static string NotNullOrWhiteSpace(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", parameterName);
        }
        return value;
    }

    /// <summary>
    /// 값이 지정된 범위 내에 있는지 확인합니다.
    /// </summary>
    /// <typeparam name="T">값 타입</typeparam>
    /// <param name="value">확인할 값</param>
    /// <param name="min">최소값</param>
    /// <param name="max">최대값</param>
    /// <param name="parameterName">매개변수 이름</param>
    /// <returns>범위 내의 값</returns>
    /// <exception cref="ArgumentOutOfRangeException">값이 범위를 벗어난 경우</exception>
    public static T InRange<T>(
        T value,
        T min,
        T max,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"Value must be between {min} and {max}.");
        }
        return value;
    }

    /// <summary>
    /// 값이 양수인지 확인합니다.
    /// </summary>
    /// <param name="value">확인할 값</param>
    /// <param name="parameterName">매개변수 이름</param>
    /// <returns>양수 값</returns>
    /// <exception cref="ArgumentOutOfRangeException">값이 양수가 아닌 경우</exception>
    public static int Positive(
        int value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be positive.");
        }
        return value;
    }

    /// <summary>
    /// 컬렉션이 비어 있지 않은지 확인합니다.
    /// </summary>
    /// <typeparam name="T">요소 타입</typeparam>
    /// <param name="collection">확인할 컬렉션</param>
    /// <param name="parameterName">매개변수 이름</param>
    /// <returns>비어 있지 않은 컬렉션</returns>
    /// <exception cref="ArgumentException">컬렉션이 비어 있는 경우</exception>
    public static IEnumerable<T> NotEmpty<T>(
        [NotNull] IEnumerable<T>? collection,
        [CallerArgumentExpression(nameof(collection))] string? parameterName = null)
    {
        NotNull(collection, parameterName);

        // ICollection으로 최적화
        if (collection is ICollection<T> col)
        {
            if (col.Count == 0)
            {
                throw new ArgumentException("Collection cannot be empty.", parameterName);
            }
            return collection;
        }

        // 첫 번째 요소 확인
        if (!collection.Any())
        {
            throw new ArgumentException("Collection cannot be empty.", parameterName);
        }
        return collection;
    }

    /// <summary>
    /// 파일이 존재하는지 확인합니다.
    /// </summary>
    /// <param name="path">파일 경로</param>
    /// <param name="parameterName">매개변수 이름</param>
    /// <returns>파일 경로</returns>
    /// <exception cref="FileNotFoundException">파일이 존재하지 않는 경우</exception>
    public static string FileExists(
        string path,
        [CallerArgumentExpression(nameof(path))] string? parameterName = null)
    {
        NotNullOrWhiteSpace(path, parameterName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File not found: {path}", path);
        }
        return path;
    }
}

namespace FluxImprover.Abstractions.Utilities;

/// <summary>
/// 컬렉션 확장 메서드
/// </summary>
public static class CollectionExtensions
{
    private static readonly Random RandomInstance = Random.Shared;

    /// <summary>
    /// 컬렉션을 지정된 크기의 배치로 분할합니다.
    /// </summary>
    /// <typeparam name="T">요소 타입</typeparam>
    /// <param name="source">원본 컬렉션</param>
    /// <param name="batchSize">배치 크기</param>
    /// <returns>배치 컬렉션</returns>
    public static IEnumerable<IReadOnlyList<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        var batch = new List<T>(batchSize);

        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }

        if (batch.Count > 0)
        {
            yield return batch;
        }
    }

    /// <summary>
    /// 컬렉션의 요소를 무작위로 섞습니다.
    /// </summary>
    /// <typeparam name="T">요소 타입</typeparam>
    /// <param name="source">원본 컬렉션</param>
    /// <returns>섞인 컬렉션</returns>
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        var list = source.ToList();

        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = RandomInstance.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    /// <summary>
    /// 지정된 키 선택기로 중복을 제거합니다.
    /// </summary>
    /// <typeparam name="TSource">요소 타입</typeparam>
    /// <typeparam name="TKey">키 타입</typeparam>
    /// <param name="source">원본 컬렉션</param>
    /// <param name="keySelector">키 선택 함수</param>
    /// <returns>중복이 제거된 컬렉션</returns>
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();

        foreach (var item in source)
        {
            if (seen.Add(keySelector(item)))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// 병렬로 비동기 작업을 실행합니다.
    /// </summary>
    /// <typeparam name="T">요소 타입</typeparam>
    /// <param name="source">원본 컬렉션</param>
    /// <param name="action">실행할 비동기 작업</param>
    /// <param name="maxDegreeOfParallelism">최대 병렬 수</param>
    public static async Task ForEachAsync<T>(
        this IEnumerable<T> source,
        Func<T, Task> action,
        int maxDegreeOfParallelism = 4)
    {
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = source.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                await action(item);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 병렬로 비동기 변환을 실행합니다.
    /// </summary>
    /// <typeparam name="TSource">원본 타입</typeparam>
    /// <typeparam name="TResult">결과 타입</typeparam>
    /// <param name="source">원본 컬렉션</param>
    /// <param name="selector">변환 함수</param>
    /// <param name="maxDegreeOfParallelism">최대 병렬 수</param>
    /// <returns>변환된 컬렉션</returns>
    public static async Task<IReadOnlyList<TResult>> SelectAsync<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, Task<TResult>> selector,
        int maxDegreeOfParallelism = 4)
    {
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = source.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await selector(item);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 무작위로 지정된 수의 요소를 선택합니다.
    /// </summary>
    /// <typeparam name="T">요소 타입</typeparam>
    /// <param name="source">원본 컬렉션</param>
    /// <param name="count">선택할 개수</param>
    /// <returns>무작위로 선택된 요소들</returns>
    public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> source, int count)
    {
        return source.Shuffle().Take(count);
    }

    /// <summary>
    /// 안전하게 인덱스로 요소를 가져옵니다.
    /// </summary>
    /// <typeparam name="T">요소 타입</typeparam>
    /// <param name="list">리스트</param>
    /// <param name="index">인덱스</param>
    /// <param name="defaultValue">기본값</param>
    /// <returns>요소 또는 기본값</returns>
    public static T? SafeGet<T>(this IList<T> list, int index, T? defaultValue = default)
    {
        if (index < 0 || index >= list.Count)
        {
            return defaultValue;
        }
        return list[index];
    }

    /// <summary>
    /// 컬렉션이 null이거나 비어 있는지 확인합니다.
    /// </summary>
    /// <typeparam name="T">요소 타입</typeparam>
    /// <param name="collection">확인할 컬렉션</param>
    /// <returns>null이거나 비어 있으면 true</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        if (collection is null)
            return true;

        if (collection is ICollection<T> col)
            return col.Count == 0;

        return !collection.Any();
    }
}

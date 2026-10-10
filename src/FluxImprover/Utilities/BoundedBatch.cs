namespace FluxImprover.Utilities;

/// <summary>
/// Runs one asynchronous operation per item with at most a given number in flight, and returns the results in input order.
/// </summary>
internal static class BoundedBatch
{
    /// <summary>The parallelism a batch runs with: <paramref name="maxDegreeOfParallelism"/> when enabled (at least 1), else 1.</summary>
    public static int Parallelism(bool enableParallelProcessing, int maxDegreeOfParallelism) =>
        enableParallelProcessing ? Math.Max(1, maxDegreeOfParallelism) : 1;

    public static async Task<IReadOnlyList<TResult>> RunAsync<TItem, TResult>(
        IEnumerable<TItem> items,
        int parallelism,
        Func<TItem, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        var list = items as IReadOnlyList<TItem> ?? items.ToList();
        if (list.Count == 0)
            return [];

        if (parallelism <= 1 || list.Count == 1)
        {
            var sequential = new List<TResult>(list.Count);
            foreach (var item in list)
            {
                cancellationToken.ThrowIfCancellationRequested();
                sequential.Add(await operation(item, cancellationToken).ConfigureAwait(false));
            }

            return sequential;
        }

        using var semaphore = new SemaphoreSlim(parallelism);
        var tasks = list.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await operation(item, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}

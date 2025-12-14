namespace FluxImprover.LocalAI;

/// <summary>
/// Lazy initialization wrapper for LocalAICompletionService.
/// Ensures thread-safe, single initialization of the service.
/// </summary>
internal sealed class LocalAIInitializer : IAsyncDisposable
{
    private readonly LocalAICompletionOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private LocalAICompletionService? _service;
    private bool _disposed;

    public LocalAIInitializer(LocalAICompletionOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets or initializes the LocalAICompletionService.
    /// </summary>
    public async Task<LocalAICompletionService> GetServiceAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_service is not null)
            return _service;

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_service is not null)
                return _service;

            _service = await LocalAICompletionServiceBuilder
                .BuildAsync(_options, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return _service;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_service is not null)
        {
            await _service.DisposeAsync().ConfigureAwait(false);
        }

        _lock.Dispose();
    }
}

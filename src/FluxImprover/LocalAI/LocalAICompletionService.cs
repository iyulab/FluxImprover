namespace FluxImprover.LocalAI;

using System.Runtime.CompilerServices;
using FluxImprover.Services;
using global::LocalAI.Generator.Abstractions;
using global::LocalAI.Generator.Models;

/// <summary>
/// LocalAI.Generator 기반 ITextCompletionService 구현
/// </summary>
public sealed class LocalAICompletionService : ITextCompletionService, IAsyncDisposable
{
    private readonly IGeneratorModel _generator;
    private readonly LocalAIGenerationDefaults? _defaults;
    private bool _disposed;

    /// <summary>
    /// LocalAICompletionService 생성자
    /// </summary>
    /// <param name="generator">텍스트 생성기 모델</param>
    /// <param name="defaults">기본 생성 옵션</param>
    internal LocalAICompletionService(IGeneratorModel generator, LocalAIGenerationDefaults? defaults = null)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _defaults = defaults;
    }

    /// <summary>
    /// 사용 중인 모델 ID
    /// </summary>
    public string ModelId => _generator.ModelId;

    /// <summary>
    /// 최대 컨텍스트 길이
    /// </summary>
    public int MaxContextLength => _generator.MaxContextLength;

    /// <inheritdoc />
    public async Task<string> CompleteAsync(
        string prompt,
        CompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var generationOptions = OptionsMapper.ToGenerationOptions(options, _defaults);

        if (OptionsMapper.HasChatContext(options))
        {
            var messages = OptionsMapper.BuildChatMessages(prompt, options);
            return await _generator.GenerateChatCompleteAsync(messages, generationOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            var finalPrompt = OptionsMapper.ApplyJsonModeIfNeeded(prompt, options);
            return await _generator.GenerateCompleteAsync(finalPrompt, generationOptions, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        CompletionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var generationOptions = OptionsMapper.ToGenerationOptions(options, _defaults);

        if (OptionsMapper.HasChatContext(options))
        {
            var messages = OptionsMapper.BuildChatMessages(prompt, options);
            await foreach (var token in _generator.GenerateChatAsync(messages, generationOptions, cancellationToken)
                .ConfigureAwait(false))
            {
                yield return token;
            }
        }
        else
        {
            var finalPrompt = OptionsMapper.ApplyJsonModeIfNeeded(prompt, options);
            await foreach (var token in _generator.GenerateAsync(finalPrompt, generationOptions, cancellationToken)
                .ConfigureAwait(false))
            {
                yield return token;
            }
        }
    }

    /// <summary>
    /// 모델 워밍업 수행
    /// </summary>
    public Task WarmupAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _generator.WarmupAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_generator is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else if (_generator is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

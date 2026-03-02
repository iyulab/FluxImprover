using System.Runtime.CompilerServices;
using FluxImprover.Services;
using LMSupply.Generator.Abstractions;
using Microsoft.Extensions.Logging;

using LMChatMessage = LMSupply.Generator.Models.ChatMessage;
using LMChatRole = LMSupply.Generator.Models.ChatRole;
using LMGenerationOptions = LMSupply.Generator.Models.GenerationOptions;

namespace FluxImprover.LMSupply;

/// <summary>
/// Adapts LMSupply.Generator's IGeneratorModel to FluxImprover's ITextGenerationService.
/// Enables fully offline LLM text completion using local GGUF/ONNX models.
/// </summary>
public sealed partial class LMSupplyCompletionService : ITextGenerationService, IAsyncDisposable
{
    private readonly IGeneratorModel _model;
    private readonly ILogger<LMSupplyCompletionService> _logger;
    private readonly float _defaultTemperature;
    private readonly int _defaultMaxTokens;

    /// <summary>
    /// Creates a new LMSupply completion service.
    /// </summary>
    /// <param name="model">The loaded LMSupply generator model.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="defaultTemperature">Default temperature when not specified in options. Defaults to 0.3.</param>
    /// <param name="defaultMaxTokens">Default max tokens when not specified in options. Defaults to 512.</param>
    public LMSupplyCompletionService(
        IGeneratorModel model,
        ILogger<LMSupplyCompletionService> logger,
        float defaultTemperature = 0.3f,
        int defaultMaxTokens = 512)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(logger);

        _model = model;
        _logger = logger;
        _defaultTemperature = defaultTemperature;
        _defaultMaxTokens = defaultMaxTokens;
    }

    /// <inheritdoc />
    public async Task<string> CompleteAsync(
        string prompt,
        CompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messages = BuildChatMessages(prompt, options);
        var genOptions = BuildGenerationOptions(options);

        LogCompletion(_logger, _model.ModelId, prompt.Length);

        var result = await _model.GenerateChatCompleteAsync(messages, genOptions, cancellationToken);

        LogCompletionDone(_logger, _model.ModelId, result.Length);
        return result;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        CompletionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = BuildChatMessages(prompt, options);
        var genOptions = BuildGenerationOptions(options);

        LogCompletionStream(_logger, _model.ModelId, prompt.Length);

        await foreach (var token in _model.GenerateChatAsync(messages, genOptions, cancellationToken))
        {
            yield return token;
        }
    }

    private static List<LMChatMessage> BuildChatMessages(string prompt, CompletionOptions? options)
    {
        var messages = new List<LMChatMessage>();

        if (!string.IsNullOrEmpty(options?.SystemPrompt))
        {
            messages.Add(LMChatMessage.System(options.SystemPrompt));
        }

        if (options?.Messages is not null)
        {
            foreach (var msg in options.Messages)
            {
                var role = msg.Role.ToLowerInvariant() switch
                {
                    "system" => LMChatRole.System,
                    "assistant" => LMChatRole.Assistant,
                    _ => LMChatRole.User
                };
                messages.Add(new LMChatMessage(role, msg.Content));
            }
        }

        messages.Add(LMChatMessage.User(prompt));
        return messages;
    }

    private LMGenerationOptions BuildGenerationOptions(CompletionOptions? options)
    {
        var genOptions = new LMGenerationOptions
        {
            Temperature = options?.Temperature ?? _defaultTemperature,
            MaxTokens = options?.MaxTokens ?? _defaultMaxTokens,
            DoSample = true
        };

        if (options?.JsonMode == true)
        {
            genOptions.JsonSchema = options.ResponseSchema;
        }

        return genOptions;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _model.DisposeAsync();
    }

    #region LoggerMessage

    [LoggerMessage(Level = LogLevel.Debug, Message = "Completing with local model {ModelId} (prompt: {Length} chars)")]
    private static partial void LogCompletion(ILogger logger, string modelId, int length);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Completion done with local model {ModelId} (response: {Length} chars)")]
    private static partial void LogCompletionDone(ILogger logger, string modelId, int length);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Streaming with local model {ModelId} (prompt: {Length} chars)")]
    private static partial void LogCompletionStream(ILogger logger, string modelId, int length);

    #endregion
}

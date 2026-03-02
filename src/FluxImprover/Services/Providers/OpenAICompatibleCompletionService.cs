using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace FluxImprover.Services.Providers;

/// <summary>
/// Lightweight ITextGenerationService implementation for OpenAI-compatible APIs.
/// Supports OpenAI, Azure OpenAI, Ollama, and any OpenAI-compatible endpoint.
/// </summary>
public sealed partial class OpenAICompatibleCompletionService : ITextGenerationService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly ILogger<OpenAICompatibleCompletionService> _logger;
    private readonly bool _ownsHttpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Creates a new OpenAI-compatible completion service.
    /// </summary>
    /// <param name="endpoint">Base API URL (e.g., "https://api.openai.com/v1").</param>
    /// <param name="apiKey">API key for authentication. Pass null for endpoints that don't require authentication.</param>
    /// <param name="model">Model name (e.g., "gpt-4o-mini").</param>
    /// <param name="logger">Logger instance.</param>
    public OpenAICompatibleCompletionService(
        string endpoint,
        string? apiKey,
        string model,
        ILogger<OpenAICompatibleCompletionService> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentNullException.ThrowIfNull(logger);

        _model = model;
        _logger = logger;
        _ownsHttpClient = true;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(endpoint.TrimEnd('/') + "/")
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    /// <summary>
    /// Creates a new OpenAI-compatible completion service with a pre-configured HttpClient.
    /// Use this overload for testing or when you need custom HTTP handlers.
    /// </summary>
    /// <param name="httpClient">Pre-configured HttpClient with BaseAddress set.</param>
    /// <param name="model">Model name (e.g., "gpt-4o-mini").</param>
    /// <param name="logger">Logger instance.</param>
    public OpenAICompatibleCompletionService(
        HttpClient httpClient,
        string model,
        ILogger<OpenAICompatibleCompletionService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _model = model;
        _logger = logger;
        _ownsHttpClient = false;
    }

    /// <inheritdoc />
    public async Task<string> CompleteAsync(
        string prompt,
        CompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(prompt, options, stream: false);

        LogCompletion(_logger, _model, prompt.Length);

        var response = await _httpClient.PostAsJsonAsync(
            "chat/completions", request, JsonOptions, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
            JsonOptions, cancellationToken);

        var content = result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;

        LogCompletionDone(_logger, _model, content.Length);
        return content;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        CompletionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(prompt, options, stream: true);

        LogCompletionStream(_logger, _model, prompt.Length);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json")
        };

        var response = await _httpClient.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(responseStream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]") yield break;

            var chunk = JsonSerializer.Deserialize<ChatCompletionStreamResponse>(data, JsonOptions);
            var delta = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(delta))
            {
                yield return delta;
            }
        }
    }

    private ChatCompletionRequest BuildRequest(string prompt, CompletionOptions? options, bool stream)
    {
        var messages = new List<MessageDto>();

        if (!string.IsNullOrEmpty(options?.SystemPrompt))
        {
            messages.Add(new MessageDto { Role = "system", Content = options.SystemPrompt });
        }

        if (options?.Messages is not null)
        {
            foreach (var msg in options.Messages)
            {
                messages.Add(new MessageDto { Role = msg.Role, Content = msg.Content });
            }
        }

        messages.Add(new MessageDto { Role = "user", Content = prompt });

        return new ChatCompletionRequest
        {
            Model = _model,
            Messages = messages,
            Temperature = options?.Temperature,
            MaxTokens = options?.MaxTokens,
            Stream = stream,
            ResponseFormat = options?.JsonMode == true
                ? new ResponseFormatDto { Type = "json_object" }
                : null
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    #region LoggerMessage

    [LoggerMessage(Level = LogLevel.Debug, Message = "Completing with {Model} (prompt: {Length} chars)")]
    private static partial void LogCompletion(ILogger logger, string model, int length);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Completion done with {Model} (response: {Length} chars)")]
    private static partial void LogCompletionDone(ILogger logger, string model, int length);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Streaming with {Model} (prompt: {Length} chars)")]
    private static partial void LogCompletionStream(ILogger logger, string model, int length);

    #endregion

    #region DTO Models

    internal sealed class ChatCompletionRequest
    {
        public string Model { get; init; } = string.Empty;
        public List<MessageDto> Messages { get; init; } = [];
        public float? Temperature { get; init; }
        public int? MaxTokens { get; init; }
        public bool? Stream { get; init; }
        public ResponseFormatDto? ResponseFormat { get; init; }
    }

    internal sealed class MessageDto
    {
        public string Role { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
    }

    internal sealed class ResponseFormatDto
    {
        public string Type { get; init; } = string.Empty;
    }

    internal sealed class ChatCompletionResponse
    {
        public List<ChoiceDto>? Choices { get; init; }
    }

    internal sealed class ChoiceDto
    {
        public MessageDto? Message { get; init; }
    }

    internal sealed class ChatCompletionStreamResponse
    {
        public List<StreamChoiceDto>? Choices { get; init; }
    }

    internal sealed class StreamChoiceDto
    {
        public DeltaDto? Delta { get; init; }
    }

    internal sealed class DeltaDto
    {
        public string? Content { get; init; }
    }

    #endregion
}

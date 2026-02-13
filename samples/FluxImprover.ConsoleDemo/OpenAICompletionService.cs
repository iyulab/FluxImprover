namespace FluxImprover.ConsoleDemo;

using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluxImprover.Services;

/// <summary>
/// OpenAI API 기반 ITextCompletionService 구현
/// </summary>
public sealed class OpenAICompletionService : ITextCompletionService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly JsonSerializerOptions _jsonOptions;

    public OpenAICompletionService(string apiKey, string model = "gpt-4o-mini", string? baseUrl = null)
    {
        _model = model;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl ?? "https://api.openai.com/v1/"),
            Timeout = TimeSpan.FromMinutes(2)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<string> CompleteAsync(
        string prompt,
        CompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(prompt, options, stream: false);
        var response = await _httpClient.PostAsJsonAsync("chat/completions", request, _jsonOptions, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(_jsonOptions, cancellationToken);
        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        CompletionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(prompt, options, stream: true);
        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions") { Content = content };
        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line[6..];
            if (data == "[DONE]") break;

            var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data, _jsonOptions);
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

        if (options?.Messages is { Count: > 0 })
        {
            messages.AddRange(options.Messages.Select(m => new MessageDto { Role = m.Role, Content = m.Content }));
        }

        messages.Add(new MessageDto { Role = "user", Content = prompt });

        var request = new ChatCompletionRequest
        {
            Model = _model,
            Messages = messages,
            Temperature = options?.Temperature ?? 0.7f,
            MaxTokens = options?.MaxTokens,
            Stream = stream
        };

        if (options?.JsonMode == true)
        {
            request.ResponseFormat = new ResponseFormat { Type = "json_object" };
        }

        return request;
    }

    public void Dispose() => _httpClient.Dispose();
}

#region DTOs

internal sealed class ChatCompletionRequest
{
    public required string Model { get; set; }
    public required List<MessageDto> Messages { get; set; }
    public float Temperature { get; set; } = 0.7f;
    public int? MaxTokens { get; set; }
    public bool Stream { get; set; }
    public ResponseFormat? ResponseFormat { get; set; }
}

internal sealed class MessageDto
{
    public required string Role { get; set; }
    public required string Content { get; set; }
}

internal sealed class ResponseFormat
{
    public required string Type { get; set; }
}

internal sealed class ChatCompletionResponse
{
    public List<Choice>? Choices { get; set; }
}

internal sealed class Choice
{
    public MessageDto? Message { get; set; }
    public DeltaDto? Delta { get; set; }
}

internal sealed class DeltaDto
{
    public string? Content { get; set; }
}

internal sealed class ChatCompletionChunk
{
    public List<Choice>? Choices { get; set; }
}

#endregion

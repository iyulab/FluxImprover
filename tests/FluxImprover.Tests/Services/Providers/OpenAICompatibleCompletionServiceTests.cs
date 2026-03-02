using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using FluxImprover.Services;
using FluxImprover.Services.Providers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FluxImprover.Tests.Services.Providers;

public class OpenAICompatibleCompletionServiceTests : IDisposable
{
    private readonly ILogger<OpenAICompatibleCompletionService> _logger =
        Substitute.For<ILogger<OpenAICompatibleCompletionService>>();

    private OpenAICompatibleCompletionService? _sut;

    public void Dispose()
    {
        _sut?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithEndpoint_CreatesInstance()
    {
        _sut = new OpenAICompatibleCompletionService(
            "https://api.openai.com/v1", "sk-test", "gpt-4o-mini", _logger);

        _sut.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullApiKey_CreatesInstanceWithoutAuth()
    {
        _sut = new OpenAICompatibleCompletionService(
            "https://localhost:11434/v1", null, "llama3", _logger);

        _sut.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidEndpoint_Throws(string? endpoint)
    {
        var act = () => new OpenAICompatibleCompletionService(
            endpoint!, "key", "model", _logger);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidModel_Throws(string? model)
    {
        var act = () => new OpenAICompatibleCompletionService(
            "https://api.openai.com/v1", "key", model!, _logger);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        var act = () => new OpenAICompatibleCompletionService(
            "https://api.openai.com/v1", "key", "model", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithHttpClient_CreatesInstance()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://api.openai.com/v1/") };

        _sut = new OpenAICompatibleCompletionService(httpClient, "gpt-4o-mini", _logger);

        _sut.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullHttpClient_Throws()
    {
        var act = () => new OpenAICompatibleCompletionService(
            (HttpClient)null!, "model", _logger);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region CompleteAsync Tests

    [Fact]
    public async Task CompleteAsync_ReturnsCompletionContent()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { role = "assistant", content = "Hello, world!" } }
            }
        });

        using var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        _sut = new OpenAICompatibleCompletionService(httpClient, "gpt-4o-mini", _logger);

        // Act
        var result = await _sut.CompleteAsync("Say hello");

        // Assert
        result.Should().Be("Hello, world!");
    }

    [Fact]
    public async Task CompleteAsync_WithOptions_IncludesSystemPromptAndMessages()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { role = "assistant", content = "Response" } }
            }
        });

        string? capturedBody = null;
        using var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK, body => capturedBody = body);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        _sut = new OpenAICompatibleCompletionService(httpClient, "gpt-4o-mini", _logger);

        var options = new CompletionOptions
        {
            SystemPrompt = "You are helpful",
            Messages = [new ChatMessage("user", "Previous message")],
            Temperature = 0.5f,
            MaxTokens = 100
        };

        // Act
        await _sut.CompleteAsync("New prompt", options);

        // Assert
        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("system");
        capturedBody.Should().Contain("You are helpful");
        capturedBody.Should().Contain("Previous message");
        capturedBody.Should().Contain("New prompt");
    }

    [Fact]
    public async Task CompleteAsync_WithJsonMode_SetsResponseFormat()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { role = "assistant", content = "{\"answer\": 42}" } }
            }
        });

        string? capturedBody = null;
        using var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK, body => capturedBody = body);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        _sut = new OpenAICompatibleCompletionService(httpClient, "gpt-4o-mini", _logger);

        var options = new CompletionOptions { JsonMode = true };

        // Act
        await _sut.CompleteAsync("Return JSON", options);

        // Assert
        capturedBody.Should().Contain("json_object");
    }

    [Fact]
    public async Task CompleteAsync_WithEmptyChoices_ReturnsEmptyString()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new { choices = Array.Empty<object>() });

        using var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        _sut = new OpenAICompatibleCompletionService(httpClient, "gpt-4o-mini", _logger);

        // Act
        var result = await _sut.CompleteAsync("Test");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteAsync_WithHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler("Error", HttpStatusCode.InternalServerError);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        _sut = new OpenAICompatibleCompletionService(httpClient, "gpt-4o-mini", _logger);

        // Act & Assert
        await _sut.Invoking(s => s.CompleteAsync("Test"))
            .Should().ThrowAsync<HttpRequestException>();
    }

    #endregion

    #region CompleteStreamingAsync Tests

    [Fact]
    public async Task CompleteStreamingAsync_YieldsTokens()
    {
        // Arrange
        var sseResponse = new StringBuilder();
        sseResponse.AppendLine("data: {\"choices\":[{\"delta\":{\"content\":\"Hello\"}}]}");
        sseResponse.AppendLine();
        sseResponse.AppendLine("data: {\"choices\":[{\"delta\":{\"content\":\" world\"}}]}");
        sseResponse.AppendLine();
        sseResponse.AppendLine("data: [DONE]");

        using var handler = new MockHttpMessageHandler(sseResponse.ToString(), HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        _sut = new OpenAICompatibleCompletionService(httpClient, "gpt-4o-mini", _logger);

        // Act
        var tokens = new List<string>();
        await foreach (var token in _sut.CompleteStreamingAsync("Test"))
        {
            tokens.Add(token);
        }

        // Assert
        tokens.Should().HaveCount(2);
        tokens.Should().ContainInOrder("Hello", " world");
    }

    [Fact]
    public async Task CompleteStreamingAsync_SkipsEmptyDeltas()
    {
        // Arrange
        var sseResponse = new StringBuilder();
        sseResponse.AppendLine("data: {\"choices\":[{\"delta\":{\"role\":\"assistant\"}}]}");
        sseResponse.AppendLine();
        sseResponse.AppendLine("data: {\"choices\":[{\"delta\":{\"content\":\"Result\"}}]}");
        sseResponse.AppendLine();
        sseResponse.AppendLine("data: [DONE]");

        using var handler = new MockHttpMessageHandler(sseResponse.ToString(), HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        _sut = new OpenAICompatibleCompletionService(httpClient, "gpt-4o-mini", _logger);

        // Act
        var tokens = new List<string>();
        await foreach (var token in _sut.CompleteStreamingAsync("Test"))
        {
            tokens.Add(token);
        }

        // Assert
        tokens.Should().ContainSingle().Which.Should().Be("Result");
    }

    [Fact]
    public async Task CompleteStreamingAsync_WithHttpError_Throws()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler("Error", HttpStatusCode.InternalServerError);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        _sut = new OpenAICompatibleCompletionService(httpClient, "gpt-4o-mini", _logger);

        // Act & Assert
        var tokens = new List<string>();
        var act = async () =>
        {
            await foreach (var token in _sut.CompleteStreamingAsync("Test"))
            {
                tokens.Add(token);
            }
        };
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task CompleteStreamingAsync_WithAlreadyCancelledToken_ThrowsOperationCanceled()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler("unused", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        _sut = new OpenAICompatibleCompletionService(httpClient, "gpt-4o-mini", _logger);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        var act = async () =>
        {
            await foreach (var _ in _sut.CompleteStreamingAsync("Test", cancellationToken: cts.Token)) { }
        };
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WithOwnedHttpClient_DisposesClient()
    {
        // Arrange
        _sut = new OpenAICompatibleCompletionService(
            "https://api.openai.com/v1", "key", "model", _logger);

        // Act
        _sut.Dispose();

        // Assert - no exception on double-dispose
        _sut.Dispose();
    }

    [Fact]
    public void Dispose_WithExternalHttpClient_DoesNotDisposeClient()
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://api.openai.com/v1/") };
        _sut = new OpenAICompatibleCompletionService(httpClient, "model", _logger);

        // Act
        _sut.Dispose();

        // Assert - httpClient still usable (BaseAddress still accessible)
        httpClient.BaseAddress.Should().NotBeNull();
    }

    #endregion

    #region Request Format Tests

    [Fact]
    public async Task CompleteAsync_SendsCorrectEndpoint()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { role = "assistant", content = "OK" } }
            }
        });

        Uri? capturedUri = null;
        using var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK, uri: u => capturedUri = u);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        _sut = new OpenAICompatibleCompletionService(httpClient, "gpt-4o-mini", _logger);

        // Act
        await _sut.CompleteAsync("Test");

        // Assert
        capturedUri.Should().NotBeNull();
        capturedUri!.PathAndQuery.Should().Contain("chat/completions");
    }

    [Fact]
    public async Task CompleteAsync_WithoutOptions_SendsMinimalRequest()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { role = "assistant", content = "OK" } }
            }
        });

        string? capturedBody = null;
        using var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK, body => capturedBody = body);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        _sut = new OpenAICompatibleCompletionService(httpClient, "gpt-4o-mini", _logger);

        // Act
        await _sut.CompleteAsync("Hello");

        // Assert
        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("gpt-4o-mini");
        capturedBody.Should().Contain("Hello");
        // Should not contain system message when no options
        capturedBody.Should().NotContain("\"system\"");
    }

    #endregion

    #region MockHttpMessageHandler

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly HttpStatusCode _statusCode;
        private readonly Action<string>? _captureBody;
        private readonly Action<Uri>? _captureUri;

        public MockHttpMessageHandler(
            string response,
            HttpStatusCode statusCode,
            Action<string>? captureBody = null,
            Action<Uri>? uri = null)
        {
            _response = response;
            _statusCode = statusCode;
            _captureBody = captureBody;
            _captureUri = uri;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _captureUri?.Invoke(request.RequestUri!);

            if (request.Content is not null)
            {
                var body = await request.Content.ReadAsStringAsync(cancellationToken);
                _captureBody?.Invoke(body);
            }

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json")
            };
        }
    }

    #endregion
}

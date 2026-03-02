using FluentAssertions;
using FluxImprover.LMSupply;
using FluxImprover.Services;
using LMSupply.Generator.Abstractions;
using LMSupply.Generator.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

using LMChatMessage = LMSupply.Generator.Models.ChatMessage;
using LMChatRole = LMSupply.Generator.Models.ChatRole;
using LMGenerationOptions = LMSupply.Generator.Models.GenerationOptions;
using FluxChatMessage = FluxImprover.Services.ChatMessage;

namespace FluxImprover.Tests.Services.Providers;

public class LMSupplyCompletionServiceTests : IAsyncDisposable
{
    private readonly IGeneratorModel _model = Substitute.For<IGeneratorModel>();
    private readonly ILogger<LMSupplyCompletionService> _logger =
        Substitute.For<ILogger<LMSupplyCompletionService>>();

    private LMSupplyCompletionService? _sut;

    public async ValueTask DisposeAsync()
    {
        if (_sut is not null)
        {
            await _sut.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidArgs_CreatesInstance()
    {
        _sut = new LMSupplyCompletionService(_model, _logger);

        _sut.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullModel_Throws()
    {
        var act = () => new LMSupplyCompletionService(null!, _logger);

        act.Should().Throw<ArgumentNullException>().WithParameterName("model");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        var act = () => new LMSupplyCompletionService(_model, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithCustomDefaults_UsesDefaults()
    {
        _sut = new LMSupplyCompletionService(_model, _logger,
            defaultTemperature: 0.7f, defaultMaxTokens: 1024);

        _sut.Should().NotBeNull();
    }

    #endregion

    #region CompleteAsync Tests

    [Fact]
    public async Task CompleteAsync_ReturnsModelResponse()
    {
        // Arrange
        _model.ModelId.Returns("test-model");
        _model.GenerateChatCompleteAsync(
                Arg.Any<IEnumerable<LMChatMessage>>(),
                Arg.Any<LMGenerationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns("Generated response");

        _sut = new LMSupplyCompletionService(_model, _logger);

        // Act
        var result = await _sut.CompleteAsync("Hello");

        // Assert
        result.Should().Be("Generated response");
    }

    [Fact]
    public async Task CompleteAsync_WithSystemPrompt_IncludesSystemMessage()
    {
        // Arrange
        _model.ModelId.Returns("test-model");
        IEnumerable<LMChatMessage>? capturedMessages = null;
        _model.GenerateChatCompleteAsync(
                Arg.Do<IEnumerable<LMChatMessage>>(m => capturedMessages = m.ToList()),
                Arg.Any<LMGenerationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns("OK");

        _sut = new LMSupplyCompletionService(_model, _logger);

        var options = new CompletionOptions { SystemPrompt = "Be helpful" };

        // Act
        await _sut.CompleteAsync("Question", options);

        // Assert
        var messages = capturedMessages!.ToList();
        messages.Should().HaveCount(2);
        messages[0].Role.Should().Be(LMChatRole.System);
        messages[0].Content.Should().Be("Be helpful");
        messages[1].Role.Should().Be(LMChatRole.User);
        messages[1].Content.Should().Be("Question");
    }

    [Fact]
    public async Task CompleteAsync_WithPreviousMessages_IncludesAll()
    {
        // Arrange
        _model.ModelId.Returns("test-model");
        IEnumerable<LMChatMessage>? capturedMessages = null;
        _model.GenerateChatCompleteAsync(
                Arg.Do<IEnumerable<LMChatMessage>>(m => capturedMessages = m.ToList()),
                Arg.Any<LMGenerationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns("OK");

        _sut = new LMSupplyCompletionService(_model, _logger);

        var options = new CompletionOptions
        {
            SystemPrompt = "System",
            Messages =
            [
                new FluxChatMessage("user", "First"),
                new FluxChatMessage("assistant", "Reply")
            ]
        };

        // Act
        await _sut.CompleteAsync("Follow-up", options);

        // Assert
        var messages = capturedMessages!.ToList();
        messages.Should().HaveCount(4); // system + user + assistant + user prompt
        messages[0].Role.Should().Be(LMChatRole.System);
        messages[1].Role.Should().Be(LMChatRole.User);
        messages[1].Content.Should().Be("First");
        messages[2].Role.Should().Be(LMChatRole.Assistant);
        messages[2].Content.Should().Be("Reply");
        messages[3].Role.Should().Be(LMChatRole.User);
        messages[3].Content.Should().Be("Follow-up");
    }

    [Fact]
    public async Task CompleteAsync_WithTemperatureAndMaxTokens_PassesOptions()
    {
        // Arrange
        _model.ModelId.Returns("test-model");
        LMGenerationOptions? capturedOptions = null;
        _model.GenerateChatCompleteAsync(
                Arg.Any<IEnumerable<LMChatMessage>>(),
                Arg.Do<LMGenerationOptions>(o => capturedOptions = o),
                Arg.Any<CancellationToken>())
            .Returns("OK");

        _sut = new LMSupplyCompletionService(_model, _logger);

        var options = new CompletionOptions
        {
            Temperature = 0.9f,
            MaxTokens = 2048
        };

        // Act
        await _sut.CompleteAsync("Test", options);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Temperature.Should().Be(0.9f);
        capturedOptions.MaxTokens.Should().Be(2048);
        capturedOptions.DoSample.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteAsync_WithoutOptions_UsesDefaults()
    {
        // Arrange
        _model.ModelId.Returns("test-model");
        LMGenerationOptions? capturedOptions = null;
        _model.GenerateChatCompleteAsync(
                Arg.Any<IEnumerable<LMChatMessage>>(),
                Arg.Do<LMGenerationOptions>(o => capturedOptions = o),
                Arg.Any<CancellationToken>())
            .Returns("OK");

        _sut = new LMSupplyCompletionService(_model, _logger,
            defaultTemperature: 0.5f, defaultMaxTokens: 256);

        // Act
        await _sut.CompleteAsync("Test");

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Temperature.Should().Be(0.5f);
        capturedOptions.MaxTokens.Should().Be(256);
    }

    [Fact]
    public async Task CompleteAsync_WithJsonMode_SetsJsonSchema()
    {
        // Arrange
        _model.ModelId.Returns("test-model");
        LMGenerationOptions? capturedOptions = null;
        _model.GenerateChatCompleteAsync(
                Arg.Any<IEnumerable<LMChatMessage>>(),
                Arg.Do<LMGenerationOptions>(o => capturedOptions = o),
                Arg.Any<CancellationToken>())
            .Returns("{\"key\":\"value\"}");

        _sut = new LMSupplyCompletionService(_model, _logger);

        var schema = "{\"type\":\"object\",\"properties\":{\"key\":{\"type\":\"string\"}}}";
        var options = new CompletionOptions
        {
            JsonMode = true,
            ResponseSchema = schema
        };

        // Act
        await _sut.CompleteAsync("Return JSON", options);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.JsonSchema.Should().Be(schema);
    }

    [Fact]
    public async Task CompleteAsync_WithJsonModeFalse_DoesNotSetJsonSchema()
    {
        // Arrange
        _model.ModelId.Returns("test-model");
        LMGenerationOptions? capturedOptions = null;
        _model.GenerateChatCompleteAsync(
                Arg.Any<IEnumerable<LMChatMessage>>(),
                Arg.Do<LMGenerationOptions>(o => capturedOptions = o),
                Arg.Any<CancellationToken>())
            .Returns("text");

        _sut = new LMSupplyCompletionService(_model, _logger);

        var options = new CompletionOptions { JsonMode = false, ResponseSchema = "should-be-ignored" };

        // Act
        await _sut.CompleteAsync("Test", options);

        // Assert
        capturedOptions!.JsonSchema.Should().BeNull();
    }

    #endregion

    #region CompleteStreamingAsync Tests

    [Fact]
    public async Task CompleteStreamingAsync_YieldsTokens()
    {
        // Arrange
        _model.ModelId.Returns("test-model");
        var tokens = new[] { "Hello", " ", "world" };
        _model.GenerateChatAsync(
                Arg.Any<IEnumerable<LMChatMessage>>(),
                Arg.Any<LMGenerationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(tokens.ToAsyncEnumerable());

        _sut = new LMSupplyCompletionService(_model, _logger);

        // Act
        var results = new List<string>();
        await foreach (var token in _sut.CompleteStreamingAsync("Test"))
        {
            results.Add(token);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().ContainInOrder("Hello", " ", "world");
    }

    [Fact]
    public async Task CompleteStreamingAsync_WithOptions_PassesOptionsToModel()
    {
        // Arrange
        _model.ModelId.Returns("test-model");
        LMGenerationOptions? capturedOptions = null;
        _model.GenerateChatAsync(
                Arg.Any<IEnumerable<LMChatMessage>>(),
                Arg.Do<LMGenerationOptions>(o => capturedOptions = o),
                Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerable.Empty<string>());

        _sut = new LMSupplyCompletionService(_model, _logger);

        var options = new CompletionOptions { Temperature = 1.0f, MaxTokens = 500 };

        // Act
        await foreach (var _ in _sut.CompleteStreamingAsync("Test", options)) { }

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Temperature.Should().Be(1.0f);
        capturedOptions.MaxTokens.Should().Be(500);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_DisposesUnderlyingModel()
    {
        // Arrange
        _sut = new LMSupplyCompletionService(_model, _logger);

        // Act
        await _sut.DisposeAsync();

        // Assert
        await _model.Received(1).DisposeAsync();
    }

    #endregion

    #region Role Mapping Tests

    [Theory]
    [InlineData("system", LMChatRole.System)]
    [InlineData("SYSTEM", LMChatRole.System)]
    [InlineData("System", LMChatRole.System)]
    [InlineData("assistant", LMChatRole.Assistant)]
    [InlineData("ASSISTANT", LMChatRole.Assistant)]
    [InlineData("user", LMChatRole.User)]
    [InlineData("USER", LMChatRole.User)]
    [InlineData("unknown", LMChatRole.User)] // defaults to User
    public async Task CompleteAsync_MapsRolesCorrectly(string inputRole, LMChatRole expectedRole)
    {
        // Arrange
        _model.ModelId.Returns("test-model");
        IEnumerable<LMChatMessage>? capturedMessages = null;
        _model.GenerateChatCompleteAsync(
                Arg.Do<IEnumerable<LMChatMessage>>(m => capturedMessages = m.ToList()),
                Arg.Any<LMGenerationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns("OK");

        _sut = new LMSupplyCompletionService(_model, _logger);

        var options = new CompletionOptions
        {
            Messages = [new FluxChatMessage(inputRole, "content")]
        };

        // Act
        await _sut.CompleteAsync("Prompt", options);

        // Assert
        var messages = capturedMessages!.ToList();
        // messages[0] is from options.Messages, messages[1] is the prompt
        messages[0].Role.Should().Be(expectedRole);
    }

    #endregion
}

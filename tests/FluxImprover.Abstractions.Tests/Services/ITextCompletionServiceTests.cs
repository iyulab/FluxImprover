namespace FluxImprover.Abstractions.Tests.Services;

using FluentAssertions;
using FluxImprover.Abstractions.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public class ITextCompletionServiceTests
{
    [Fact]
    public async Task CompleteAsync_WithValidPrompt_ReturnsNonEmptyString()
    {
        // Arrange
        var service = Substitute.For<ITextCompletionService>();
        service.CompleteAsync(Arg.Any<string>(), Arg.Any<CompletionOptions?>(), Arg.Any<CancellationToken>())
            .Returns("Generated response");

        // Act
        var result = await service.CompleteAsync("Test prompt");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Be("Generated response");
    }

    [Fact]
    public async Task CompleteAsync_WithOptions_PassesOptionsCorrectly()
    {
        // Arrange
        var service = Substitute.For<ITextCompletionService>();
        var options = new CompletionOptions
        {
            Temperature = 0.5f,
            MaxTokens = 100,
            SystemPrompt = "You are a helpful assistant."
        };

        service.CompleteAsync(Arg.Any<string>(), Arg.Any<CompletionOptions?>(), Arg.Any<CancellationToken>())
            .Returns("Response with options");

        // Act
        var result = await service.CompleteAsync("prompt", options);

        // Assert
        await service.Received(1).CompleteAsync("prompt", options, Arg.Any<CancellationToken>());
        result.Should().Be("Response with options");
    }

    [Fact]
    public async Task CompleteAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var service = Substitute.For<ITextCompletionService>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        service.CompleteAsync(Arg.Any<string>(), Arg.Any<CompletionOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await service.Invoking(s => s.CompleteAsync("prompt", null, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CompleteStreamingAsync_WithValidPrompt_YieldsTokens()
    {
        // Arrange
        var service = Substitute.For<ITextCompletionService>();
        var tokens = new[] { "Hello", " ", "World" };

        service.CompleteStreamingAsync(Arg.Any<string>(), Arg.Any<CompletionOptions?>(), Arg.Any<CancellationToken>())
            .Returns(tokens.ToAsyncEnumerable());

        // Act
        var results = new List<string>();
        await foreach (var token in service.CompleteStreamingAsync("Test prompt"))
        {
            results.Add(token);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().ContainInOrder("Hello", " ", "World");
    }
}

public class CompletionOptionsTests
{
    [Fact]
    public void DefaultOptions_HasValidDefaults()
    {
        // Act
        var options = new CompletionOptions();

        // Assert
        options.Temperature.Should().Be(0.7f);
        options.MaxTokens.Should().BeNull();
        options.SystemPrompt.Should().BeNull();
        options.Messages.Should().BeNull();
    }

    [Fact]
    public void Options_WithCustomValues_RetainsValues()
    {
        // Act
        var options = new CompletionOptions
        {
            Temperature = 0.3f,
            MaxTokens = 500,
            SystemPrompt = "Custom system prompt"
        };

        // Assert
        options.Temperature.Should().Be(0.3f);
        options.MaxTokens.Should().Be(500);
        options.SystemPrompt.Should().Be("Custom system prompt");
    }
}

public class ChatMessageTests
{
    [Fact]
    public void ChatMessage_WithRoleAndContent_StoresCorrectly()
    {
        // Act
        var message = new ChatMessage("user", "Hello!");

        // Assert
        message.Role.Should().Be("user");
        message.Content.Should().Be("Hello!");
    }

    [Fact]
    public void ChatMessage_Equality_WorksCorrectly()
    {
        // Arrange
        var message1 = new ChatMessage("user", "Hello");
        var message2 = new ChatMessage("user", "Hello");
        var message3 = new ChatMessage("assistant", "Hello");

        // Assert
        message1.Should().Be(message2);
        message1.Should().NotBe(message3);
    }
}

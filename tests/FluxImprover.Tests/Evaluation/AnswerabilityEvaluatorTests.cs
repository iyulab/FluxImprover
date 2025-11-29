namespace FluxImprover.Tests.Evaluation;

using FluentAssertions;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.Services;
using FluxImprover.Evaluation;
using NSubstitute;
using Xunit;

public sealed class AnswerabilityEvaluatorTests
{
    private readonly ITextCompletionService _completionService;
    private readonly AnswerabilityEvaluator _sut;

    public AnswerabilityEvaluatorTests()
    {
        _completionService = Substitute.For<ITextCompletionService>();
        _sut = new AnswerabilityEvaluator(_completionService);
    }

    [Fact]
    public async Task EvaluateAsync_WithAnswerableQuestion_ReturnsHighScore()
    {
        // Arrange
        var context = "The capital of France is Paris. It is known for the Eiffel Tower.";
        var question = "What is the capital of France?";
        var expectedResponse = @"{""score"": 1.0, ""reasoning"": ""The context directly contains information to answer this question."", ""answerable"": true}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.EvaluateAsync(context, question);

        // Assert
        result.Should().NotBeNull();
        result.Score.Should().BeApproximately(1.0, 0.01);
        result.MetricName.Should().Be("Answerability");
    }

    [Fact]
    public async Task EvaluateAsync_WithUnanswerableQuestion_ReturnsLowScore()
    {
        // Arrange
        var context = "The capital of France is Paris.";
        var question = "What is the population of Tokyo?";
        var expectedResponse = @"{""score"": 0.0, ""reasoning"": ""The context does not contain information about Tokyo."", ""answerable"": false}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.EvaluateAsync(context, question);

        // Assert
        result.Score.Should().BeApproximately(0.0, 0.01);
    }

    [Fact]
    public async Task EvaluateAsync_WithPartiallyAnswerableQuestion_ReturnsMidScore()
    {
        // Arrange
        var context = "Paris is the capital of France. The Eiffel Tower is located there.";
        var question = "What is the capital of France and when was it founded?";
        var expectedResponse = @"{""score"": 0.5, ""reasoning"": ""The capital is mentioned but the founding date is not."", ""answerable"": true}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.EvaluateAsync(context, question);

        // Assert
        result.Score.Should().BeApproximately(0.5, 0.01);
    }

    [Fact]
    public async Task EvaluateAsync_WithEmptyContext_ReturnsZeroScore()
    {
        // Arrange & Act
        var result = await _sut.EvaluateAsync(string.Empty, "What is X?");

        // Assert
        result.Score.Should().Be(0.0);
        await _completionService.DidNotReceive().CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_WithEmptyQuestion_ReturnsZeroScore()
    {
        // Arrange & Act
        var result = await _sut.EvaluateAsync("Some context", string.Empty);

        // Assert
        result.Score.Should().Be(0.0);
    }

    [Fact]
    public async Task EvaluateAsync_WithOptions_UsesProvidedOptions()
    {
        // Arrange
        var options = new EvaluationOptions { Temperature = 0.15f };
        var expectedResponse = @"{""score"": 0.8, ""reasoning"": ""Good"", ""answerable"": true}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _sut.EvaluateAsync("context", "question", options);

        // Assert
        await _completionService.Received(1).CompleteAsync(
            Arg.Any<string>(),
            Arg.Is<CompletionOptions>(o => o.Temperature == 0.15f),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsAnswerabilityInDetails()
    {
        // Arrange
        var expectedResponse = @"{""score"": 0.8, ""reasoning"": ""Can be answered with context"", ""answerable"": true, ""evidence"": ""Paris is the capital""}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.EvaluateAsync("context", "question");

        // Assert
        result.Details.Should().ContainKey("reasoning");
        result.Details.Should().ContainKey("answerable");
    }

    [Fact]
    public async Task EvaluateAsync_WithInvalidJsonResponse_ReturnsZeroScore()
    {
        // Arrange
        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("invalid json");

        // Act
        var result = await _sut.EvaluateAsync("context", "question");

        // Assert
        result.Score.Should().Be(0.0);
    }

    [Fact]
    public async Task EvaluateBatchAsync_WithMultiplePairs_ReturnsAllResults()
    {
        // Arrange
        var pairs = new[]
        {
            ("Context 1", "Question 1"),
            ("Context 2", "Question 2")
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(
                @"{""score"": 1.0, ""reasoning"": ""Answerable"", ""answerable"": true}",
                @"{""score"": 0.0, ""reasoning"": ""Not answerable"", ""answerable"": false}");

        // Act
        var results = await _sut.EvaluateBatchAsync(pairs);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task EvaluateAsync_WithMultipleContexts_HandlesContextList()
    {
        // Arrange
        var contexts = new[] { "Context about Paris.", "Context about France." };
        var question = "What is the capital of France?";
        var expectedResponse = @"{""score"": 1.0, ""reasoning"": ""Combined context provides answer"", ""answerable"": true}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.EvaluateWithMultipleContextsAsync(contexts, question);

        // Assert
        result.Score.Should().BeApproximately(1.0, 0.01);
    }
}

namespace FluxImprover.Tests.Evaluation;

using FluentAssertions;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.Services;
using FluxImprover.Evaluation;
using NSubstitute;
using Xunit;

public sealed class RelevancyEvaluatorTests
{
    private readonly ITextCompletionService _completionService;
    private readonly RelevancyEvaluator _sut;

    public RelevancyEvaluatorTests()
    {
        _completionService = Substitute.For<ITextCompletionService>();
        _sut = new RelevancyEvaluator(_completionService);
    }

    [Fact]
    public async Task EvaluateAsync_WithRelevantAnswer_ReturnsHighScore()
    {
        // Arrange
        var question = "What is the capital of France?";
        var answer = "The capital of France is Paris.";
        var expectedResponse = @"{""score"": 1.0, ""reasoning"": ""The answer directly addresses the question.""}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.EvaluateAsync(question, answer);

        // Assert
        result.Should().NotBeNull();
        result.Score.Should().BeApproximately(1.0, 0.01);
        result.MetricName.Should().Be("Relevancy");
    }

    [Fact]
    public async Task EvaluateAsync_WithIrrelevantAnswer_ReturnsLowScore()
    {
        // Arrange
        var question = "What is the capital of France?";
        var answer = "Python is a programming language.";
        var expectedResponse = @"{""score"": 0.0, ""reasoning"": ""The answer is completely unrelated to the question.""}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.EvaluateAsync(question, answer);

        // Assert
        result.Score.Should().BeApproximately(0.0, 0.01);
    }

    [Fact]
    public async Task EvaluateAsync_WithPartiallyRelevantAnswer_ReturnsMidScore()
    {
        // Arrange
        var question = "What is the capital of France and its population?";
        var answer = "Paris is the capital of France.";
        var expectedResponse = @"{""score"": 0.5, ""reasoning"": ""The answer addresses the capital but not the population.""}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.EvaluateAsync(question, answer);

        // Assert
        result.Score.Should().BeApproximately(0.5, 0.01);
    }

    [Fact]
    public async Task EvaluateAsync_WithEmptyQuestion_ReturnsZeroScore()
    {
        // Arrange & Act
        var result = await _sut.EvaluateAsync(string.Empty, "Some answer");

        // Assert
        result.Score.Should().Be(0.0);
        await _completionService.DidNotReceive().CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_WithEmptyAnswer_ReturnsZeroScore()
    {
        // Arrange & Act
        var result = await _sut.EvaluateAsync("What is X?", string.Empty);

        // Assert
        result.Score.Should().Be(0.0);
    }

    [Fact]
    public async Task EvaluateAsync_WithOptions_UsesProvidedOptions()
    {
        // Arrange
        var options = new EvaluationOptions { Temperature = 0.2f };
        var expectedResponse = @"{""score"": 0.8, ""reasoning"": ""Good""}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _sut.EvaluateAsync("question", "answer", options);

        // Assert
        await _completionService.Received(1).CompleteAsync(
            Arg.Any<string>(),
            Arg.Is<CompletionOptions>(o => o.Temperature == 0.2f),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_WithContext_IncludesContextInEvaluation()
    {
        // Arrange
        var question = "What is the main topic?";
        var answer = "Machine learning";
        var context = "This document discusses machine learning and AI.";
        var expectedResponse = @"{""score"": 0.9, ""reasoning"": ""Answer matches context topic""}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.EvaluateAsync(question, answer, context: context);

        // Assert
        result.Score.Should().BeApproximately(0.9, 0.01);
        await _completionService.Received(1).CompleteAsync(
            Arg.Is<string>(s => s.Contains(context)),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsReasoningInDetails()
    {
        // Arrange
        var expectedResponse = @"{""score"": 0.8, ""reasoning"": ""The answer is relevant but could be more specific.""}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.EvaluateAsync("question", "answer");

        // Assert
        result.Details.Should().ContainKey("reasoning");
    }

    [Fact]
    public async Task EvaluateBatchAsync_WithMultiplePairs_ReturnsAllResults()
    {
        // Arrange
        var pairs = new[]
        {
            ("Question 1", "Answer 1"),
            ("Question 2", "Answer 2")
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(
                @"{""score"": 0.9, ""reasoning"": ""Good""}",
                @"{""score"": 0.7, ""reasoning"": ""Fair""}");

        // Act
        var results = await _sut.EvaluateBatchAsync(pairs);

        // Assert
        results.Should().HaveCount(2);
    }
}

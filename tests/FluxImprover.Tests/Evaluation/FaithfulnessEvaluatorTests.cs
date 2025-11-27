namespace FluxImprover.Tests.Evaluation;

using FluentAssertions;
using FluxImprover.Abstractions.Models;
using FluxImprover.Abstractions.Options;
using FluxImprover.Abstractions.Services;
using FluxImprover.Evaluation;
using NSubstitute;
using Xunit;

public sealed class FaithfulnessEvaluatorTests
{
    private readonly ITextCompletionService _completionService;
    private readonly FaithfulnessEvaluator _sut;

    public FaithfulnessEvaluatorTests()
    {
        _completionService = Substitute.For<ITextCompletionService>();
        _sut = new FaithfulnessEvaluator(_completionService);
    }

    [Fact]
    public async Task EvaluateAsync_WithFaithfulAnswer_ReturnsHighScore()
    {
        // Arrange
        var context = "The capital of France is Paris. Paris has a population of 2 million.";
        var answer = "Paris is the capital of France.";
        var expectedResponse = @"{""score"": 1.0, ""reasoning"": ""The answer is fully supported by the context."", ""claims"": [{""claim"": ""Paris is the capital of France"", ""supported"": true}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.EvaluateAsync(context, answer);

        // Assert
        result.Should().NotBeNull();
        result.Score.Should().BeApproximately(1.0, 0.01);
        result.MetricName.Should().Be("Faithfulness");
    }

    [Fact]
    public async Task EvaluateAsync_WithUnfaithfulAnswer_ReturnsLowScore()
    {
        // Arrange
        var context = "The capital of France is Paris.";
        var answer = "Berlin is the capital of France.";
        var expectedResponse = @"{""score"": 0.0, ""reasoning"": ""The answer contradicts the context."", ""claims"": [{""claim"": ""Berlin is the capital of France"", ""supported"": false}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.EvaluateAsync(context, answer);

        // Assert
        result.Score.Should().BeApproximately(0.0, 0.01);
    }

    [Fact]
    public async Task EvaluateAsync_WithOptions_UsesProvidedOptions()
    {
        // Arrange
        var context = "Some context";
        var answer = "Some answer";
        var options = new EvaluationOptions { Temperature = 0.1f };
        var expectedResponse = @"{""score"": 0.8, ""reasoning"": ""Mostly faithful""}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _sut.EvaluateAsync(context, answer, options);

        // Assert
        await _completionService.Received(1).CompleteAsync(
            Arg.Any<string>(),
            Arg.Is<CompletionOptions>(o => o.Temperature == 0.1f),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_WithEmptyContext_ReturnsZeroScore()
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
        var result = await _sut.EvaluateAsync("Some context", string.Empty);

        // Assert
        result.Score.Should().Be(0.0);
    }

    [Fact]
    public async Task EvaluateAsync_WithInvalidJsonResponse_ReturnsZeroScore()
    {
        // Arrange
        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("invalid json response");

        // Act
        var result = await _sut.EvaluateAsync("context", "answer");

        // Assert
        result.Score.Should().Be(0.0);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsClaimsInDetails()
    {
        // Arrange
        var expectedResponse = @"{""score"": 0.5, ""reasoning"": ""Partial support"", ""claims"": [{""claim"": ""Claim 1"", ""supported"": true}, {""claim"": ""Claim 2"", ""supported"": false}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.EvaluateAsync("context", "answer with claims");

        // Assert
        result.Details.Should().ContainKey("claims");
        result.Details.Should().ContainKey("reasoning");
    }

    [Fact]
    public async Task EvaluateBatchAsync_WithMultiplePairs_ReturnsAllResults()
    {
        // Arrange
        var pairs = new[]
        {
            ("Context 1", "Answer 1"),
            ("Context 2", "Answer 2")
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

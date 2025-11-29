namespace FluxImprover.Tests.QAGeneration;

using FluentAssertions;
using FluxImprover.Options;
using FluxImprover.Services;
using FluxImprover.Evaluation;
using FluxImprover.QAGeneration;
using NSubstitute;
using Xunit;

public sealed class QAFilterServiceTests
{
    private readonly ITextCompletionService _completionService;
    private readonly FaithfulnessEvaluator _faithfulnessEvaluator;
    private readonly RelevancyEvaluator _relevancyEvaluator;
    private readonly AnswerabilityEvaluator _answerabilityEvaluator;
    private readonly QAFilterService _sut;

    public QAFilterServiceTests()
    {
        _completionService = Substitute.For<ITextCompletionService>();
        _faithfulnessEvaluator = new FaithfulnessEvaluator(_completionService);
        _relevancyEvaluator = new RelevancyEvaluator(_completionService);
        _answerabilityEvaluator = new AnswerabilityEvaluator(_completionService);
        _sut = new QAFilterService(_faithfulnessEvaluator, _relevancyEvaluator, _answerabilityEvaluator);
    }

    [Fact]
    public async Task FilterAsync_WithHighQualityPairs_ReturnsAllPairs()
    {
        // Arrange
        var pairs = new[]
        {
            new GeneratedQAPair { Question = "Q1", Answer = "A1", Context = "Context 1" },
            new GeneratedQAPair { Question = "Q2", Answer = "A2", Context = "Context 2" }
        };

        SetupHighScoreEvaluators();

        // Act
        var result = await _sut.FilterAsync(pairs);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FilterAsync_WithLowQualityPairs_FiltersOutBadPairs()
    {
        // Arrange
        var pairs = new[]
        {
            new GeneratedQAPair { Question = "Q1", Answer = "A1", Context = "Context 1" },
            new GeneratedQAPair { Question = "Q2", Answer = "A2", Context = "Context 2" }
        };

        // First pair returns high score, second pair returns low score
        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(
                @"{""score"": 0.9, ""reasoning"": ""Good""}",
                @"{""score"": 0.9, ""reasoning"": ""Good""}",
                @"{""score"": 0.9, ""reasoning"": ""Good""}",
                @"{""score"": 0.3, ""reasoning"": ""Bad""}",
                @"{""score"": 0.3, ""reasoning"": ""Bad""}",
                @"{""score"": 0.3, ""reasoning"": ""Bad""}");

        // Act
        var result = await _sut.FilterAsync(pairs);

        // Assert
        result.Should().HaveCount(1);
        result[0].Question.Should().Be("Q1");
    }

    [Fact]
    public async Task FilterAsync_WithCustomThresholds_UsesProvidedThresholds()
    {
        // Arrange
        var pairs = new[]
        {
            new GeneratedQAPair { Question = "Q1", Answer = "A1", Context = "Context" }
        };

        var options = new QAFilterOptions
        {
            MinFaithfulness = 0.8,
            MinRelevancy = 0.7,
            MinAnswerability = 0.6
        };

        // Score of 0.75 is below MinFaithfulness of 0.8
        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""score"": 0.75, ""reasoning"": ""Below threshold""}");

        // Act
        var result = await _sut.FilterAsync(pairs, options);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange & Act
        var result = await _sut.FilterAsync(Array.Empty<GeneratedQAPair>());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterAsync_AddsEvaluationScoresToPairs()
    {
        // Arrange
        var pairs = new[]
        {
            new GeneratedQAPair { Question = "Q1", Answer = "A1", Context = "Context" }
        };

        SetupHighScoreEvaluators();

        // Act
        var result = await _sut.FilterAsync(pairs);

        // Assert
        result.Should().ContainSingle();
        result[0].Evaluation.Should().NotBeNull();
        result[0].Evaluation!.Faithfulness.Should().BeApproximately(0.9, 0.01);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsEvaluationWithoutFiltering()
    {
        // Arrange
        var pair = new GeneratedQAPair { Question = "Q1", Answer = "A1", Context = "Context" };

        // Low scores but still returned (no filtering)
        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(
                @"{""score"": 0.3, ""reasoning"": ""Low faithfulness""}",
                @"{""score"": 0.4, ""reasoning"": ""Low relevancy""}",
                @"{""score"": 0.5, ""reasoning"": ""Low answerability""}");

        // Act
        var result = await _sut.EvaluateAsync(pair);

        // Assert
        result.Should().NotBeNull();
        result.Evaluation!.Faithfulness.Should().BeApproximately(0.3, 0.01);
        result.Evaluation.Relevancy.Should().BeApproximately(0.4, 0.01);
    }

    [Fact]
    public async Task FilterAsync_WithMissingContext_SkipsEvaluation()
    {
        // Arrange
        var pairs = new[]
        {
            new GeneratedQAPair { Question = "Q1", Answer = "A1", Context = null }
        };

        // Act
        var result = await _sut.FilterAsync(pairs);

        // Assert
        result.Should().BeEmpty();
    }

    private void SetupHighScoreEvaluators()
    {
        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""score"": 0.9, ""reasoning"": ""Good""}");
    }
}

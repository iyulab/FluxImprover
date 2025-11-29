namespace FluxImprover.Tests.QAGeneration;

using FluentAssertions;
using FluxImprover.Evaluation;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.QAGeneration;
using FluxImprover.Services;
using NSubstitute;
using Xunit;

public sealed class QAPipelineTests
{
    private readonly ITextCompletionService _completionService;
    private readonly QAGeneratorService _generator;
    private readonly QAFilterService _filter;
    private readonly QAPipeline _sut;

    public QAPipelineTests()
    {
        _completionService = Substitute.For<ITextCompletionService>();
        _generator = new QAGeneratorService(_completionService);

        var faithfulness = new FaithfulnessEvaluator(_completionService);
        var relevancy = new RelevancyEvaluator(_completionService);
        var answerability = new AnswerabilityEvaluator(_completionService);
        _filter = new QAFilterService(faithfulness, relevancy, answerability);

        _sut = new QAPipeline(_generator, _filter);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidContext_GeneratesAndFiltersQAPairs()
    {
        // Arrange
        var context = "Paris is the capital of France.";

        // Setup generator response
        _completionService.CompleteAsync(
            Arg.Is<string>(s => s.Contains("Generate")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""qa_pairs"": [{""question"": ""Q1"", ""answer"": ""A1""}]}");

        // Setup evaluator responses
        _completionService.CompleteAsync(
            Arg.Is<string>(s => s.Contains("Evaluate") || s.Contains("faithfulness") || s.Contains("relevancy") || s.Contains("answerable")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""score"": 0.9, ""reasoning"": ""Good""}");

        // Act
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.GeneratedCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithSkipFilter_ReturnsAllGenerated()
    {
        // Arrange
        var context = "Test context";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""qa_pairs"": [{""question"": ""Q1"", ""answer"": ""A1""}, {""question"": ""Q2"", ""answer"": ""A2""}]}");

        var options = new QAPipelineOptions { SkipFiltering = true };

        // Act
        var result = await _sut.ExecuteAsync(context, options);

        // Assert
        result.GeneratedCount.Should().Be(2);
        result.FilteredCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyContext_ReturnsEmptyResult()
    {
        // Arrange & Act
        var result = await _sut.ExecuteAsync(string.Empty);

        // Assert
        result.GeneratedCount.Should().Be(0);
        result.QAPairs.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithMultipleContexts_ProcessesAll()
    {
        // Arrange
        var contexts = new[] { "Context 1", "Context 2" };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(
                @"{""qa_pairs"": [{""question"": ""Q1"", ""answer"": ""A1""}]}",
                @"{""score"": 0.9, ""reasoning"": ""Good""}",
                @"{""score"": 0.9, ""reasoning"": ""Good""}",
                @"{""score"": 0.9, ""reasoning"": ""Good""}",
                @"{""qa_pairs"": [{""question"": ""Q2"", ""answer"": ""A2""}]}",
                @"{""score"": 0.9, ""reasoning"": ""Good""}",
                @"{""score"": 0.9, ""reasoning"": ""Good""}",
                @"{""score"": 0.9, ""reasoning"": ""Good""}");

        // Act
        var results = await _sut.ExecuteBatchAsync(contexts);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteFromChunkAsync_UsesChunkContent()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "chunk-1",
            Content = "Chunk content about something."
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(
                @"{""qa_pairs"": [{""question"": ""Q1"", ""answer"": ""A1""}]}",
                @"{""score"": 0.9, ""reasoning"": ""Good""}");

        var options = new QAPipelineOptions { SkipFiltering = true };

        // Act
        var result = await _sut.ExecuteFromChunkAsync(chunk, options);

        // Assert
        result.QAPairs.Should().ContainSingle();
        result.QAPairs[0].SourceId.Should().Be("chunk-1");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFilteredOutCount()
    {
        // Arrange
        var context = "Test context";

        // Generate 2 pairs
        _completionService.CompleteAsync(
            Arg.Is<string>(s => s.Contains("Generate") || s.Contains("qa_pairs")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""qa_pairs"": [{""question"": ""Q1"", ""answer"": ""A1""}, {""question"": ""Q2"", ""answer"": ""A2""}]}");

        // First passes, second fails
        _completionService.CompleteAsync(
            Arg.Is<string>(s => !s.Contains("Generate") && !s.Contains("qa_pairs")),
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
        var result = await _sut.ExecuteAsync(context);

        // Assert
        result.GeneratedCount.Should().Be(2);
        result.FilteredCount.Should().Be(1);
        result.FilteredOutCount.Should().Be(1);
    }
}

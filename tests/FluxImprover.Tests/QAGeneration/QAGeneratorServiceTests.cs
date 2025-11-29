namespace FluxImprover.Tests.QAGeneration;

using FluentAssertions;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.QAGeneration;
using FluxImprover.Services;
using NSubstitute;
using Xunit;

public sealed class QAGeneratorServiceTests
{
    private readonly ITextCompletionService _completionService;
    private readonly QAGeneratorService _sut;

    public QAGeneratorServiceTests()
    {
        _completionService = Substitute.For<ITextCompletionService>();
        _sut = new QAGeneratorService(_completionService);
    }

    [Fact]
    public async Task GenerateAsync_WithValidContext_ReturnsQAPairs()
    {
        // Arrange
        var context = "Paris is the capital of France. It is known for the Eiffel Tower.";
        var expectedResponse = @"{""qa_pairs"": [{""question"": ""What is the capital of France?"", ""answer"": ""Paris is the capital of France.""}, {""question"": ""What is Paris known for?"", ""answer"": ""Paris is known for the Eiffel Tower.""}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GenerateAsync(context);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(2);
        result[0].Question.Should().Be("What is the capital of France?");
    }

    [Fact]
    public async Task GenerateAsync_WithOptions_UsesProvidedOptions()
    {
        // Arrange
        var context = "Sample context for QA generation.";
        var options = new QAGenerationOptions
        {
            PairsPerChunk = 5,
            Temperature = 0.7f
        };
        var expectedResponse = @"{""qa_pairs"": [{""question"": ""Q1"", ""answer"": ""A1""}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _sut.GenerateAsync(context, options);

        // Assert
        await _completionService.Received(1).CompleteAsync(
            Arg.Is<string>(s => s.Contains("5")),
            Arg.Is<CompletionOptions>(o => o.Temperature == 0.7f),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_WithEmptyContext_ReturnsEmptyList()
    {
        // Arrange & Act
        var result = await _sut.GenerateAsync(string.Empty);

        // Assert
        result.Should().BeEmpty();
        await _completionService.DidNotReceive().CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_WithInvalidJsonResponse_ReturnsEmptyList()
    {
        // Arrange
        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("invalid json");

        // Act
        var result = await _sut.GenerateAsync("Some context");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_SetsSourceIdFromParameter()
    {
        // Arrange
        var context = "Test context";
        var sourceId = "doc-123";
        var expectedResponse = @"{""qa_pairs"": [{""question"": ""Q1"", ""answer"": ""A1""}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GenerateAsync(context, sourceId: sourceId);

        // Assert
        result.Should().AllSatisfy(pair => pair.SourceId.Should().Be(sourceId));
    }

    [Fact]
    public async Task GenerateBatchAsync_WithMultipleContexts_ReturnsAllQAPairs()
    {
        // Arrange
        var contexts = new[] { "Context 1 about AI", "Context 2 about ML" };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(
                @"{""qa_pairs"": [{""question"": ""Q1"", ""answer"": ""A1""}]}",
                @"{""qa_pairs"": [{""question"": ""Q2"", ""answer"": ""A2""}]}");

        // Act
        var results = await _sut.GenerateBatchAsync(contexts);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GenerateFromChunkAsync_UsesChunkContentAndId()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "chunk-456",
            Content = "The Eiffel Tower is in Paris."
        };
        var expectedResponse = @"{""qa_pairs"": [{""question"": ""Where is the Eiffel Tower?"", ""answer"": ""The Eiffel Tower is in Paris.""}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GenerateFromChunkAsync(chunk);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(pair => pair.SourceId.Should().Be("chunk-456"));
    }

    [Fact]
    public async Task GenerateAsync_WithIncludeMultiHop_IncludesMultiHopInPrompt()
    {
        // Arrange
        var options = new QAGenerationOptions { IncludeMultiHop = true };
        var expectedResponse = @"{""qa_pairs"": [{""question"": ""Q1"", ""answer"": ""A1""}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _sut.GenerateAsync("Context", options);

        // Assert
        await _completionService.Received(1).CompleteAsync(
            Arg.Is<string>(s => s.Contains("multi-hop", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_SetsContextOnGeneratedPairs()
    {
        // Arrange
        var context = "Test context content";
        var expectedResponse = @"{""qa_pairs"": [{""question"": ""Q1"", ""answer"": ""A1""}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GenerateAsync(context);

        // Assert
        result.Should().AllSatisfy(pair => pair.Context.Should().Be(context));
    }
}

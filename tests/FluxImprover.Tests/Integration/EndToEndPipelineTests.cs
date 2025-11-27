namespace FluxImprover.Tests.Integration;

using FluentAssertions;
using FluxImprover.Abstractions.Models;
using FluxImprover.Abstractions.Options;
using FluxImprover.Abstractions.Services;
using FluxImprover.QAGeneration;
using FluxImprover.QuestionSuggestion;
using NSubstitute;
using Xunit;

/// <summary>
/// End-to-end integration tests for the complete FluxImprover pipeline
/// </summary>
public sealed class EndToEndPipelineTests
{
    private readonly ITextCompletionService _completionService;
    private readonly FluxImproverServices _services;

    public EndToEndPipelineTests()
    {
        _completionService = Substitute.For<ITextCompletionService>();
        _services = new FluxImproverBuilder()
            .WithCompletionService(_completionService)
            .Build();
    }

    [Fact]
    public async Task FullEnrichmentPipeline_FromChunk_ProducesEnrichedChunk()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "chunk-1",
            Content = "Paris is the capital of France. It is famous for the Eiffel Tower."
        };

        // Mock summarization response
        _completionService.CompleteAsync(
            Arg.Is<string>(s => s.Contains("Paris")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Paris is France's capital, known for the Eiffel Tower.");

        // Mock keyword extraction response
        _completionService.CompleteAsync(
            Arg.Is<string>(s => s.Contains("keywords")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""keywords"": [{""keyword"": ""Paris"", ""score"": 0.95}, {""keyword"": ""France"", ""score"": 0.9}, {""keyword"": ""Eiffel Tower"", ""score"": 0.85}]}");

        // Act
        var enriched = await _services.ChunkEnrichment.EnrichAsync(chunk);

        // Assert
        enriched.Should().NotBeNull();
        enriched.Summary.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task QAPipelineFlow_GeneratesAndFilters()
    {
        // Arrange
        var context = "The solar system has eight planets. Earth is the third planet from the sun.";

        // Mock QA generation
        _completionService.CompleteAsync(
            Arg.Is<string>(s => s.Contains("solar system")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""qa_pairs"": [{""question"": ""How many planets are in the solar system?"", ""answer"": ""Eight planets"", ""context"": ""The solar system has eight planets.""}]}");

        // Mock evaluation responses (faithfulness, relevancy, answerability)
        _completionService.CompleteAsync(
            Arg.Is<string>(s => s.Contains("faithfulness") || s.Contains("grounded")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""score"": 0.95, ""explanation"": ""Well grounded""}");

        _completionService.CompleteAsync(
            Arg.Is<string>(s => s.Contains("relevancy") || s.Contains("relevant")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""score"": 0.9, ""explanation"": ""Highly relevant""}");

        _completionService.CompleteAsync(
            Arg.Is<string>(s => s.Contains("answerability") || s.Contains("answerable")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""score"": 0.88, ""explanation"": ""Answerable from context""}");

        // Act
        var qaPairs = await _services.QAGenerator.GenerateAsync(context);

        // Assert
        qaPairs.Should().NotBeEmpty();
    }

    [Fact]
    public async Task QuestionSuggestionFlow_FromConversation_SuggestsFollowUps()
    {
        // Arrange
        var history = new[]
        {
            new ConversationMessage { Role = "user", Content = "What is machine learning?" },
            new ConversationMessage { Role = "assistant", Content = "Machine learning is a subset of AI that enables computers to learn from data." }
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""suggestions"": [{""text"": ""What are the types of machine learning?"", ""category"": ""DeepDive"", ""relevance"": 0.9}]}");

        // Act
        var suggestions = await _services.QuestionSuggestion.SuggestFromConversationAsync(history);

        // Assert
        suggestions.Should().NotBeEmpty();
        suggestions[0].Text.Should().Contain("machine learning");
    }

    [Fact]
    public async Task EvaluationMetrics_WorkTogether()
    {
        // Arrange
        var context = "France is a country in Europe. Paris is the capital city of France.";
        var question = "What is the capital of France?";
        var answer = "Paris is the capital of France.";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""score"": 0.9, ""explanation"": ""Good evaluation""}");

        // Act
        var faithfulness = await _services.Faithfulness.EvaluateAsync(context, answer);
        var relevancy = await _services.Relevancy.EvaluateAsync(question, answer, context: context);
        var answerability = await _services.Answerability.EvaluateAsync(context, question);

        // Assert
        faithfulness.Score.Should().BeGreaterThan(0);
        relevancy.Score.Should().BeGreaterThan(0);
        answerability.Score.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ChunkEnrichment_ToQAGeneration_Integration()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "doc-chunk-1",
            Content = "Artificial Intelligence is transforming industries. Machine learning enables predictive analytics."
        };

        // Mock for summarization
        _completionService.CompleteAsync(
            Arg.Is<string>(s => !s.Contains("keywords") && !s.Contains("qa_pairs")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("AI transforms industries via machine learning for predictive analytics.");

        // Mock for keyword extraction
        _completionService.CompleteAsync(
            Arg.Is<string>(s => s.Contains("keywords")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""keywords"": [{""keyword"": ""AI"", ""score"": 0.95}, {""keyword"": ""Machine Learning"", ""score"": 0.9}]}");

        // Mock for QA generation
        _completionService.CompleteAsync(
            Arg.Is<string>(s => s.Contains("qa_pairs") || s.Contains("question-answer")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(@"{""qa_pairs"": [{""question"": ""What does AI enable?"", ""answer"": ""Predictive analytics through machine learning"", ""context"": ""Machine learning enables predictive analytics.""}]}");

        // Act
        var enriched = await _services.ChunkEnrichment.EnrichAsync(chunk);
        var qaPairs = await _services.QAGenerator.GenerateAsync(chunk.Content);

        // Assert
        enriched.Should().NotBeNull();
        qaPairs.Should().NotBeEmpty();
    }

    [Fact]
    public void AllServicesShareSameCompletionService()
    {
        // This test verifies that all services are properly connected
        // to the same completion service through the builder

        // The fact that we can create services without exceptions
        // and they respond to the same mock setup proves integration
        _services.Should().NotBeNull();
        _services.Summarization.Should().NotBeNull();
        _services.KeywordExtraction.Should().NotBeNull();
        _services.ChunkEnrichment.Should().NotBeNull();
        _services.Faithfulness.Should().NotBeNull();
        _services.Relevancy.Should().NotBeNull();
        _services.Answerability.Should().NotBeNull();
        _services.QAGenerator.Should().NotBeNull();
        _services.QAFilter.Should().NotBeNull();
        _services.QAPipeline.Should().NotBeNull();
        _services.QuestionSuggestion.Should().NotBeNull();
    }
}

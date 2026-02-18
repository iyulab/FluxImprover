namespace FluxImprover.Tests.QuestionSuggestion;

using FluentAssertions;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.QuestionSuggestion;
using FluxImprover.Services;
using NSubstitute;
using Xunit;

public sealed class QuestionSuggestionServiceTests
{
    private readonly ITextCompletionService _completionService;
    private readonly QuestionSuggestionService _sut;

    public QuestionSuggestionServiceTests()
    {
        _completionService = Substitute.For<ITextCompletionService>();
        _sut = new QuestionSuggestionService(_completionService);
    }

    [Fact]
    public async Task SuggestAsync_WithValidContext_ReturnsSuggestions()
    {
        // Arrange
        var context = "Paris is the capital of France. It is known for the Eiffel Tower.";
        var expectedResponse = @"{""suggestions"": [{""text"": ""What are other famous landmarks in Paris?"", ""category"": ""FollowUp"", ""relevance"": 0.9}, {""text"": ""When was the Eiffel Tower built?"", ""category"": ""DeepDive"", ""relevance"": 0.85}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.SuggestAsync(context);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(2);
        result[0].Text.Should().Be("What are other famous landmarks in Paris?");
        result[0].Category.Should().Be(QuestionCategory.FollowUp);
    }

    [Fact]
    public async Task SuggestAsync_WithOptions_UsesProvidedOptions()
    {
        // Arrange
        var context = "Sample context";
        var options = new QuestionSuggestionOptions
        {
            MaxSuggestions = 3,
            Temperature = 0.5f
        };
        var expectedResponse = @"{""suggestions"": [{""text"": ""Q1"", ""category"": ""FollowUp"", ""relevance"": 0.9}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _sut.SuggestAsync(context, options);

        // Assert
        await _completionService.Received(1).CompleteAsync(
            Arg.Is<string>(s => s.Contains('3')),
            Arg.Is<CompletionOptions>(o => o.Temperature == 0.5f),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SuggestAsync_WithEmptyContext_ReturnsEmptyList()
    {
        // Arrange & Act
        var result = await _sut.SuggestAsync(string.Empty);

        // Assert
        result.Should().BeEmpty();
        await _completionService.DidNotReceive().CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SuggestAsync_WithInvalidJsonResponse_ReturnsEmptyList()
    {
        // Arrange
        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("invalid json");

        // Act
        var result = await _sut.SuggestAsync("Some context");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SuggestAsync_WithCategories_IncludesCategoriesInPrompt()
    {
        // Arrange
        var options = new QuestionSuggestionOptions
        {
            Categories = [QuestionCategory.DeepDive, QuestionCategory.Alternative]
        };
        var expectedResponse = @"{""suggestions"": [{""text"": ""Q1"", ""category"": ""DeepDive"", ""relevance"": 0.9}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _sut.SuggestAsync("Context", options);

        // Assert
        await _completionService.Received(1).CompleteAsync(
            Arg.Is<string>(s => s.Contains("DeepDive") && s.Contains("Alternative")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SuggestAsync_WithReasoning_IncludesReasoningInResult()
    {
        // Arrange
        var options = new QuestionSuggestionOptions { IncludeReasoning = true };
        var expectedResponse = @"{""suggestions"": [{""text"": ""Why is Paris important?"", ""category"": ""DeepDive"", ""relevance"": 0.9, ""reasoning"": ""Explores cultural significance""}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.SuggestAsync("Paris is the capital of France", options);

        // Assert
        result.Should().ContainSingle();
        result[0].Reasoning.Should().Be("Explores cultural significance");
    }

    [Fact]
    public async Task SuggestFromQAAsync_WithQAPair_GeneratesSuggestionsBasedOnQA()
    {
        // Arrange
        var qaPair = new QAPair
        {
            Id = "qa-1",
            Question = "What is the capital of France?",
            Answer = "Paris is the capital of France."
        };
        var expectedResponse = @"{""suggestions"": [{""text"": ""What is the population of Paris?"", ""category"": ""FollowUp"", ""relevance"": 0.9}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.SuggestFromQAAsync(qaPair);

        // Assert
        result.Should().NotBeEmpty();
        await _completionService.Received(1).CompleteAsync(
            Arg.Is<string>(s => s.Contains("capital of France")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SuggestFromConversationAsync_WithHistory_UsesPreviousMessages()
    {
        // Arrange
        var history = new[]
        {
            new ConversationMessage { Role = "user", Content = "Tell me about Paris" },
            new ConversationMessage { Role = "assistant", Content = "Paris is the capital of France." }
        };
        var expectedResponse = @"{""suggestions"": [{""text"": ""What landmarks should I visit?"", ""category"": ""FollowUp"", ""relevance"": 0.9}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.SuggestFromConversationAsync(history);

        // Assert
        result.Should().NotBeEmpty();
        await _completionService.Received(1).CompleteAsync(
            Arg.Is<string>(s => s.Contains("Tell me about Paris") && s.Contains("Paris is the capital")),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SuggestAsync_FiltersLowRelevanceSuggestions()
    {
        // Arrange
        var options = new QuestionSuggestionOptions { MinRelevanceScore = 0.7f };
        var expectedResponse = @"{""suggestions"": [{""text"": ""Q1"", ""category"": ""FollowUp"", ""relevance"": 0.9}, {""text"": ""Q2"", ""category"": ""FollowUp"", ""relevance"": 0.5}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.SuggestAsync("Context", options);

        // Assert
        result.Should().ContainSingle();
        result[0].Text.Should().Be("Q1");
    }

    [Fact]
    public async Task SuggestAsync_LimitsToMaxSuggestions()
    {
        // Arrange
        var options = new QuestionSuggestionOptions { MaxSuggestions = 2 };
        var expectedResponse = @"{""suggestions"": [{""text"": ""Q1"", ""category"": ""FollowUp"", ""relevance"": 0.9}, {""text"": ""Q2"", ""category"": ""FollowUp"", ""relevance"": 0.85}, {""text"": ""Q3"", ""category"": ""FollowUp"", ""relevance"": 0.8}]}";

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.SuggestAsync("Context", options);

        // Assert
        result.Should().HaveCount(2);
    }
}

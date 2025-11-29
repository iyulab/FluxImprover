namespace FluxImprover.Tests.Prompts;

using FluentAssertions;
using FluxImprover.Prompts;
using Xunit;

public sealed class EmbeddedPromptsTests
{
    [Fact]
    public void QAGeneration_ShouldExist()
    {
        // Act
        var template = EmbeddedPrompts.QAGeneration;

        // Assert
        template.Should().NotBeNull();
        template.Content.Should().NotBeNullOrWhiteSpace();
        template.Content.Should().Contain("question");
    }

    [Fact]
    public void FaithfulnessEvaluation_ShouldExist()
    {
        // Act
        var template = EmbeddedPrompts.FaithfulnessEvaluation;

        // Assert
        template.Should().NotBeNull();
        template.Content.Should().NotBeNullOrWhiteSpace();
        template.Content.Should().Contain("faithfulness");
    }

    [Fact]
    public void RelevancyEvaluation_ShouldExist()
    {
        // Act
        var template = EmbeddedPrompts.RelevancyEvaluation;

        // Assert
        template.Should().NotBeNull();
        template.Content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AnswerabilityEvaluation_ShouldExist()
    {
        // Act
        var template = EmbeddedPrompts.AnswerabilityEvaluation;

        // Assert
        template.Should().NotBeNull();
        template.Content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void QuestionSuggestion_ShouldExist()
    {
        // Act
        var template = EmbeddedPrompts.QuestionSuggestion;

        // Assert
        template.Should().NotBeNull();
        template.Content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Summarization_ShouldExist()
    {
        // Act
        var template = EmbeddedPrompts.Summarization;

        // Assert
        template.Should().NotBeNull();
        template.Content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void KeywordExtraction_ShouldExist()
    {
        // Act
        var template = EmbeddedPrompts.KeywordExtraction;

        // Assert
        template.Should().NotBeNull();
        template.Content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetAll_ShouldReturnAllTemplates()
    {
        // Act
        var all = EmbeddedPrompts.GetAll();

        // Assert
        all.Should().NotBeEmpty();
        all.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void QAGeneration_ShouldHaveCorrectVariables()
    {
        // Act
        var template = EmbeddedPrompts.QAGeneration;
        var variables = template.GetVariables();

        // Assert
        variables.Should().Contain("context");
    }

    [Fact]
    public void Templates_ShouldBeRenderable()
    {
        // Arrange
        var template = EmbeddedPrompts.Summarization;

        // Act
        var rendered = template.Render(new { text = "Sample text to summarize", maxLength = 100 });

        // Assert
        rendered.Should().NotContain("{{text}}");
        rendered.Should().Contain("Sample text to summarize");
    }
}

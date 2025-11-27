namespace FluxImprover.Tests.Prompts;

using FluentAssertions;
using FluxImprover.Prompts;
using Xunit;

public sealed class PromptBuilderTests
{
    [Fact]
    public void Build_WithSystemAndUser_ShouldCreatePrompt()
    {
        // Arrange & Act
        var prompt = new PromptBuilder()
            .WithSystemPrompt("You are a helpful assistant.")
            .WithUserPrompt("Hello!")
            .Build();

        // Assert
        prompt.System.Should().Be("You are a helpful assistant.");
        prompt.User.Should().Be("Hello!");
    }

    [Fact]
    public void Build_WithVariable_ShouldInterpolate()
    {
        // Arrange & Act
        var prompt = new PromptBuilder()
            .WithUserPrompt("Hello, {{name}}!")
            .WithVariable("name", "World")
            .Build();

        // Assert
        prompt.User.Should().Be("Hello, World!");
    }

    [Fact]
    public void Build_WithMultipleVariables_ShouldInterpolateAll()
    {
        // Arrange & Act
        var prompt = new PromptBuilder()
            .WithUserPrompt("{{greeting}} {{name}}")
            .WithVariable("greeting", "Hi")
            .WithVariable("name", "Alice")
            .Build();

        // Assert
        prompt.User.Should().Be("Hi Alice");
    }

    [Fact]
    public void Build_WithContext_ShouldIncludeInPrompt()
    {
        // Arrange & Act
        var prompt = new PromptBuilder()
            .WithSystemPrompt("Answer based on context.")
            .WithContext("The sky is blue.")
            .WithUserPrompt("What color is the sky?")
            .Build();

        // Assert
        prompt.Context.Should().Be("The sky is blue.");
    }

    [Fact]
    public void Build_WithMultipleContexts_ShouldJoin()
    {
        // Arrange & Act
        var prompt = new PromptBuilder()
            .WithContext("Context 1")
            .WithContext("Context 2")
            .WithUserPrompt("Question")
            .Build();

        // Assert
        prompt.Context.Should().Contain("Context 1");
        prompt.Context.Should().Contain("Context 2");
    }

    [Fact]
    public void Build_WithExamples_ShouldInclude()
    {
        // Arrange & Act
        var prompt = new PromptBuilder()
            .WithSystemPrompt("You generate QA pairs.")
            .WithExample("Q: What is 2+2?\nA: 4")
            .WithExample("Q: What is 3+3?\nA: 6")
            .WithUserPrompt("Generate a QA pair.")
            .Build();

        // Assert
        prompt.Examples.Should().HaveCount(2);
        prompt.Examples[0].Should().Contain("2+2");
        prompt.Examples[1].Should().Contain("3+3");
    }

    [Fact]
    public void Build_WithJsonMode_ShouldSetFlag()
    {
        // Arrange & Act
        var prompt = new PromptBuilder()
            .WithUserPrompt("Generate JSON")
            .WithJsonMode()
            .Build();

        // Assert
        prompt.JsonMode.Should().BeTrue();
    }

    [Fact]
    public void Build_WithTemplate_ShouldApplyVariables()
    {
        // Arrange
        var template = new PromptTemplate("Generate {{count}} questions about {{topic}}");

        // Act
        var prompt = new PromptBuilder()
            .WithTemplate(template)
            .WithVariable("count", 3)
            .WithVariable("topic", "AI")
            .Build();

        // Assert
        prompt.User.Should().Be("Generate 3 questions about AI");
    }

    [Fact]
    public void Build_WithMaxTokens_ShouldSetLimit()
    {
        // Arrange & Act
        var prompt = new PromptBuilder()
            .WithUserPrompt("Test")
            .WithMaxTokens(1000)
            .Build();

        // Assert
        prompt.MaxTokens.Should().Be(1000);
    }

    [Fact]
    public void Build_WithTemperature_ShouldSetValue()
    {
        // Arrange & Act
        var prompt = new PromptBuilder()
            .WithUserPrompt("Test")
            .WithTemperature(0.5f)
            .Build();

        // Assert
        prompt.Temperature.Should().Be(0.5f);
    }

    [Fact]
    public void Build_Combined_ShouldCreateFullPrompt()
    {
        // Arrange & Act
        var prompt = new PromptBuilder()
            .WithSystemPrompt("You are a {{role}}.")
            .WithContext("Important context here.")
            .WithExample("Example 1")
            .WithUserPrompt("{{action}} the document.")
            .WithVariable("role", "reviewer")
            .WithVariable("action", "Review")
            .WithJsonMode()
            .WithMaxTokens(500)
            .WithTemperature(0.7f)
            .Build();

        // Assert
        prompt.System.Should().Be("You are a reviewer.");
        prompt.Context.Should().Contain("Important context");
        prompt.User.Should().Be("Review the document.");
        prompt.JsonMode.Should().BeTrue();
        prompt.MaxTokens.Should().Be(500);
        prompt.Temperature.Should().Be(0.7f);
    }
}

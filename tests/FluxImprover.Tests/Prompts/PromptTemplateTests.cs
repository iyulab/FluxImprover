namespace FluxImprover.Tests.Prompts;

using FluentAssertions;
using FluxImprover.Prompts;
using Xunit;

public sealed class PromptTemplateTests
{
    private static readonly string[] KeywordValues = ["AI", "ML", "NLP"];
    [Fact]
    public void Render_WithSimpleVariable_ShouldInterpolate()
    {
        // Arrange
        var template = new PromptTemplate("Hello, {{name}}!");

        // Act
        var result = template.Render(new { name = "World" });

        // Assert
        result.Should().Be("Hello, World!");
    }

    [Fact]
    public void Render_WithMultipleVariables_ShouldInterpolateAll()
    {
        // Arrange
        var template = new PromptTemplate("{{greeting}}, {{name}}! Your score is {{score}}.");

        // Act
        var result = template.Render(new { greeting = "Hi", name = "Alice", score = 100 });

        // Assert
        result.Should().Be("Hi, Alice! Your score is 100.");
    }

    [Fact]
    public void Render_WithDictionary_ShouldInterpolate()
    {
        // Arrange
        var template = new PromptTemplate("{{key}} = {{value}}");

        // Act
        var result = template.Render(new Dictionary<string, object>
        {
            ["key"] = "answer",
            ["value"] = 42
        });

        // Assert
        result.Should().Be("answer = 42");
    }

    [Fact]
    public void Render_WithMissingVariable_ShouldKeepPlaceholder()
    {
        // Arrange
        var template = new PromptTemplate("Hello, {{name}}! Age: {{age}}");

        // Act
        var result = template.Render(new { name = "Bob" });

        // Assert
        result.Should().Be("Hello, Bob! Age: {{age}}");
    }

    [Fact]
    public void Render_WithNoVariables_ShouldReturnOriginal()
    {
        // Arrange
        var template = new PromptTemplate("No variables here");

        // Act
        var result = template.Render(new { name = "ignored" });

        // Assert
        result.Should().Be("No variables here");
    }

    [Fact]
    public void Render_WithNestedProperty_ShouldInterpolate()
    {
        // Arrange
        var template = new PromptTemplate("{{user.name}} has {{user.score}} points");

        // Act
        var result = template.Render(new Dictionary<string, object>
        {
            ["user.name"] = "Alice",
            ["user.score"] = 50
        });

        // Assert
        result.Should().Be("Alice has 50 points");
    }

    [Fact]
    public void Render_WithListVariable_ShouldJoinWithCommas()
    {
        // Arrange
        var template = new PromptTemplate("Keywords: {{keywords}}");

        // Act
        var result = template.Render(new { keywords = KeywordValues });

        // Assert
        result.Should().Be("Keywords: AI, ML, NLP");
    }

    [Fact]
    public void Render_CaseInsensitive_ShouldMatch()
    {
        // Arrange
        var template = new PromptTemplate("{{NAME}} and {{name}} and {{Name}}");

        // Act
        var result = template.Render(new { name = "Test" });

        // Assert
        result.Should().Be("Test and Test and Test");
    }

    [Fact]
    public void GetVariables_ShouldReturnAllVariableNames()
    {
        // Arrange
        var template = new PromptTemplate("{{a}} and {{b}} and {{c}}");

        // Act
        var variables = template.GetVariables();

        // Assert
        variables.Should().BeEquivalentTo("a", "b", "c");
    }

    [Fact]
    public void Validate_WithAllVariables_ShouldReturnTrue()
    {
        // Arrange
        var template = new PromptTemplate("{{name}}, {{age}}");

        // Act
        var isValid = template.Validate(new { name = "Test", age = 25 }, out var missingVariables);

        // Assert
        isValid.Should().BeTrue();
        missingVariables.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMissingVariables_ShouldReturnFalse()
    {
        // Arrange
        var template = new PromptTemplate("{{name}}, {{age}}, {{email}}");

        // Act
        var isValid = template.Validate(new { name = "Test" }, out var missingVariables);

        // Assert
        isValid.Should().BeFalse();
        missingVariables.Should().BeEquivalentTo("age", "email");
    }
}

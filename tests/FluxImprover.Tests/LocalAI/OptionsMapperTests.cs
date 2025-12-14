namespace FluxImprover.Tests.LocalAI;

using FluxImprover.LocalAI;
using FluxImprover.Services;
using FluentAssertions;
using Xunit;

public class OptionsMapperTests
{
    [Fact]
    public void ToGenerationOptions_WithNullOptions_ReturnsDefaultGenerationOptions()
    {
        // Act
        var result = OptionsMapper.ToGenerationOptions(null);

        // Assert
        result.Should().NotBeNull();
        result.Temperature.Should().Be(0.7f);
        result.MaxTokens.Should().Be(512);
    }

    [Fact]
    public void ToGenerationOptions_WithDefaults_AppliesDefaultValues()
    {
        // Arrange
        var defaults = new LocalAIGenerationDefaults
        {
            Temperature = 0.5f,
            MaxTokens = 1024,
            TopP = 0.8f,
            TopK = 40,
            RepetitionPenalty = 1.2f
        };

        // Act
        var result = OptionsMapper.ToGenerationOptions(null, defaults);

        // Assert
        result.Temperature.Should().Be(0.5f);
        result.MaxTokens.Should().Be(1024);
        result.TopP.Should().Be(0.8f);
        result.TopK.Should().Be(40);
        result.RepetitionPenalty.Should().Be(1.2f);
    }

    [Fact]
    public void ToGenerationOptions_WithOptions_OverridesDefaults()
    {
        // Arrange
        var defaults = new LocalAIGenerationDefaults
        {
            Temperature = 0.5f,
            MaxTokens = 1024
        };
        var options = new CompletionOptions
        {
            Temperature = 0.9f,
            MaxTokens = 2048
        };

        // Act
        var result = OptionsMapper.ToGenerationOptions(options, defaults);

        // Assert
        result.Temperature.Should().Be(0.9f);
        result.MaxTokens.Should().Be(2048);
    }

    [Fact]
    public void ToGenerationOptions_PartialDefaults_AppliesOnlySpecifiedValues()
    {
        // Arrange
        var defaults = new LocalAIGenerationDefaults
        {
            Temperature = 0.3f
            // MaxTokens not set
        };

        // Act
        var result = OptionsMapper.ToGenerationOptions(null, defaults);

        // Assert
        result.Temperature.Should().Be(0.3f);
        result.MaxTokens.Should().Be(512); // Default value
    }

    [Theory]
    [InlineData("system")]
    [InlineData("System")]
    [InlineData("SYSTEM")]
    public void ToLocalAIChatMessage_SystemRole_CreatesSystemMessage(string role)
    {
        // Arrange
        var message = new ChatMessage(role, "System prompt content");

        // Act
        var result = OptionsMapper.ToLocalAIChatMessage(message);

        // Assert
        result.Role.Should().Be(global::LocalAI.Generator.Models.ChatRole.System);
        result.Content.Should().Be("System prompt content");
    }

    [Theory]
    [InlineData("user")]
    [InlineData("User")]
    [InlineData("USER")]
    public void ToLocalAIChatMessage_UserRole_CreatesUserMessage(string role)
    {
        // Arrange
        var message = new ChatMessage(role, "User message");

        // Act
        var result = OptionsMapper.ToLocalAIChatMessage(message);

        // Assert
        result.Role.Should().Be(global::LocalAI.Generator.Models.ChatRole.User);
        result.Content.Should().Be("User message");
    }

    [Theory]
    [InlineData("assistant")]
    [InlineData("Assistant")]
    [InlineData("ASSISTANT")]
    public void ToLocalAIChatMessage_AssistantRole_CreatesAssistantMessage(string role)
    {
        // Arrange
        var message = new ChatMessage(role, "Assistant response");

        // Act
        var result = OptionsMapper.ToLocalAIChatMessage(message);

        // Assert
        result.Role.Should().Be(global::LocalAI.Generator.Models.ChatRole.Assistant);
        result.Content.Should().Be("Assistant response");
    }

    [Fact]
    public void ToLocalAIChatMessage_UnknownRole_DefaultsToUser()
    {
        // Arrange
        var message = new ChatMessage("unknown", "Some content");

        // Act
        var result = OptionsMapper.ToLocalAIChatMessage(message);

        // Assert
        result.Role.Should().Be(global::LocalAI.Generator.Models.ChatRole.User);
    }

    [Fact]
    public void BuildChatMessages_WithSystemPrompt_IncludesSystemMessage()
    {
        // Arrange
        var options = new CompletionOptions
        {
            SystemPrompt = "You are a helpful assistant"
        };

        // Act
        var result = OptionsMapper.BuildChatMessages("Hello", options).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Role.Should().Be(global::LocalAI.Generator.Models.ChatRole.System);
        result[0].Content.Should().Be("You are a helpful assistant");
        result[1].Role.Should().Be(global::LocalAI.Generator.Models.ChatRole.User);
        result[1].Content.Should().Be("Hello");
    }

    [Fact]
    public void BuildChatMessages_WithPreviousMessages_IncludesHistory()
    {
        // Arrange
        var options = new CompletionOptions
        {
            Messages =
            [
                new ChatMessage("user", "First question"),
                new ChatMessage("assistant", "First answer")
            ]
        };

        // Act
        var result = OptionsMapper.BuildChatMessages("Second question", options).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Content.Should().Be("First question");
        result[1].Content.Should().Be("First answer");
        result[2].Content.Should().Be("Second question");
    }

    [Fact]
    public void BuildChatMessages_WithJsonMode_AppendsJsonInstruction()
    {
        // Arrange
        var options = new CompletionOptions
        {
            JsonMode = true
        };

        // Act
        var result = OptionsMapper.BuildChatMessages("Generate data", options).ToList();

        // Assert
        result.Last().Content.Should().Contain("Respond with valid JSON only");
    }

    [Fact]
    public void BuildChatMessages_WithJsonModeAndSchema_AppendsSchemaInstruction()
    {
        // Arrange
        var schema = "{\"type\": \"object\", \"properties\": {\"name\": {\"type\": \"string\"}}}";
        var options = new CompletionOptions
        {
            JsonMode = true,
            ResponseSchema = schema
        };

        // Act
        var result = OptionsMapper.BuildChatMessages("Generate data", options).ToList();

        // Assert
        result.Last().Content.Should().Contain("Respond with valid JSON matching this schema");
        result.Last().Content.Should().Contain(schema);
    }

    [Fact]
    public void ApplyJsonModeIfNeeded_WithoutJsonMode_ReturnsOriginalPrompt()
    {
        // Arrange
        var options = new CompletionOptions { JsonMode = false };

        // Act
        var result = OptionsMapper.ApplyJsonModeIfNeeded("Original prompt", options);

        // Assert
        result.Should().Be("Original prompt");
    }

    [Fact]
    public void HasChatContext_WithSystemPrompt_ReturnsTrue()
    {
        // Arrange
        var options = new CompletionOptions { SystemPrompt = "System" };

        // Act & Assert
        OptionsMapper.HasChatContext(options).Should().BeTrue();
    }

    [Fact]
    public void HasChatContext_WithMessages_ReturnsTrue()
    {
        // Arrange
        var options = new CompletionOptions
        {
            Messages = [new ChatMessage("user", "Hello")]
        };

        // Act & Assert
        OptionsMapper.HasChatContext(options).Should().BeTrue();
    }

    [Fact]
    public void HasChatContext_WithoutContext_ReturnsFalse()
    {
        // Act & Assert
        OptionsMapper.HasChatContext(null).Should().BeFalse();
        OptionsMapper.HasChatContext(new CompletionOptions()).Should().BeFalse();
    }
}

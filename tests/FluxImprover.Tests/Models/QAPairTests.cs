using Xunit;
namespace FluxImprover.Tests.Models;

using System.Text.Json;
using FluentAssertions;
using FluxImprover.Models;

public class QAPairTests
{
    [Fact]
    public void QAPair_WithRequiredProperties_CreatesSuccessfully()
    {
        // Act
        var qa = new QAPair
        {
            Id = "qa-001",
            Question = "What is RAG?",
            Answer = "RAG stands for Retrieval Augmented Generation."
        };

        // Assert
        qa.Id.Should().Be("qa-001");
        qa.Question.Should().Be("What is RAG?");
        qa.Answer.Should().Be("RAG stands for Retrieval Augmented Generation.");
    }

    [Fact]
    public void QAPair_WithContexts_StoresCorrectly()
    {
        // Act
        var qa = new QAPair
        {
            Id = "qa-001",
            Question = "What is RAG?",
            Answer = "RAG is...",
            Contexts =
            [
                new ContextReference("chunk-1", "RAG combines retrieval...", true, "doc-001"),
                new ContextReference("chunk-2", "Additional context...", false, "doc-001")
            ]
        };

        // Assert
        qa.Contexts.Should().HaveCount(2);
        qa.Contexts[0].IsGold.Should().BeTrue();
        qa.Contexts[1].IsGold.Should().BeFalse();
    }

    [Fact]
    public void QAPair_WithClassification_StoresCorrectly()
    {
        // Act
        var qa = new QAPair
        {
            Id = "qa-001",
            Question = "Compare X and Y",
            Answer = "X is... while Y is...",
            Classification = new QAClassification(
                QuestionType.Comparative,
                Difficulty.Hard,
                RequiredContextCount: 2)
        };

        // Assert
        qa.Classification.Should().NotBeNull();
        qa.Classification!.Type.Should().Be(QuestionType.Comparative);
        qa.Classification.Difficulty.Should().Be(Difficulty.Hard);
        qa.Classification.RequiredContextCount.Should().Be(2);
    }

    [Fact]
    public void QAPair_Serialization_RoundTrips()
    {
        // Arrange
        var original = new QAPair
        {
            Id = "qa-001",
            Question = "What is RAG?",
            Answer = "RAG is Retrieval Augmented Generation.",
            Contexts =
            [
                new ContextReference("chunk-1", "RAG context", true, "doc-001")
            ],
            Classification = new QAClassification(QuestionType.Factual, Difficulty.Easy),
            FaithfulnessScore = 0.95
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<QAPair>(json, options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(original.Id);
        deserialized.Question.Should().Be(original.Question);
        deserialized.FaithfulnessScore.Should().Be(0.95);
        deserialized.Contexts.Should().HaveCount(1);
    }

    [Fact]
    public void QAPair_WithSupportingFacts_StoresCorrectly()
    {
        // Act
        var qa = new QAPair
        {
            Id = "qa-001",
            Question = "Question",
            Answer = "Answer",
            SupportingFacts =
            [
                new SupportingFact("chunk-1", 0),
                new SupportingFact("chunk-1", 2)
            ]
        };

        // Assert
        qa.SupportingFacts.Should().HaveCount(2);
        qa.SupportingFacts![0].SentenceIndex.Should().Be(0);
    }
}

public class ContextReferenceTests
{
    [Fact]
    public void ContextReference_StoresAllProperties()
    {
        // Act
        var context = new ContextReference(
            ChunkId: "chunk-001",
            Text: "The context text",
            IsGold: true,
            SourceDocument: "document.pdf");

        // Assert
        context.ChunkId.Should().Be("chunk-001");
        context.Text.Should().Be("The context text");
        context.IsGold.Should().BeTrue();
        context.SourceDocument.Should().Be("document.pdf");
    }

    [Fact]
    public void ContextReference_DefaultIsGold_IsFalse()
    {
        // Act
        var context = new ContextReference("id", "text");

        // Assert
        context.IsGold.Should().BeFalse();
        context.SourceDocument.Should().BeNull();
    }
}

public class QAClassificationTests
{
    [Fact]
    public void QAClassification_StoresTypeAndDifficulty()
    {
        // Act
        var classification = new QAClassification(
            QuestionType.Reasoning,
            Difficulty.Medium);

        // Assert
        classification.Type.Should().Be(QuestionType.Reasoning);
        classification.Difficulty.Should().Be(Difficulty.Medium);
        classification.RequiredContextCount.Should().Be(1); // default
    }

    [Theory]
    [InlineData(QuestionType.Factual)]
    [InlineData(QuestionType.Reasoning)]
    [InlineData(QuestionType.Comparative)]
    [InlineData(QuestionType.MultiHop)]
    [InlineData(QuestionType.Conditional)]
    public void QuestionType_AllValues_AreValid(QuestionType type)
    {
        // Act
        var classification = new QAClassification(type, Difficulty.Easy);

        // Assert
        classification.Type.Should().Be(type);
    }
}

public class SupportingFactTests
{
    [Fact]
    public void SupportingFact_StoresChunkIdAndSentenceIndex()
    {
        // Act
        var fact = new SupportingFact("chunk-001", 5);

        // Assert
        fact.ChunkId.Should().Be("chunk-001");
        fact.SentenceIndex.Should().Be(5);
    }
}

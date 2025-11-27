namespace FluxImprover.Abstractions.Tests.Models;

using System.Text.Json;
using FluentAssertions;
using FluxImprover.Abstractions.Models;

public class EvaluationResultTests
{
    [Fact]
    public void EvaluationResult_WithScores_StoresCorrectly()
    {
        // Act
        var result = new EvaluationResult
        {
            Faithfulness = 0.95,
            Relevancy = 0.88,
            Answerability = AnswerabilityGrade.A,
            OverallScore = 0.91
        };

        // Assert
        result.Faithfulness.Should().Be(0.95);
        result.Relevancy.Should().Be(0.88);
        result.Answerability.Should().Be(AnswerabilityGrade.A);
        result.OverallScore.Should().Be(0.91);
    }

    [Fact]
    public void EvaluationResult_ScoresAreInValidRange()
    {
        // Act
        var result = new EvaluationResult
        {
            Faithfulness = 0.5,
            Relevancy = 0.75,
            OverallScore = 0.625
        };

        // Assert
        result.Faithfulness.Should().BeInRange(0, 1);
        result.Relevancy.Should().BeInRange(0, 1);
        result.OverallScore.Should().BeInRange(0, 1);
    }

    [Fact]
    public void EvaluationResult_WithDetails_StoresCorrectly()
    {
        // Act
        var result = new EvaluationResult
        {
            Faithfulness = 0.8,
            Details = new EvaluationDetails
            {
                FaithfulnessClaims =
                [
                    new ClaimVerification("Claim 1", true, "Supported by context"),
                    new ClaimVerification("Claim 2", false, "Not found in context")
                ],
                RelevancyReasoning = "Answer directly addresses the question"
            }
        };

        // Assert
        result.Details.Should().NotBeNull();
        result.Details!.FaithfulnessClaims.Should().HaveCount(2);
        result.Details.FaithfulnessClaims![0].IsSupported.Should().BeTrue();
    }

    [Fact]
    public void EvaluationResult_Serialization_RoundTrips()
    {
        // Arrange
        var original = new EvaluationResult
        {
            Faithfulness = 0.9,
            Relevancy = 0.85,
            Answerability = AnswerabilityGrade.B,
            OverallScore = 0.875
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<EvaluationResult>(json, options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Faithfulness.Should().Be(0.9);
        deserialized.Answerability.Should().Be(AnswerabilityGrade.B);
    }

    [Theory]
    [InlineData(AnswerabilityGrade.A, true)]
    [InlineData(AnswerabilityGrade.B, true)]
    [InlineData(AnswerabilityGrade.C, false)]
    [InlineData(AnswerabilityGrade.D, false)]
    public void EvaluationResult_IsPassed_BasedOnAnswerability(AnswerabilityGrade grade, bool expectedPass)
    {
        // Act
        var result = new EvaluationResult { Answerability = grade };

        // Assert
        result.IsPassed.Should().Be(expectedPass);
    }
}

public class ClaimVerificationTests
{
    [Fact]
    public void ClaimVerification_StoresAllProperties()
    {
        // Act
        var claim = new ClaimVerification(
            Claim: "RAG uses retrieval",
            IsSupported: true,
            Reasoning: "Found in paragraph 2");

        // Assert
        claim.Claim.Should().Be("RAG uses retrieval");
        claim.IsSupported.Should().BeTrue();
        claim.Reasoning.Should().Be("Found in paragraph 2");
    }
}

public class EvaluationInputTests
{
    [Fact]
    public void EvaluationInput_StoresAllProperties()
    {
        // Act
        var input = new EvaluationInput
        {
            Question = "What is RAG?",
            Answer = "RAG is Retrieval Augmented Generation",
            Contexts =
            [
                new ContextReference("chunk-1", "RAG combines...", true)
            ],
            GroundTruth = "RAG stands for Retrieval Augmented Generation"
        };

        // Assert
        input.Question.Should().Be("What is RAG?");
        input.Answer.Should().Be("RAG is Retrieval Augmented Generation");
        input.Contexts.Should().HaveCount(1);
        input.GroundTruth.Should().NotBeNull();
    }
}

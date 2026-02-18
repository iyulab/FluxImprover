namespace FluxImprover.Tests.Models;

using FluentAssertions;
using FluxImprover.Models;
using Xunit;

public sealed class EvaluationResultAdditionalTests
{
    [Theory]
    [InlineData(AnswerabilityGrade.A, true)]
    [InlineData(AnswerabilityGrade.B, true)]
    [InlineData(AnswerabilityGrade.C, false)]
    [InlineData(AnswerabilityGrade.D, false)]
    public void IsPassed_ByGrade_ReturnsExpected(AnswerabilityGrade grade, bool expected)
    {
        var result = new EvaluationResult
        {
            Answerability = grade
        };

        result.IsPassed.Should().Be(expected);
    }

    [Fact]
    public void IsPassed_NullAnswerability_ReturnsFalse()
    {
        var result = new EvaluationResult
        {
            Answerability = null
        };

        result.IsPassed.Should().BeFalse();
    }

    [Fact]
    public void EvaluationResult_DefaultValues_AreNull()
    {
        var result = new EvaluationResult();

        result.Faithfulness.Should().BeNull();
        result.Relevancy.Should().BeNull();
        result.Answerability.Should().BeNull();
        result.OverallScore.Should().BeNull();
        result.Details.Should().BeNull();
    }

    [Fact]
    public void EvaluationResult_CanBeFullyInitialized()
    {
        var details = new EvaluationDetails
        {
            RelevancyReasoning = "Answer is directly relevant.",
            AnswerabilityReasoning = "Context provides full answer.",
            FaithfulnessClaims =
            [
                new ClaimVerification("ML is AI", true, "Supported by context"),
                new ClaimVerification("ML learns from data", false, "Not in context")
            ]
        };

        var result = new EvaluationResult
        {
            Faithfulness = 0.85,
            Relevancy = 0.92,
            Answerability = AnswerabilityGrade.A,
            OverallScore = 0.88,
            Details = details
        };

        result.Faithfulness.Should().Be(0.85);
        result.Relevancy.Should().Be(0.92);
        result.Answerability.Should().Be(AnswerabilityGrade.A);
        result.OverallScore.Should().Be(0.88);
        result.Details!.FaithfulnessClaims.Should().HaveCount(2);
    }

    [Fact]
    public void EvaluationResult_IsImmutableRecord()
    {
        var result = new EvaluationResult
        {
            Faithfulness = 0.5,
            Relevancy = 0.6
        };

        var modified = result with { Faithfulness = 0.9 };

        result.Faithfulness.Should().Be(0.5);
        modified.Faithfulness.Should().Be(0.9);
        modified.Relevancy.Should().Be(0.6);
    }
}

public sealed class ClaimVerificationAdditionalTests
{
    [Fact]
    public void ClaimVerification_NotSupported_WithoutReasoning_DefaultsNull()
    {
        var claim = new ClaimVerification("Sky is green", false);

        claim.IsSupported.Should().BeFalse();
        claim.Reasoning.Should().BeNull();
    }

    [Fact]
    public void ClaimVerification_Equality_SameValues()
    {
        var claim1 = new ClaimVerification("Claim A", true, "Reason");
        var claim2 = new ClaimVerification("Claim A", true, "Reason");

        claim1.Should().Be(claim2);
    }
}

public sealed class EvaluationDetailsTests
{
    [Fact]
    public void EvaluationDetails_DefaultValues_AreNull()
    {
        var details = new EvaluationDetails();

        details.FaithfulnessClaims.Should().BeNull();
        details.RelevancyReasoning.Should().BeNull();
        details.AnswerabilityReasoning.Should().BeNull();
    }

    [Fact]
    public void EvaluationDetails_CanBeFullyInitialized()
    {
        var details = new EvaluationDetails
        {
            FaithfulnessClaims =
            [
                new ClaimVerification("Claim 1", true),
                new ClaimVerification("Claim 2", false)
            ],
            RelevancyReasoning = "Highly relevant",
            AnswerabilityReasoning = "Fully answerable"
        };

        details.FaithfulnessClaims.Should().HaveCount(2);
        details.RelevancyReasoning.Should().Be("Highly relevant");
        details.AnswerabilityReasoning.Should().Be("Fully answerable");
    }
}

public sealed class AnswerabilityGradeTests
{
    [Fact]
    public void AnswerabilityGrade_HasFourValues()
    {
        var values = Enum.GetValues<AnswerabilityGrade>();

        values.Should().HaveCount(4);
        values.Should().Contain(AnswerabilityGrade.A);
        values.Should().Contain(AnswerabilityGrade.B);
        values.Should().Contain(AnswerabilityGrade.C);
        values.Should().Contain(AnswerabilityGrade.D);
    }
}

public sealed class DatasetMetadataAdditionalTests
{
    [Fact]
    public void DatasetMetadata_DefaultValues_AreNull()
    {
        var metadata = new DatasetMetadata();

        metadata.CreatedAt.Should().BeNull();
        metadata.Generator.Should().BeNull();
        metadata.SourceDocuments.Should().BeNull();
        metadata.TotalSamples.Should().BeNull();
        metadata.Configuration.Should().BeNull();
    }

    [Fact]
    public void DatasetMetadata_IsImmutableRecord()
    {
        var metadata = new DatasetMetadata
        {
            Generator = "original",
            TotalSamples = 10
        };

        var modified = metadata with { TotalSamples = 50 };

        metadata.TotalSamples.Should().Be(10);
        modified.TotalSamples.Should().Be(50);
        modified.Generator.Should().Be("original");
    }
}

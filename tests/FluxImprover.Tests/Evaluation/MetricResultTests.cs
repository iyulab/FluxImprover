namespace FluxImprover.Tests.Evaluation;

using FluentAssertions;
using FluxImprover.Evaluation;
using Xunit;

public sealed class MetricResultTests
{
    #region IsPassed

    [Theory]
    [InlineData(0.5, true)]
    [InlineData(0.51, true)]
    [InlineData(1.0, true)]
    [InlineData(0.49, false)]
    [InlineData(0.0, false)]
    [InlineData(0.499, false)]
    public void IsPassed_ScoreThreshold_ReturnsExpected(double score, bool expected)
    {
        var result = new MetricResult
        {
            MetricName = "test",
            Score = score
        };

        result.IsPassed.Should().Be(expected);
    }

    #endregion

    #region Failed Factory

    [Fact]
    public void Failed_WithReason_CreatesFailedResult()
    {
        var result = MetricResult.Failed("faithfulness", "No supporting context found");

        result.MetricName.Should().Be("faithfulness");
        result.Score.Should().Be(0.0);
        result.IsPassed.Should().BeFalse();
        result.Details.Should().ContainKey("reason");
        result.Details["reason"].Should().Be("No supporting context found");
    }

    [Fact]
    public void Failed_WithoutReason_CreatesFailedResultWithEmptyDetails()
    {
        var result = MetricResult.Failed("relevancy");

        result.MetricName.Should().Be("relevancy");
        result.Score.Should().Be(0.0);
        result.IsPassed.Should().BeFalse();
        result.Details.Should().BeEmpty();
    }

    [Fact]
    public void Failed_NullReason_DoesNotAddReasonToDetails()
    {
        var result = MetricResult.Failed("test", null);

        result.Details.Should().NotContainKey("reason");
    }

    #endregion

    #region Properties

    [Fact]
    public void MetricResult_CanBeFullyInitialized()
    {
        var details = new Dictionary<string, object?>
        {
            ["claim_count"] = 5,
            ["supported_count"] = 4
        };

        var result = new MetricResult
        {
            MetricName = "faithfulness",
            Score = 0.8,
            Details = details
        };

        result.MetricName.Should().Be("faithfulness");
        result.Score.Should().Be(0.8);
        result.Details.Should().HaveCount(2);
        result.Details["claim_count"].Should().Be(5);
    }

    [Fact]
    public void MetricResult_DefaultDetails_IsEmpty()
    {
        var result = new MetricResult
        {
            MetricName = "test",
            Score = 0.5
        };

        result.Details.Should().BeEmpty();
    }

    [Fact]
    public void MetricResult_IsImmutableRecord()
    {
        var result = new MetricResult
        {
            MetricName = "original",
            Score = 0.5
        };

        var modified = result with { Score = 0.9 };

        result.Score.Should().Be(0.5);
        modified.Score.Should().Be(0.9);
        modified.MetricName.Should().Be("original");
    }

    [Fact]
    public void MetricResult_Equality_SameValues_AreEqual()
    {
        var details = new Dictionary<string, object?>();

        var result1 = new MetricResult
        {
            MetricName = "test",
            Score = 0.7,
            Details = details
        };
        var result2 = new MetricResult
        {
            MetricName = "test",
            Score = 0.7,
            Details = details
        };

        result1.Should().Be(result2);
    }

    #endregion
}

namespace FluxImprover.Tests.Evaluation;

using AwesomeAssertions;
using FluxImprover.Evaluation;
using FluxImprover.Options;
using FluxImprover.Services;
using NSubstitute;
using Xunit;

public sealed class AnswerCoverageEvaluatorTests
{
    private readonly ITextGenerationService _completionService = Substitute.For<ITextGenerationService>();
    private readonly AnswerCoverageEvaluator _sut;

    public AnswerCoverageEvaluatorTests()
    {
        _sut = new AnswerCoverageEvaluator(_completionService);
    }

    private void Respond(string json) =>
        _completionService.CompleteAsync(Arg.Any<string>(), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>()).Returns(json);

    [Fact]
    public async Task Score_counts_supported_facts_once_and_partial_facts_half()
    {
        Respond(@"{""facts"": [{""fact"": ""Paris is the capital"", ""verdict"": ""supported""}, {""fact"": ""2 million people"", ""verdict"": ""partial""}, {""fact"": ""on the Seine"", ""verdict"": ""missing""}, {""fact"": ""since 987"", ""verdict"": ""missing""}]}");

        var result = await _sut.EvaluateAsync("Paris is the capital; it is big.", "Paris is the capital of France, has 2 million people, lies on the Seine and has been the capital since 987.",
            cancellationToken: TestContext.Current.CancellationToken);

        result.MetricName.Should().Be("AnswerCoverage");
        result.Score.Should().BeApproximately((1 + 0.5) / 4, 1e-9);
        result.Details["supportedFacts"].Should().Be(1);
        result.Details["partialFacts"].Should().Be(1);
        result.Details["totalFacts"].Should().Be(4);
    }

    [Fact]
    public async Task Given_facts_are_sent_in_order_and_an_unknown_verdict_counts_as_missing()
    {
        string? prompt = null;
        _completionService.CompleteAsync(Arg.Do<string>(p => prompt = p), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns(@"{""facts"": [{""fact"": ""A"", ""verdict"": ""supported""}, {""fact"": ""B"", ""verdict"": ""maybe""}]}");

        var result = await _sut.EvaluateAsync("answer text", ["A", "B"], cancellationToken: TestContext.Current.CancellationToken);

        prompt.Should().Contain("1. A").And.Contain("2. B");
        result.Score.Should().BeApproximately(0.5, 1e-9);
        var facts = (IEnumerable<Dictionary<string, object?>>)result.Details["facts"]!;
        facts.Select(f => f["verdict"]).Should().Equal("supported", "missing");
    }

    [Fact]
    public async Task Empty_inputs_and_a_response_without_facts_fail_without_calling_or_scoring()
    {
        (await _sut.EvaluateAsync("", "reference", cancellationToken: TestContext.Current.CancellationToken)).Score.Should().Be(0);
        (await _sut.EvaluateAsync("answer", Array.Empty<string>(), cancellationToken: TestContext.Current.CancellationToken)).Score.Should().Be(0);
        await _completionService.DidNotReceiveWithAnyArgs().CompleteAsync(default!, default, TestContext.Current.CancellationToken);

        Respond(@"{""facts"": []}");
        var empty = await _sut.EvaluateAsync("answer", "reference", cancellationToken: TestContext.Current.CancellationToken);
        empty.IsPassed.Should().BeFalse();
        empty.Details["reason"].Should().Be("The response judged no facts");
    }

    [Fact]
    public async Task IncludeDetails_false_returns_the_score_alone()
    {
        Respond(@"{""facts"": [{""fact"": ""A"", ""verdict"": ""supported""}, {""fact"": ""B"", ""verdict"": ""missing""}]}");

        var result = await _sut.EvaluateAsync("answer", "reference", new EvaluationOptions { IncludeDetails = false }, TestContext.Current.CancellationToken);

        result.Score.Should().BeApproximately(0.5, 1e-9);
        result.Details.Should().BeEmpty();
    }
}

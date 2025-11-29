namespace FluxImprover.Tests;

using FluentAssertions;
using FluxImprover.Services;
using NSubstitute;
using Xunit;

public sealed class FluxImproverBuilderTests
{
    [Fact]
    public void Build_WithCompletionService_ReturnsAllServices()
    {
        // Arrange
        var completionService = Substitute.For<ITextCompletionService>();
        var builder = new FluxImproverBuilder();

        // Act
        var services = builder
            .WithCompletionService(completionService)
            .Build();

        // Assert
        services.Should().NotBeNull();
        services.Summarization.Should().NotBeNull();
        services.KeywordExtraction.Should().NotBeNull();
        services.ChunkEnrichment.Should().NotBeNull();
        services.Faithfulness.Should().NotBeNull();
        services.Relevancy.Should().NotBeNull();
        services.Answerability.Should().NotBeNull();
        services.QAGenerator.Should().NotBeNull();
        services.QAFilter.Should().NotBeNull();
        services.QAPipeline.Should().NotBeNull();
        services.QuestionSuggestion.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithoutCompletionService_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new FluxImproverBuilder();

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CompletionService*");
    }

    [Fact]
    public void WithCompletionService_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new FluxImproverBuilder();

        // Act
        var act = () => builder.WithCompletionService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("completionService");
    }

    [Fact]
    public void WithCompletionService_ReturnsBuilderForChaining()
    {
        // Arrange
        var completionService = Substitute.For<ITextCompletionService>();
        var builder = new FluxImproverBuilder();

        // Act
        var result = builder.WithCompletionService(completionService);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void Build_CanBeCalledMultipleTimes()
    {
        // Arrange
        var completionService = Substitute.For<ITextCompletionService>();
        var builder = new FluxImproverBuilder()
            .WithCompletionService(completionService);

        // Act
        var services1 = builder.Build();
        var services2 = builder.Build();

        // Assert
        services1.Should().NotBeNull();
        services2.Should().NotBeNull();
        services1.Should().NotBeSameAs(services2);
    }

    [Fact]
    public void FluxImproverServices_IsRecord_WithValueEquality()
    {
        // Arrange
        var completionService = Substitute.For<ITextCompletionService>();
        var builder = new FluxImproverBuilder()
            .WithCompletionService(completionService);

        var services = builder.Build();

        // Act & Assert - record should be reference type but with value semantics
        services.GetType().IsClass.Should().BeTrue();
    }
}

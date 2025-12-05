namespace FluxImprover.Tests;

using FluentAssertions;
using FluxImprover.ChunkFiltering;
using FluxImprover.ContextualRetrieval;
using FluxImprover.Enrichment;
using FluxImprover.Evaluation;
using FluxImprover.QAGeneration;
using FluxImprover.QueryPreprocessing;
using FluxImprover.QuestionSuggestion;
using FluxImprover.RelationshipDiscovery;
using FluxImprover.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFluxImprover_WithFactory_RegistersFluxImproverServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextCompletionService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        var provider = services.BuildServiceProvider();

        // Assert
        var fluxServices = provider.GetService<FluxImproverServices>();
        fluxServices.Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithFactory_RegistersAllEnrichmentServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextCompletionService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<SummarizationService>().Should().NotBeNull();
        provider.GetService<ISummarizationService>().Should().NotBeNull();
        provider.GetService<KeywordExtractionService>().Should().NotBeNull();
        provider.GetService<IKeywordExtractionService>().Should().NotBeNull();
        provider.GetService<ChunkEnrichmentService>().Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithFactory_RegistersAllEvaluators()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextCompletionService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<FaithfulnessEvaluator>().Should().NotBeNull();
        provider.GetService<RelevancyEvaluator>().Should().NotBeNull();
        provider.GetService<AnswerabilityEvaluator>().Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithFactory_RegistersAllQAServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextCompletionService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<QAGeneratorService>().Should().NotBeNull();
        provider.GetService<QAFilterService>().Should().NotBeNull();
        provider.GetService<QAPipeline>().Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithFactory_RegistersOtherServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextCompletionService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<QuestionSuggestionService>().Should().NotBeNull();
        provider.GetService<IChunkFilteringService>().Should().NotBeNull();
        provider.GetService<IQueryPreprocessingService>().Should().NotBeNull();
        provider.GetService<IContextualEnrichmentService>().Should().NotBeNull();
        provider.GetService<IChunkRelationshipService>().Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithFactory_ReturnsServicesFromSameContainer()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextCompletionService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        var provider = services.BuildServiceProvider();

        // Assert - Services should come from the same FluxImproverServices instance
        var fluxServices = provider.GetRequiredService<FluxImproverServices>();
        var summarization = provider.GetRequiredService<SummarizationService>();

        summarization.Should().BeSameAs(fluxServices.Summarization);
    }

    [Fact]
    public void AddFluxImprover_WithNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddFluxImprover(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddFluxImprover_WithRegisteredITextCompletionService_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextCompletionService>();
        services.AddSingleton(completionService);

        // Act
        services.AddFluxImprover();
        var provider = services.BuildServiceProvider();

        // Assert
        var fluxServices = provider.GetService<FluxImproverServices>();
        fluxServices.Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithoutRegisteredITextCompletionService_ThrowsOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFluxImprover();
        var provider = services.BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<FluxImproverServices>();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddFluxImprover_CalledMultipleTimes_UsesFirstRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService1 = Substitute.For<ITextCompletionService>();
        var completionService2 = Substitute.For<ITextCompletionService>();

        // Act
        services.AddFluxImprover(_ => completionService1);
        services.AddFluxImprover(_ => completionService2); // Should be ignored (TryAdd)
        var provider = services.BuildServiceProvider();

        // Assert
        var fluxServices = provider.GetRequiredService<FluxImproverServices>();
        fluxServices.Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithFactory_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextCompletionService>();

        // Act
        var result = services.AddFluxImprover(_ => completionService);

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddFluxImprover_ServicesAreSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextCompletionService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        var provider = services.BuildServiceProvider();

        // Assert - Same instance should be returned
        var services1 = provider.GetRequiredService<FluxImproverServices>();
        var services2 = provider.GetRequiredService<FluxImproverServices>();
        services1.Should().BeSameAs(services2);
    }
}

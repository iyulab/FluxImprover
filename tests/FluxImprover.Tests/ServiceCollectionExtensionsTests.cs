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
        var completionService = Substitute.For<ITextGenerationService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        using var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var fluxServices = scope.ServiceProvider.GetService<FluxImproverServices>();
        fluxServices.Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithFactory_RegistersAllEnrichmentServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextGenerationService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        using var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetService<SummarizationService>().Should().NotBeNull();
        sp.GetService<ISummarizationService>().Should().NotBeNull();
        sp.GetService<KeywordExtractionService>().Should().NotBeNull();
        sp.GetService<IKeywordExtractionService>().Should().NotBeNull();
        sp.GetService<ChunkEnrichmentService>().Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithFactory_RegistersAllEvaluators()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextGenerationService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        using var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetService<FaithfulnessEvaluator>().Should().NotBeNull();
        sp.GetService<RelevancyEvaluator>().Should().NotBeNull();
        sp.GetService<AnswerabilityEvaluator>().Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithFactory_RegistersAllQAServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextGenerationService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        using var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetService<QAGeneratorService>().Should().NotBeNull();
        sp.GetService<QAFilterService>().Should().NotBeNull();
        sp.GetService<QAPipeline>().Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithFactory_RegistersOtherServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextGenerationService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        using var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetService<QuestionSuggestionService>().Should().NotBeNull();
        sp.GetService<IChunkFilteringService>().Should().NotBeNull();
        sp.GetService<IQueryPreprocessingService>().Should().NotBeNull();
        sp.GetService<IContextualEnrichmentService>().Should().NotBeNull();
        sp.GetService<IChunkRelationshipService>().Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithFactory_ReturnsServicesFromSameContainer()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextGenerationService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        using var provider = services.BuildServiceProvider();

        // Assert - Services should come from the same FluxImproverServices instance within a scope
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var fluxServices = sp.GetRequiredService<FluxImproverServices>();
        var summarization = sp.GetRequiredService<SummarizationService>();

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
    public void AddFluxImprover_WithRegisteredITextGenerationService_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextGenerationService>();
        services.AddSingleton(completionService);

        // Act
        services.AddFluxImprover();
        using var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var fluxServices = scope.ServiceProvider.GetService<FluxImproverServices>();
        fluxServices.Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithoutRegisteredITextGenerationService_ThrowsOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFluxImprover();
        using var provider = services.BuildServiceProvider();

        // Act
        using var scope = provider.CreateScope();
        var act = () => scope.ServiceProvider.GetRequiredService<FluxImproverServices>();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddFluxImprover_CalledMultipleTimes_UsesFirstRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService1 = Substitute.For<ITextGenerationService>();
        var completionService2 = Substitute.For<ITextGenerationService>();

        // Act
        services.AddFluxImprover(_ => completionService1);
        services.AddFluxImprover(_ => completionService2); // Should be ignored (TryAdd)
        using var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var fluxServices = scope.ServiceProvider.GetRequiredService<FluxImproverServices>();
        fluxServices.Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_WithFactory_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextGenerationService>();

        // Act
        var result = services.AddFluxImprover(_ => completionService);

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddFluxImprover_DefaultLifetime_IsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextGenerationService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        using var provider = services.BuildServiceProvider();

        // Assert - Different scopes should get different instances
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var services1 = scope1.ServiceProvider.GetRequiredService<FluxImproverServices>();
        var services2 = scope2.ServiceProvider.GetRequiredService<FluxImproverServices>();
        services1.Should().NotBeSameAs(services2);
    }

    [Fact]
    public void AddFluxImprover_WithinSameScope_ReturnsSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextGenerationService>();

        // Act
        services.AddFluxImprover(_ => completionService);
        using var provider = services.BuildServiceProvider();

        // Assert - Same scope should return same instance
        using var scope = provider.CreateScope();
        var services1 = scope.ServiceProvider.GetRequiredService<FluxImproverServices>();
        var services2 = scope.ServiceProvider.GetRequiredService<FluxImproverServices>();
        services1.Should().BeSameAs(services2);
    }

    [Fact]
    public void AddFluxImprover_WithSingletonLifetime_ReturnsSameInstanceAcrossScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextGenerationService>();

        // Act
        services.AddFluxImprover(_ => completionService, ServiceLifetime.Singleton);
        using var provider = services.BuildServiceProvider();

        // Assert
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var services1 = scope1.ServiceProvider.GetRequiredService<FluxImproverServices>();
        var services2 = scope2.ServiceProvider.GetRequiredService<FluxImproverServices>();
        services1.Should().BeSameAs(services2);
    }

    [Fact]
    public void AddFluxImprover_Scoped_ResolvesFromServiceScopeFactory()
    {
        // Arrange — simulates the real-world pattern:
        // Singleton tool class uses IServiceScopeFactory to create per-request scopes
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextGenerationService>();
        services.AddFluxImprover(_ => completionService);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        // Act — resolve from a manually created scope (as Singleton tools do)
        using var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var queryPreprocessing = scope.ServiceProvider.GetService<IQueryPreprocessingService>();
        var chunkFiltering = scope.ServiceProvider.GetService<IChunkFilteringService>();
        var fluxServices = scope.ServiceProvider.GetService<FluxImproverServices>();

        // Assert
        queryPreprocessing.Should().NotBeNull();
        chunkFiltering.Should().NotBeNull();
        fluxServices.Should().NotBeNull();
    }

    [Fact]
    public void AddFluxImprover_Scoped_WithValidateScopes_DoesNotThrow()
    {
        // Arrange — ASP.NET Core Development environment enables ValidateScopes
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextGenerationService>();
        services.AddFluxImprover(_ => completionService);

        // Act — build with scope validation (as ASP.NET Core does in Development)
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        // Assert — all services should resolve without scope validation errors
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var resolve = () =>
        {
            _ = sp.GetRequiredService<FluxImproverServices>();
            _ = sp.GetRequiredService<SummarizationService>();
            _ = sp.GetRequiredService<ISummarizationService>();
            _ = sp.GetRequiredService<KeywordExtractionService>();
            _ = sp.GetRequiredService<IKeywordExtractionService>();
            _ = sp.GetRequiredService<ChunkEnrichmentService>();
            _ = sp.GetRequiredService<FaithfulnessEvaluator>();
            _ = sp.GetRequiredService<RelevancyEvaluator>();
            _ = sp.GetRequiredService<AnswerabilityEvaluator>();
            _ = sp.GetRequiredService<QAGeneratorService>();
            _ = sp.GetRequiredService<QAFilterService>();
            _ = sp.GetRequiredService<QAPipeline>();
            _ = sp.GetRequiredService<QuestionSuggestionService>();
            _ = sp.GetRequiredService<IChunkFilteringService>();
            _ = sp.GetRequiredService<IQueryPreprocessingService>();
            _ = sp.GetRequiredService<IContextualEnrichmentService>();
            _ = sp.GetRequiredService<IChunkRelationshipService>();
        };

        resolve.Should().NotThrow();
    }

    [Fact]
    public void AddFluxImprover_Scoped_CannotResolveFromRootWithValidateScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextGenerationService>();
        services.AddFluxImprover(_ => completionService);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        // Act — resolving scoped service from root provider should throw
        var act = () => provider.GetRequiredService<FluxImproverServices>();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddFluxImprover_ParameterlessOverload_DefaultsToScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var completionService = Substitute.For<ITextGenerationService>();
        services.AddSingleton(completionService);

        // Act
        services.AddFluxImprover();
        using var provider = services.BuildServiceProvider();

        // Assert — different scopes, different instances
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var s1 = scope1.ServiceProvider.GetRequiredService<FluxImproverServices>();
        var s2 = scope2.ServiceProvider.GetRequiredService<FluxImproverServices>();
        s1.Should().NotBeSameAs(s2);
    }
}

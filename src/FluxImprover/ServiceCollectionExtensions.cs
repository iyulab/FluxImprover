namespace FluxImprover;

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
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Extension methods for registering FluxImprover services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all FluxImprover services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="completionServiceFactory">
    /// Factory function to create the ITextGenerationService from the service provider.
    /// This is the only external dependency required by FluxImprover.
    /// </param>
    /// <param name="lifetime">
    /// The service lifetime for all FluxImprover services. Defaults to <see cref="ServiceLifetime.Scoped"/>,
    /// which is compatible with the standard ASP.NET Core <c>IServiceScopeFactory.CreateScope()</c> pattern.
    /// Use <see cref="ServiceLifetime.Singleton"/> only when the <paramref name="completionServiceFactory"/>
    /// and all its dependencies are also singletons.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Register with your own ITextGenerationService implementation
    /// services.AddFluxImprover(sp => sp.GetRequiredService&lt;MyTextCompletionService&gt;());
    ///
    /// // Or create directly
    /// services.AddFluxImprover(_ => new OpenAITextGenerationService(apiKey));
    ///
    /// // Use Singleton lifetime when all dependencies are singletons
    /// services.AddFluxImprover(_ => new OpenAITextGenerationService(apiKey), ServiceLifetime.Singleton);
    ///
    /// // Then inject individual services as needed
    /// public class MyService(ChunkEnrichmentService enrichment, QAPipeline pipeline) { }
    /// </code>
    /// </example>
    public static IServiceCollection AddFluxImprover(
        this IServiceCollection services,
        Func<IServiceProvider, ITextGenerationService> completionServiceFactory,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(completionServiceFactory);

        // Register the core services container
        services.TryAdd(new ServiceDescriptor(
            typeof(FluxImproverServices),
            sp =>
            {
                var completionService = completionServiceFactory(sp);
                return new FluxImproverBuilder()
                    .WithCompletionService(completionService)
                    .Build();
            },
            lifetime));

        // Register individual services as facades for convenience
        // This allows consumers to inject specific services directly
        RegisterEnrichmentServices(services, lifetime);
        RegisterEvaluationServices(services, lifetime);
        RegisterQAServices(services, lifetime);
        RegisterOtherServices(services, lifetime);

        return services;
    }

    /// <summary>
    /// Adds all FluxImprover services using an already registered ITextGenerationService.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">
    /// The service lifetime for all FluxImprover services. Defaults to <see cref="ServiceLifetime.Scoped"/>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This overload requires ITextGenerationService to be already registered in the container.
    /// </remarks>
    /// <example>
    /// <code>
    /// // First register your ITextGenerationService
    /// services.AddSingleton&lt;ITextGenerationService, MyTextCompletionService&gt;();
    ///
    /// // Then add FluxImprover
    /// services.AddFluxImprover();
    /// </code>
    /// </example>
    public static IServiceCollection AddFluxImprover(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        return services.AddFluxImprover(
            sp => sp.GetRequiredService<ITextGenerationService>(),
            lifetime);
    }

    private static void TryAddService<TService>(
        IServiceCollection services,
        Func<IServiceProvider, TService> factory,
        ServiceLifetime lifetime) where TService : class
    {
        services.TryAdd(new ServiceDescriptor(typeof(TService), sp => factory(sp), lifetime));
    }

    private static void RegisterEnrichmentServices(IServiceCollection services, ServiceLifetime lifetime)
    {
        TryAddService(services,
            sp => sp.GetRequiredService<FluxImproverServices>().Summarization, lifetime);

        TryAddService<ISummarizationService>(services,
            sp => sp.GetRequiredService<SummarizationService>(), lifetime);

        TryAddService(services,
            sp => sp.GetRequiredService<FluxImproverServices>().KeywordExtraction, lifetime);

        TryAddService<IKeywordExtractionService>(services,
            sp => sp.GetRequiredService<KeywordExtractionService>(), lifetime);

        TryAddService(services,
            sp => sp.GetRequiredService<FluxImproverServices>().ChunkEnrichment, lifetime);
    }

    private static void RegisterEvaluationServices(IServiceCollection services, ServiceLifetime lifetime)
    {
        TryAddService(services,
            sp => sp.GetRequiredService<FluxImproverServices>().Faithfulness, lifetime);

        TryAddService(services,
            sp => sp.GetRequiredService<FluxImproverServices>().Relevancy, lifetime);

        TryAddService(services,
            sp => sp.GetRequiredService<FluxImproverServices>().Answerability, lifetime);
    }

    private static void RegisterQAServices(IServiceCollection services, ServiceLifetime lifetime)
    {
        TryAddService(services,
            sp => sp.GetRequiredService<FluxImproverServices>().QAGenerator, lifetime);

        TryAddService(services,
            sp => sp.GetRequiredService<FluxImproverServices>().QAFilter, lifetime);

        TryAddService(services,
            sp => sp.GetRequiredService<FluxImproverServices>().QAPipeline, lifetime);
    }

    private static void RegisterOtherServices(IServiceCollection services, ServiceLifetime lifetime)
    {
        TryAddService(services,
            sp => sp.GetRequiredService<FluxImproverServices>().QuestionSuggestion, lifetime);

        TryAddService<IChunkFilteringService>(services,
            sp => sp.GetRequiredService<FluxImproverServices>().ChunkFiltering, lifetime);

        TryAddService<IQueryPreprocessingService>(services,
            sp => sp.GetRequiredService<FluxImproverServices>().QueryPreprocessing, lifetime);

        TryAddService<IContextualEnrichmentService>(services,
            sp => sp.GetRequiredService<FluxImproverServices>().ContextualEnrichment, lifetime);

        TryAddService<IChunkRelationshipService>(services,
            sp => sp.GetRequiredService<FluxImproverServices>().ChunkRelationship, lifetime);
    }
}

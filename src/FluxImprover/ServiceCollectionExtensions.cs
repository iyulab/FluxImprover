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
    /// Factory function to create the ITextCompletionService from the service provider.
    /// This is the only external dependency required by FluxImprover.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Register with your own ITextCompletionService implementation
    /// services.AddFluxImprover(sp => sp.GetRequiredService&lt;MyTextCompletionService&gt;());
    ///
    /// // Or create directly
    /// services.AddFluxImprover(_ => new OpenAITextCompletionService(apiKey));
    ///
    /// // Then inject individual services as needed
    /// public class MyService(ChunkEnrichmentService enrichment, QAPipeline pipeline) { }
    /// </code>
    /// </example>
    public static IServiceCollection AddFluxImprover(
        this IServiceCollection services,
        Func<IServiceProvider, ITextCompletionService> completionServiceFactory)
    {
        ArgumentNullException.ThrowIfNull(completionServiceFactory);

        // Register the core services container
        services.TryAddSingleton(sp =>
        {
            var completionService = completionServiceFactory(sp);
            return new FluxImproverBuilder()
                .WithCompletionService(completionService)
                .Build();
        });

        // Register individual services as facades for convenience
        // This allows consumers to inject specific services directly
        RegisterEnrichmentServices(services);
        RegisterEvaluationServices(services);
        RegisterQAServices(services);
        RegisterOtherServices(services);

        return services;
    }

    /// <summary>
    /// Adds all FluxImprover services using an already registered ITextCompletionService.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This overload requires ITextCompletionService to be already registered in the container.
    /// </remarks>
    /// <example>
    /// <code>
    /// // First register your ITextCompletionService
    /// services.AddSingleton&lt;ITextCompletionService, MyTextCompletionService&gt;();
    ///
    /// // Then add FluxImprover
    /// services.AddFluxImprover();
    /// </code>
    /// </example>
    public static IServiceCollection AddFluxImprover(this IServiceCollection services)
    {
        return services.AddFluxImprover(sp => sp.GetRequiredService<ITextCompletionService>());
    }

    private static void RegisterEnrichmentServices(IServiceCollection services)
    {
        services.TryAddSingleton(sp =>
            sp.GetRequiredService<FluxImproverServices>().Summarization);

        services.TryAddSingleton<ISummarizationService>(sp =>
            sp.GetRequiredService<SummarizationService>());

        services.TryAddSingleton(sp =>
            sp.GetRequiredService<FluxImproverServices>().KeywordExtraction);

        services.TryAddSingleton<IKeywordExtractionService>(sp =>
            sp.GetRequiredService<KeywordExtractionService>());

        services.TryAddSingleton(sp =>
            sp.GetRequiredService<FluxImproverServices>().ChunkEnrichment);
    }

    private static void RegisterEvaluationServices(IServiceCollection services)
    {
        services.TryAddSingleton(sp =>
            sp.GetRequiredService<FluxImproverServices>().Faithfulness);

        services.TryAddSingleton(sp =>
            sp.GetRequiredService<FluxImproverServices>().Relevancy);

        services.TryAddSingleton(sp =>
            sp.GetRequiredService<FluxImproverServices>().Answerability);
    }

    private static void RegisterQAServices(IServiceCollection services)
    {
        services.TryAddSingleton(sp =>
            sp.GetRequiredService<FluxImproverServices>().QAGenerator);

        services.TryAddSingleton(sp =>
            sp.GetRequiredService<FluxImproverServices>().QAFilter);

        services.TryAddSingleton(sp =>
            sp.GetRequiredService<FluxImproverServices>().QAPipeline);
    }

    private static void RegisterOtherServices(IServiceCollection services)
    {
        services.TryAddSingleton(sp =>
            sp.GetRequiredService<FluxImproverServices>().QuestionSuggestion);

        services.TryAddSingleton(sp =>
            sp.GetRequiredService<FluxImproverServices>().ChunkFiltering);

        services.TryAddSingleton(sp =>
            sp.GetRequiredService<FluxImproverServices>().QueryPreprocessing);

        services.TryAddSingleton(sp =>
            sp.GetRequiredService<FluxImproverServices>().ContextualEnrichment);

        services.TryAddSingleton(sp =>
            sp.GetRequiredService<FluxImproverServices>().ChunkRelationship);
    }
}

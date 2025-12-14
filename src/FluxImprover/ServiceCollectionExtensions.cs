namespace FluxImprover;

using FluxImprover.ChunkFiltering;
using FluxImprover.ContextualRetrieval;
using FluxImprover.Enrichment;
using FluxImprover.Evaluation;
using FluxImprover.LocalAI;
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

    /// <summary>
    /// Adds all FluxImprover services with LocalAI.Generator as the default LLM backend.
    /// Uses the default model preset.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// LocalAI requires async initialization. The services will be initialized on first use.
    /// Consider calling <see cref="InitializeLocalAIAsync"/> during application startup
    /// to ensure the model is loaded before first use.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddFluxImproverWithLocalAI();
    ///
    /// // Optional: Initialize during startup
    /// await app.Services.InitializeLocalAIAsync();
    /// </code>
    /// </example>
    public static IServiceCollection AddFluxImproverWithLocalAI(this IServiceCollection services)
    {
        return services.AddFluxImproverWithLocalAI(_ => { });
    }

    /// <summary>
    /// Adds all FluxImprover services with LocalAI.Generator as the default LLM backend.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for LocalAI options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// LocalAI requires async initialization. The services will be initialized on first use.
    /// Consider calling <see cref="InitializeLocalAIAsync"/> during application startup
    /// to ensure the model is loaded before first use.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddFluxImproverWithLocalAI(options =>
    /// {
    ///     options.ModelPreset = GeneratorModelPreset.Quality;
    ///     options.GenerationDefaults = new LocalAIGenerationDefaults
    ///     {
    ///         Temperature = 0.7f,
    ///         MaxTokens = 1024
    ///     };
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddFluxImproverWithLocalAI(
        this IServiceCollection services,
        Action<LocalAICompletionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        // Register LocalAI options
        var options = new LocalAICompletionOptions();
        configure(options);

        // Register the lazy initialization wrapper
        services.TryAddSingleton(_ => new LocalAIInitializer(options));

        // Register ITextCompletionService backed by LocalAI
        services.TryAddSingleton<ITextCompletionService>(sp =>
        {
            var initializer = sp.GetRequiredService<LocalAIInitializer>();
            return initializer.GetServiceAsync().GetAwaiter().GetResult();
        });

        // Register LocalAICompletionService for direct access
        services.TryAddSingleton(sp =>
            (LocalAICompletionService)sp.GetRequiredService<ITextCompletionService>());

        // Register FluxImproverServices
        services.TryAddSingleton(sp =>
        {
            var completionService = sp.GetRequiredService<ITextCompletionService>();
            return new FluxImproverBuilder()
                .WithCompletionService(completionService)
                .Build();
        });

        // Register individual services as facades for convenience
        RegisterEnrichmentServices(services);
        RegisterEvaluationServices(services);
        RegisterQAServices(services);
        RegisterOtherServices(services);

        return services;
    }

    /// <summary>
    /// Initializes the LocalAI service asynchronously.
    /// Call this during application startup to ensure the model is loaded before first use.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The initialized LocalAICompletionService.</returns>
    public static async Task<LocalAICompletionService> InitializeLocalAIAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var initializer = serviceProvider.GetRequiredService<LocalAIInitializer>();
        return await initializer.GetServiceAsync(cancellationToken).ConfigureAwait(false);
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

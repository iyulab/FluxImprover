using FluxImprover.Services;
using LMSupply.Generator.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluxImprover.LMSupply;

/// <summary>
/// Extension methods for registering LMSupplyCompletionService with DI.
/// </summary>
public static class LMSupplyCompletionServiceExtensions
{
    /// <summary>
    /// Adds FluxImprover with a LMSupply local model completion service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="modelFactory">Factory function to create the IGeneratorModel from the service provider.</param>
    /// <param name="defaultTemperature">Default temperature when not specified in options. Defaults to 0.3.</param>
    /// <param name="defaultMaxTokens">Default max tokens when not specified in options. Defaults to 512.</param>
    /// <param name="lifetime">
    /// The service lifetime for all FluxImprover services. Defaults to <see cref="ServiceLifetime.Scoped"/>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFluxImproverWithLMSupply(
        this IServiceCollection services,
        Func<IServiceProvider, IGeneratorModel> modelFactory,
        float defaultTemperature = 0.3f,
        int defaultMaxTokens = 512,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        return services.AddFluxImprover(sp =>
        {
            var model = modelFactory(sp);
            var logger = sp.GetRequiredService<ILogger<LMSupplyCompletionService>>();
            return new LMSupplyCompletionService(model, logger, defaultTemperature, defaultMaxTokens);
        }, lifetime);
    }

    /// <summary>
    /// Adds FluxImprover with a LMSupply local model completion service using an already registered IGeneratorModel.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultTemperature">Default temperature when not specified in options. Defaults to 0.3.</param>
    /// <param name="defaultMaxTokens">Default max tokens when not specified in options. Defaults to 512.</param>
    /// <param name="lifetime">
    /// The service lifetime for all FluxImprover services. Defaults to <see cref="ServiceLifetime.Scoped"/>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFluxImproverWithLMSupply(
        this IServiceCollection services,
        float defaultTemperature = 0.3f,
        int defaultMaxTokens = 512,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        return services.AddFluxImproverWithLMSupply(
            sp => sp.GetRequiredService<IGeneratorModel>(),
            defaultTemperature,
            defaultMaxTokens,
            lifetime);
    }
}

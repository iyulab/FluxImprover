using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluxImprover.Services.Providers;

/// <summary>
/// Extension methods for registering OpenAICompatibleCompletionService with DI.
/// </summary>
public static class OpenAICompletionServiceExtensions
{
    /// <summary>
    /// Adds FluxImprover with an OpenAI-compatible completion service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="endpoint">Base API URL (e.g., "https://api.openai.com/v1").</param>
    /// <param name="apiKey">API key for authentication. Pass null for endpoints that don't require authentication.</param>
    /// <param name="model">Model name (e.g., "gpt-4o-mini").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFluxImproverWithOpenAI(
        this IServiceCollection services,
        string endpoint,
        string? apiKey,
        string model)
    {
        return services.AddFluxImprover(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<OpenAICompatibleCompletionService>>();
            return new OpenAICompatibleCompletionService(endpoint, apiKey, model, logger);
        });
    }
}

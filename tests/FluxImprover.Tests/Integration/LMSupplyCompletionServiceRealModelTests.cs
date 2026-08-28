namespace FluxImprover.Tests.Integration;

using AwesomeAssertions;
using FluxImprover.LMSupply;
using FluxImprover.Options;
using FluxImprover.Services;
// Global-qualified: FluxImprover.LMSupply is itself a real namespace here, and C#'s using-directive
// resolution checks enclosing-namespace-relative matches before the global one — an unqualified
// `using LMSupply.Generator;` resolves as `FluxImprover.LMSupply.Generator` (which doesn't exist)
// instead of the intended top-level `LMSupply.Generator`.
using global::LMSupply.Generator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Drives <see cref="LMSupplyCompletionService"/> against a real local model — the rest of this
/// project's "Integration" tests (<see cref="EndToEndPipelineTests"/> etc.) substitute
/// <see cref="ITextGenerationService"/>, so this package's own adapter had no test proving it
/// actually completes text through a loaded <c>IGeneratorModel</c>.
/// </summary>
/// <remarks>
/// Uses the "phi-4-mini" alias (ONNX Runtime GenAI, CPU-only in this environment) with a small
/// <see cref="CompletionOptions.MaxTokens"/> — a prior local-model integration test elsewhere in
/// this umbrella measured ~6.4s/token for this model family on CPU, so token count is kept low to
/// keep this test's wall-clock bounded rather than proving throughput.
/// </remarks>
[Trait("Category", "Integration")]
public sealed class LMSupplyCompletionServiceRealModelTests
{
    private const string ModelAlias = "phi-4-mini";

    [Fact]
    public async Task CompleteAsync_RealLocalModel_ProducesNonEmptyCompletion()
    {
        // LMSupplyCompletionService.DisposeAsync() disposes the model it was given (see its
        // source) — a separate `await using` on `model` here would double-dispose the underlying
        // ONNX Runtime GenAI native handle.
        var model = await LocalGenerator.LoadAsync(ModelAlias);
        await using var service = new LMSupplyCompletionService(model, NullLogger<LMSupplyCompletionService>.Instance);

        var result = await service.CompleteAsync(
            "Reply with exactly one word: the color of the sky on a clear day.",
            new CompletionOptions { MaxTokens = 16, Temperature = 0.1f });

        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AddFluxImproverWithLMSupply_RealLocalModel_ContainerDisposesCompletionServiceAutomatically()
    {
        // ISSUE-FluxImprover-20260824-030000, AC1+AC2, resolved in v0.11.0: AddFluxImprover now
        // registers the completion service the factory creates as its own ITextGenerationService
        // service, so the container tracks and disposes it (and, transitively, the
        // LMSupplyCompletionService's underlying native model handle) without any manual cleanup
        // by the caller — and GetRequiredService<ITextGenerationService>() resolves directly
        // instead of throwing. Provable only against a real model: a mock can't detect a leaked
        // ONNX Runtime GenAI native handle (OGA leak diagnostic on process exit), which is what
        // this test's absence of that diagnostic actually proves. No manual `model.DisposeAsync()`
        // anywhere below — that used to be required (see git history) and would now double-dispose.
        var model = await LocalGenerator.LoadAsync(ModelAlias);
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<LMSupplyCompletionService>>(NullLogger<LMSupplyCompletionService>.Instance);
        services.AddSingleton(model);
        services.AddFluxImproverWithLMSupply(defaultMaxTokens: 16, lifetime: ServiceLifetime.Singleton);

        await using (var provider = services.BuildServiceProvider())
        {
            // AC2 — resolvable directly, not only reachable through FluxImproverServices's
            // constructor-injected member services.
            var completionService = provider.GetRequiredService<ITextGenerationService>();
            completionService.Should().BeOfType<LMSupplyCompletionService>();

            var fluxImprover = provider.GetRequiredService<FluxImproverServices>();
            var summary = await fluxImprover.Summarization.SummarizeAsync(
                "Paris is the capital of France. It is well known for the Eiffel Tower and the Louvre museum.",
                new EnrichmentOptions { MaxTokens = 16, Temperature = 0.1f });

            summary.Should().NotBeNullOrWhiteSpace();
        }
        // AC1 — the `await using` block above disposed the provider, which must have disposed the
        // registered ITextGenerationService (LMSupplyCompletionService) and, through it, the model.
    }
}

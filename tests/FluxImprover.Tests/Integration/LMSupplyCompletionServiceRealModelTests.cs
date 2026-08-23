namespace FluxImprover.Tests.Integration;

using FluentAssertions;
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
    public async Task AddFluxImproverWithLMSupply_RealLocalModel_DrivesRealSummarization()
    {
        // AddFluxImprover(Func<IServiceProvider, ITextGenerationService>, ...) does not register
        // ITextGenerationService itself as a resolvable service — only the composed
        // FluxImproverServices facade (and its member services, which take it via constructor
        // injection internally). GetRequiredService<ITextGenerationService>() confirmed this by
        // throwing on an earlier version of this test. So the real wiring proof is driving one of
        // FluxImproverServices's member services end to end, not resolving the interface directly.
        //
        // Deliberate manual disposal below, NOT `await using`: FluxImproverServices is a plain
        // record with no IAsyncDisposable, and the LMSupplyCompletionService that
        // AddFluxImproverWithLMSupply's factory creates internally is never exposed to the
        // container as its own tracked service (only wrapped inside the FluxImproverServices
        // graph it returns) — so the container has no path to dispose it, and neither does a
        // caller resolving FluxImproverServices. Confirmed empirically: `await using var
        // provider` alone left an ONNX Runtime GenAI native handle leaked (OGA leak diagnostic on
        // process exit) until this test disposed `model` directly. Filed as a structural finding:
        // claudedocs/FluxImprover/issues/ISSUE-FluxImprover-20260824-030000-di-completion-service-never-disposed.md
        var model = await LocalGenerator.LoadAsync(ModelAlias);
        try
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILogger<LMSupplyCompletionService>>(NullLogger<LMSupplyCompletionService>.Instance);
            services.AddSingleton(model);
            services.AddFluxImproverWithLMSupply(defaultMaxTokens: 16, lifetime: ServiceLifetime.Singleton);

            await using var provider = services.BuildServiceProvider();
            var fluxImprover = provider.GetRequiredService<FluxImproverServices>();

            var summary = await fluxImprover.Summarization.SummarizeAsync(
                "Paris is the capital of France. It is well known for the Eiffel Tower and the Louvre museum.",
                new EnrichmentOptions { MaxTokens = 16, Temperature = 0.1f });

            summary.Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            await model.DisposeAsync();
        }
    }
}

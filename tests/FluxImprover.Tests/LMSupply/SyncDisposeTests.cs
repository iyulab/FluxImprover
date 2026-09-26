using FluxImprover.LMSupply;
using FluxImprover.Services;
using LMSupply.Generator.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace FluxImprover.Tests.LMSupply;

/// <summary>
/// <c>AddFluxImproverWithLMSupply</c> registers the completion service Scoped by default, and a scope disposed with
/// <c>Dispose()</c> throws on a service that is only <see cref="IAsyncDisposable"/>. The service implements both, so
/// <c>using var scope = provider.CreateScope()</c> — the usual shape outside ASP.NET — does not crash.
/// </summary>
public class SyncDisposeTests
{
    [Fact]
    public void Scope_DisposedSynchronously_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>));
        services.AddFluxImproverWithLMSupply(_ => Substitute.For<IGeneratorModel>(), disposeModel: true);
        using var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<ITextGenerationService>();

        var dispose = Record.Exception(scope.Dispose);

        Assert.Null(dispose);
    }
}

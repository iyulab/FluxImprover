using System.Reflection;
using Iyu.Conventions.Testing.Snippets;
using Xunit;

namespace FluxImprover.Tests;

/// <summary>
/// Compiles every C# block in README.md against the current assemblies — what a reader who copies a block runs into: the
/// receiver, the arguments, the members read and the namespaces. The first version of this test found the Quick Start
/// naming both the DI container and the built <c>FluxImproverServices</c> <c>services</c> in one block, and a factory
/// example with a <c>GetRequiredService&lt;...&gt;()</c> placeholder.
/// </summary>
/// <remarks>
/// Each block is compiled as a top-level program by <see cref="SnippetCompiler"/>: its own usings, the common usings below,
/// and the stand-ins below when the block uses the name without declaring it — values and types a reader already has from
/// the surrounding text (a service collection, the built services, their own completion service).
/// </remarks>
public class ReadmeSnippetCompileTests
{
    // A block that is deliberately not a program, by the heading it sits under, with the reason.
    // Shrink this, never grow it silently.
    private static readonly SnippetRoster Fragments = new(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ITextGenerationService Interface"] = "restates the shape of an existing interface",
        ["CompletionOptions"] = "restates the shape of an existing record",
    });

    private static readonly string[] CommonUsings =
    [
        "System", "System.Collections.Generic", "System.Linq", "System.Net.Http", "System.Threading", "System.Threading.Tasks",
        "FluxImprover", "FluxImprover.ChunkFiltering", "FluxImprover.Models", "FluxImprover.Options",
        "FluxImprover.QAGeneration", "FluxImprover.QueryPreprocessing", "FluxImprover.QuestionSuggestion",
        "FluxImprover.Services", "FluxImprover.Services.Providers", "FluxImprover.LMSupply",
        "LMSupply.Generator.Abstractions", "Microsoft.Extensions.DependencyInjection", "Microsoft.Extensions.Logging",
    ];

    private static readonly Dictionary<string, string> StandIns = new(StringComparer.Ordinal)
    {
        ["services"] = "IServiceCollection services = null!;",
        ["improver"] = "FluxImproverServices improver = null!;",
        ["loggerFactory"] = "ILoggerFactory loggerFactory = null!;",
        ["endpoint"] = "string endpoint = \"\";",
        ["apiKey"] = "string apiKey = \"\";",
        ["model"] = "string model = \"\";",

        // The reader's own completion service.
        ["MyCompletionService"] = """
            sealed class MyCompletionService : ITextGenerationService
            {
                public MyCompletionService() { }
                public MyCompletionService(string apiKey) { }
                public MyCompletionService(HttpClient http) { }
                public Task<string> CompleteAsync(string prompt, CompletionOptions? options = null, CancellationToken cancellationToken = default) => Task.FromResult("");
                public async IAsyncEnumerable<string> CompleteStreamingAsync(string prompt, CompletionOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default) { await Task.CompletedTask; yield break; }
            }
            """,
    };

    private static readonly SnippetCompiler Compiler = new(SnippetCompiler.ReferencesFor(
        [.. new[]
        {
            "FluxImprover", "FluxImprover.LMSupply", "LMSupply.Generator",
            "Microsoft.Extensions.DependencyInjection", "Microsoft.Extensions.DependencyInjection.Abstractions",
            "Microsoft.Extensions.Logging.Abstractions",
        }.Select(Assembly.Load)]))
    {
        Usings = CommonUsings,
        StandIns = StandIns,
    };

    private static readonly IReadOnlyList<DocSnippet> Snippets = DocSnippets.Read(ReadmePath(), "README.md");

    public static TheoryData<string> Blocks() => new(Snippets.Select(s => s.Key));

    [Theory]
    [MemberData(nameof(Blocks))]
    public void ReadmeBlock_Compiles(string key)
    {
        var snippet = Snippets.Single(s => s.Key == key);
        if (!Fragments.IsFragment(snippet))
            Compiler.ShouldCompile(snippet);
    }

    [Fact]
    public void EveryReadmeBlock_IsFoundAndFragmentsNameRealHeadings() => Fragments.ShouldMatch([.. Snippets], atLeast: 14);

    /// <summary>Positive control: the compiler rejects what the old Quick Start did — one name for two things.</summary>
    [Fact]
    public void Compile_RejectsTheOldQuickStart()
    {
        var errors = Compiler.Compile("""
            services.AddFluxImproverWithOpenAI(endpoint: "https://api.openai.com/v1", apiKey: "k", model: "m");
            var services = new FluxImproverBuilder().Build();
            """);

        Assert.NotEmpty(errors);
    }

    private static string ReadmePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FluxImprover.slnx")))
            dir = dir.Parent;
        return Path.Combine(
            dir?.FullName ?? throw new InvalidOperationException("FluxImprover.slnx not found above the test output directory"),
            "README.md");
    }
}

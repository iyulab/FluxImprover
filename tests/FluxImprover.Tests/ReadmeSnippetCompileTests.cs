using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace FluxImprover.Tests;

/// <summary>
/// Compiles every <c>```csharp</c> block in README.md against the current assemblies — what a reader who copies a block
/// runs into: the receiver, the arguments, the members read and the namespaces. The first version of this test found the
/// Quick Start naming both the DI container and the built <c>FluxImproverServices</c> <c>services</c> in one block, and a
/// factory example with a <c>GetRequiredService&lt;...&gt;()</c> placeholder.
/// </summary>
/// <remarks>
/// A block is compiled as a top-level program: its <c>using</c> lines are hoisted, the common usings below are added, and
/// the stand-ins below are declared when the block uses the name without declaring it — values and types a reader already
/// has from the surrounding text (a service collection, the built services, their own completion service).
/// </remarks>
public class ReadmeSnippetCompileTests
{
    // A block that is deliberately not a program is listed here by the heading it sits under, with the reason.
    // Shrink this, never grow it silently.
    private static readonly Dictionary<string, string> Fragments = new(StringComparer.Ordinal)
    {
        ["ITextGenerationService Interface"] = "restates the shape of an existing interface",
        ["CompletionOptions"] = "restates the shape of an existing record",
    };

    private const string CommonUsings = """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Net.Http;
        using System.Threading;
        using System.Threading.Tasks;
        using FluxImprover;
        using FluxImprover.ChunkFiltering;
        using FluxImprover.Models;
        using FluxImprover.Options;
        using FluxImprover.QAGeneration;
        using FluxImprover.QueryPreprocessing;
        using FluxImprover.QuestionSuggestion;
        using FluxImprover.Services;
        using FluxImprover.Services.Providers;
        using FluxImprover.LMSupply;
        using LMSupply.Generator.Abstractions;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Logging;
        """;

    private static readonly (string Name, string Declaration)[] StandIns =
    [
        ("services", "IServiceCollection services = null!;"),
        ("improver", "FluxImproverServices improver = null!;"),
        ("loggerFactory", "ILoggerFactory loggerFactory = null!;"),
        ("endpoint", "string endpoint = \"\";"),
        ("apiKey", "string apiKey = \"\";"),
        ("model", "string model = \"\";"),
    ];

    private static readonly (string Name, string Declaration)[] StandInTypes =
    [
        ("MyCompletionService", """
            sealed class MyCompletionService : ITextGenerationService
            {
                public MyCompletionService() { }
                public MyCompletionService(string apiKey) { }
                public MyCompletionService(HttpClient http) { }
                public Task<string> CompleteAsync(string prompt, CompletionOptions? options = null, CancellationToken cancellationToken = default) => Task.FromResult("");
                public async IAsyncEnumerable<string> CompleteStreamingAsync(string prompt, CompletionOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default) { await Task.CompletedTask; yield break; }
            }
            """),
    ];

    private static readonly string[] AssembliesToLoad =
    [
        "FluxImprover", "FluxImprover.LMSupply", "LMSupply.Generator",
        "Microsoft.Extensions.DependencyInjection", "Microsoft.Extensions.DependencyInjection.Abstractions",
        "Microsoft.Extensions.Logging.Abstractions",
    ];

    public static TheoryData<string> Blocks()
    {
        var data = new TheoryData<string>();
        foreach (var block in ReadBlocks())
            data.Add(block.Key);
        return data;
    }

    [Theory]
    [MemberData(nameof(Blocks))]
    public void ReadmeBlock_Compiles(string key)
    {
        var block = ReadBlocks().Single(b => b.Key == key);
        if (Fragments.ContainsKey(block.Heading))
            return;

        var errors = Compile(block.Code);

        Assert.True(errors.IsEmpty,
            $"README block {key} does not compile against the current API:\n" +
            string.Join("\n", errors.Select(e => e.ToString())) + "\n--- source ---\n" + Program(block.Code));
    }

    [Fact]
    public void EveryReadmeBlock_IsFoundAndFragmentsNameRealHeadings()
    {
        var blocks = ReadBlocks();
        Assert.True(blocks.Count >= 14, $"expected the README's C# blocks, found {blocks.Count}");
        Assert.All(Fragments.Keys, heading => Assert.Contains(blocks, b => b.Heading == heading));
    }

    /// <summary>Positive control: the compiler rejects what the old Quick Start did — one name for two things.</summary>
    [Fact]
    public void Compile_RejectsTheOldQuickStart()
    {
        var errors = Compile("""
            services.AddFluxImproverWithOpenAI(endpoint: "https://api.openai.com/v1", apiKey: "k", model: "m");
            var services = new FluxImproverBuilder().Build();
            """);

        Assert.NotEmpty(errors);
    }

    private sealed record Block(string Key, string Heading, string Code);

    private static List<Block> ReadBlocks()
    {
        var lines = File.ReadAllText(ReadmePath()).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var blocks = new List<Block>();
        var heading = "(top)";
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith('#'))
                heading = lines[i].TrimStart('#').Trim();
            if (lines[i].Trim() != "```csharp")
                continue;

            var start = i + 1;
            var code = new StringBuilder();
            for (i++; i < lines.Length && lines[i].Trim() != "```"; i++)
                code.AppendLine(lines[i]);
            blocks.Add(new Block($"line {start}: {heading}", heading, code.ToString()));
        }

        return blocks;
    }

    private static string Program(string code)
    {
        var lines = code.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        // Blocks inside a list item are indented; a using directive is recognized on its trimmed text.
        bool IsUsingDirective(string l)
        {
            var t = l.Trim();
            return t.StartsWith("using ", StringComparison.Ordinal) && t.EndsWith(';') && !t.StartsWith("using var ", StringComparison.Ordinal);
        }

        var body = string.Join("\n", lines.Where(l => !IsUsingDirective(l)));
        var standIns = StandIns
            .Where(s => Regex.IsMatch(body, $@"\b{s.Name}\b")
                        && !Regex.IsMatch(body, $@"\b(var|[A-Z][\w<>?,\s]*)\s+{s.Name}\s*[=;]"))
            .Select(s => s.Declaration);
        var standInTypes = StandInTypes
            .Where(t => Regex.IsMatch(body, $@"\b{t.Name}\b") && !Regex.IsMatch(body, $@"\b(class|record|struct)\s+{t.Name}\b"))
            .Select(t => t.Declaration);

        return string.Join("\n", lines.Where(IsUsingDirective)) + "\n" + CommonUsings + "\n"
               + string.Join("\n", standIns) + "\n" + body + "\n" + string.Join("\n", standInTypes);
    }

    private static ImmutableArray<Diagnostic> Compile(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(Program(code), new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "ReadmeSnippet", [tree], References(),
            new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));
        return compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray();
    }

    private static List<MetadataReference> References()
    {
        foreach (var name in AssembliesToLoad)
            Assembly.Load(name);

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trusted)
            paths.UnionWith(trusted.Split(Path.PathSeparator).Where(p => p.Length > 0));
        paths.UnionWith(AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.Location.Length > 0)
            .Select(a => a.Location));
        return paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();
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

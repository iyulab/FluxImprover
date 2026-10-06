using Flux.Abstractions;
namespace FluxImprover.Tests.ContextualRetrieval;

using AwesomeAssertions;
using FluxImprover.ContextualRetrieval;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.Services;
using NSubstitute;
using Xunit;

/// <summary>
/// Context generation sent the whole document with every chunk, so a long document overflowed a small serving context
/// on every chunk and the cost grew as chunks times document length. A document longer than the budget now goes as a
/// profile (opening + heading outline) and the text around the chunk.
/// </summary>
public sealed class BoundedDocumentContextTests
{
    private static string LongDocument()
    {
        var sections = Enumerable.Range(1, 60).Select(i =>
            $"## Section {i}\n\n" + string.Join(" ", Enumerable.Repeat($"Paragraph text for section {i}.", 40)));
        return "# Annual Report\n\nThis report covers the fiscal year.\n\n" + string.Join("\n\n", sections);
    }

    private static (ContextualEnrichmentService Service, List<string> Prompts) Service()
    {
        var prompts = new List<string>();
        var generation = Substitute.For<ITextGenerationService>();
        generation.CompleteAsync(Arg.Do<string>(prompts.Add), Arg.Any<CompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns("context");
        return (new ContextualEnrichmentService(generation), prompts);
    }

    [Fact]
    public async Task LongDocument_PromptStaysWithinTheBudget_AndCarriesTheProfileAndTheChunksNeighbourhood()
    {
        var document = LongDocument();
        var chunkText = string.Join(" ", Enumerable.Repeat("Paragraph text for section 47.", 40));
        var (service, prompts) = Service();

        await service.EnrichAsync(new Chunk { Id = "c47", Content = chunkText }, document,
            new ContextualEnrichmentOptions { MaxDocumentContextLength = 4000 }, TestContext.Current.CancellationToken);

        var prompt = prompts.Single();
        document.Length.Should().BeGreaterThan(50_000);
        (prompt.Length - chunkText.Length).Should().BeLessThan(4000 + 1500, "document context is bounded; the rest is instructions");
        prompt.Should().Contain("# Annual Report", "the opening is in the profile");
        prompt.Should().Contain("## Section 60", "the outline names every section");
        prompt.Should().Contain("## Section 47", "the window is around the chunk");
        prompt.Should().NotContain("Paragraph text for section 3.", "text far from the chunk is not sent");
    }

    [Fact]
    public async Task ShortDocument_IsSentWhole()
    {
        var (service, prompts) = Service();

        await service.EnrichAsync(new Chunk { Id = "c", Content = "chunk" }, "A short document with the chunk.",
            cancellationToken: TestContext.Current.CancellationToken);

        prompts.Single().Should().Contain("## Full Document\nA short document with the chunk.");
    }

    [Fact]
    public async Task BudgetZero_SendsTheWholeDocument()
    {
        var document = LongDocument();
        var (service, prompts) = Service();

        await service.EnrichAsync(new Chunk { Id = "c", Content = "Paragraph text for section 2." }, document,
            new ContextualEnrichmentOptions { MaxDocumentContextLength = 0 }, TestContext.Current.CancellationToken);

        prompts.Single().Should().Contain(document);
    }
}

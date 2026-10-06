using System.Text;

namespace FluxImprover.ContextualRetrieval;

/// <summary>
/// The document text sent with a chunk for context generation, bounded by a character budget. A document within the
/// budget is sent whole. A longer one is reduced to what situates a chunk: a profile (the document's opening and its
/// heading outline — identical for every chunk of the document) and the text around the chunk.
/// </summary>
internal static class DocumentContext
{
    /// <summary>Share of the budget the profile may use; the rest is the window around the chunk.</summary>
    private const double ProfileShare = 0.35;

    /// <summary>Share of the profile budget the heading outline may use; the rest is the document's opening.</summary>
    private const double OutlineShare = 0.7;

    /// <summary>
    /// Builds the context block for <paramref name="chunk"/> from <paramref name="document"/>.
    /// </summary>
    /// <param name="document">The full document text.</param>
    /// <param name="chunk">The chunk's text, used to find where it sits.</param>
    /// <param name="position">0-based chunk position, used when the chunk text cannot be found verbatim.</param>
    /// <param name="total">Chunk count, with <paramref name="position"/>.</param>
    /// <param name="budget">Maximum characters; 0 means no limit.</param>
    /// <returns>The heading the prompt shows and the text under it.</returns>
    internal static (string Heading, string Text) For(string document, string chunk, int? position, int? total, int budget) =>
        Prepare(document, budget).For(chunk, position, total);

    /// <summary>
    /// The part of the context that is the same for every chunk of <paramref name="document"/> — whether it fits whole, and
    /// otherwise its profile — computed once for a document and reused for each of its chunks.
    /// </summary>
    internal static Prepared Prepare(string document, int budget) =>
        budget <= 0 || document.Length <= budget
            ? new Prepared(document, budget, Profile: null)
            : new Prepared(document, budget, Profile(document, (int)(budget * ProfileShare)));

    /// <summary>A document's context with its profile computed (see <see cref="Prepare"/>).</summary>
    internal sealed record Prepared(string Document, int Budget, string? Profile)
    {
        /// <summary>The context block for <paramref name="chunk"/>: the whole document, or the profile and the text around the chunk.</summary>
        public (string Heading, string Text) For(string chunk, int? position, int? total)
        {
            if (Profile is null)
                return ("## Full Document", Document);

            var window = Window(Document, chunk, position, total, Math.Max(0, Budget - Profile.Length));
            var sb = new StringBuilder();
            sb.Append("### Document profile (opening and outline)\n").Append(Profile);
            sb.Append("\n\n### Text around the chunk\n").Append(window);
            return ("## Document Context (excerpt — the document is longer)", sb.ToString());
        }
    }

    /// <summary>
    /// The document's opening and its Markdown heading outline, within <paramref name="budget"/> characters. The outline
    /// takes most of the budget; when it does not fit, the deepest heading level is dropped first.
    /// </summary>
    private static string Profile(string document, int budget)
    {
        var headings = document.Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.StartsWith('#'))
            .ToList();

        var outlineBudget = (int)(budget * OutlineShare);
        var outline = string.Join("\n", headings);
        for (var maxLevel = 5; outline.Length > outlineBudget && maxLevel >= 1; maxLevel--)
        {
            var level = maxLevel;
            headings = headings.Where(h => h.TakeWhile(c => c == '#').Count() <= level).ToList();
            outline = string.Join("\n", headings);
        }

        outline = Truncate(outline, outlineBudget);
        var opening = Truncate(document, budget - outline.Length);
        return outline.Length == 0 ? opening : opening + "\n\nOutline:\n" + outline;
    }

    /// <summary>The text centred on the chunk, within <paramref name="budget"/> characters.</summary>
    private static string Window(string document, string chunk, int? position, int? total, int budget)
    {
        if (budget <= 0)
            return string.Empty;

        var at = chunk.Length > 0 ? document.IndexOf(chunk, StringComparison.Ordinal) : -1;
        int center;
        if (at >= 0)
            center = at + (chunk.Length / 2);
        else if (position is { } p && total is > 0 and var t)
            center = (int)((p + 0.5) / t * document.Length);
        else
            center = document.Length / 2;

        var start = Math.Clamp(center - (budget / 2), 0, Math.Max(0, document.Length - budget));
        var length = Math.Min(budget, document.Length - start);
        var text = document.Substring(start, length);
        return (start > 0 ? "…" : string.Empty) + text + (start + length < document.Length ? "…" : string.Empty);
    }

    private static string Truncate(string text, int max) =>
        max <= 0 ? string.Empty : text.Length <= max ? text : text[..max];
}

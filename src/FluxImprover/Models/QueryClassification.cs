namespace FluxImprover.Models;

/// <summary>
/// Represents the classified intent of a user query.
/// </summary>
public enum QueryClassification
{
    /// <summary>
    /// General information retrieval query.
    /// </summary>
    General,

    /// <summary>
    /// Question requiring a direct answer.
    /// </summary>
    Question,

    /// <summary>
    /// Search query looking for specific information.
    /// </summary>
    Search,

    /// <summary>
    /// Request for a definition or explanation.
    /// </summary>
    Definition,

    /// <summary>
    /// Comparison between multiple items or concepts.
    /// </summary>
    Comparison,

    /// <summary>
    /// How-to or procedural query.
    /// </summary>
    HowTo,

    /// <summary>
    /// Troubleshooting or problem-solving query.
    /// </summary>
    Troubleshooting,

    /// <summary>
    /// Code-related query (implementation, syntax, etc.).
    /// </summary>
    Code,

    /// <summary>
    /// Conceptual or theoretical query.
    /// </summary>
    Conceptual
}

using System.Text.Json;
using System.Text.RegularExpressions;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.Services;

namespace FluxImprover.ChunkFiltering;

/// <summary>
/// LLM-based chunk filtering implementation with 3-stage assessment.
/// Stage 1: Initial assessment based on content analysis and LLM evaluation.
/// Stage 2: Self-reflection to correct biases and improve accuracy.
/// Stage 3: Critic validation for final verification.
/// </summary>
public sealed class ChunkFilteringService : IChunkFilteringService
{
    private readonly ITextCompletionService _completionService;

    /// <summary>
    /// Initializes a new instance of ChunkFilteringService.
    /// </summary>
    public ChunkFilteringService(ITextCompletionService completionService)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FilteredChunk>> FilterAsync(
        IEnumerable<Chunk> chunks,
        string? query,
        ChunkFilteringOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ChunkFilteringOptions();
        var chunkList = chunks.ToList();
        var results = new List<FilteredChunk>();

        // Process in batches for efficiency
        for (var i = 0; i < chunkList.Count; i += options.BatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = chunkList.Skip(i).Take(options.BatchSize);
            var tasks = batch.Select(chunk => AssessAndFilterAsync(chunk, query, options, cancellationToken));
            var batchResults = await Task.WhenAll(tasks).ConfigureAwait(false);
            results.AddRange(batchResults);
        }

        // Apply filtering and sorting
        var filtered = results.Where(fc => fc.Passed);

        if (options.MaxChunks.HasValue)
        {
            filtered = filtered
                .OrderByDescending(fc => fc.CombinedScore)
                .Take(options.MaxChunks.Value);
        }

        if (options.PreserveOrder)
        {
            // Preserve original order by chunk index if available
            filtered = filtered.OrderBy(fc => GetChunkIndex(fc.Chunk));
        }

        return filtered.ToList();
    }

    /// <inheritdoc />
    public async Task<ChunkAssessment> AssessAsync(
        Chunk chunk,
        string? query,
        ChunkFilteringOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ChunkFilteringOptions();
        return await PerformThreeStageAssessmentAsync(chunk, query, options, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<FilteredChunk> AssessAndFilterAsync(
        Chunk chunk,
        string? query,
        ChunkFilteringOptions options,
        CancellationToken cancellationToken)
    {
        var assessment = await PerformThreeStageAssessmentAsync(chunk, query, options, cancellationToken)
            .ConfigureAwait(false);

        var qualityScore = CalculateQualityScore(assessment);
        var relevanceScore = assessment.FinalScore;
        var combinedScore = CalculateCombinedScore(relevanceScore, qualityScore, options.QualityWeight);
        var passed = combinedScore >= options.MinRelevanceScore;

        return new FilteredChunk
        {
            Chunk = chunk,
            RelevanceScore = relevanceScore,
            QualityScore = qualityScore,
            CombinedScore = combinedScore,
            Passed = passed,
            Assessment = assessment,
            Reason = GenerateReason(assessment, passed, options)
        };
    }

    private async Task<ChunkAssessment> PerformThreeStageAssessmentAsync(
        Chunk chunk,
        string? query,
        ChunkFilteringOptions options,
        CancellationToken cancellationToken)
    {
        var factors = new List<AssessmentFactor>();
        var reasoning = new Dictionary<string, string>();

        // Stage 1: Initial Assessment
        var (initialScore, initialReasoning, initialFactors) =
            await PerformInitialAssessmentAsync(chunk, query, options, cancellationToken).ConfigureAwait(false);

        factors.AddRange(initialFactors);
        reasoning["initial"] = initialReasoning;

        double? reflectionScore = null;
        double? criticScore = null;

        // Stage 2: Self-Reflection
        if (options.UseSelfReflection)
        {
            var (refScore, refReasoning, refFactors) =
                PerformSelfReflection(chunk, query, initialScore, initialFactors);

            reflectionScore = refScore;
            reasoning["reflection"] = refReasoning;
            MergeFactors(factors, refFactors);
        }

        // Stage 3: Critic Validation
        if (options.UseCriticValidation)
        {
            var previousScore = reflectionScore ?? initialScore;
            var (critScore, critReasoning, critFactors) =
                PerformCriticValidation(chunk, query, previousScore, factors);

            criticScore = critScore;
            reasoning["critic"] = critReasoning;
            MergeFactors(factors, critFactors);
        }

        var finalScore = CalculateFinalScore(initialScore, reflectionScore, criticScore);
        var confidence = CalculateConfidence(initialScore, reflectionScore, criticScore, factors);
        var suggestions = GenerateSuggestions(finalScore, factors);

        return new ChunkAssessment
        {
            InitialScore = initialScore,
            ReflectionScore = reflectionScore,
            CriticScore = criticScore,
            FinalScore = finalScore,
            Confidence = confidence,
            Factors = factors,
            Suggestions = suggestions,
            Reasoning = reasoning
        };
    }

    private async Task<(double Score, string Reasoning, List<AssessmentFactor> Factors)>
        PerformInitialAssessmentAsync(
            Chunk chunk,
            string? query,
            ChunkFilteringOptions options,
            CancellationToken cancellationToken)
    {
        var factors = new List<AssessmentFactor>();
        var scores = new List<double>();

        // Content relevance analysis
        var contentScore = EvaluateContentRelevance(chunk.Content, query);
        factors.Add(new AssessmentFactor
        {
            Name = "Content Relevance",
            Contribution = contentScore,
            Explanation = $"Content alignment with query: {contentScore:F2}"
        });
        scores.Add(contentScore);

        // Information density
        var densityScore = EvaluateInformationDensity(chunk.Content);
        factors.Add(new AssessmentFactor
        {
            Name = "Information Density",
            Contribution = densityScore * 0.5,
            Explanation = $"Information richness: {densityScore:F2}"
        });
        scores.Add(densityScore);

        // Structural importance
        var structuralScore = EvaluateStructuralImportance(chunk);
        factors.Add(new AssessmentFactor
        {
            Name = "Structural Importance",
            Contribution = structuralScore * 0.3,
            Explanation = $"Document structure relevance: {structuralScore:F2}"
        });
        scores.Add(structuralScore);

        // Custom criteria evaluation
        foreach (var criterion in options.Criteria)
        {
            var criterionScore = EvaluateCriterion(chunk, query, criterion);
            factors.Add(new AssessmentFactor
            {
                Name = criterion.Type.ToString(),
                Contribution = criterionScore * criterion.Weight,
                Explanation = $"Criterion {criterion.Type}: {criterionScore:F2}"
            });
            scores.Add(criterionScore * criterion.Weight);
        }

        // LLM assessment for query relevance
        if (!string.IsNullOrEmpty(query))
        {
            var llmScore = await GetLLMAssessmentAsync(chunk, query, cancellationToken).ConfigureAwait(false);
            factors.Add(new AssessmentFactor
            {
                Name = "LLM Assessment",
                Contribution = llmScore * 0.8,
                Explanation = $"LLM relevance assessment: {llmScore:F2}"
            });
            scores.Add(llmScore);
        }

        var finalScore = scores.Count > 0 ? scores.Average() : 0.5;
        var topFactor = factors.OrderByDescending(f => Math.Abs(f.Contribution)).First();
        var reasoning = $"Initial assessment based on {factors.Count} factors. Primary factor: {topFactor.Name}";

        return (finalScore, reasoning, factors);
    }

    private (double Score, string Reasoning, List<AssessmentFactor> Factors)
        PerformSelfReflection(
            Chunk chunk,
            string? query,
            double initialScore,
            List<AssessmentFactor> initialFactors)
    {
        var factors = new List<AssessmentFactor>();

        // Check for assessment bias
        var biasCheck = CheckForBias(initialFactors);
        if (Math.Abs(biasCheck) > 0.1)
        {
            factors.Add(new AssessmentFactor
            {
                Name = "Bias Correction",
                Contribution = -biasCheck,
                Explanation = $"Correcting for assessment bias: {biasCheck:F2}"
            });
        }

        // Evaluate completeness
        var completenessScore = EvaluateCompleteness(chunk.Content);
        if (completenessScore < 0.7)
        {
            factors.Add(new AssessmentFactor
            {
                Name = "Completeness Adjustment",
                Contribution = (completenessScore - 0.7) * 0.5,
                Explanation = $"Adjusting for incomplete coverage: {completenessScore:F2}"
            });
        }

        // Alternative perspective check
        var alternativeScore = EvaluateAlternativePerspective(chunk.Content, query, initialScore);
        if (Math.Abs(alternativeScore - initialScore) > 0.2)
        {
            factors.Add(new AssessmentFactor
            {
                Name = "Alternative Perspective",
                Contribution = (alternativeScore - initialScore) * 0.3,
                Explanation = $"Alternative view suggests: {alternativeScore:F2}"
            });
        }

        var adjustment = factors.Sum(f => f.Contribution);
        var reflectedScore = Clamp(initialScore + adjustment);
        var reasoning = $"Self-reflection identified {factors.Count} adjustments. Score adjusted from {initialScore:F2} to {reflectedScore:F2}";

        return (reflectedScore, reasoning, factors);
    }

    private (double Score, string Reasoning, List<AssessmentFactor> Factors)
        PerformCriticValidation(
            Chunk chunk,
            string? query,
            double previousScore,
            List<AssessmentFactor> existingFactors)
    {
        var factors = new List<AssessmentFactor>();

        // Check consistency across evaluations
        var consistencyScore = EvaluateConsistency(existingFactors);
        if (consistencyScore < 0.8)
        {
            factors.Add(new AssessmentFactor
            {
                Name = "Consistency Issue",
                Contribution = (consistencyScore - 1) * 0.3,
                Explanation = $"Inconsistency detected: {consistencyScore:F2}"
            });
        }

        // Pattern validation
        var validationScore = ValidateAgainstPatterns(chunk.Content);
        factors.Add(new AssessmentFactor
        {
            Name = "Pattern Validation",
            Contribution = (validationScore - 0.5) * 0.5,
            Explanation = $"Pattern matching validation: {validationScore:F2}"
        });

        // Edge case detection
        var edgeCaseScore = CheckEdgeCases(chunk.Content);
        if (edgeCaseScore != 0)
        {
            factors.Add(new AssessmentFactor
            {
                Name = "Edge Case Detection",
                Contribution = edgeCaseScore,
                Explanation = $"Edge case adjustment: {edgeCaseScore:F2}"
            });
        }

        var criticAdjustment = factors.Sum(f => f.Contribution);
        var criticScore = Clamp(previousScore + criticAdjustment);
        var reasoning = $"Critic validation performed {factors.Count} checks. Final validation score: {criticScore:F2}";

        return (criticScore, reasoning, factors);
    }

    private async Task<double> GetLLMAssessmentAsync(
        Chunk chunk,
        string query,
        CancellationToken cancellationToken)
    {
        var contentPreview = chunk.Content.Length > 500
            ? chunk.Content[..500] + "..."
            : chunk.Content;

        var prompt = $"""
            Rate the relevance of this text chunk to the query.
            Query: {query}
            Chunk: {contentPreview}

            Provide a relevance score from 0.0 to 1.0 where:
            - 0.0 = completely irrelevant
            - 0.5 = somewhat relevant
            - 1.0 = highly relevant

            Output only the numeric score.
            """;

        try
        {
            var response = await _completionService.CompleteAsync(prompt, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (double.TryParse(response.Trim(), out var score))
            {
                return Clamp(score);
            }
        }
        catch
        {
            // Fallback on LLM failure
        }

        return EvaluateContentRelevance(chunk.Content, query);
    }

    #region Evaluation Helpers

    private static double EvaluateContentRelevance(string content, string? query)
    {
        if (string.IsNullOrEmpty(query))
            return 0.5;

        var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var contentLower = content.ToLowerInvariant();

        var matchCount = queryWords.Count(word => contentLower.Contains(word));
        return (double)matchCount / queryWords.Length;
    }

    private static double EvaluateInformationDensity(string content)
    {
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return 0;

        var uniqueWords = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var density = (double)uniqueWords / words.Length;

        if (words.Any(w => w.Any(char.IsDigit))) density += 0.1;
        if (words.Any(w => w.Contains('_') || w.Contains('-') || w.Contains('.'))) density += 0.1;

        return Math.Min(1, density);
    }

    private static double EvaluateStructuralImportance(Chunk chunk)
    {
        var score = 0.5;
        var content = chunk.Content;

        if (content.StartsWith('#') || content.Contains("HEADING", StringComparison.OrdinalIgnoreCase))
            score += 0.2;

        if (content.Contains("```") || content.Contains("CODE", StringComparison.OrdinalIgnoreCase))
            score += 0.15;

        if (content.Contains("TABLE", StringComparison.OrdinalIgnoreCase) || content.Contains('|'))
            score += 0.15;

        // Check metadata for position
        if (chunk.Metadata?.TryGetValue("index", out var indexObj) == true)
        {
            if (indexObj is int index && index < 3)
                score += 0.1;
        }

        return Math.Min(1, score);
    }

    private static double EvaluateCriterion(Chunk chunk, string? query, FilterCriterion criterion)
    {
        return criterion.Type switch
        {
            CriterionType.KeywordPresence => EvaluateKeywordPresence(chunk.Content, criterion.Value),
            CriterionType.TopicRelevance => EvaluateContentRelevance(chunk.Content, query) * 1.2,
            CriterionType.InformationDensity => EvaluateInformationDensity(chunk.Content),
            CriterionType.FactualContent => EvaluateFactualContent(chunk.Content),
            CriterionType.Recency => EvaluateRecency(chunk),
            CriterionType.SourceCredibility => EvaluateSourceCredibility(chunk),
            CriterionType.Completeness => EvaluateCompleteness(chunk.Content),
            _ => 0.5
        };
    }

    private static double EvaluateKeywordPresence(string content, object? value)
    {
        var contentLower = content.ToLowerInvariant();

        if (value is string keyword)
            return contentLower.Contains(keyword.ToLowerInvariant()) ? 1.0 : 0.0;

        if (value is IEnumerable<string> keywords)
        {
            var keywordList = keywords.ToList();
            if (keywordList.Count == 0) return 0.5;

            var matches = keywordList.Count(k => contentLower.Contains(k.ToLowerInvariant()));
            return (double)matches / keywordList.Count;
        }

        return 0.5;
    }

    private static double EvaluateFactualContent(string content)
    {
        var score = 0.5;

        if (Regex.IsMatch(content, @"\d+"))
            score += 0.2;

        if (content.Contains('[') && content.Contains(']'))
            score += 0.15;

        var words = content.Split(' ');
        var capitalizedCount = words.Count(w => w.Length > 2 && char.IsUpper(w[0]));
        if (capitalizedCount > 2)
            score += 0.15;

        return Math.Min(1, score);
    }

    private static double EvaluateRecency(Chunk chunk)
    {
        if (chunk.Metadata?.TryGetValue("processed_at", out var dateObj) == true)
        {
            if (dateObj is DateTime processedAt)
            {
                var age = DateTime.UtcNow - processedAt;
                if (age.TotalDays < 7) return 1.0;
                if (age.TotalDays < 30) return 0.8;
                if (age.TotalDays < 90) return 0.6;
                return 0.4;
            }
        }
        return 0.5;
    }

    private static double EvaluateSourceCredibility(Chunk chunk)
    {
        if (chunk.Metadata?.TryGetValue("file_type", out var typeObj) == true)
        {
            var fileType = typeObj?.ToString()?.ToUpperInvariant();
            return fileType switch
            {
                "PDF" => 0.8,
                "DOCX" => 0.7,
                "WEB" => 0.5,
                "TXT" or "TEXT" => 0.4,
                _ => 0.5
            };
        }
        return 0.5;
    }

    private static double EvaluateCompleteness(string content)
    {
        var trimmed = content.Trim();
        if (string.IsNullOrEmpty(trimmed)) return 0;

        var hasStart = char.IsUpper(trimmed[0]);
        var hasEnd = trimmed[^1] is '.' or '!' or '?';

        return (hasStart ? 0.5 : 0) + (hasEnd ? 0.5 : 0);
    }

    private static double CheckForBias(List<AssessmentFactor> factors)
    {
        if (factors.Count == 0) return 0;

        var maxContribution = factors.Max(f => Math.Abs(f.Contribution));
        var totalContribution = factors.Sum(f => Math.Abs(f.Contribution));

        if (totalContribution == 0) return 0;

        var concentration = maxContribution / totalContribution;
        return concentration > 0.7 ? (concentration - 0.7) * 0.5 : 0;
    }

    private static double EvaluateAlternativePerspective(string content, string? query, double initialScore)
    {
        if (string.IsNullOrEmpty(query))
            return 0.5;

        var directRelevance = EvaluateContentRelevance(content, query);

        if (directRelevance > 0.8) return directRelevance;
        if (directRelevance < 0.2) return 0.3;

        return directRelevance;
    }

    private static double EvaluateConsistency(List<AssessmentFactor> factors)
    {
        var contributions = factors.Select(f => f.Contribution).ToList();
        if (contributions.Count < 2) return 1.0;

        var mean = contributions.Average();
        var variance = contributions.Select(c => Math.Pow(c - mean, 2)).Average();

        return Math.Max(0, 1 - variance * 2);
    }

    private static double ValidateAgainstPatterns(string content)
    {
        var score = 0.5;

        if (content.Length > 100 && content.Length < 2000)
            score += 0.1;

        if (content.Contains(". ") || content.Contains(".\n"))
            score += 0.1;

        if (content.Length < 50)
            score -= 0.2;

        var newlineRatio = (double)content.Count(c => c == '\n') / content.Length;
        if (newlineRatio > 0.05)
            score -= 0.1;

        return Clamp(score);
    }

    private static double CheckEdgeCases(string content)
    {
        var adjustment = 0.0;

        if (content.Length < 50)
            adjustment -= 0.3;

        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 0)
        {
            var numberWords = words.Count(w => w.All(c => char.IsDigit(c) || c == '.' || c == ','));
            if ((double)numberWords / words.Length > 0.8)
                adjustment -= 0.2;

            if (words.Length > 10)
            {
                var uniqueWords = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                if ((double)uniqueWords / words.Length < 0.3)
                    adjustment -= 0.2;
            }
        }

        return adjustment;
    }

    #endregion

    #region Score Calculation

    private static double CalculateFinalScore(double initial, double? reflection, double? critic)
    {
        var scores = new List<(double Score, double Weight)> { (initial, 0.4) };

        if (reflection.HasValue)
            scores.Add((reflection.Value, 0.3));

        if (critic.HasValue)
            scores.Add((critic.Value, 0.3));

        var totalWeight = scores.Sum(s => s.Weight);
        return scores.Sum(s => s.Score * s.Weight / totalWeight);
    }

    private static double CalculateConfidence(
        double initial,
        double? reflection,
        double? critic,
        List<AssessmentFactor> factors)
    {
        // Consistency-based confidence
        var scores = new List<double> { initial };
        if (reflection.HasValue) scores.Add(reflection.Value);
        if (critic.HasValue) scores.Add(critic.Value);

        var mean = scores.Average();
        var variance = scores.Select(s => Math.Pow(s - mean, 2)).Average();
        var consistency = Math.Max(0, 1 - variance * 2);

        // Factor diversity
        var factorDiversity = Math.Min(1, factors.Count / 10.0);

        // Score extremity (extreme scores = higher confidence)
        var extremity = Math.Abs(mean - 0.5) * 2;

        return consistency * 0.5 + factorDiversity * 0.3 + extremity * 0.2;
    }

    private static double CalculateQualityScore(ChunkAssessment assessment)
    {
        var quality = 0.5;

        var densityFactor = assessment.Factors.FirstOrDefault(f => f.Name == "Information Density");
        if (densityFactor != null)
            quality = Math.Max(quality, densityFactor.Contribution + 0.5);

        var completeness = assessment.Factors.FirstOrDefault(f => f.Name == "Completeness Adjustment");
        if (completeness != null)
            quality += completeness.Contribution * 0.5;

        return Clamp(quality);
    }

    private static double CalculateCombinedScore(double relevance, double quality, double qualityWeight)
    {
        return relevance * (1 - qualityWeight) + quality * qualityWeight;
    }

    #endregion

    #region Helper Methods

    private static void MergeFactors(List<AssessmentFactor> target, List<AssessmentFactor> source)
    {
        foreach (var factor in source)
        {
            var existing = target.FirstOrDefault(f => f.Name == factor.Name);
            if (existing != null)
            {
                var index = target.IndexOf(existing);
                target[index] = existing with
                {
                    Contribution = (existing.Contribution + factor.Contribution) / 2,
                    Explanation = $"{existing.Explanation} | {factor.Explanation}"
                };
            }
            else
            {
                target.Add(factor);
            }
        }
    }

    private static List<string> GenerateSuggestions(double finalScore, List<AssessmentFactor> factors)
    {
        var suggestions = new List<string>();

        if (finalScore < 0.5)
            suggestions.Add("Consider refining chunk boundaries to capture more complete context");

        var densityFactor = factors.FirstOrDefault(f => f.Name == "Information Density");
        if (densityFactor != null && densityFactor.Contribution < 0.3)
            suggestions.Add("Low information density - consider merging with adjacent chunks");

        var edgeFactor = factors.FirstOrDefault(f => f.Name == "Edge Case Detection");
        if (edgeFactor != null && edgeFactor.Contribution < -0.1)
            suggestions.Add("Edge case detected - review chunk extraction logic");

        return suggestions;
    }

    private static string GenerateReason(ChunkAssessment assessment, bool passed, ChunkFilteringOptions options)
    {
        var reasons = new List<string>();

        if (passed)
        {
            reasons.Add($"Relevance: {assessment.FinalScore:F2}");

            var topFactor = assessment.Factors.OrderByDescending(f => Math.Abs(f.Contribution)).FirstOrDefault();
            if (topFactor != null)
                reasons.Add($"Key factor: {topFactor.Name}");
        }
        else
        {
            reasons.Add($"Below threshold ({options.MinRelevanceScore:F2})");

            var worstFactor = assessment.Factors.OrderBy(f => f.Contribution).FirstOrDefault();
            if (worstFactor != null)
                reasons.Add($"Issue: {worstFactor.Name}");
        }

        if (assessment.Confidence < 0.5)
            reasons.Add("Low confidence assessment");

        return string.Join(", ", reasons);
    }

    private static int GetChunkIndex(Chunk chunk)
    {
        if (chunk.Metadata?.TryGetValue("index", out var indexObj) == true)
        {
            if (indexObj is int index) return index;
            if (int.TryParse(indexObj?.ToString(), out var parsed)) return parsed;
        }
        return int.MaxValue;
    }

    private static double Clamp(double value) => Math.Max(0, Math.Min(1, value));

    #endregion
}

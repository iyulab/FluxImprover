namespace FluxImprover.Tests.ChunkFiltering;

using FluentAssertions;
using FluxImprover.ChunkFiltering;
using FluxImprover.Models;
using FluxImprover.Options;
using FluxImprover.Services;
using NSubstitute;
using Xunit;

public sealed class ChunkFilteringServiceTests
{
    private readonly ITextCompletionService _completionService;
    private readonly ChunkFilteringService _sut;

    public ChunkFilteringServiceTests()
    {
        _completionService = Substitute.For<ITextCompletionService>();
        _sut = new ChunkFilteringService(_completionService);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCompletionService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ChunkFilteringService(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("completionService");
    }

    #endregion

    #region FilterAsync Tests

    [Fact]
    public async Task FilterAsync_WithEmptyChunks_ReturnsEmptyList()
    {
        // Arrange
        var chunks = Array.Empty<Chunk>();

        // Act
        var result = await _sut.FilterAsync(chunks, "test query");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterAsync_WithHighRelevanceChunk_PassesFilter()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "Machine learning is a subset of artificial intelligence that enables computers to learn from data."
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("0.9");

        var options = new ChunkFilteringOptions { MinRelevanceScore = 0.5 };

        // Act
        var result = await _sut.FilterAsync([chunk], "machine learning", options);

        // Assert
        result.Should().HaveCount(1);
        result[0].Passed.Should().BeTrue();
        result[0].Chunk.Id.Should().Be("test-1");
    }

    [Fact]
    public async Task FilterAsync_WithLowRelevanceChunk_FiltersOut()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "The weather today is sunny and warm."
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("0.1");

        var options = new ChunkFilteringOptions { MinRelevanceScore = 0.7 };

        // Act
        var result = await _sut.FilterAsync([chunk], "machine learning", options);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterAsync_WithMaxChunksLimit_ReturnsOnlyTopChunks()
    {
        // Arrange
        var chunks = new[]
        {
            new Chunk { Id = "1", Content = "AI is transforming technology." },
            new Chunk { Id = "2", Content = "Machine learning algorithms learn from data." },
            new Chunk { Id = "3", Content = "Deep learning uses neural networks." }
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("0.8");

        var options = new ChunkFilteringOptions
        {
            MinRelevanceScore = 0.3,
            MaxChunks = 2
        };

        // Act
        var result = await _sut.FilterAsync(chunks, "AI", options);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FilterAsync_WithPreserveOrder_MaintainsOriginalOrder()
    {
        // Arrange
        var chunks = new[]
        {
            new Chunk { Id = "1", Content = "First paragraph about AI.", Metadata = new Dictionary<string, object> { ["index"] = 0 } },
            new Chunk { Id = "2", Content = "Second paragraph about machine learning.", Metadata = new Dictionary<string, object> { ["index"] = 1 } },
            new Chunk { Id = "3", Content = "Third paragraph about deep learning.", Metadata = new Dictionary<string, object> { ["index"] = 2 } }
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("0.8");

        var options = new ChunkFilteringOptions
        {
            MinRelevanceScore = 0.3,
            PreserveOrder = true
        };

        // Act
        var result = await _sut.FilterAsync(chunks, "AI", options);

        // Assert
        result.Should().HaveCount(3);
        result[0].Chunk.Id.Should().Be("1");
        result[1].Chunk.Id.Should().Be("2");
        result[2].Chunk.Id.Should().Be("3");
    }

    [Fact]
    public async Task FilterAsync_WithCancellation_ThrowsOperationCanceled()
    {
        // Arrange
        var chunks = new[] { new Chunk { Id = "1", Content = "Test content." } };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.FilterAsync(chunks, "query", cancellationToken: cts.Token));
    }

    [Fact]
    public async Task FilterAsync_WithNullQuery_UsesQualityOnlyFiltering()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "This is a well-structured paragraph with complete sentences. It contains useful information."
        };

        var options = new ChunkFilteringOptions
        {
            MinRelevanceScore = 0.3,
            UseSelfReflection = false,
            UseCriticValidation = false
        };

        // Act
        var result = await _sut.FilterAsync([chunk], null, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].QualityScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FilterAsync_WithCustomCriteria_AppliesCriteria()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "Kubernetes Docker container orchestration deployment scaling."
        };

        var options = new ChunkFilteringOptions
        {
            MinRelevanceScore = 0.3,
            UseSelfReflection = false,
            UseCriticValidation = false,
            Criteria =
            [
                new FilterCriterion
                {
                    Type = CriterionType.KeywordPresence,
                    Value = "Kubernetes",
                    Weight = 1.0
                }
            ]
        };

        // Act
        var result = await _sut.FilterAsync([chunk], "container technology", options);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region AssessAsync Tests

    [Fact]
    public async Task AssessAsync_ReturnsChunkAssessment()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "React is a JavaScript library for building user interfaces."
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("0.85");

        // Act
        var result = await _sut.AssessAsync(chunk, "React JavaScript");

        // Assert
        result.Should().NotBeNull();
        result.InitialScore.Should().BeGreaterThan(0);
        result.FinalScore.Should().BeGreaterThan(0);
        result.Confidence.Should().BeInRange(0, 1);
        result.Factors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AssessAsync_WithSelfReflection_IncludesReflectionScore()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "Python programming language is widely used for data science."
        };

        var options = new ChunkFilteringOptions
        {
            UseSelfReflection = true,
            UseCriticValidation = false
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("0.75");

        // Act
        var result = await _sut.AssessAsync(chunk, "Python programming", options);

        // Assert
        result.ReflectionScore.Should().NotBeNull();
        result.Reasoning.Should().ContainKey("reflection");
    }

    [Fact]
    public async Task AssessAsync_WithCriticValidation_IncludesCriticScore()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "TypeScript adds static typing to JavaScript for better code quality."
        };

        var options = new ChunkFilteringOptions
        {
            UseSelfReflection = true,
            UseCriticValidation = true
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("0.80");

        // Act
        var result = await _sut.AssessAsync(chunk, "TypeScript", options);

        // Assert
        result.CriticScore.Should().NotBeNull();
        result.Reasoning.Should().ContainKey("critic");
    }

    [Fact]
    public async Task AssessAsync_WithoutSelfReflectionAndCritic_OnlyUsesInitialScore()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "Node.js is a JavaScript runtime built on Chrome's V8 engine."
        };

        var options = new ChunkFilteringOptions
        {
            UseSelfReflection = false,
            UseCriticValidation = false
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("0.70");

        // Act
        var result = await _sut.AssessAsync(chunk, "Node.js", options);

        // Assert
        result.ReflectionScore.Should().BeNull();
        result.CriticScore.Should().BeNull();
        result.Reasoning.Should().NotContainKey("reflection");
        result.Reasoning.Should().NotContainKey("critic");
    }

    [Fact]
    public async Task AssessAsync_WithShortContent_ReturnsLowerScore()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "Short."
        };

        var options = new ChunkFilteringOptions
        {
            UseSelfReflection = false,
            UseCriticValidation = false
        };

        // Act
        var result = await _sut.AssessAsync(chunk, "test query", options);

        // Assert
        result.FinalScore.Should().BeLessThan(0.5);
        result.Suggestions.Should().Contain(s => s.Contains("refin") || s.Contains("chunk"));
    }

    #endregion

    #region Three-Stage Assessment Tests

    [Fact]
    public async Task ThreeStageAssessment_FactorsIncludeContentRelevance()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "Docker containers provide lightweight isolation for applications."
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("0.85");

        // Act
        var result = await _sut.AssessAsync(chunk, "Docker containers");

        // Assert
        result.Factors.Should().Contain(f => f.Name == "Content Relevance");
    }

    [Fact]
    public async Task ThreeStageAssessment_FactorsIncludeInformationDensity()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "Kubernetes orchestrates container deployments, scaling, and management across multiple hosts."
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("0.80");

        // Act
        var result = await _sut.AssessAsync(chunk, "Kubernetes");

        // Assert
        result.Factors.Should().Contain(f => f.Name == "Information Density");
    }

    [Fact]
    public async Task ThreeStageAssessment_FactorsIncludeLLMAssessment()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "GraphQL provides a more efficient alternative to REST APIs."
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("0.90");

        // Act
        var result = await _sut.AssessAsync(chunk, "GraphQL API");

        // Assert
        result.Factors.Should().Contain(f => f.Name == "LLM Assessment");
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public async Task FilterAsync_WithLLMFailure_FallsBackToHeuristicScoring()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "PostgreSQL database management system for storing and querying data."
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns<string>(x => throw new Exception("LLM unavailable"));

        var options = new ChunkFilteringOptions { MinRelevanceScore = 0.3 };

        // Act
        var result = await _sut.FilterAsync([chunk], "PostgreSQL database", options);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task FilterAsync_WithInvalidLLMResponse_FallsBackToHeuristicScoring()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "Redis is an in-memory data structure store used as database and cache."
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("not a number");

        var options = new ChunkFilteringOptions { MinRelevanceScore = 0.3 };

        // Act
        var result = await _sut.FilterAsync([chunk], "Redis cache", options);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task FilterAsync_WithBatchProcessing_ProcessesInBatches()
    {
        // Arrange
        var chunks = Enumerable.Range(1, 10)
            .Select(i => new Chunk { Id = $"chunk-{i}", Content = $"Content for chunk {i} about technology." })
            .ToList();

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("0.7");

        var options = new ChunkFilteringOptions
        {
            BatchSize = 3,
            MinRelevanceScore = 0.3
        };

        // Act
        var result = await _sut.FilterAsync(chunks, "technology", options);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AssessAsync_WithCodeContent_RecognizesStructuralImportance()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = """
                ```python
                def hello_world():
                    print("Hello, World!")
                ```
                """
        };

        var options = new ChunkFilteringOptions
        {
            UseSelfReflection = false,
            UseCriticValidation = false
        };

        // Act
        var result = await _sut.AssessAsync(chunk, "Python code", options);

        // Assert
        result.Factors.Should().Contain(f => f.Name == "Structural Importance");
        var structuralFactor = result.Factors.First(f => f.Name == "Structural Importance");
        structuralFactor.Contribution.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AssessAsync_WithHeadingContent_RecognizesStructuralImportance()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "# Introduction to Machine Learning\nMachine learning is a powerful technique."
        };

        var options = new ChunkFilteringOptions
        {
            UseSelfReflection = false,
            UseCriticValidation = false
        };

        // Act
        var result = await _sut.AssessAsync(chunk, "machine learning intro", options);

        // Assert
        var structuralFactor = result.Factors.First(f => f.Name == "Structural Importance");
        structuralFactor.Contribution.Should().BeGreaterThan(0);
    }

    #endregion

    #region Criterion Type Tests

    [Fact]
    public async Task FilterAsync_WithInformationDensityCriterion_EvaluatesDensity()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "Diverse unique words technology software development programming algorithms data structures."
        };

        var options = new ChunkFilteringOptions
        {
            MinRelevanceScore = 0.3,
            UseSelfReflection = false,
            UseCriticValidation = false,
            Criteria =
            [
                new FilterCriterion
                {
                    Type = CriterionType.InformationDensity,
                    Weight = 1.0
                }
            ]
        };

        // Act
        var result = await _sut.FilterAsync([chunk], null, options);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FilterAsync_WithFactualContentCriterion_EvaluatesFactualContent()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "In 2023, there were 500 million users [1]. The Python 3.12 release improved performance by 25%."
        };

        var options = new ChunkFilteringOptions
        {
            MinRelevanceScore = 0.3,
            UseSelfReflection = false,
            UseCriticValidation = false,
            Criteria =
            [
                new FilterCriterion
                {
                    Type = CriterionType.FactualContent,
                    Weight = 1.0
                }
            ]
        };

        // Act
        var result = await _sut.FilterAsync([chunk], null, options);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FilterAsync_WithRecencyCriterion_EvaluatesRecency()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "Recent developments in AI technology.",
            Metadata = new Dictionary<string, object>
            {
                ["processed_at"] = DateTime.UtcNow.AddDays(-3)
            }
        };

        var options = new ChunkFilteringOptions
        {
            MinRelevanceScore = 0.3,
            UseSelfReflection = false,
            UseCriticValidation = false,
            Criteria =
            [
                new FilterCriterion
                {
                    Type = CriterionType.Recency,
                    Weight = 1.0
                }
            ]
        };

        // Act
        var result = await _sut.FilterAsync([chunk], null, options);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FilterAsync_WithSourceCredibilityCriterion_EvaluatesSourceCredibility()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "Academic research findings from peer-reviewed journal.",
            Metadata = new Dictionary<string, object>
            {
                ["file_type"] = "PDF"
            }
        };

        var options = new ChunkFilteringOptions
        {
            MinRelevanceScore = 0.3,
            UseSelfReflection = false,
            UseCriticValidation = false,
            Criteria =
            [
                new FilterCriterion
                {
                    Type = CriterionType.SourceCredibility,
                    Weight = 1.0
                }
            ]
        };

        // Act
        var result = await _sut.FilterAsync([chunk], null, options);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FilterAsync_WithCompletenessCriterion_EvaluatesCompleteness()
    {
        // Arrange
        var chunk = new Chunk
        {
            Id = "test-1",
            Content = "This is a complete sentence with proper structure. It ends with punctuation."
        };

        var options = new ChunkFilteringOptions
        {
            MinRelevanceScore = 0.3,
            UseSelfReflection = false,
            UseCriticValidation = false,
            Criteria =
            [
                new FilterCriterion
                {
                    Type = CriterionType.Completeness,
                    Weight = 1.0
                }
            ]
        };

        // Act
        var result = await _sut.FilterAsync([chunk], null, options);

        // Assert
        result.Should().NotBeEmpty();
    }

    #endregion

    #region Quality Weight Tests

    [Fact]
    public async Task FilterAsync_WithHighQualityWeight_PrioritizesQuality()
    {
        // Arrange
        var chunks = new[]
        {
            new Chunk { Id = "high-quality", Content = "Well-structured informative content with diverse vocabulary, proper sentences, and complete information." },
            new Chunk { Id = "low-quality", Content = "bad bad bad bad bad" }
        };

        _completionService.CompleteAsync(
            Arg.Any<string>(),
            Arg.Any<CompletionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("0.5");

        var options = new ChunkFilteringOptions
        {
            MinRelevanceScore = 0.3,
            QualityWeight = 0.8,
            UseSelfReflection = false,
            UseCriticValidation = false
        };

        // Act
        var result = await _sut.FilterAsync(chunks, "content", options);

        // Assert
        if (result.Count > 0)
        {
            var highQualityResult = result.FirstOrDefault(r => r.Chunk.Id == "high-quality");
            var lowQualityResult = result.FirstOrDefault(r => r.Chunk.Id == "low-quality");

            if (highQualityResult != null && lowQualityResult != null)
            {
                highQualityResult.CombinedScore.Should().BeGreaterThan(lowQualityResult.CombinedScore);
            }
        }
    }

    #endregion
}

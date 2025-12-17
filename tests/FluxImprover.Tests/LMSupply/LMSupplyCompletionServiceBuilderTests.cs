namespace FluxImprover.Tests.LMSupply;

using FluxImprover.LMSupply;
using global::LMSupply.Generator;
using FluentAssertions;
using Xunit;

public class LMSupplyCompletionServiceBuilderTests
{
    [Fact]
    public void Create_ReturnsNewBuilderInstance()
    {
        // Act
        var builder = LMSupplyCompletionServiceBuilder.Create();

        // Assert
        builder.Should().NotBeNull();
    }

    [Fact]
    public void WithModelPreset_SetsPreset_ReturnsSameBuilder()
    {
        // Arrange
        var builder = LMSupplyCompletionServiceBuilder.Create();

        // Act
        var result = builder.WithModelPreset(GeneratorModelPreset.Fast);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithModelPath_SetsPath_ReturnsSameBuilder()
    {
        // Arrange
        var builder = LMSupplyCompletionServiceBuilder.Create();

        // Act
        var result = builder.WithModelPath("/path/to/model");

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithHuggingFaceModel_SetsModelId_ReturnsSameBuilder()
    {
        // Arrange
        var builder = LMSupplyCompletionServiceBuilder.Create();

        // Act
        var result = builder.WithHuggingFaceModel("microsoft/phi-4-onnx");

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithCacheDirectory_SetsDirectory_ReturnsSameBuilder()
    {
        // Arrange
        var builder = LMSupplyCompletionServiceBuilder.Create();

        // Act
        var result = builder.WithCacheDirectory("/cache");

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithMaxContextLength_SetsLength_ReturnsSameBuilder()
    {
        // Arrange
        var builder = LMSupplyCompletionServiceBuilder.Create();

        // Act
        var result = builder.WithMaxContextLength(4096);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithVerboseLogging_EnablesLogging_ReturnsSameBuilder()
    {
        // Arrange
        var builder = LMSupplyCompletionServiceBuilder.Create();

        // Act
        var result = builder.WithVerboseLogging();

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithGenerationDefaults_ConfiguresDefaults_ReturnsSameBuilder()
    {
        // Arrange
        var builder = LMSupplyCompletionServiceBuilder.Create();

        // Act
        var result = builder.WithGenerationDefaults(defaults =>
        {
            defaults.Temperature = 0.5f;
            defaults.MaxTokens = 1024;
        });

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithTemperature_SetsTemperature_ReturnsSameBuilder()
    {
        // Arrange
        var builder = LMSupplyCompletionServiceBuilder.Create();

        // Act
        var result = builder.WithTemperature(0.8f);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithMaxTokens_SetsMaxTokens_ReturnsSameBuilder()
    {
        // Arrange
        var builder = LMSupplyCompletionServiceBuilder.Create();

        // Act
        var result = builder.WithMaxTokens(2048);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void FluentChaining_AllMethods_ReturnsSameBuilder()
    {
        // Arrange & Act
        var builder = LMSupplyCompletionServiceBuilder.Create()
            .WithModelPreset(GeneratorModelPreset.Default)
            .WithCacheDirectory("/cache")
            .WithMaxContextLength(4096)
            .WithVerboseLogging()
            .WithTemperature(0.7f)
            .WithMaxTokens(1024);

        // Assert
        builder.Should().NotBeNull();
    }
}

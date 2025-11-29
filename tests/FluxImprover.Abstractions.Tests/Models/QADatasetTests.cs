namespace FluxImprover.Abstractions.Tests.Models;

using System.Text.Json;
using FluentAssertions;
using FluxImprover.Models;

public class QADatasetTests
{
    [Fact]
    public void QADataset_WithSamples_StoresCorrectly()
    {
        // Arrange
        var samples = new List<QAPair>
        {
            new() { Id = "qa-001", Question = "Q1", Answer = "A1" },
            new() { Id = "qa-002", Question = "Q2", Answer = "A2" }
        };

        // Act
        var dataset = new QADataset
        {
            Samples = samples
        };

        // Assert
        dataset.Samples.Should().HaveCount(2);
        dataset.Version.Should().Be("1.0");
    }

    [Fact]
    public void QADataset_WithMetadata_StoresCorrectly()
    {
        // Act
        var dataset = new QADataset
        {
            Samples = [],
            Metadata = new DatasetMetadata
            {
                CreatedAt = DateTimeOffset.UtcNow,
                Generator = "FluxImprover",
                SourceDocuments = 5,
                TotalSamples = 100
            }
        };

        // Assert
        dataset.Metadata.Should().NotBeNull();
        dataset.Metadata!.Generator.Should().Be("FluxImprover");
        dataset.Metadata.SourceDocuments.Should().Be(5);
    }

    [Fact]
    public void QADataset_Serialization_RoundTrips()
    {
        // Arrange
        var original = new QADataset
        {
            Version = "1.0",
            Samples =
            [
                new() { Id = "qa-001", Question = "Q1", Answer = "A1" }
            ],
            Metadata = new DatasetMetadata
            {
                CreatedAt = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
                Generator = "FluxImprover",
                TotalSamples = 1
            }
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<QADataset>(json, options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Version.Should().Be("1.0");
        deserialized.Samples.Should().HaveCount(1);
        deserialized.Metadata!.Generator.Should().Be("FluxImprover");
    }

    [Fact]
    public void QADataset_EmptySamples_IsValid()
    {
        // Act
        var dataset = new QADataset { Samples = [] };

        // Assert
        dataset.Samples.Should().BeEmpty();
        dataset.Samples.Should().NotBeNull();
    }
}

public class DatasetMetadataTests
{
    [Fact]
    public void DatasetMetadata_StoresAllProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var metadata = new DatasetMetadata
        {
            CreatedAt = createdAt,
            Generator = "FluxImprover v1.0",
            SourceDocuments = 10,
            TotalSamples = 250,
            Configuration = new Dictionary<string, object>
            {
                ["pairsPerChunk"] = 3,
                ["language"] = "ko"
            }
        };

        // Assert
        metadata.CreatedAt.Should().Be(createdAt);
        metadata.Generator.Should().Be("FluxImprover v1.0");
        metadata.SourceDocuments.Should().Be(10);
        metadata.TotalSamples.Should().Be(250);
        metadata.Configuration.Should().ContainKey("pairsPerChunk");
    }
}

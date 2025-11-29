namespace FluxImprover.Abstractions.Tests.Utilities;

using System.Text.Json;
using FluxImprover.Utilities;

public sealed class JsonHelpersTests
{
    [Fact]
    public void Serialize_SimpleObject_ShouldReturnValidJson()
    {
        // Arrange
        var obj = new { Name = "Test", Value = 42 };

        // Act
        var json = JsonHelpers.Serialize(obj);

        // Assert
        json.Should().Contain("\"name\"");
        json.Should().Contain("\"value\"");
        json.Should().Contain("42");
    }

    [Fact]
    public void Deserialize_ValidJson_ShouldReturnObject()
    {
        // Arrange
        var json = """{"name":"Test","value":42}""";

        // Act
        var result = JsonHelpers.Deserialize<TestObject>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_InvalidJson_ShouldReturnNull()
    {
        // Arrange
        var json = "invalid json";

        // Act
        var result = JsonHelpers.Deserialize<TestObject>(json);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryDeserialize_ValidJson_ShouldReturnTrueAndObject()
    {
        // Arrange
        var json = """{"name":"Test","value":42}""";

        // Act
        var success = JsonHelpers.TryDeserialize<TestObject>(json, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
    }

    [Fact]
    public void TryDeserialize_InvalidJson_ShouldReturnFalse()
    {
        // Arrange
        var json = "invalid json";

        // Act
        var success = JsonHelpers.TryDeserialize<TestObject>(json, out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void Serialize_WithIndentation_ShouldReturnFormattedJson()
    {
        // Arrange
        var obj = new { Name = "Test" };

        // Act
        var json = JsonHelpers.Serialize(obj, indented: true);

        // Assert
        json.Should().Contain("\n");
    }

    [Fact]
    public void ExtractJsonFromText_WithMarkdownCodeBlock_ShouldReturnJson()
    {
        // Arrange
        var text = """
            Here is the result:
            ```json
            {"name":"Test","value":42}
            ```
            That's all.
            """;

        // Act
        var json = JsonHelpers.ExtractJsonFromText(text);

        // Assert
        json.Should().Be("""{"name":"Test","value":42}""");
    }

    [Fact]
    public void ExtractJsonFromText_WithBareJson_ShouldReturnJson()
    {
        // Arrange
        var text = """Some text {"name":"Test"} more text""";

        // Act
        var json = JsonHelpers.ExtractJsonFromText(text);

        // Assert
        json.Should().Be("""{"name":"Test"}""");
    }

    [Fact]
    public void ExtractJsonFromText_WithArray_ShouldReturnJsonArray()
    {
        // Arrange
        var text = """Result: [{"id":1},{"id":2}] done""";

        // Act
        var json = JsonHelpers.ExtractJsonFromText(text);

        // Assert
        json.Should().Be("""[{"id":1},{"id":2}]""");
    }

    [Fact]
    public void ExtractJsonFromText_NoJson_ShouldReturnNull()
    {
        // Arrange
        var text = "No JSON here";

        // Act
        var json = JsonHelpers.ExtractJsonFromText(text);

        // Assert
        json.Should().BeNull();
    }

    private sealed class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}

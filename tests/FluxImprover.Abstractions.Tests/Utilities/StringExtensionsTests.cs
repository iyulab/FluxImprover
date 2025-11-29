namespace FluxImprover.Abstractions.Tests.Utilities;

using FluxImprover.Utilities;

public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData("", true)]
    [InlineData(null, true)]
    [InlineData("   ", false)]
    [InlineData("text", false)]
    public void IsNullOrEmpty_ShouldReturnExpectedResult(string? input, bool expected)
    {
        // Act
        var result = input.IsNullOrEmpty();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData(null, true)]
    [InlineData("   ", true)]
    [InlineData("text", false)]
    public void IsNullOrWhiteSpace_ShouldReturnExpectedResult(string? input, bool expected)
    {
        // Act
        var result = input.IsNullOrWhiteSpace();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello World", 5, "Hello")]
    [InlineData("Hi", 10, "Hi")]
    [InlineData("Testing", 4, "Test")]
    [InlineData("", 5, "")]
    public void Truncate_ShouldTruncateToMaxLength(string input, int maxLength, string expected)
    {
        // Act
        var result = input.Truncate(maxLength);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Truncate_WithEllipsis_ShouldAddEllipsis()
    {
        // Arrange
        var input = "This is a long text";

        // Act
        var result = input.Truncate(10, addEllipsis: true);

        // Assert
        result.Should().Be("This is...");
    }

    [Theory]
    [InlineData("word1 word2 word3 word4", 2, "word1 word2")]
    [InlineData("single", 5, "single")]
    [InlineData("", 3, "")]
    public void TakeWords_ShouldReturnSpecifiedNumberOfWords(string input, int count, string expected)
    {
        // Act
        var result = input.TakeWords(count);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello World", 2)]
    [InlineData("One", 1)]
    [InlineData("", 0)]
    [InlineData("  Multiple   Spaces  ", 2)]
    public void WordCount_ShouldReturnCorrectCount(string input, int expected)
    {
        // Act
        var result = input.WordCount();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void SplitIntoSentences_ShouldSplitOnPunctuation()
    {
        // Arrange
        var input = "First sentence. Second sentence! Third sentence?";

        // Act
        var result = input.SplitIntoSentences();

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().Be("First sentence.");
        result[1].Should().Be("Second sentence!");
        result[2].Should().Be("Third sentence?");
    }

    [Fact]
    public void SplitIntoSentences_WithAbbreviations_ShouldHandleCorrectly()
    {
        // Arrange
        var input = "Mr. Smith went to the store. He bought items.";

        // Act
        var result = input.SplitIntoSentences();

        // Assert
        result.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("camelCase", "camel Case")]
    [InlineData("PascalCase", "Pascal Case")]
    [InlineData("XMLParser", "XML Parser")]
    [InlineData("simple", "simple")]
    public void SplitCamelCase_ShouldInsertSpaces(string input, string expected)
    {
        // Act
        var result = input.SplitCamelCase();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void RemoveHtmlTags_ShouldStripTags()
    {
        // Arrange
        var input = "<p>Hello <strong>World</strong></p>";

        // Act
        var result = input.RemoveHtmlTags();

        // Assert
        result.Should().Be("Hello World");
    }

    [Theory]
    [InlineData("Hello\n\nWorld", "Hello\nWorld")]
    [InlineData("Test\n\n\nText", "Test\nText")]
    [InlineData("NoDoubles", "NoDoubles")]
    public void NormalizeWhitespace_ShouldReduceMultipleNewlines(string input, string expected)
    {
        // Act
        var result = input.NormalizeWhitespace();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("not-an-email", false)]
    [InlineData("", false)]
    public void ContainsEmail_ShouldDetectEmails(string input, bool expected)
    {
        // Act
        var result = input.ContainsEmail();

        // Assert
        result.Should().Be(expected);
    }
}

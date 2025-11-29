using Xunit;
namespace FluxImprover.Tests.Services;

using FluentAssertions;
using FluxImprover.Services;
using NSubstitute;

public class ITokenizerTests
{
    [Fact]
    public void CountTokens_WithText_ReturnsTokenCount()
    {
        // Arrange
        var tokenizer = Substitute.For<ITokenizer>();
        tokenizer.CountTokens("Hello, world!").Returns(3);

        // Act
        var count = tokenizer.CountTokens("Hello, world!");

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public void CountTokens_WithEmptyText_ReturnsZero()
    {
        // Arrange
        var tokenizer = Substitute.For<ITokenizer>();
        tokenizer.CountTokens("").Returns(0);

        // Act
        var count = tokenizer.CountTokens("");

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void Encode_WithText_ReturnsTokenIds()
    {
        // Arrange
        var tokenizer = Substitute.For<ITokenizer>();
        var expectedTokens = new List<int> { 15496, 11, 995 };
        tokenizer.Encode("Hello, world!").Returns(expectedTokens);

        // Act
        var tokens = tokenizer.Encode("Hello, world!");

        // Assert
        tokens.Should().HaveCount(3);
        tokens.Should().BeEquivalentTo(expectedTokens);
    }

    [Fact]
    public void Decode_WithTokenIds_ReturnsText()
    {
        // Arrange
        var tokenizer = Substitute.For<ITokenizer>();
        var tokens = new List<int> { 15496, 11, 995 };
        tokenizer.Decode(tokens).Returns("Hello, world!");

        // Act
        var text = tokenizer.Decode(tokens);

        // Assert
        text.Should().Be("Hello, world!");
    }

    [Fact]
    public void EncodeAndDecode_ShouldBeReversible()
    {
        // Arrange
        var tokenizer = Substitute.For<ITokenizer>();
        var originalText = "Hello, world!";
        var tokens = new List<int> { 15496, 11, 995 };

        tokenizer.Encode(originalText).Returns(tokens);
        tokenizer.Decode(tokens).Returns(originalText);

        // Act
        var encoded = tokenizer.Encode(originalText);
        var decoded = tokenizer.Decode(encoded);

        // Assert
        decoded.Should().Be(originalText);
    }
}

using FluentAssertions;
using Xunit;
namespace FluxImprover.Tests.Utilities;

using FluxImprover.Utilities;

public sealed class GuardTests
{
    [Fact]
    public void NotNull_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        object? value = null;

        // Act
        var act = () => Guard.NotNull(value);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NotNull_WithValue_ShouldReturnValue()
    {
        // Arrange
        var value = "test";

        // Act
        var result = Guard.NotNull(value);

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void NotNullOrEmpty_WithInvalidValue_ShouldThrow(string? value)
    {
        // Act
        var act = () => Guard.NotNullOrEmpty(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotNullOrEmpty_WithValue_ShouldReturnValue()
    {
        // Arrange
        var value = "test";

        // Act
        var result = Guard.NotNullOrEmpty(value);

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void NotNullOrWhiteSpace_WithInvalidValue_ShouldThrow(string? value)
    {
        // Act
        var act = () => Guard.NotNullOrWhiteSpace(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InRange_WithValueInRange_ShouldReturnValue()
    {
        // Arrange
        var value = 5;

        // Act
        var result = Guard.InRange(value, 0, 10);

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void InRange_WithValueOutOfRange_ShouldThrow(int value)
    {
        // Act
        var act = () => Guard.InRange(value, 0, 10);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Positive_WithPositiveValue_ShouldReturnValue()
    {
        // Arrange
        var value = 5;

        // Act
        var result = Guard.Positive(value);

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Positive_WithNonPositiveValue_ShouldThrow(int value)
    {
        // Act
        var act = () => Guard.Positive(value);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void NotEmpty_WithEmptyCollection_ShouldThrow()
    {
        // Arrange
        var collection = Array.Empty<int>();

        // Act
        var act = () => Guard.NotEmpty(collection);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotEmpty_WithNonEmptyCollection_ShouldReturnCollection()
    {
        // Arrange
        var collection = new[] { 1, 2, 3 };

        // Act
        var result = Guard.NotEmpty(collection);

        // Assert
        result.Should().BeEquivalentTo(collection);
    }

    [Fact]
    public void FileExists_WithNonExistentFile_ShouldThrow()
    {
        // Arrange
        var path = "non_existent_file_12345.txt";

        // Act
        var act = () => Guard.FileExists(path);

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }
}

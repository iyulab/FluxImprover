namespace FluxImprover.Abstractions.Tests.Utilities;

using FluxImprover.Utilities;

public sealed class CollectionExtensionsTests
{
    [Fact]
    public void Batch_ShouldSplitIntoCorrectSizes()
    {
        // Arrange
        var items = Enumerable.Range(1, 10).ToList();

        // Act
        var batches = items.Batch(3).ToList();

        // Assert
        batches.Should().HaveCount(4);
        batches[0].Should().HaveCount(3);
        batches[1].Should().HaveCount(3);
        batches[2].Should().HaveCount(3);
        batches[3].Should().HaveCount(1);
    }

    [Fact]
    public void Batch_WithEmptyCollection_ShouldReturnEmpty()
    {
        // Arrange
        var items = Array.Empty<int>();

        // Act
        var batches = items.Batch(3).ToList();

        // Assert
        batches.Should().BeEmpty();
    }

    [Fact]
    public void Shuffle_ShouldReturnAllElements()
    {
        // Arrange
        var items = Enumerable.Range(1, 10).ToList();

        // Act
        var shuffled = items.Shuffle().ToList();

        // Assert
        shuffled.Should().HaveCount(10);
        shuffled.Should().Contain(items);
    }

    [Fact]
    public void DistinctBy_ShouldRemoveDuplicates()
    {
        // Arrange
        var items = new[]
        {
            new { Name = "A", Value = 1 },
            new { Name = "A", Value = 2 },
            new { Name = "B", Value = 3 }
        };

        // Act
        var distinct = items.DistinctBy(x => x.Name).ToList();

        // Assert
        distinct.Should().HaveCount(2);
    }

    [Fact]
    public async Task ForEachAsync_ShouldProcessAllItems()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var results = new List<int>();

        // Act
        await items.ForEachAsync(async item =>
        {
            await Task.Delay(1);
            lock (results) results.Add(item * 2);
        }, maxDegreeOfParallelism: 2);

        // Assert
        results.Should().HaveCount(3);
        results.Should().Contain(new[] { 2, 4, 6 });
    }

    [Fact]
    public async Task SelectAsync_ShouldTransformAllItems()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };

        // Act
        var results = await items.SelectAsync(async item =>
        {
            await Task.Delay(1);
            return item * 2;
        }, maxDegreeOfParallelism: 2);

        // Assert
        results.Should().BeEquivalentTo(new[] { 2, 4, 6 });
    }

    [Fact]
    public void TakeRandom_ShouldReturnCorrectCount()
    {
        // Arrange
        var items = Enumerable.Range(1, 100).ToList();

        // Act
        var random = items.TakeRandom(5).ToList();

        // Assert
        random.Should().HaveCount(5);
        random.Should().OnlyContain(x => items.Contains(x));
    }

    [Fact]
    public void TakeRandom_WithCountGreaterThanSource_ShouldReturnAll()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };

        // Act
        var random = items.TakeRandom(10).ToList();

        // Assert
        random.Should().HaveCount(3);
    }

    [Fact]
    public void SafeGet_WithValidIndex_ShouldReturnValue()
    {
        // Arrange
        var list = new List<string> { "a", "b", "c" };

        // Act
        var result = list.SafeGet(1);

        // Assert
        result.Should().Be("b");
    }

    [Fact]
    public void SafeGet_WithInvalidIndex_ShouldReturnDefault()
    {
        // Arrange
        var list = new List<string> { "a", "b", "c" };

        // Act
        var result = list.SafeGet(10);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SafeGet_WithInvalidIndex_ShouldReturnSpecifiedDefault()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        var result = list.SafeGet(10, defaultValue: -1);

        // Assert
        result.Should().Be(-1);
    }

    [Fact]
    public void IsNullOrEmpty_WithNull_ShouldReturnTrue()
    {
        // Arrange
        List<int>? list = null;

        // Act
        var result = list.IsNullOrEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_WithEmpty_ShouldReturnTrue()
    {
        // Arrange
        var list = new List<int>();

        // Act
        var result = list.IsNullOrEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_WithItems_ShouldReturnFalse()
    {
        // Arrange
        var list = new List<int> { 1 };

        // Act
        var result = list.IsNullOrEmpty();

        // Assert
        result.Should().BeFalse();
    }
}

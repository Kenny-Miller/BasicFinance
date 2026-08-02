using BasicFinance.Domain.Queries;
using BasicFinance.Infrastructure.Enums;
using BasicFinance.Infrastructure.Extensions;
using BasicFinance.Infrastructure.UnitTests.Helpers;
using Xunit;

namespace BasicFinance.Infrastructure.UnitTests.Extensions;

public class QueryableExtensionsTests
{
    [Fact]
    public void OrderBy_Ascending_ReturnsAscendingResults()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new(3, "Charlie"),
            new(1, "Alpha"),
            new(2, "Beta"),
        }.AsQueryable();

        // Act
        var result = data.OrderBy(e => e.Id, SortDirection.Ascending).ToList();

        // Assert
        Assert.Equal([1, 2, 3], result.Select(e => e.Id));
    }

    [Fact]
    public void OrderBy_Descending_ReturnsDescendingResults()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new(3, "Charlie"),
            new(1, "Alpha"),
            new(2, "Beta"),
        }.AsQueryable();

        // Act
        var result = data.OrderBy(e => e.Id, SortDirection.Descending).ToList();

        // Assert
        Assert.Equal([3, 2, 1], result.Select(e => e.Id));
    }

    [Fact]
    public void OrderBy_WithSortedQuery_Ascending_ReturnsAscendingResults()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new(3, "Charlie"),
            new(1, "Alpha"),
            new(2, "Beta"),
        }.AsQueryable();

        var query = new MockSortedQuery(sortDirection: "asc");

        // Act
        var result = data.OrderBy(e => e.Id, query).ToList();

        // Assert
        Assert.Equal([1, 2, 3], result.Select(e => e.Id));
    }

    [Fact]
    public void OrderBy_WithSortedQuery_Descending_ReturnsDescendingResults()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new(3, "Charlie"),
            new(1, "Alpha"),
            new(2, "Beta"),
        }.AsQueryable();

        var query = new MockSortedQuery(sortDirection: "desc");

        // Act
        var result = data.OrderBy(e => e.Id, query).ToList();

        // Assert
        Assert.Equal([3, 2, 1], result.Select(e => e.Id));
    }

    [Fact]
    public void OrderBy_StringProperty_Ascending_ReturnsAlphabetical()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new(1, "Charlie"),
            new(2, "Alpha"),
            new(3, "Beta"),
        }.AsQueryable();

        // Act
        var result = data.OrderBy(e => e.Name, SortDirection.Ascending).ToList();

        // Assert
        Assert.Equal(["Alpha", "Beta", "Charlie"], result.Select(e => e.Name));
    }

    [Fact]
    public void OrderBy_StringProperty_Descending_ReturnsReverseAlphabetical()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new(1, "Charlie"),
            new(2, "Alpha"),
            new(3, "Beta"),
        }.AsQueryable();

        // Act
        var result = data.OrderBy(e => e.Name, SortDirection.Descending).ToList();

        // Assert
        Assert.Equal(["Charlie", "Beta", "Alpha"], result.Select(e => e.Name));
    }
}
using BasicFinance.Domain.Queries;
using BasicFinance.Infrastructure.Enums;
using BasicFinance.Infrastructure.Extensions;
using BasicFinance.Infrastructure.UnitTests.Helpers;
using Xunit;

namespace BasicFinance.Infrastructure.UnitTests.Extensions;

public class OrderedQueryableExtensionsTests
{
    [Fact]
    public void Paginate_ValidPageAndSize_ReturnsCorrectSlice()
    {
        // Arrange
        var data = Enumerable.Range(1, 50).Select(i => new TestEntity(i, $"Item{i}")).ToList().AsQueryable();
        var ordered = data.OrderBy(e => e.Id, SortDirection.Ascending);

        var query = new MockPagedQuery(page: 2, pageSize: 10);

        // Act
        var result = ordered.Paginate(query).ToList();

        // Assert
        Assert.Equal(10, result.Count);
        Assert.Equal(11, result.First().Id);
        Assert.Equal(20, result.Last().Id);
    }

    [Fact]
    public void Paginate_NullPage_DefaultsToFirstPage()
    {
        // Arrange
        var data = Enumerable.Range(1, 50).Select(i => new TestEntity(i, $"Item{i}")).ToList().AsQueryable();
        var ordered = data.OrderBy(e => e.Id, SortDirection.Ascending);

        var query = new MockPagedQuery(page: null, pageSize: 10);

        // Act
        var result = ordered.Paginate(query).ToList();

        // Assert
        Assert.Equal(10, result.Count);
        Assert.Equal(1, result.First().Id);
        Assert.Equal(10, result.Last().Id);
    }

    [Fact]
    public void Paginate_ZeroPage_DefaultsToFirstPage()
    {
        // Arrange
        var data = Enumerable.Range(1, 50).Select(i => new TestEntity(i, $"Item{i}")).ToList().AsQueryable();
        var ordered = data.OrderBy(e => e.Id, SortDirection.Ascending);

        var query = new MockPagedQuery(page: 0, pageSize: 10);

        // Act
        var result = ordered.Paginate(query).ToList();

        // Assert
        Assert.Equal(10, result.Count);
        Assert.Equal(1, result.First().Id);
    }

    [Fact]
    public void Paginate_NegativePage_DefaultsToFirstPage()
    {
        // Arrange
        var data = Enumerable.Range(1, 50).Select(i => new TestEntity(i, $"Item{i}")).ToList().AsQueryable();
        var ordered = data.OrderBy(e => e.Id, SortDirection.Ascending);

        var query = new MockPagedQuery(page: -1, pageSize: 10);

        // Act
        var result = ordered.Paginate(query).ToList();

        // Assert
        Assert.Equal(10, result.Count);
        Assert.Equal(1, result.First().Id);
    }

    [Fact]
    public void Paginate_NullPageSize_DefaultsToDefaultPageSize()
    {
        // Arrange
        var data = Enumerable.Range(1, 50).Select(i => new TestEntity(i, $"Item{i}")).ToList().AsQueryable();
        var ordered = data.OrderBy(e => e.Id, SortDirection.Ascending);

        var query = new MockPagedQuery(page: 1, pageSize: null);

        // Act
        var result = ordered.Paginate(query).ToList();

        // Assert
        Assert.Equal(25, result.Count);
    }

    [Fact]
    public void Paginate_ZeroPageSize_DefaultsToDefaultPageSize()
    {
        // Arrange
        var data = Enumerable.Range(1, 50).Select(i => new TestEntity(i, $"Item{i}")).ToList().AsQueryable();
        var ordered = data.OrderBy(e => e.Id, SortDirection.Ascending);

        var query = new MockPagedQuery(page: 1, pageSize: 0);

        // Act
        var result = ordered.Paginate(query).ToList();

        // Assert
        Assert.Equal(25, result.Count);
    }

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
        var ordered = data.OrderBy(e => e.Id, SortDirection.Ascending);

        var query = new MockSortedQuery(sortDirection: "asc");

        // Act
        var result = ordered.OrderBy(e => e.Name, query).ToList();

        // Assert
        Assert.Equal(["Alpha", "Beta", "Charlie"], result.Select(e => e.Name));
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
        var ordered = data.OrderBy(e => e.Id, SortDirection.Ascending);

        var query = new MockSortedQuery(sortDirection: "desc");

        // Act
        var result = ordered.OrderBy(e => e.Name, query).ToList();

        // Assert
        Assert.Equal(["Charlie", "Beta", "Alpha"], result.Select(e => e.Name));
    }

    [Fact]
    public void OrderBy_WithSortDirection_Ascending()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new(3, "Charlie"),
            new(1, "Alpha"),
            new(2, "Beta"),
        }.AsQueryable();
        var ordered = data.OrderBy(e => e.Id, SortDirection.Ascending);

        // Act
        var result = ordered.OrderBy(e => e.Name, SortDirection.Ascending).ToList();

        // Assert
        Assert.Equal(["Alpha", "Beta", "Charlie"], result.Select(e => e.Name));
    }

    [Fact]
    public void ThenBy_WithSortedQuery_Ascending()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new(1, "Charlie"),
            new(2, "Alpha"),
            new(1, "Beta"),
        }.AsQueryable();
        var ordered = data.OrderBy(e => e.Id, SortDirection.Ascending);

        var query = new MockSortedQuery(sortDirection: "asc");

        // Act
        var result = ordered.ThenBy(e => e.Name, query).ToList();

        // Assert
        Assert.Equal(["Beta", "Charlie", "Alpha"], result.Select(e => e.Name));
    }

    [Fact]
    public void ThenBy_WithSortedQuery_Descending()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new(1, "Charlie"),
            new(2, "Alpha"),
            new(1, "Beta"),
        }.AsQueryable();
        var ordered = data.OrderBy(e => e.Id, SortDirection.Ascending);

        var query = new MockSortedQuery(sortDirection: "desc");

        // Act
        var result = ordered.ThenBy(e => e.Name, query).ToList();

        // Assert
        Assert.Equal(["Charlie", "Beta", "Alpha"], result.Select(e => e.Name));
    }

    [Fact]
    public void ThenBy_WithSortDirection_Ascending()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new(1, "Charlie"),
            new(2, "Alpha"),
            new(1, "Beta"),
        }.AsQueryable();
        var ordered = data.OrderBy(e => e.Id, SortDirection.Ascending);

        // Act
        var result = ordered.ThenBy(e => e.Name, SortDirection.Ascending).ToList();

        // Assert
        Assert.Equal(["Beta", "Charlie", "Alpha"], result.Select(e => e.Name));
    }

    [Fact]
    public void ThenBy_WithSortDirection_Descending()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new(1, "Charlie"),
            new(2, "Alpha"),
            new(1, "Beta"),
        }.AsQueryable();
        var ordered = data.OrderBy(e => e.Id, SortDirection.Ascending);

        // Act
        var result = ordered.ThenBy(e => e.Name, SortDirection.Descending).ToList();

        // Assert
        Assert.Equal(["Charlie", "Beta", "Alpha"], result.Select(e => e.Name));
    }
}
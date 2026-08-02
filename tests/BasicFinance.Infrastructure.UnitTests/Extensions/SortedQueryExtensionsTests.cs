using BasicFinance.Domain.Queries;
using BasicFinance.Infrastructure.Enums;
using BasicFinance.Infrastructure.Extensions;
using BasicFinance.Infrastructure.UnitTests.Helpers;
using Xunit;

namespace BasicFinance.Infrastructure.UnitTests.Extensions;

public class SortedQueryExtensionsTests
{
    [Fact]
    public void TypedSortDirection_AscendingString_ReturnsAscending()
    {
        // Arrange
        var query = new MockSortedQuery(sortDirection: "asc");

        // Act
        var result = query.TypedSortDirection;

        // Assert
        Assert.Equal(SortDirection.Ascending, result);
    }

    [Fact]
    public void TypedSortDirection_DescendingString_ReturnsDescending()
    {
        // Arrange
        var query = new MockSortedQuery(sortDirection: "desc");

        // Act
        var result = query.TypedSortDirection;

        // Assert
        Assert.Equal(SortDirection.Descending, result);
    }

    [Fact]
    public void TypedSortDirection_UppercaseDESC_ReturnsDescending()
    {
        // Arrange
        var query = new MockSortedQuery(sortDirection: "DESC");

        // Act
        var result = query.TypedSortDirection;

        // Assert
        Assert.Equal(SortDirection.Descending, result);
    }

    [Fact]
    public void TypedSortDirection_MixedCase_ReturnsDescending()
    {
        // Arrange
        var query = new MockSortedQuery(sortDirection: "Desc");

        // Act
        var result = query.TypedSortDirection;

        // Assert
        Assert.Equal(SortDirection.Descending, result);
    }

    [Fact]
    public void TypedSortDirection_Null_ReturnsAscending()
    {
        // Arrange
        var query = new MockSortedQuery(sortDirection: null);

        // Act
        var result = query.TypedSortDirection;

        // Assert
        Assert.Equal(SortDirection.Ascending, result);
    }

    [Fact]
    public void TypedSortDirection_EmptyString_ReturnsAscending()
    {
        // Arrange
        var query = new MockSortedQuery(sortDirection: "");

        // Act
        var result = query.TypedSortDirection;

        // Assert
        Assert.Equal(SortDirection.Ascending, result);
    }

    [Fact]
    public void TypedSortDirection_UnknownValue_ReturnsAscending()
    {
        // Arrange
        var query = new MockSortedQuery(sortDirection: "invalid");

        // Act
        var result = query.TypedSortDirection;

        // Assert
        Assert.Equal(SortDirection.Ascending, result);
    }
}
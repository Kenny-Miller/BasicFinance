using BasicFinance.Domain.Queries;

namespace BasicFinance.Infrastructure.UnitTests.Helpers;

internal sealed class MockSortedQuery : ISortedQuery
{
    public string? SortField { get; }
    public string? SortDirection { get; }

    public MockSortedQuery(string? sortField = null, string? sortDirection = null)
    {
        SortField = sortField;
        SortDirection = sortDirection;
    }
}
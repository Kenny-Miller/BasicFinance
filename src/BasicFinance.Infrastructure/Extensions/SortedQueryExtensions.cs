using BasicFinance.Domain.Queries;
using BasicFinance.Infrastructure.Enums;

namespace BasicFinance.Infrastructure.Extensions
{
    public static class SortedQueryExtensions
    {
        extension(ISortedQuery sortedQuery)
        {
            public SortDirection TypedSortDirection => sortedQuery.SortDirection?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true
                ? SortDirection.Descending
                : SortDirection.Ascending;
        }
    }
}

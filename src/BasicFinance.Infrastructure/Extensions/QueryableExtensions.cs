using System.Linq.Expressions;
using BasicFinance.Domain.Queries;
using BasicFinance.Infrastructure.Enums;

namespace BasicFinance.Infrastructure.Extensions
{
    public static class QueryableExtensions
    {
        extension<T>(IQueryable<T> queryable)
        {
            /// <summary>
            /// Sorts the elements of the queryable according to the specified key selector and sort direction.
            /// </summary>
            /// <param name="keySelector"></param>
            /// <param name="sortDirection"></param>
            /// <returns></returns>
            public IOrderedQueryable<T> OrderBy(Expression<Func<T, object>> keySelector, SortDirection sortDirection)
            {
                return sortDirection == SortDirection.Ascending
                     ? queryable.OrderBy(keySelector)
                     : queryable.OrderByDescending(keySelector);
            }

            /// <summary>
            /// Performs a subsequent ordering of the elements in the queryable according to the specified key selector and query sort direction.
            /// </summary>
            /// <param name="keySelector"></param>
            /// <param name="query"></param>
            /// <returns></returns>
            public IOrderedQueryable<T> OrderBy(Expression<Func<T, object>> keySelector, ISortedQuery query)
            {
                return queryable.OrderBy(keySelector, query.TypedSortDirection);
            }
        }
    }
}

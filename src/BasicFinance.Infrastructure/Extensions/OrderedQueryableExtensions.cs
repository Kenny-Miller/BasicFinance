using System.Linq.Expressions;
using BasicFinance.Domain.Queries;
using BasicFinance.Infrastructure.Enums;

namespace BasicFinance.Infrastructure.Extensions
{
    public static class OrderedQueryableExtensions
    {
        extension<T>(IOrderedQueryable<T> queryable)
        {
            /// <summary>
            /// Paginates the queryable based on the provided IPagedQuery.
            /// </summary>
            /// <remarks>
            /// If the page or page size is not provided or invalid, it defaults to the values specified in <see cref="QueryConstants"/>.
            /// </remarks>
            /// <param name="query"></param>
            /// <returns></returns>
            public IQueryable<T> Paginate(IPagedQuery query)
            {
                var page = !query.Page.HasValue || query.Page.Value <= 0 ? QueryConstants.DefaultPage : query.Page.Value;
                var pageSize = !query.PageSize.HasValue || query.PageSize.Value <= 0 ? QueryConstants.DefaultPageSize : query.PageSize.Value;

                return queryable
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);
            }

            /// <summary>
            /// Sorts the elements of the queryable according to the specified key selector and query sort direction.
            /// </summary>
            /// <param name="keySelector"></param>
            /// <param name="query"></param>
            /// <returns></returns>
            public IOrderedQueryable<T> OrderBy(Expression<Func<T, object>> keySelector, ISortedQuery query)
            {
                ArgumentNullException.ThrowIfNull(query);

                return queryable.OrderBy(keySelector, query.TypedSortDirection);
            }

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
            public IOrderedQueryable<T> ThenBy(Expression<Func<T, object>> keySelector, ISortedQuery query)
            {
                return queryable.ThenBy(keySelector, query.TypedSortDirection);
            }

            /// <summary>
            /// Performs a subsequent ordering of the elements in the queryable according to the specified key selector and sort direction.
            /// </summary>
            /// <param name="keySelector"></param>
            /// <param name="sortDirection"></param>
            /// <returns></returns>
            public IOrderedQueryable<T> ThenBy(Expression<Func<T, object>> keySelector, SortDirection sortDirection)
            {
                return sortDirection == SortDirection.Ascending
                     ? queryable.ThenBy(keySelector)
                     : queryable.ThenByDescending(keySelector);
            }
        }
    }
}
using System.Collections.Frozen;
using System.Linq.Expressions;
using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Queries;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using BasicFinance.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Contains all logic associated with the <see cref="ListAccounts"/> Endpoint.
    /// </summary>
    public static class ListAccounts
    {
        /// <summary>
        /// Request Dto for the <see cref="ListAccounts"/> endpoint.
        /// </summary>
        /// <param name="Page">The 1-based page number to return. Defaults to the first page.</param>
        /// <param name="PageSize">The maximum number of items per page. Defaults to the standard page size.</param>
        /// <param name="SortField">The field to sort by. Unknown values fall back to the account name.</param>
        /// <param name="SortDirection">The sort direction ('asc' or 'desc'). Defaults to ascending.</param>
        /// <param name="AccountTypeCode">Optional filter: the account type code (e.g. 'CHK').</param>
        /// <param name="Institution">Optional filter: the institution's full name (e.g. 'Wells Fargo').</param>
        public record Request(
            int? Page,
            int? PageSize,
            string? SortField,
            string? SortDirection,
            string? AccountTypeCode,
            string? Institution) : IPagedQuery, ISortedQuery;

        /// <summary>
        /// Retrieves <see cref="Account"/>s associated with the authenticated user
        /// based on the provided search criteria.
        /// </summary>
        /// <param name="request">The request query parameters.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted accounts.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a <see cref="ListResult{TValue}"/> of <see cref="AccountDto"/> when successful,
        /// or <see cref="BadRequest"/> on failure.
        /// </returns>
        [Authorize]
        [WolverineGet("api/Accounts/")]
        public static async Task<Ok<ListResult<AccountDto>>> HandleAsync(
            [FromQuery] Request request,
            AuthenticatedUser user,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var sortField = request.SortField ?? nameof(AccountDto.Name);
            var sortExpression = SortFieldExpressionSelectors.GetValueOrDefault(sortField, x => x.AccountName);

            var baseQuery = dbContext.Accounts
                .AsNoTracking()
                .Include(x => x.AccountType)
                .Include(x => x.Institution)
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive);

            baseQuery = ApplyFilters(baseQuery, request);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var accounts = await baseQuery
                .OrderBy(sortExpression, request)
                    .ThenBy(x => x.AccountName, request)
                .Paginate(request)
                .ToAccountDto()
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(new ListResult<AccountDto>(accounts, request.Page, request.PageSize, totalCount));
        }

        /// <summary>
        /// Applies optional filter predicates to the base query.
        /// </summary>
        private static IQueryable<Account> ApplyFilters(IQueryable<Account> query, Request request)
        {
            if (!string.IsNullOrEmpty(request.AccountTypeCode))
            {
                query = query.Where(x => x.AccountType.AccountTypeCode == request.AccountTypeCode);
            }

            if (!string.IsNullOrEmpty(request.Institution))
            {
                query = query.Where(x => x.Institution.Name == request.Institution);
            }

            return query;
        }

        /// <summary>
        /// Reference dictionary mapping sortable field names to their corresponding selectors for the <see cref="Account"/>.
        /// </summary>
        private static readonly FrozenDictionary<string, Expression<Func<Account, object>>> SortFieldExpressionSelectors = new Dictionary<string, Expression<Func<Account, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(AccountDto.Id)] = x => x.AccountId,
            [nameof(AccountDto.Name)] = x => x.AccountName,
            [nameof(AccountDto.AccountTypeCode)] = x => x.AccountType.AccountTypeCode,
            [nameof(AccountDto.InstitutionCode)] = x => x.Institution.InstitutionCode,
            [nameof(AccountDto.LatestBalance)] = x => x.Ledger.OrderByDescending(y => y.BalanceRecordedDate)
                    .ThenByDescending(y => y.SystemCreatedDate)
                    .First().Balance,
            [nameof(AccountDto.LatestBalanceRecordedDate)] = x => x.Ledger.OrderByDescending(y => y.BalanceRecordedDate)
                    .ThenByDescending(y => y.SystemCreatedDate)
                    .First().BalanceRecordedDate,
        }.ToFrozenDictionary();
    }
}
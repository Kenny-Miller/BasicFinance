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
        /// <param name="Page"></param>
        /// <param name="PageSize"></param>
        /// <param name="SortField"></param>
        /// <param name="SortDirection"></param>
        /// <param name="AccountTypeCode"></param>
        /// <param name="Institution"></param>
        public record Request(
            int? Page,
            int? PageSize,
            string? SortField,
            string? SortDirection,
            string? AccountTypeCode,
            string? Institution) : IPagedQuery, ISortedQuery;

        /// <summary>
        /// Dto containing <see cref="Account"/> data.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="AccountTypeCode"></param>
        /// <param name="Institution"></param>
        /// <param name="AccountName"></param>
        /// <param name="Balance"></param>
        /// <param name="BalanceRecordedDate"></param>
        public record AccountDto(Guid Id, string AccountTypeCode, string Institution, string AccountName, decimal Balance, DateTimeOffset BalanceRecordedDate);

        /// <summary>
        /// Retrieves <see cref="Account"/>s associated with the authenticated user
        /// based on the provided search criteria.
        /// </summary>
        /// <param name="request">The request query parameters.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted spreadsheets.</param>
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
            var sortField = request.SortField ?? nameof(AccountDto.AccountName);
            var sortExpressionSelector = SortFieldExpressionSelectors.GetValueOrDefault(sortField, x => x.AccountName);

            var baseQuery = dbContext.Accounts
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive);

            baseQuery = ApplyFilters(baseQuery, request);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var accounts = await baseQuery
                .OrderBy(sortExpressionSelector, request)
                    .ThenBy(x => x.AccountName, request)
                .Paginate(request)
                .Select(x => new AccountDto(
                    x.AccountId,
                    x.AccountType.AccountTypeCode,
                    x.Institution,
                    x.AccountName,
                    x.Balance,
                    x.BalanceRecordedDate))
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(new ListResult<AccountDto>(accounts, request.Page, request.PageSize, totalCount));
        }

        /// <summary>
        /// Applies optional filter predicates to the base query.
        /// </summary>
        private static IQueryable<Account> ApplyFilters(IQueryable<Account> query, Request request)
        {
            if (string.IsNullOrEmpty(request.AccountTypeCode))
            {
                query = query.Where(x => x.AccountType.AccountTypeCode == request.AccountTypeCode);
            }

            if (string.IsNullOrEmpty(request.Institution))
            {
                query = query.Where(x => x.Institution == request.Institution);
            }

            return query;
        }

        /// <summary>
        /// Reference dictionary mapping sortable field names to their corresponding selectors for the <see cref="Account"/>.
        /// </summary>
        private static readonly FrozenDictionary<string, Expression<Func<Account, object>>> SortFieldExpressionSelectors = new Dictionary<string, Expression<Func<Account, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(AccountDto.Id)] = x => x.AccountId,
            [nameof(AccountDto.AccountTypeCode)] = x => x.AccountType.AccountTypeCode,
            [nameof(AccountDto.Institution)] = x => x.Institution,
            [nameof(AccountDto.AccountName)] = x => x.AccountName,
            [nameof(AccountDto.Balance)] = x => x.Balance,
            [nameof(AccountDto.BalanceRecordedDate)] = x => x.BalanceRecordedDate,
        }.ToFrozenDictionary();
    }
}
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
            var sortField = request.SortField ?? nameof(AccountDto.Name);

            var baseAccounts = ApplyFilters(
                dbContext.Accounts
                    .AsNoTracking()
                    .Where(x => x.UserId == user.Id)
                    .Where(x => x.IsActive),
                request);

            var totalCount = await baseAccounts.CountAsync(cancellationToken);

            var accounts = await BuildPagedQuery(
                    baseAccounts,
                    AccountLedgerQueries.LatestPerAccountForUser(dbContext, user.Id),
                    sortField,
                    request)
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
        /// Left-joins the base accounts to their most recent ledger entry and applies
        /// sort, tie-break sort, pagination, and the final projection. The join projects
        /// to an anonymous type so that ordering and pagination are translated to SQL.
        /// </summary>
        private static IQueryable<AccountDto> BuildPagedQuery(
            IQueryable<Account> baseAccounts,
            IQueryable<AccountLedger> latestLedger,
            string sortField,
            Request request)
        {
            var items =
                from a in baseAccounts
                join lg in latestLedger on a.AccountId equals lg.AccountId into joined
                from lg in joined.DefaultIfEmpty()
                select new
                {
                    a.AccountId,
                    a.AccountName,
                    AccountTypeCode = a.AccountType.AccountTypeCode,
                    Institution = a.Institution.Name,
                    Balance = lg != null ? lg.Balance : 0m,
                    BalanceRecordedDate = lg != null ? lg.BalanceRecordedDate : DateTimeOffset.MinValue
                };

            var sorted = sortField switch
            {
                nameof(AccountDto.Id) => items.OrderBy(x => x.AccountId, request),
                nameof(AccountDto.AccountTypeCode) => items.OrderBy(x => x.AccountTypeCode, request),
                nameof(AccountDto.Institution) => items.OrderBy(x => x.Institution, request),
                nameof(AccountDto.Balance) => items.OrderBy(x => x.Balance, request),
                nameof(AccountDto.BalanceRecordedDate) => items.OrderBy(x => x.BalanceRecordedDate, request),
                _ => items.OrderBy(x => x.AccountName, request)
            };

            return sorted
                .ThenBy(x => x.AccountName, request)
                .Paginate(request)
                .Select(x => new AccountDto(
                    x.AccountId,
                    x.AccountName,
                    x.AccountTypeCode,
                    x.Institution,
                    x.Balance,
                    x.BalanceRecordedDate));
        }
    }
}
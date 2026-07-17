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

namespace BasicFinance.Api.Features.Transactions
{
    /// <summary>
    /// Contains all logic associated with the Get Recent Transactions Endpoint.
    /// </summary>
    public static class GetUserTransactions
    {
        /// <summary>
        /// Request Dto for the <see cref="GetUserTransactions"/> endpoint.
        /// </summary>
        /// <param name="Page"></param>
        /// <param name="PageSize"></param>
        /// <param name="SortField"></param>
        /// <param name="SortDirection"></param>
        /// <param name="StartDate"></param>
        /// <param name="EndDate"></param>
        /// <param name="MinAmount"></param>
        /// <param name="MaxAmount"></param>
        /// <param name="TransactionTypeId"></param>
        /// <param name="TransactionCategoryId"></param>
        /// <param name="AccountId"></param>
        /// <param name="Search"></param>
        public record Request(
            int? Page,
            int? PageSize,
            string? SortField,
            string? SortDirection,
            DateTime? StartDate,
            DateTime? EndDate,
            decimal? MinAmount,
            decimal? MaxAmount,
            int? TransactionTypeId,
            int? TransactionCategoryId,
            Guid? AccountId,
            string? Search) : IPagedQuery, ISortedQuery;

        /// <summary>
        /// Dto containing <see cref="Transaction"/> data.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="TransactionTypeName"></param>
        /// <param name="TransactionCategoryName"></param>
        /// <param name="AccountName"></param>
        /// <param name="Date"></param>
        /// <param name="Amount"></param>
        /// <param name="Description"></param>
        public record TransactionDto(
            Guid Id,
            string TransactionTypeName,
            string TransactionCategoryName,
            string AccountName,
            DateTimeOffset Date,
            decimal Amount,
            string Description);

        /// <summary>
        /// Lists <see cref="Transaction"/>s associated with the authenticated user.
        /// </summary>
        /// <param name="request">The request query parameters.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted spreadsheets.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a <see cref="ListResult{TValue}"/> of <see cref="TransactionDto"/> when successful,
        /// or <see cref="BadRequest"/> on failure.
        /// </returns>
        [Authorize]
        [WolverineGet("api/transactions/")]
        public static async Task<Ok<ListResult<TransactionDto>>> HandleAsync(
            [FromQuery] Request request,
            AuthenticatedUser user,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var sortField = request.SortField ?? nameof(TransactionDto.Date);
            var sortExpressionSelector = SortFieldExpressionSelectors.GetValueOrDefault(sortField, x => x.Date);

            var baseQuery = dbContext.Transactions
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive);

            baseQuery = ApplyFilters(baseQuery, request);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var transactions = await baseQuery
                .OrderBy(sortExpressionSelector, request)
                    .ThenBy(x => x.TransactionId, request)
                .Paginate(request)
                .Select(x => new TransactionDto(
                    x.TransactionId,
                    x.TransactionType.TransactionTypeName,
                    x.TransactionCategory.TransactionCategoryName,
                    x.Account.AccountName,
                    x.Date,
                    x.Amount,
                    x.Description))
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(new ListResult<TransactionDto>(transactions, request.Page, request.PageSize, totalCount));
        }

        /// <summary>
        /// Applies optional filter predicates to the base query.
        /// </summary>
        private static IQueryable<Transaction> ApplyFilters(IQueryable<Transaction> query, Request request)
        {
            if (request.StartDate.HasValue)
            {
                var start = new DateTimeOffset(DateTime.SpecifyKind(request.StartDate.Value, DateTimeKind.Utc));
                query = query.Where(x => x.Date >= start);
            }

            if (request.EndDate.HasValue)
            {
                var end = new DateTimeOffset(DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc));
                query = query.Where(x => x.Date <= end);
            }

            if (request.MinAmount.HasValue)
            {
                query = query.Where(x => x.Amount >= request.MinAmount.Value);
            }

            if (request.MaxAmount.HasValue)
            {
                query = query.Where(x => x.Amount <= request.MaxAmount.Value);
            }

            if (request.TransactionTypeId.HasValue)
            {
                query = query.Where(x => x.TransactionTypeId == request.TransactionTypeId.Value);
            }

            if (request.TransactionCategoryId.HasValue)
            {
                query = query.Where(x => x.TransactionCategoryId == request.TransactionCategoryId.Value);
            }

            if (request.AccountId.HasValue)
            {
                query = query.Where(x => x.AccountId == request.AccountId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search!.Trim();
                query = query.Where(x => x.Description.Contains(searchTerm));
            }

            return query;
        }

        /// <summary>
        /// Reference dictionary mapping sortable field names to their corresponding selectors for the <see cref="Transaction"/>.
        /// </summary>
        private static readonly FrozenDictionary<string, Expression<Func<Transaction, object>>> SortFieldExpressionSelectors = new Dictionary<string, Expression<Func<Transaction, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(TransactionDto.Id)] = x => x.TransactionId,
            [nameof(TransactionDto.TransactionTypeName)] = x => x.TransactionType.TransactionTypeCode,
            [nameof(TransactionDto.TransactionCategoryName)] = x => x.TransactionCategory.TransactionCategoryCode,
            [nameof(TransactionDto.AccountName)] = x => x.Account.AccountName,
            [nameof(TransactionDto.Date)] = x => x.Date,
            [nameof(TransactionDto.Amount)] = x => x.Amount,
            [nameof(TransactionDto.Description)] = x => x.Description,
        }.ToFrozenDictionary();
    }
}
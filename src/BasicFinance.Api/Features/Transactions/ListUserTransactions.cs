using System.Collections.Frozen;
using System.Linq.Expressions;
using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Queries;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using BasicFinance.Infrastructure.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Transactions
{
    /// <summary>
    /// Contains all logic associated with the List Recent Transactions Endpoint.
    /// </summary>
    public static class ListUserTransactions
    {
        /// <summary>
        /// Request Dto for the <see cref="ListUserTransactions"/> endpoint.
        /// </summary>
        /// <param name="Page"></param>
        /// <param name="PageSize"></param>
        /// <param name="SortField"></param>
        /// <param name="SortDirection"></param>
        public record Request(int? Page, int? PageSize, string? SortField, string? SortDirection) : IPagedQuery, ISortedQuery;

        /// <summary>
        /// Dto containing <see cref="Transaction"/> data.
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <param name="AccountName"></param>
        /// <param name="Date"></param>
        /// <param name="Amount"></param>
        /// <param name="Description"></param>
        /// <param name="Category"></param>
        public record TransactionDto(Guid TransactionId, string AccountName, DateTimeOffset Date, decimal Amount, string Description, string Category);

        /// <summary>
        /// Lists <see cref="Transaction"/>s associated with the authenticated user.
        /// </summary>
        /// <param name="request">The request query parameters.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted spreadsheets.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a list of <see cref="TransactionDto"/> when successful,
        /// or <see cref="BadRequest"/> on failure.
        /// </returns>
        [Authorize]
        [WolverineGet("api/transactions/")]
        public static async Task<Ok<ListResult<TransactionDto>>> HandleAsync(
            Request request,
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

            var totalCount = await baseQuery.CountAsync(cancellationToken);
            var userSpreadSheets = await baseQuery
                .OrderBy(sortExpressionSelector, request)
                    .ThenBy(x => x.TransactionId, request)
                .Paginate(request)
                 .Select(x => new TransactionDto(x.TransactionId, x.Account.AccountName, x.Date, x.Amount, x.Description, x.Category))
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(new ListResult<TransactionDto>(userSpreadSheets, request.Page, request.PageSize, totalCount));
        }

        /// <summary>
        /// Reference dictionary mapping sortable field names to their corresponding selectors for the <see cref="Transaction"/>.
        /// </summary>
        private static readonly FrozenDictionary<string, Expression<Func<Transaction, object>>> SortFieldExpressionSelectors = new Dictionary<string, Expression<Func<Transaction, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(TransactionDto.TransactionId)] = x => x.TransactionId,
            [nameof(TransactionDto.AccountName)] = x => x.Account.AccountName,
            [nameof(TransactionDto.Date)] = x => x.Date,
            [nameof(TransactionDto.Amount)] = x => x.Amount,
            [nameof(TransactionDto.Description)] = x => x.Description,
            [nameof(TransactionDto.Category)] = x => x.Category,
        }.ToFrozenDictionary();
    }
}

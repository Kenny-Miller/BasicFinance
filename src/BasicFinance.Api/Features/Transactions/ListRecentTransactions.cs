using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Dtos;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Transactions
{
    // <summary>
    /// Contains all logic associated with the List Recent Transactions Endpoint.
    /// </summary>
    public static class ListRecentTransactions
    {
        /// <summary>
        /// Lists the recent Transactions that belong to the logged in user.
        /// </summary>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted spreadsheets.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a list of <see cref="TransactionDto"/> when successful,
        /// or <see cref="BadRequest"/> on failure.
        /// </returns>
        [Authorize]
        [WolverineGet("api/transactions/recent")]
        public static async Task<Results<Ok<List<TransactionDto>>, BadRequest>> HandleAsync(
            AuthenticatedUser user,
            AppDbContext dbContext)
        {
            var transactions = await dbContext.Transactions
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.Date)
                .Take(25)
                .Select(Transaction.ToDtoExpression)
                .ToListAsync();

            return TypedResults.Ok(transactions);
        }
    }
}

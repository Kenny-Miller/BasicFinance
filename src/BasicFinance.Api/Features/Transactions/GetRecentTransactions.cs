using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Transactions
{
    /// <summary>
    /// Contains all logic associated with the Get Recent Transactions Endpoint.
    /// </summary>
    public static class GetRecentTransactions
    {
        /// <summary>
        /// Retrieves the most recent transactions for the authenticated user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="dbContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Authorize]
        [WolverineGet("api/transactions/recent")]
        public static async Task<Ok<List<GetUserTransactions.TransactionDto>>> HandleAsync(
            AuthenticatedUser user,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var recentTransactions = await dbContext.Transactions
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.Date)
                .Take(5)
                .Select(x => new GetUserTransactions.TransactionDto(
                    x.TransactionId,
                    x.TransactionType.TransactionTypeName,
                    x.TransactionCategory.TransactionCategoryName,
                    x.Account.AccountName,
                    x.Date,
                    x.Amount,
                    x.Description))
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(recentTransactions);
        }
    }
}
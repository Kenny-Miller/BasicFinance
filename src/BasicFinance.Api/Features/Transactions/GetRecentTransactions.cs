using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Transactions
{
    public static class GetRecentTransactions
    {
        [Authorize]
        [WolverineGet("api/transactions/recent")]
        public static async Task<Ok<List<ListUserTransactions.TransactionDto>>> HandleAsync(
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
                .Select(x => new ListUserTransactions.TransactionDto(
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

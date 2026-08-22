using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Contains all logic associated with the <see cref="GetMyAccounts"/> Endpoint.
    /// </summary>
    public static class GetMyAccounts
    {
        /// <summary>
        /// Retrieves distinct institutions for the authenticated user.
        /// Only includes institutions that have at least one active account belonging to the user.
        /// </summary>s
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a list of <see cref="AccountDto"/> when successful.
        /// </returns>
        [Authorize]
        [WolverineGet("api/my/accounts")]
        public static async Task<Ok<List<AccountDto>>> HandleAsync(
            AuthenticatedUser user,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var baseAccounts = dbContext.Accounts
                .AsNoTracking()
                .Where(a => a.UserId == user.Id)
                .Where(x => x.IsActive);

            var accounts = await baseAccounts
                .WithLatestLedger(AccountLedgerQueries.LatestPerAccountForUser(dbContext, user.Id))
                .Select(x => new AccountDto(
                    x.AccountId,
                    x.AccountName,
                    x.AccountTypeCode,
                    x.Institution,
                    x.Balance,
                    x.BalanceRecordedDate))
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(accounts);
        }
    }
}

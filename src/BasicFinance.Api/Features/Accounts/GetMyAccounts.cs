using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
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
        /// Retrieves the active <see cref="Account"/>s for the authenticated user.
        /// </summary>
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
            var accounts = await dbContext.Accounts
                .AsNoTracking()
                .Include(x => x.AccountType)
                .Include(x => x.Institution)
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive)
                .ToAccountDto()
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(accounts);
        }
    }
}

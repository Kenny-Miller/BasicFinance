using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.AccountTypes
{
    /// <summary>
    /// Contains all logic associated with the <see cref="ListAccountTypes"/> Endpoint.
    /// </summary>
    public static class ListAccountTypes
    {
        /// <summary>
        /// Retrieves the active <see cref="AccountType"/>s.
        /// </summary>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a list of <see cref="AccountTypeDto"/> when successful.
        /// </returns>
        [Authorize]
        [WolverineGet("api/account-types/")]
        public static async Task<Ok<List<AccountTypeDto>>> HandleAsync(
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var accountTypes = await dbContext.AccountTypes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.AccountTypeName)
                .Select(x => new AccountTypeDto(x.AccountTypeId, x.AccountTypeCode, x.AccountTypeName, x.IsLiability))
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(accountTypes);
        }
    }
}

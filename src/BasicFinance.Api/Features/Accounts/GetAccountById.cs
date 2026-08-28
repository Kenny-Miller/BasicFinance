using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Accounts;

/// <summary>
/// Contains all logic associated with the <see cref="GetAccountById"/> Endpoint.
/// </summary>
public static class GetAccountById
{
    /// <summary>
    /// Gets the <see cref="Account"/> associated with the authenticated user that has the specified Id.
    /// </summary>
    /// <param name="accountId">The Id of the account to retrieve.</param>
    /// <param name="user">The authenticated user performing the request.</param>
    /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted accounts.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// Returns <see cref="Ok{TValue}"/> when successful,
    /// or <see cref="NotFound"/> when the account does not exist, is not active, or does not belong to the user.
    /// </returns>
    [Authorize]
    [WolverineGet("api/accounts/{accountId:guid}")]
    public static async Task<Results<Ok<AccountDto>, NotFound>> HandleAsync(
        [FromRoute] Guid accountId,
        AuthenticatedUser user,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts
            .AsNoTracking()
            .Include(x => x.AccountType)
            .Include(x => x.Institution)
            .Where(x => x.UserId == user.Id)
            .Where(x => x.IsActive)
            .Where(x => x.AccountId == accountId)
            .ToAccountDto()
            .SingleOrDefaultAsync(cancellationToken);

        return account != null
            ? TypedResults.Ok(account)
            : TypedResults.NotFound();
    }
}
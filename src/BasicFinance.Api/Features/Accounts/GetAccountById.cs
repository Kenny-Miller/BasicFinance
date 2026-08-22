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
    /// Gets a <see cref="Account"/>s associated with the authenticated user and the specified Id.
    /// </summary>
    /// <param name="accountId">The request query parameters.</param>
    /// <param name="user">The authenticated user performing the request.</param>
    /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted spreadsheets.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// Returns <see cref="Ok{TValue}"/> when successful,
    /// or <see cref="BadRequest"/> on failure.
    /// </returns>
    [Authorize]
    [WolverineGet("api/Accounts/{accountId:guid}")]
    public static async Task<Results<Ok<AccountDto>, BadRequest<string>>> HandleAsync(
        [FromRoute] Guid accountId,
        AuthenticatedUser user,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts
            .AsNoTracking()
            .Include(x => x.AccountType)
            .Include(x => x.Institution)
            .Where(x => x.AccountId == accountId)
            .Where(x => x.UserId == user.Id)
            .Where(x => x.IsActive)
            .Select(x => new AccountDto(
                x.AccountId,
                x.AccountName,
                x.AccountType.AccountTypeCode,
                x.Institution.Name,
                x.Balance,
                x.BalanceRecordedDate))
            .SingleOrDefaultAsync(cancellationToken);

        return account != null
            ? TypedResults.Ok(account)
            : TypedResults.BadRequest("Account with the specified Id was not found");
    }
}
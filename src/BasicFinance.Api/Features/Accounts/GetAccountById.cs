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
public class GetAccountById
{
    /// <summary>
    /// Dto containing <see cref="Account"/> data.
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="AccountTypeCode"></param>
    /// <param name="Institution"></param>
    /// <param name="AccountName"></param>
    /// <param name="Balance"></param>
    /// <param name="BalanceRecordedDate"></param>
    public record AccountDto(Guid Id, string AccountTypeCode, string Institution, string AccountName, decimal Balance, DateTimeOffset BalanceRecordedDate);

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
            .Where(x => x.AccountId == accountId)
            .Where(x => x.UserId == user.Id)
            .Where(x => x.IsActive)
            .Select(x => new AccountDto(
                x.AccountId,
                x.AccountType.AccountTypeCode,
                x.Institution,
                x.AccountName,
                x.Balance,
                x.BalanceRecordedDate))
            .SingleOrDefaultAsync(cancellationToken);

        return account != null
            ? TypedResults.Ok(account)
            : TypedResults.BadRequest("Account with the specified Id was not found");
    }
}

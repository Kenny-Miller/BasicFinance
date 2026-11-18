using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using BasicFinance.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Insights;

/// <summary>
/// Contains all logic associated with the <see cref="GetAccountBalancesByAccount"/> Endpoint.
/// </summary>
public static class GetAccountBalancesByAccount
{
    /// <summary>
    /// Query parameters for the <see cref="GetAccountBalancesByAccount"/> endpoint.
    /// </summary>
    /// <param name="TimePeriod">The period the percentage change is measured over: the period containing <paramref name="AsOf"/>.</param>
    /// <param name="AsOf">Any day within the period to report. Defaults to today; later days are treated as today.</param>
    /// <param name="InstitutionId">Optionally limits the accounts to a single institution.</param>
    public record GetAccountBalancesByAccountRequest(TimePeriod TimePeriod, DateTime? AsOf, int? InstitutionId);

    /// <summary>
    /// Response Dto for the <see cref="GetAccountBalancesByAccount"/> endpoint.
    /// </summary>
    /// <param name="Accounts">One entry per account, ordered by account type and then account name.</param>
    /// <param name="Page">The period's window and the <c>asOf</c> values of the neighbouring pages.</param>
    public record GetAccountBalancesByAccountResponse(List<AccountBalance> Accounts, InsightPage Page);

    /// <summary>
    /// Latest balance and change over the period for a single account.
    /// </summary>
    /// <param name="AccountId">The unique identifier of the account.</param>
    /// <param name="AccountName">The name of the account.</param>
    /// <param name="AccountTypeCode">The code of the account's type (e.g. <c>CHK</c>).</param>
    /// <param name="AccountTypeName">The display name of the account's type (e.g. <c>Checking</c>).</param>
    /// <param name="InstitutionName">The name of the institution the account belongs to.</param>
    /// <param name="IsLiability">Whether the account's type is a liability. Liability balances are negative.</param>
    /// <param name="Balance">The account's latest balance within or before the period, or zero when it has none.</param>
    /// <param name="PercentChange">
    /// Change in the size of the balance since the start of the period, rounded to one decimal place.
    /// For liabilities, positive means the amount owed grew. Zero when the opening balance is zero.
    /// </param>
    public record AccountBalance(
        Guid AccountId,
        string AccountName,
        string AccountTypeCode,
        string AccountTypeName,
        string InstitutionName,
        bool IsLiability,
        decimal Balance,
        decimal PercentChange);

    /// <summary>
    /// Retrieves the latest balance and the change over the period for each of the user's accounts.
    /// </summary>
    /// <remarks>
    /// The opening balance is each account's latest ledger entry recorded before the period starts (zero when
    /// there is none); the closing balance is its latest ledger entry recorded before the period ends. Only active
    /// accounts of active account types at active institutions are included, ordered by account type and then
    /// account name.
    /// </remarks>
    /// <param name="request">The request containing the time period, optional day and optional institution filter.</param>
    /// <param name="user">The authenticated user performing the request.</param>
    /// <param name="timeProvider">Time provider for consistent date calculations.</param>
    /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Returns <see cref="Ok{TValue}"/> with one <see cref="AccountBalance"/> per account.</returns>
    [Authorize]
    [WolverineGet("api/insights/account-balances/by-account")]
    public static async Task<Ok<GetAccountBalancesByAccountResponse>> HandleAsync(
        [FromQuery] GetAccountBalancesByAccountRequest request,
        AuthenticatedUser user,
        TimeProvider timeProvider,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var anchor = InsightDates.Resolve(request.AsOf, timeProvider);
        var window = anchor.AsOf.ToPeriodWindow(request.TimePeriod);
        var windowStart = window.Start;
        var windowEnd = window.EndExclusive;

        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .Where(x => x.IsActive)
            .Where(x => x.AccountType.IsActive)
            .Where(x => x.Institution.IsActive)
            .Where(x => !request.InstitutionId.HasValue || x.InstitutionId == request.InstitutionId.Value)
            .OrderBy(x => x.AccountTypeId)
            .ThenBy(x => x.AccountName)
            .Select(x => new
            {
                x.AccountId,
                x.AccountName,
                x.AccountType.AccountTypeCode,
                x.AccountType.AccountTypeName,
                InstitutionName = x.Institution.Name,
                x.AccountType.IsLiability,
                HasEarlierEntries = x.Ledger.Any(l => l.BalanceRecordedDate < windowStart),
                OpeningBalance = x.Ledger
                    .Where(l => l.BalanceRecordedDate < windowStart)
                    .OrderByDescending(l => l.BalanceRecordedDate)
                    .ThenByDescending(l => l.SystemCreatedDate)
                    .Select(l => l.Balance)
                    .FirstOrDefault(),
                ClosingBalance = x.Ledger
                    .Where(l => l.BalanceRecordedDate < windowEnd)
                    .OrderByDescending(l => l.BalanceRecordedDate)
                    .ThenByDescending(l => l.SystemCreatedDate)
                    .Select(l => l.Balance)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var balances = accounts
            .Select(x => new AccountBalance(
                x.AccountId,
                x.AccountName,
                x.AccountTypeCode,
                x.AccountTypeName,
                x.InstitutionName,
                x.IsLiability,
                x.ClosingBalance,
                BalanceCalculations.CalculatePercentChange(x.OpeningBalance, x.ClosingBalance, x.IsLiability)))
            .ToList();

        var page = InsightPage.ForPeriod(window, request.TimePeriod, anchor, accounts.Exists(x => x.HasEarlierEntries));
        return TypedResults.Ok(new GetAccountBalancesByAccountResponse(balances, page));
    }
}

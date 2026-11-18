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
/// Contains all logic associated with the <see cref="GetAccountBalancesByType"/> Endpoint.
/// </summary>
public static class GetAccountBalancesByType
{
    /// <summary>
    /// Query parameters for the <see cref="GetAccountBalancesByType"/> endpoint.
    /// </summary>
    /// <param name="TimePeriod">The period the percentage change is measured over: the period containing <paramref name="AsOf"/>.</param>
    /// <param name="AsOf">Any day within the period to report. Defaults to today; later days are treated as today.</param>
    /// <param name="InstitutionId">Optionally limits the balances to accounts at a single institution.</param>
    public record GetAccountBalancesByTypeRequest(TimePeriod TimePeriod, DateTime? AsOf, int? InstitutionId);

    /// <summary>
    /// Response Dto for the <see cref="GetAccountBalancesByType"/> endpoint.
    /// </summary>
    /// <param name="AccountTypeBalances">One entry per active account type, including types the user has no accounts for.</param>
    /// <param name="TotalPositiveBalance">Sum of the latest balances of all non-liability account types ("You have").</param>
    /// <param name="TotalNegativeBalance">Sum of the latest balances of all liability account types ("You owe"). Negative or zero.</param>
    /// <param name="Page">The period's window and the <c>asOf</c> values of the neighbouring pages.</param>
    public record GetAccountBalancesByTypeResponse(
        List<AccountTypeBalance> AccountTypeBalances,
        decimal TotalPositiveBalance,
        decimal TotalNegativeBalance,
        InsightPage Page);

    /// <summary>
    /// Latest balance and change over the period for a single account type.
    /// </summary>
    /// <param name="AccountTypeCode">The code of the account type (e.g. <c>CHK</c>).</param>
    /// <param name="Balance">The sum of the latest balance of every account of this type, or zero when there are none.</param>
    /// <param name="PercentChanged">
    /// Change in the size of the balance since the start of the period, rounded to one decimal place.
    /// For liabilities, positive means the amount owed grew. Zero when the opening balance is zero.
    /// </param>
    public record AccountTypeBalance(string AccountTypeCode, decimal Balance, decimal PercentChanged);

    /// <summary>
    /// Retrieves the latest balance and the change over the period for each active account type,
    /// along with the total asset and liability balances.
    /// </summary>
    /// <remarks>
    /// Each account contributes its latest ledger entry recorded before the period starts to the opening balance
    /// (nothing when there is none) and its latest ledger entry recorded before the period ends to the closing
    /// balance. The per-account balances come back in one query and are summed by account type in memory. Only
    /// active accounts at active institutions are included, optionally limited to one institution.
    /// </remarks>
    /// <param name="request">The request containing the time period, optional day and optional institution filter.</param>
    /// <param name="user">The authenticated user performing the request.</param>
    /// <param name="timeProvider">Time provider for consistent date calculations.</param>
    /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Returns <see cref="Ok{TValue}"/> with the balances by account type and the totals.</returns>
    [Authorize]
    [WolverineGet("api/insights/account-balances/by-type")]
    public static async Task<Ok<GetAccountBalancesByTypeResponse>> HandleAsync(
        [FromQuery] GetAccountBalancesByTypeRequest request,
        AuthenticatedUser user,
        TimeProvider timeProvider,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var anchor = InsightDates.Resolve(request.AsOf, timeProvider);
        var window = anchor.AsOf.ToPeriodWindow(request.TimePeriod);
        var windowStart = window.Start;
        var windowEnd = window.EndExclusive;

        var accountTypes = await dbContext.AccountTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.AccountTypeId)
            .Select(x => new
            {
                x.AccountTypeCode,
                x.IsLiability,
                Accounts = x.Accounts
                    .Where(a => a.UserId == user.Id)
                    .Where(a => a.IsActive)
                    .Where(a => !request.InstitutionId.HasValue || a.InstitutionId == request.InstitutionId)
                    .Where(a => a.Institution.IsActive)
                    .Select(a => new
                    {
                        HasEarlierEntries = a.Ledger.Any(l => l.BalanceRecordedDate < windowStart),
                        OpeningBalance = a.Ledger
                            .Where(l => l.BalanceRecordedDate < windowStart)
                            .OrderByDescending(l => l.BalanceRecordedDate)
                            .ThenByDescending(l => l.SystemCreatedDate)
                            .Select(l => l.Balance)
                            .FirstOrDefault(),
                        ClosingBalance = a.Ledger
                            .Where(l => l.BalanceRecordedDate < windowEnd)
                            .OrderByDescending(l => l.BalanceRecordedDate)
                            .ThenByDescending(l => l.SystemCreatedDate)
                            .Select(l => l.Balance)
                            .FirstOrDefault(),
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        var hasEarlierEntries = false;
        var totalPositiveBalance = 0m;
        var totalNegativeBalance = 0m;
        var balanceByAccountType = new List<AccountTypeBalance>(accountTypes.Count);
        foreach (var accountType in accountTypes)
        {
            var openingBalance = accountType.Accounts.Sum(x => x.OpeningBalance);
            var closingBalance = accountType.Accounts.Sum(x => x.ClosingBalance);
            hasEarlierEntries |= accountType.Accounts.Exists(x => x.HasEarlierEntries);

            if (!accountType.IsLiability)
            {
                totalPositiveBalance += closingBalance;
            }
            else
            {
                totalNegativeBalance += closingBalance;
            }

            balanceByAccountType.Add(new(
                accountType.AccountTypeCode,
                closingBalance,
                BalanceCalculations.CalculatePercentChange(openingBalance, closingBalance, accountType.IsLiability)));
        }

        var page = InsightPage.ForPeriod(window, request.TimePeriod, anchor, hasEarlierEntries);
        return TypedResults.Ok(new GetAccountBalancesByTypeResponse(balanceByAccountType, totalPositiveBalance, totalNegativeBalance, page));
    }
}

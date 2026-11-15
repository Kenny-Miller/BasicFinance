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
/// Contains all logic associated with the <see cref="GetMyNetworth"/> Endpoint.
/// </summary>
public static class GetMyNetworth
{
    /// <summary>
    /// Query parameters for the <see cref="GetMyNetworth"/> endpoint.
    /// </summary>
    /// <param name="TimePeriod">The period the timeline spans.</param>
    /// <param name="InstitutionId">Optionally limits the net worth to accounts at a single institution.</param>
    public record GetMyNetworthRequest(TimePeriod TimePeriod, int? InstitutionId);

    /// <summary>
    /// Response Dto for the <see cref="GetMyNetworth"/> endpoint.
    /// </summary>
    /// <param name="NetworthTimeline">Net worth for each day of the current period (UTC days), oldest first. Days after today repeat the latest known value.</param>
    /// <param name="TotalNetworth">Net worth as of the last day of the timeline.</param>
    /// <param name="NetworthChange">Change in net worth since the start of the period (the opening net worth).</param>
    /// <param name="NetworthChangePercentage">
    /// <paramref name="NetworthChange"/> as a percentage of the absolute opening net worth, rounded to two decimals.
    /// Negative when net worth decreased. Zero when the opening net worth is zero.
    /// </param>
    public record GetMyNetworthResponse(
        List<NetworthOverTime> NetworthTimeline,
        decimal TotalNetworth,
        decimal NetworthChange,
        decimal NetworthChangePercentage);

    /// <summary>
    /// Net worth as of a point in time.
    /// </summary>
    /// <param name="Date">The UTC day the net worth is calculated as of.</param>
    /// <param name="Networth">The sum of every account's last known balance at the end of <paramref name="Date"/>.</param>
    public record NetworthOverTime(DateTime Date, decimal Networth);

    /// <summary>
    /// Retrieves the user's daily net worth over the current period containing today.
    /// </summary>
    /// <remarks>
    /// Runs two queries: the latest ledger entry per account recorded before the period starts (the opening
    /// balances), and the latest ledger entry per account per day within the period. Each account's balance is
    /// then carried forward day by day in memory, so an account without an entry on a given day contributes its
    /// last known balance. Accounts with no entry before the period contribute zero until their first entry.
    /// Only active accounts at active institutions are included, optionally limited to one institution.
    /// </remarks>
    /// <param name="request">The request containing the time period and optional institution filter.</param>
    /// <param name="user">The authenticated user performing the request.</param>
    /// <param name="timeProvider">Time provider for consistent date calculations.</param>
    /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Returns <see cref="Ok{TValue}"/> with the net worth timeline.</returns>
    [Authorize]
    [WolverineGet("api/insights/networth")]
    public static async Task<Ok<GetMyNetworthResponse>> HandleAsync(
        [FromQuery] GetMyNetworthRequest request,
        AuthenticatedUser user,
        TimeProvider timeProvider,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var currentPeriodDateRange = now.ToPeriodRange(request.TimePeriod);

        var userAccountLedgersQuery = dbContext.AccountLedgers
            .Where(x => x.Account.UserId == user.Id)
            .Where(x => x.Account.IsActive)
            .Where(x => x.Account.Institution.IsActive)
            .Where(x => !request.InstitutionId.HasValue || x.Account.InstitutionId == request.InstitutionId.Value);

        var previousPeriodAccountBalanceByAccountId = await userAccountLedgersQuery
            .Where(x => x.BalanceRecordedDate < currentPeriodDateRange.RangeStartDate)
            .GroupBy(x => x.AccountId)
            .Select(x => x
                .OrderByDescending(y => y.BalanceRecordedDate)
                .ThenByDescending(y => y.SystemCreatedDate)
                .First())
            .ToDictionaryAsync(x => x.AccountId, x => x.Balance, cancellationToken);

        var currentPeriodAccountLedgersByDate = await userAccountLedgersQuery
            .Where(x => x.BalanceRecordedDate >= currentPeriodDateRange.RangeStartDate && x.BalanceRecordedDate <= currentPeriodDateRange.RangeEndDate)
            .GroupBy(x => new { x.BalanceRecordedDate.Date, x.AccountId })
            .Select(x => x
                .OrderByDescending(y => y.BalanceRecordedDate)
                .ThenByDescending(y => y.SystemCreatedDate)
                .First())
            .ToDictionaryAsync(x => (x.BalanceRecordedDate.Date, x.AccountId), x => x.Balance, cancellationToken);

        var accountIds = previousPeriodAccountBalanceByAccountId
            .Select(x => x.Key)
            .Union(currentPeriodAccountLedgersByDate.Keys
                .Select(x => x.AccountId))
            .ToList();

        foreach (var accountId in accountIds)
        {
            if (currentPeriodAccountLedgersByDate.TryGetValue((currentPeriodDateRange.RangeStartDate.Date, accountId), out var _))
            {
                continue;
            }

            currentPeriodAccountLedgersByDate[(currentPeriodDateRange.RangeStartDate.Date, accountId)] = previousPeriodAccountBalanceByAccountId.GetValueOrDefault(accountId, 0m);
        }

        var previousPeriodNetworth = previousPeriodAccountBalanceByAccountId.Sum(x => x.Value);

        var timelineRange = (currentPeriodDateRange.RangeEndDate - currentPeriodDateRange.RangeStartDate).Days;
        var timeline = new List<NetworthOverTime>(timelineRange + 1);
        for (var i = 0; i <= timelineRange; i++)
        {
            var date = currentPeriodDateRange.RangeStartDate.AddDays(i);
            var balance = 0m;

            foreach (var accountId in accountIds)
            {
                if (!currentPeriodAccountLedgersByDate.TryGetValue((date.Date, accountId), out var currentBalance))
                {
                    currentBalance = currentPeriodAccountLedgersByDate[(date.AddDays(-1).Date, accountId)];
                    currentPeriodAccountLedgersByDate[(date.Date, accountId)] = currentBalance;
                }

                balance += currentBalance;
            }

            timeline.Add(new(date.Date, balance));
        }

        var currentNetworth = timeline[^1].Networth;
        var networthDifference = currentNetworth - previousPeriodNetworth;
        var networthChangePercentage = previousPeriodNetworth == 0 ? 0 : Math.Round(networthDifference / Math.Abs(previousPeriodNetworth) * 100, 2);
        return TypedResults.Ok(new GetMyNetworthResponse(timeline, timeline[^1].Networth, networthDifference, networthChangePercentage));
    }
}

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
/// Contains all logic associated with the <see cref="GetNetworth"/> Endpoint.
/// </summary>
public static class GetNetworth
{
    /// <summary>
    /// Query parameters for the <see cref="GetNetworth"/> endpoint.
    /// </summary>
    /// <param name="Range">How far the timeline looks back from <paramref name="AsOf"/>.</param>
    /// <param name="AsOf">The last day of the timeline. Any time of day may be given. Defaults to today; later days are treated as today.</param>
    /// <param name="InstitutionId">Optionally limits the net worth to accounts at a single institution.</param>
    public record GetNetworthRequest(LookbackRange Range, DateTime? AsOf, int? InstitutionId);

    /// <summary>
    /// Response Dto for the <see cref="GetNetworth"/> endpoint.
    /// </summary>
    /// <param name="NetworthTimeline">Net worth for each day of the window (UTC days), oldest first.</param>
    /// <param name="TotalNetworth">Net worth as of the last day of the timeline.</param>
    /// <param name="NetworthChange">Change in net worth since the start of the window (the opening net worth).</param>
    /// <param name="NetworthChangePercentage">
    /// <paramref name="NetworthChange"/> as a percentage of the absolute opening net worth, rounded to two decimals.
    /// Negative when net worth decreased. Zero when the opening net worth is zero.
    /// </param>
    /// <param name="Page">The window covered and the <c>asOf</c> values of the neighbouring pages.</param>
    public record GetNetworthResponse(
        List<NetworthOverTime> NetworthTimeline,
        decimal TotalNetworth,
        decimal NetworthChange,
        decimal NetworthChangePercentage,
        InsightPage Page);

    /// <summary>
    /// Net worth as of a point in time.
    /// </summary>
    /// <param name="Date">The UTC day the net worth is calculated as of.</param>
    /// <param name="Networth">The sum of every account's last known balance at the end of <paramref name="Date"/>.</param>
    public record NetworthOverTime(DateTime Date, decimal Networth);

    /// <summary>
    /// Retrieves the user's daily net worth over a trailing window ending at the requested day.
    /// </summary>
    /// <remarks>
    /// First finds the user's earliest ledger entry, which gives <see cref="LookbackRange.All"/> its start and
    /// tells whether an older page exists. A second query over the user's accounts then returns, per account, its
    /// opening balance (the latest ledger entry recorded before the window starts, or zero when there is none) and
    /// its latest ledger entry for each day within the window. Each account's balance is then carried forward day
    /// by day in memory, so an account without an entry on a given day contributes its last known balance.
    /// Only active accounts at active institutions are included, optionally limited to one institution.
    /// </remarks>
    /// <param name="request">The request containing the lookback range, optional end day and optional institution filter.</param>
    /// <param name="user">The authenticated user performing the request.</param>
    /// <param name="timeProvider">Time provider for consistent date calculations.</param>
    /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Returns <see cref="Ok{TValue}"/> with the net worth timeline.</returns>
    [Authorize]
    [WolverineGet("api/insights/networth")]
    public static async Task<Ok<GetNetworthResponse>> HandleAsync(
        [FromQuery] GetNetworthRequest request,
        AuthenticatedUser user,
        TimeProvider timeProvider,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var anchor = InsightDates.Resolve(request.AsOf, timeProvider);

        var userAccounts = dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .Where(x => x.IsActive)
            .Where(x => x.Institution.IsActive)
            .Where(x => !request.InstitutionId.HasValue || x.InstitutionId == request.InstitutionId.Value);

        // The earliest entry is where an unbounded (All) window starts, and tells whether an older page exists.
        var earliestEntry = await userAccounts
            .SelectMany(x => x.Ledger)
            .MinAsync(l => (DateTimeOffset?)l.BalanceRecordedDate, cancellationToken);

        var window = anchor.AsOf.ToLookbackWindow(request.Range).Resolve(earliestEntry?.ToUniversalTime().StartOfDay);
        var windowStart = window.Start;
        var windowEnd = window.EndExclusive;

        var accounts = await userAccounts
            .Select(x => new
            {
                OpeningBalance = x.Ledger
                    .Where(l => l.BalanceRecordedDate < windowStart)
                    .OrderByDescending(l => l.BalanceRecordedDate)
                    .ThenByDescending(l => l.SystemCreatedDate)
                    .Select(l => l.Balance)
                    .FirstOrDefault(),
                DailyBalances = x.Ledger
                    .Where(l => l.BalanceRecordedDate >= windowStart && l.BalanceRecordedDate < windowEnd)
                    .GroupBy(l => l.BalanceRecordedDate.Date)
                    .Select(g => g
                        .OrderByDescending(l => l.BalanceRecordedDate)
                        .ThenByDescending(l => l.SystemCreatedDate)
                        .Select(l => new { Date = g.Key, l.Balance })
                        .First())
                    .ToList(),
            })
            .ToListAsync(cancellationToken);


        var openingNetworth = accounts.Sum(x => x.OpeningBalance);
        var runningBalances = accounts.Select(x => x.OpeningBalance).ToArray();
        var dailyBalancesByAccount = accounts
            .Select(x => x.DailyBalances.ToDictionary(d => d.Date, d => d.Balance))
            .ToArray();

        // Build the networth timeline using the latest account balance for a given day
        var windowDays = window.Days;
        var timeline = new List<NetworthOverTime>(windowDays);
        for (var day = 0; day < windowDays; day++)
        {
            var date = windowStart.AddDays(day).Date;
            for (var i = 0; i < runningBalances.Length; i++)
            {
                if (dailyBalancesByAccount[i].TryGetValue(date, out var balance))
                {
                    runningBalances[i] = balance;
                }
            }

            timeline.Add(new(date, runningBalances.Sum()));
        }

        var currentNetworth = timeline[^1].Networth;
        var networthChange = currentNetworth - openingNetworth;
        var networthChangePercentage = openingNetworth == 0 ? 0 : Math.Round(networthChange / Math.Abs(openingNetworth) * 100, 2);
        var page = InsightPage.ForLookback(window, request.Range, anchor, hasEarlierData: earliestEntry < windowStart);
        return TypedResults.Ok(new GetNetworthResponse(timeline, currentNetworth, networthChange, networthChangePercentage, page));
    }
}

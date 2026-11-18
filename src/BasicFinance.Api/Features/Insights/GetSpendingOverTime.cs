using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Insights;

/// <summary>
/// Contains all logic associated with the <see cref="GetSpendingOverTime"/> Endpoint.
/// </summary>
public static class GetSpendingOverTime
{
    /// <summary>
    /// Query parameters for the <see cref="GetSpendingOverTime"/> endpoint.
    /// </summary>
    /// <param name="TimePeriod">The period type compared: the period containing <paramref name="AsOf"/> against the one before it.</param>
    /// <param name="AsOf">Any day within the period to report. Defaults to today; later days are treated as today.</param>
    /// <param name="InstitutionId">Optionally limits the spending to accounts at a single institution.</param>
    public record GetSpendingOverTimeRequest(TimePeriod TimePeriod, DateTime? AsOf, int? InstitutionId);

    /// <summary>
    /// Response Dto for the <see cref="GetSpendingOverTime"/> endpoint.
    /// </summary>
    /// <param name="PreviousPeriodStart">The UTC start of the period before the current one.</param>
    /// <param name="CurrentTimeline">
    /// Cumulative spend for each day of the current period, starting at day 1. Days after today repeat the latest
    /// total. Same length as <paramref name="PreviousTimeline"/>.
    /// </param>
    /// <param name="PreviousTimeline">
    /// Cumulative spend for each day of the previous period, starting at day 1. Both timelines are as long as the
    /// longer of the two periods; days past the end of a shorter period repeat its final total.
    /// </param>
    /// <param name="TotalSpend">Spend in the current period to date.</param>
    /// <param name="PreviousTotalSpend">Spend in the whole of the previous period.</param>
    /// <param name="SpendChange"><paramref name="TotalSpend"/> minus <paramref name="PreviousTotalSpend"/>. Negative when spending less.</param>
    /// <param name="Page">The current period's window and the <c>asOf</c> values of the neighbouring pages.</param>
    public record GetSpendingOverTimeResponse(
        DateTime PreviousPeriodStart,
        List<CumulativeSpend> CurrentTimeline,
        List<CumulativeSpend> PreviousTimeline,
        decimal TotalSpend,
        decimal PreviousTotalSpend,
        decimal SpendChange,
        InsightPage Page);

    /// <summary>
    /// Running total of spend within a period.
    /// </summary>
    /// <param name="Day">The day of the period, starting at 1. Shared X axis for both timelines.</param>
    /// <param name="Amount">Total spend from the start of the period up to and including <paramref name="Day"/>.</param>
    public record CumulativeSpend(int Day, decimal Amount);

    /// <summary>
    /// Retrieves the cumulative daily spend for the current period alongside the previous period for comparison.
    /// </summary>
    /// <remarks>
    /// Sums the user's spend per UTC day across both periods in one query. Each day's spend is then added
    /// to a running total in memory, so a day without spend carries the previous day's total forward.
    /// A second query checks for spend before the current period, which decides whether an older page exists.
    /// Debits count as spend. Only active transactions on active accounts at active institutions are included,
    /// optionally limited to one institution, and transfers between the user's own accounts are excluded.
    /// </remarks>
    /// <param name="request">The request containing the time period, optional day and optional institution filter.</param>
    /// <param name="user">The authenticated user performing the request.</param>
    /// <param name="timeProvider">Time provider for consistent date calculations.</param>
    /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Returns <see cref="Ok{TValue}"/> with the current and previous spending timelines.</returns>
    [Authorize]
    [WolverineGet("api/insights/spending-over-time")]
    public static async Task<Ok<GetSpendingOverTimeResponse>> HandleAsync(
        [FromQuery] GetSpendingOverTimeRequest request,
        AuthenticatedUser user,
        TimeProvider timeProvider,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        InsightAnchor anchor = InsightDates.Resolve(request.AsOf, timeProvider);
        var currentWindow = anchor.AsOf.ToPeriodWindow(request.TimePeriod);
        var previousWindow = anchor.AsOf.ToPeriodWindow(request.TimePeriod, -1);
        DateTimeOffset currentStart = currentWindow.Start;
        DateTimeOffset previousStart = previousWindow.Start;
        DateTimeOffset currentEnd = currentWindow.EndExclusive;

        IQueryable<Infrastructure.Entities.Transaction> spend = InsightTransactions.ForUser(dbContext, user.Id, request.InstitutionId)
            .Where(x => x.TransactionTypeId == (int)TransactionType.Debit);

        var dailySpend = await spend
            .Where(x => x.Date >= previousStart && x.Date < currentEnd)
            .GroupBy(x => x.Date.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var hasEarlierSpend = await spend.AnyAsync(x => x.Date < currentStart, cancellationToken);

        // Determine the daily spend for each day within the date range
        var dayCount = Math.Max(currentWindow.Days, previousWindow.Days);

        var currentDaily = new decimal[dayCount];
        var previousDaily = new decimal[dayCount];
        foreach (var day in dailySpend)
        {
            if (day.Date >= currentStart.Date)
            {
                currentDaily[(day.Date - currentStart.Date).Days] += day.Amount;
            }
            else
            {
                previousDaily[(day.Date - previousStart.Date).Days] += day.Amount;
            }
        }

        // Build cumulative spend timeline from the daily spend
        List<CumulativeSpend> currentTimeline = BuildCumulativeSpend(currentDaily);
        List<CumulativeSpend> previousTimeline = BuildCumulativeSpend(previousDaily);
        var totalSpend = currentTimeline[^1].Amount;
        var previousTotalSpend = previousTimeline[^1].Amount;

        return TypedResults.Ok(new GetSpendingOverTimeResponse(
            previousStart.UtcDateTime,
            currentTimeline,
            previousTimeline,
            totalSpend,
            previousTotalSpend,
            totalSpend - previousTotalSpend,
            InsightPage.ForPeriod(currentWindow, request.TimePeriod, anchor, hasEarlierSpend)));
    }

    /// <summary>
    /// Converts per-day spend into a running total, carrying each day's total forward to the next.
    /// </summary>
    /// <param name="dailySpend">Spend for each day of the period, indexed from the first day.</param>
    /// <returns>One <see cref="CumulativeSpend"/> per day, numbered from 1.</returns>
    private static List<CumulativeSpend> BuildCumulativeSpend(decimal[] dailySpend)
    {
        var timeline = new List<CumulativeSpend>(dailySpend.Length);
        var runningTotal = 0m;
        for (var day = 0; day < dailySpend.Length; day++)
        {
            runningTotal += dailySpend[day];
            timeline.Add(new(day + 1, runningTotal));
        }

        return timeline;
    }
}

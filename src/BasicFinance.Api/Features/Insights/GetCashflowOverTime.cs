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
/// Contains all logic associated with the <see cref="GetCashflowOverTime"/> Endpoint.
/// </summary>
public static class GetCashflowOverTime
{
    /// <summary>
    /// Query parameters for the <see cref="GetCashflowOverTime"/> endpoint.
    /// </summary>
    /// <param name="Range">
    /// How many whole months the timeline covers, ending with the month containing <paramref name="AsOf"/>.
    /// <see cref="LookbackRange.All"/> starts at the month of the earliest matching transaction.
    /// </param>
    /// <param name="AsOf">Any day within the last month of the timeline. Defaults to today; later days are treated as today.</param>
    /// <param name="InstitutionId">Optionally limits the cash flow to accounts at a single institution.</param>
    public record GetCashflowOverTimeRequest(LookbackRange Range, DateTime? AsOf, int? InstitutionId);

    /// <summary>
    /// Response Dto for the <see cref="GetCashflowOverTime"/> endpoint.
    /// </summary>
    /// <param name="CashflowTimeline">One entry per month of the window, oldest first. Months without transactions are zero.</param>
    /// <param name="TotalIncome">Sum of income across the window.</param>
    /// <param name="TotalSpend">Sum of spend across the window.</param>
    /// <param name="NetCashflow"><paramref name="TotalIncome"/> minus <paramref name="TotalSpend"/>.</param>
    /// <param name="Page">The window covered and the <c>asOf</c> values of the neighbouring pages.</param>
    public record GetCashflowOverTimeResponse(
        List<MonthlyCashflow> CashflowTimeline,
        decimal TotalIncome,
        decimal TotalSpend,
        decimal NetCashflow,
        InsightPage Page);

    /// <summary>
    /// Income and spend for a single calendar month (UTC).
    /// </summary>
    /// <param name="Year">The calendar year of the month.</param>
    /// <param name="Month">The calendar month, 1–12.</param>
    /// <param name="Income">Sum of credit transactions in the month.</param>
    /// <param name="Spend">Sum of debit transactions in the month.</param>
    /// <param name="Net"><paramref name="Income"/> minus <paramref name="Spend"/>.</param>
    public record MonthlyCashflow(int Year, int Month, decimal Income, decimal Spend, decimal Net);

    /// <summary>
    /// Retrieves monthly income and spend for the whole months ending with the month of the requested day.
    /// </summary>
    /// <remarks>
    /// The window always covers whole calendar months, so a range of six months returns six entries whatever day
    /// of the month is requested. Credits count as income and debits as spend. Only active transactions on active
    /// accounts at active institutions are included, and transfers between the user's own accounts are excluded.
    /// </remarks>
    /// <param name="request">The request containing the lookback range, optional end day and optional institution filter.</param>
    /// <param name="user">The authenticated user performing the request.</param>
    /// <param name="timeProvider">Time provider for consistent date calculations.</param>
    /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Returns <see cref="Ok{TValue}"/> with the monthly cash flow.</returns>
    [Authorize]
    [WolverineGet("api/insights/cashflow-over-time")]
    public static async Task<Ok<GetCashflowOverTimeResponse>> HandleAsync(
        [FromQuery] GetCashflowOverTimeRequest request,
        AuthenticatedUser user,
        TimeProvider timeProvider,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var anchor = InsightDates.Resolve(request.AsOf, timeProvider);
        var transactions = InsightTransactions.ForUser(dbContext, user.Id, request.InstitutionId);

        // The earliest transaction is where an unbounded (All) window starts, and tells whether an older page exists.
        var earliestTransaction = await transactions.MinAsync(t => (DateTimeOffset?)t.Date, cancellationToken);

        // Anchoring on the last day of the month makes the window span whole calendar months.
        var lastDayOfMonth = anchor.AsOf.StartOfMonth.AddMonths(1).AddDays(-1);
        var window = lastDayOfMonth
            .ToLookbackWindow(request.Range)
            .Resolve(earliestTransaction?.ToUniversalTime().StartOfMonth);
        window = window with { Start = window.Start.StartOfMonth };
        var windowStart = window.Start;
        var windowEnd = window.EndExclusive;

        var monthlyTotals = await transactions
            .Where(t => t.Date >= windowStart && t.Date < windowEnd)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Income = g.Where(t => t.TransactionTypeId == (int)TransactionType.Credit).Sum(t => t.Amount),
                Spend = g.Where(t => t.TransactionTypeId == (int)TransactionType.Debit).Sum(t => t.Amount),
            })
            .ToDictionaryAsync(x => (x.Year, x.Month), cancellationToken);

        var totalIncome = 0m;
        var totalSpend = 0m;
        var timeline = new List<MonthlyCashflow>();
        for (var month = windowStart; month < windowEnd; month = month.AddMonths(1))
        {
            var (income, spend) = monthlyTotals.TryGetValue((month.Year, month.Month), out var totals)
                ? (totals.Income, totals.Spend)
                : (0m, 0m);

            totalIncome += income;
            totalSpend += spend;
            timeline.Add(new(month.Year, month.Month, income, spend, income - spend));
        }

        var page = InsightPage.ForLookback(window, request.Range, anchor, hasEarlierData: earliestTransaction < windowStart);
        return TypedResults.Ok(new GetCashflowOverTimeResponse(
            timeline,
            totalIncome,
            totalSpend,
            totalIncome - totalSpend,
            page));
    }
}

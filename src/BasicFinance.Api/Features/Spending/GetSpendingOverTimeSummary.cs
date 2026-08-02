using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Spending
{
    /// <summary>
    /// Retrieve the logged-in user's spending over time summary.
    /// </summary>
    public static class GetSpendingOverTimeSummary
    {
        /// <summary>
        /// Represents the aggregated spending summary for current and previous months.
        /// </summary>
        /// <param name="CurrentMonthActivity">List of spending over time for the current month.</param>
        /// <param name="PreviousMonthActivity">List of spending over time for the previous month.</param>
        /// <param name="TotalMonthlySpend">Total calculated spend for the current month.</param>
        /// <param name="MonthlySpendDifference">Difference in spending between the current and previous month.</param>
        public record Response(
            List<DailySpendingOverTime> CurrentMonthActivity,
            List<DailySpendingOverTime> PreviousMonthActivity,
            decimal TotalMonthlySpend,
            decimal MonthlySpendDifference);

        /// <summary>
        /// Represents the total amount spent for the month at a given date.
        /// </summary>
        /// <param name="X">The x-coordinate of the activity point.</param>
        /// <param name="Y">The y-coordinate of the activity point.</param>
        public record DailySpendingOverTime(int X, decimal Y);

        /// <summary>
        /// Retrieves spending over time summary for the authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="timeProvider">Time provider for consistent date calculations.</param>
        /// <param name="dbContext">Application database context.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with spending summary when successful,
        /// or <see cref="BadRequest"/> when no transactions exist.
        /// </returns>
        [Authorize]
        [WolverineGet("api/Spending/SpendingOverTimeSummary")]
        public static async Task<Results<Ok<Response>, BadRequest<string>>> HandleAsync(
            AuthenticatedUser user,
            TimeProvider timeProvider,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var now = new DateTime(2025, 11, 25, 13, 26, 30);
            var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var previousMonthStart = currentMonthStart.AddMonths(-1);

            var debitTransactions = await dbContext.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == user.Id)
                .Where(t => t.IsActive)
                .Where(t => t.TransactionTypeId == (int)TransactionType.Debit)
                .Where(t => t.Date.Date >= previousMonthStart)
                .Where(t => t.Date.Date < currentMonthStart.AddMonths(1))
                .Select(t => new { t.Date.Date, t.Amount })
                .ToListAsync(cancellationToken);

            var dailySpendDict = debitTransactions
                .GroupBy(t => t.Date)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            var currentMonthSpendOverTime = BuildCumulativeSpend(currentMonthStart, dailySpendDict);
            var previousMonthSpendOverTime = BuildCumulativeSpend(previousMonthStart, dailySpendDict);

            var currentMonthTotal = currentMonthSpendOverTime[^1].Y;
            var previousMonthTotal = previousMonthSpendOverTime[^1].Y;

            return TypedResults.Ok(new Response(
                currentMonthSpendOverTime,
                previousMonthSpendOverTime,
                currentMonthTotal,
                currentMonthTotal - previousMonthTotal));
        }

        /// <summary>
        /// Builds a cumulative spending per day for a given month based on daily spending data.
        /// </summary>
        /// <param name="monthStart"></param>
        /// <param name="dailySpendDict"></param>
        /// <returns></returns>
        private static List<DailySpendingOverTime> BuildCumulativeSpend(DateTime monthStart, Dictionary<DateTime, decimal> dailySpendDict)
        {
            var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
            var result = new DailySpendingOverTime[31];

            decimal cumulativeSpend = 0m;

            for (int i = 0; i < daysInMonth; i++)
            {
                var date = new DateTime(monthStart.Year, monthStart.Month, i + 1, 0, 0, 0, DateTimeKind.Unspecified);
                cumulativeSpend += dailySpendDict.TryGetValue(date, out var dailySpend) ? dailySpend : 0m;
                result[i] = new DailySpendingOverTime(i + 1, cumulativeSpend);
            }

            for (int i = daysInMonth; i < 31; i++)
            {
                result[i] = new DailySpendingOverTime(i + 1, cumulativeSpend);
            }

            return [.. result];
        }
    }
}
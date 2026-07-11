using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Spending
{
    /// <summary>
    /// Retrieve the logged-in user's spending activity by time period.
    /// </summary>
    public static class GetSpendingActivityByPeriod
    {
        /// <summary>
        /// Represents the query parameters used by the <see cref="GetSpendingActivityByPeriod"/> endpoint.
        /// </summary>
        /// <param name="StartDate"></param>
        /// <param name="SpendingPeriod"></param>
        public record Request(DateTime StartDate, SpendingPeriod SpendingPeriod = SpendingPeriod.Monthly);

        /// <summary>
        /// Defines time periods to group spending activity by.
        /// </summary>
        public enum SpendingPeriod
        {
            /// <summary>
            /// Represents a weekly spending period.
            /// </summary>
            Weekly,

            /// <summary>
            /// Represents a monthly spending period.
            /// </summary>
            Monthly,

            /// <summary>
            /// Represents a quarterly spending period.
            /// </summary>
            Quarterly,

            /// <summary>
            /// Represents a yearly spending period.
            /// </summary>
            Yearly
        }

        /// <summary>
        /// Represents the aggregated spending activity for a given time period.
        /// </summary>
        /// <param name="PeriodStartDate"></param>
        /// <param name="PeriodEndDate"></param>
        /// <param name="TotalSpend"></param>
        /// <param name="TotalIncome"></param>
        /// <param name="SpendingActivityByCategory"></param>
        public record SpendingByPeriod(
            DateTime PeriodStartDate,
            DateTime PeriodEndDate,
            decimal TotalSpend,
            decimal TotalIncome,
            Dictionary<string, SpendingActivity> SpendingActivityByCategory);

        /// <summary>
        /// Represents the amount spent and percent of total spend for a given category within a time period.
        /// </summary>
        /// <param name="Amount"></param>
        /// <param name="PercentOfSpend"></param>
        public record SpendingActivity(decimal Amount, double PercentOfSpend = 0);

        /// <summary>
        /// Retrieves spending activity for the authenticated user for a given period.
        /// </summary>
        /// <param name="request">The request containing the start date and desired spending period.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="timeProvider">Time provider for consistent date calculations.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with <see cref="SpendingByPeriod"/> when successful,
        /// or <see cref="BadRequest"/> on failure.
        /// </returns>
        [Authorize]
        [WolverineGet("api/Spending/SpendingActivityByPeriod")]
        public static async Task<Ok<SpendingByPeriod>> HandleAsync(
            [FromQuery] Request request,
            AuthenticatedUser user,
            TimeProvider timeProvider,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            //var now = timeProvider.GetUtcNow().DateTime;
            var now = new DateTime(2026, 5, 19);
            var (periodStart, periodEnd) = GetPeriodBoundaries(request.StartDate, request.SpendingPeriod, now);

            var userTransactions = dbContext.Transactions
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive)
                .Where(x => x.Date >= periodStart)
                .Where(x => x.Date < periodEnd);

            var joinedResults = await dbContext.TransactionCategories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .LeftJoin(
                    userTransactions,
                    category => category.TransactionCategoryId,
                    transaction => transaction.TransactionCategoryId,
                    (category, transaction) => new
                    {
                        CategoryCode = category.TransactionCategoryCode,
                        DebitAmount = transaction != null && transaction.TransactionTypeId == (int)TransactionType.Debit ? transaction.Amount : 0m,
                        CreditAmount = transaction != null && transaction.TransactionTypeId == (int)TransactionType.Credit ? transaction.Amount : 0m
                    })
                .ToListAsync(cancellationToken);

            var categorySpend = new Dictionary<string, decimal>();
            var totalSpend = 0m;
            var totalIncome = 0m;

            foreach (var row in joinedResults)
            {
                totalSpend += row.DebitAmount;
                totalIncome += row.CreditAmount;
                categorySpend.TryGetValue(row.CategoryCode, out var amount);
                categorySpend[row.CategoryCode] = amount + row.DebitAmount;
            }

            var spendingActivityByCategory = new Dictionary<string, SpendingActivity>();
            foreach (var (categoryCode, amount) in categorySpend)
            {
                var percentOfSpend = totalSpend == 0 ? 0 : (double)(amount / totalSpend * 100);
                spendingActivityByCategory[categoryCode] = new SpendingActivity(amount, percentOfSpend);
            }

            return TypedResults.Ok(new SpendingByPeriod(periodStart, periodEnd, totalSpend, totalIncome, spendingActivityByCategory));
        }

        /// <summary>
        /// Calculates the start and end boundaries for a given spending period.
        /// </summary>
        /// <param name="startDate">The date anchoring the period.</param>
        /// <param name="period">The period type.</param>
        /// <param name="now">The current UTC date.</param>
        /// <returns>A tuple of (periodStart, periodEnd).</returns>
        private static (DateTime Start, DateTime End) GetPeriodBoundaries(DateTime startDate, SpendingPeriod period, DateTime now)
        {
            var start = period switch
            {
                SpendingPeriod.Weekly => startDate.AddDays(-(int)startDate.DayOfWeek),
                SpendingPeriod.Monthly => new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Unspecified),
                SpendingPeriod.Quarterly => new DateTime(startDate.Year, (startDate.Month - 1) / 3 * 3 + 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                SpendingPeriod.Yearly => new DateTime(startDate.Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                _ => throw new ArgumentException("Invalid spending period", nameof(period))
            };

            var end = period switch
            {
                SpendingPeriod.Weekly => start.AddDays(7),
                SpendingPeriod.Monthly => start.AddMonths(1),
                SpendingPeriod.Quarterly => start.AddMonths(3),
                SpendingPeriod.Yearly => start.AddYears(1),
                _ => throw new ArgumentException("Invalid spending period", nameof(period))
            };

            if (end > now)
            {
                end = now;
            }

            return (start, end);
        }
    }
}

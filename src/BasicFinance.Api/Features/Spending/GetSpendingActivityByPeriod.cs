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
        /// <param name="TimePeriod"></param>
        public record Request(DateTime StartDate, TimePeriod TimePeriod = TimePeriod.Monthly);

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
        [WolverineGet("api/spending/activity-by-period")]
        public static async Task<Ok<SpendingByPeriod>> HandleAsync(
            [FromQuery] Request request,
            AuthenticatedUser user,
            TimeProvider timeProvider,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var startDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Unspecified);

            // The end is exclusive: the start of the next period, capped at tomorrow so the
            // current period includes today's transactions.
            var tomorrow = timeProvider.GetUtcNow().Date.AddDays(1);
            var periodStart = startDate.ToStartOfPeriod(request.TimePeriod);
            var nextPeriodStart = startDate.ToStartOfPeriod(request.TimePeriod, 1);
            var periodEnd = nextPeriodStart < tomorrow ? nextPeriodStart : tomorrow;

            var userTransactions = dbContext.Transactions
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive)
                .Where(x => x.Date.Date >= periodStart)
                .Where(x => x.Date.Date < periodEnd);

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
    }
}
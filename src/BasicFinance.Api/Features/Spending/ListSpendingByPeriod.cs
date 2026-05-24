using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Queries;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
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
    public class ListSpendingByPeriod
    {
        /// <summary>
        /// Represents the query parameters used by the <see cref="ListSpendingByPeriod"/> endpoint.
        /// </summary>
        /// <param name="SpendingPeriod"></param>
        public record Request(SpendingPeriod SpendingPeriod = SpendingPeriod.Monthly);

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
            /// Represents a yearly spending period.
            /// </summary>
            Yearly
        }

        /// <summary>
        /// Represents the aggregated category spending activity for a given time period.
        /// </summary>
        /// <param name="PeriodStartDate"></param>
        /// <param name="TotalSpend"></param>
        /// <param name="SpendingActivityByCategory"></param>
        public record SpendingByPeriod(DateTime PeriodStartDate, decimal TotalSpend, Dictionary<string, SpendingActivity> SpendingActivityByCategory);

        /// <summary>
        /// Represents the amount specent, percent of total spend, and change in spend for a given category within a time period.
        /// </summary>
        /// <param name="Amount"></param>
        /// <param name="PercentOfSpend"></param>
        /// <param name="Change"></param>
        public record SpendingActivity(decimal Amount, double PercentOfSpend = 0, double Change = 0);

        /// <summary>
        /// Lists <see cref="Account"/> grouped by thier <see cref="AccountType"/> associated with the authenticated user.
        /// </summary>
        /// <param name="request">The request containing the desired spending period for the report.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted spreadsheets.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a list of <see cref="SpendingByPeriod"/> when successful,
        /// or <see cref="BadRequest"/> on failure.
        /// </returns>
        [Authorize]
        [WolverineGet("api/reports/spendingActivity")]
        public static async Task<Ok<ListResult<SpendingByPeriod>>> HandleAsync(
           [FromQuery] Request request,
            AuthenticatedUser user,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var userTransactions = dbContext.Transactions
               .AsNoTracking()
               .Where(x => x.UserId == user.Id)
               .Where(x => x.IsActive);

            var groupedTranascations = request.SpendingPeriod switch
            {
                SpendingPeriod.Weekly => userTransactions.GroupBy(x => new DateTime(x.Date.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays((x.Date.DayOfYear - 1) / 7 + 1)),
                SpendingPeriod.Monthly => userTransactions.GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1, 0, 0, 0, DateTimeKind.Utc)),
                SpendingPeriod.Yearly => userTransactions.GroupBy(x => new DateTime(x.Date.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                _ => throw new ArgumentException("Invalid spending period", nameof(request))
            };

            var spendingByPeriodResults = await groupedTranascations
                .Select(x => new
                {
                    PeriodStartDate = x.Key,
                    TotalAmount = x.Sum(x => x.Amount),
                    SpendingActivityByCategory = x
                        .GroupBy(x => x.TransactionCategory.TransactionCategoryCode)
                        .Select(x => new
                        {
                            TransactionCategoryCode = x.Key,
                            TotalAmount = x.Sum(y => y.Amount)
                        })
                })
                .ToListAsync(cancellationToken);

            var allCategoryCodes = await dbContext.TransactionCategories
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => x.TransactionCategoryCode)
                .ToListAsync(cancellationToken);


            var spendingByPeriod = new List<SpendingByPeriod>();
            for (var i = 0; i < spendingByPeriodResults.Count; i++)
            {
                var result = spendingByPeriodResults[i];
                var resultCategoryDict = result.SpendingActivityByCategory.ToDictionary(x => x.TransactionCategoryCode, x => x.TotalAmount);

                var previousPeriodCategoryDict = i == 0 ? null : spendingByPeriodResults[i - 1].SpendingActivityByCategory.ToDictionary(x => x.TransactionCategoryCode, x => x.TotalAmount);

                var spendingActivityByCategory = new Dictionary<string, SpendingActivity>();
                foreach (var categoryCode in allCategoryCodes)
                {
                    var categoryTotalAmount = resultCategoryDict.TryGetValue(categoryCode, out var totalAmount) ? totalAmount : 0m;
                    var percentOfSpend = result.TotalAmount == 0 ? 0 : (int)(categoryTotalAmount / result.TotalAmount * 100);
                    var change = 0;

                    if (previousPeriodCategoryDict != null)
                    {
                        var previousAmount = previousPeriodCategoryDict.TryGetValue(categoryCode, out var prevAmount) ? prevAmount : 0m;
                        change = previousAmount == 0 ? 100 : (int)((categoryTotalAmount - previousAmount) / (previousAmount == 0 ? 1 : previousAmount) * 100);
                    }

                    spendingActivityByCategory[categoryCode] = new SpendingActivity(categoryTotalAmount, percentOfSpend, change);
                }
                spendingByPeriod.Add(new SpendingByPeriod(result.PeriodStartDate, result.TotalAmount, spendingActivityByCategory));
            }

            return TypedResults.Ok(new ListResult<SpendingByPeriod>(spendingByPeriod, 1, spendingByPeriod.Count, spendingByPeriod.Count));
        }
    }
}
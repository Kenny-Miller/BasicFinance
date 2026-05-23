using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Queries;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Reports
{
    public class ListSpendingByPeriod
    {
        public record Request(SpendingPeriod SpendingPeriod = SpendingPeriod.Monthly);

        public enum SpendingPeriod
        {
            Weekly,
            Monthly,
            Yearly
        }

        public record SpendingByPeriod(DateTime PeriodStartDate, decimal TotalSpend, Dictionary<string, SpendingActivity> SpendingActivityByCategory);
        public record SpendingActivity(decimal Amount, double PercentOfSpend = 0, double Change = 0);
        private record TranscationData(decimal Amount, DateTimeOffset Date, int TransactionCategoryId);

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
                SpendingPeriod.Weekly => userTransactions.GroupBy(x => new DateTime(x.Date.Year, 1, 1).AddDays((x.Date.DayOfYear - 1) / 7 + 1)),
                SpendingPeriod.Monthly => userTransactions.GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1)),
                SpendingPeriod.Yearly => userTransactions.GroupBy(x => new DateTime(x.Date.Year, 1, 1)),
                _ => throw new ArgumentException("Invalid spending period", nameof(request.SpendingPeriod))
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

        //private static SpendingActivity CalculateChange(SpendingActivity previous, SpendingActivity current, decimal totalSpend)
        //{
        //    // Calculate percentage of spend for current category
        //    var percentOfSpend = totalSpend == 0 ? 0 : (int)(current.Amount / totalSpend * 100);

        //    // Calculate change percentage
        //    var change = previous.Amount == 0 ? 100 : (int)((current.Amount - previous.Amount) / previous.Amount * 100);

        //    return current with { PercentOfSpend = percentOfSpend, Change = change };
        //}

        //private static IQueryable<TransactionsByPeriod> TransactionsByWeeklyPeriod(IQueryable<Infrastructure.Entities.Transaction> transactions)
        //{
        //    return transactions.GroupBy(x => new DateTime(x.Date.Year, 1, x.Date.DayOfYear / 7))
        //        .Select(x => new TransactionsByPeriod(x.Key, x.ToList()));
        //}

        //private static IQueryable<TransactionsByPeriod> TransactionsByMonthlyPeriod(IQueryable<Transaction> transactions)
        //{
        //    return transactions.GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1))
        //        .Select(x => new TransactionsByPeriod(x.Key, x.ToList()));
        //}

        //private static IQueryable<TransactionsByPeriod> TransactionsByYearlyPeriod(IQueryable<TranscationData> transactions)
        //{
        //    return transactions.GroupBy(x => new DateTime(x.Date.Year, 1, 1))
        //        .Select(x => new TransactionsByPeriod(x.Key, x.ToList()));
        //}
    }
}
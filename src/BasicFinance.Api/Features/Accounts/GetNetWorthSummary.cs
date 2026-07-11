using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using AccountType = BasicFinance.Infrastructure.Enums.AccountType;

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Contains all logic associated with the Net Worth Summary Endpoint.
    /// </summary>
    public static class GetNetWorthSummary
    {
        /// <summary>
        /// Intermediate projection holding account and history balance data.
        /// </summary>
        /// <param name="AccountId">The unique account identifier.</param>
        /// <param name="AccountTypeId">The account type identifier.</param>
        /// <param name="HistoryBalance">The balance from the history record, or null if no history exists.</param>
        /// <param name="HistoryBalanceRecordedDate">The date the history balance was recorded, or null if no history exists.</param>
        /// <param name="AccountBalance">The account's current balance as of the last sync.</param>
        private sealed record HistoryRow(
            Guid AccountId,
            int AccountTypeId,
            decimal? HistoryBalance,
            DateTimeOffset? HistoryBalanceRecordedDate,
            decimal AccountBalance);

        /// <summary>
        /// Dto containing the net worth and category totals for current and previous months.
        /// </summary>
        /// <param name="CurrentNetWorth">The total net worth for the current month (assets minus liabilities).</param>
        /// <param name="LastMonthNetWorth">The total net worth for the previous month (assets minus liabilities).</param>
        /// <param name="CurrentChecking">The total checking balance for the current month.</param>
        /// <param name="LastMonthChecking">The total checking balance for the previous month.</param>
        /// <param name="CurrentSavings">The total savings balance for the current month.</param>
        /// <param name="LastMonthSavings">The total savings balance for the previous month.</param>
        /// <param name="CurrentInvestments">The total investments balance for the current month.</param>
        /// <param name="LastMonthInvestments">The total investments balance for the previous month.</param>
        public record Response(
            decimal CurrentNetWorth,
            decimal LastMonthNetWorth,
            decimal CurrentChecking,
            decimal LastMonthChecking,
            decimal CurrentSavings,
            decimal LastMonthSavings,
            decimal CurrentInvestments,
            decimal LastMonthInvestments);

        /// <summary>
        /// Retrieves the net worth summary for the authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="timeProvider">Time provider for consistent date calculations.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with net worth summary when successful.
        /// </returns>
        [Authorize]
        [WolverineGet("api/accounts/netWorthSummary")]
        public static async Task<Ok<Response>> HandleAsync(
            AuthenticatedUser user,
            TimeProvider timeProvider,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow();
            var currentMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var previousMonthStart = currentMonthStart.AddMonths(-1);

            var joinedResults = await dbContext.Accounts
                .AsNoTracking()
                .Where(a => a.UserId == user.Id)
                .Where(a => a.IsActive)
                .LeftJoin(
                    dbContext.AccountBalanceHistories
                        .AsNoTracking()
                        .Where(h => h.IsActive)
                        .Where(h => h.BalanceRecordedDate >= previousMonthStart)
                        .Where(h => h.BalanceRecordedDate < currentMonthStart),
                    a => a.AccountId,
                    h => h.AccountId,
                    (account, history) => new HistoryRow(
                        account.AccountId,
                        account.AccountTypeId,
                        history != null ? (decimal?)history.Balance : null,
                        history != null ? (DateTimeOffset?)history.BalanceRecordedDate : null,
                        account.Balance))
                .ToListAsync(cancellationToken);

            var currentMonthTotals = CalculateCurrentMonthTotals(joinedResults);
            var previousMonthTotals = CalculatePreviousMonthTotals(joinedResults);

            var currentChecking = currentMonthTotals.GetValueOrDefault((int)AccountType.Checking, 0m);
            var currentSavings = currentMonthTotals.GetValueOrDefault((int)AccountType.Savings, 0m);
            var currentInvestments = currentMonthTotals.GetValueOrDefault((int)AccountType.Investment, 0m);
            var currentCreditCards = currentMonthTotals.GetValueOrDefault((int)AccountType.CreditCard, 0m);

            var lastMonthChecking = previousMonthTotals.GetValueOrDefault((int)AccountType.Checking, 0m);
            var lastMonthSavings = previousMonthTotals.GetValueOrDefault((int)AccountType.Savings, 0m);
            var lastMonthInvestments = previousMonthTotals.GetValueOrDefault((int)AccountType.Investment, 0m);
            var lastMonthCreditCards = previousMonthTotals.GetValueOrDefault((int)AccountType.CreditCard, 0m);

            var currentNetWorth = currentChecking + currentSavings + currentInvestments - currentCreditCards;
            var lastMonthNetWorth = lastMonthChecking + lastMonthSavings + lastMonthInvestments - lastMonthCreditCards;

            return TypedResults.Ok(new Response(
                currentNetWorth,
                lastMonthNetWorth,
                currentChecking,
                lastMonthChecking,
                currentSavings,
                lastMonthSavings,
                currentInvestments,
                lastMonthInvestments));
        }

        /// <summary>
        /// Calculates the current month totals by summing each account's balance grouped by account type.
        /// </summary>
        /// <param name="results">The joined account and history results.</param>
        /// <returns>A dictionary mapping account type IDs to their total current balance.</returns>
        private static Dictionary<int, decimal> CalculateCurrentMonthTotals(List<HistoryRow> results)
        {
            return results
                .GroupBy(r => r.AccountTypeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r => r.AccountBalance));
        }

        /// <summary>
        /// Calculates the previous month totals by picking the latest history balance per account, then summing by account type.
        /// </summary>
        /// <param name="results">The joined account and history results.</param>
        /// <returns>A dictionary mapping account type IDs to their total previous month balance.</returns>
        private static Dictionary<int, decimal> CalculatePreviousMonthTotals(List<HistoryRow> results)
        {
            var latestHistoryPerAccount = results
                .GroupBy(r => r.AccountId)
                .Select(g => g.OrderByDescending(r => r.HistoryBalanceRecordedDate)
                    .First())
                .ToList();

            return latestHistoryPerAccount
                .GroupBy(r => r.AccountTypeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r => r.HistoryBalance ?? 0m));
        }
    }
}

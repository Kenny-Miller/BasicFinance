using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Contains all logic associated with the Net Worth Summary Endpoint.
    /// </summary>
    public static class GetNetWorthSummary
    {
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

            var accountTypeBalances = await dbContext.AccountTypes
                .AsNoTracking()
                .Where(a => a.IsActive)
                .GroupJoin(
                    dbContext.Accounts
                        .AsNoTracking()
                        .Where(a => a.UserId == user.Id)
                        .Where(a => a.IsActive)
                        .Where(a => a.BalanceRecordedDate >= currentMonthStart),
                    at => at.AccountTypeId,
                    a => a.AccountTypeId,
                    (accountType, accounts) => new
                    {
                        AccounTypeId = accountType.AccountTypeId,
                        AccountData = accounts.Select(a => new
                        {
                            a.AccountId,
                            a.Balance,
                            a.BalanceRecordedDate,
                            LastMonthEndBalance = a.AccountBalanceHistory
                                .Where(ah => ah.IsActive)
                                .Where(h => h.BalanceRecordedDate >= previousMonthStart)
                                .Where(h => h.BalanceRecordedDate < currentMonthStart)
                                .OrderByDescending(h => h.BalanceRecordedDate)
                                .FirstOrDefault(),
                        })
                    })
                .ToListAsync(cancellationToken);

            var summedBalances = accountTypeBalances
                .Select(x => new
                {
                    AccountType = (AccountType)x.AccounTypeId,
                    CurrentBalance = x.AccountData.Sum(a => a.Balance),
                    LastMonthBalance = x.AccountData.Sum(a => a.LastMonthEndBalance?.Balance ?? 0)
                }).ToDictionary(x => x.AccountType);

            var checkingBalance = summedBalances.TryGetValue(AccountType.Checking, out var checking) ? checking : null;
            var savingsBalance = summedBalances.TryGetValue(AccountType.Savings, out var savings) ? savings : null;
            var investmentsBalance = summedBalances.TryGetValue(AccountType.Investment, out var investments) ? investments : null;
            var creditBalance = summedBalances.TryGetValue(AccountType.CreditCard, out var credit) ? credit : null;

            var netWorth = new
            {
                CurrentNetWorth = (checkingBalance?.CurrentBalance ?? 0) + (savingsBalance?.CurrentBalance ?? 0) + (investmentsBalance?.CurrentBalance ?? 0) - (creditBalance?.CurrentBalance ?? 0),
                LastMonthNetWorth = (checkingBalance?.LastMonthBalance ?? 0) + (savingsBalance?.LastMonthBalance ?? 0) + (investmentsBalance?.LastMonthBalance ?? 0) - (creditBalance?.LastMonthBalance ?? 0)
            };

            return TypedResults.Ok(new Response(
                netWorth.CurrentNetWorth,
                netWorth.LastMonthNetWorth,
                checkingBalance?.CurrentBalance ?? 0,
                checkingBalance?.LastMonthBalance ?? 0,
                savingsBalance?.CurrentBalance ?? 0,
                savingsBalance?.LastMonthBalance ?? 0,
                investmentsBalance?.CurrentBalance ?? 0,
                investmentsBalance?.LastMonthBalance ?? 0
            ));
        }
    }
}
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
    /// Contains all logic associated with the <see cref="GetAccountAnalytics"/> Endpoint.
    /// </summary>
    public static class GetAccountAnalytics
    {
        public record Request(DateTimeOffset RecordedDate, TimePeriod TimePeriod);

        /// <summary>
        /// Defines time periods to group activity by.
        /// </summary>
        public enum TimePeriod
        {
            /// <summary>
            /// Represents a weekly period.
            /// </summary>
            Weekly,

            /// <summary>
            /// Represents a monthly period.
            /// </summary>
            Monthly,

            /// <summary>
            /// Represents a quarterly period.
            /// </summary>
            Quarterly,

            /// <summary>
            /// Represents a yearly period.
            /// </summary>
            Yearly
        }

        public record Response(TotalBalanceBreakdown CurrentPeriodBreakdown, TotalBalanceBreakdown PreviousPeriodBreakdown);

        public record TotalBalanceBreakdown(decimal Balance, Dictionary<string, AccountTypeBreakdown> AccountTypeBreakdowns);

        public record AccountTypeBreakdown(decimal Balance, decimal PercentageOfTotalBalance, List<AccountDto> Accounts);
        public record AccountDto(
            Guid Id,
            string AccountTypeCode,
            string Institution,
            string AccountName,
            decimal Balance,
            decimal PercentageOfTotalBalance,
            decimal PercentageOfAccountTypeBalance);

        /// <summary>
        /// Retrieves the account balance summary for the authenticated user.
        /// </summary>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="timeProvider">Time provider for consistent date calculations.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with net worth summary when successful.
        /// </returns>
        [Authorize]
        [WolverineGet("api/accounts/balanceSummary")]
        public static async Task<Ok<Response>> HandleAsync(
            AuthenticatedUser user,
            TimeProvider timeProvider,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var now = new DateTime(2025, 11, 25, 13, 26, 30);
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
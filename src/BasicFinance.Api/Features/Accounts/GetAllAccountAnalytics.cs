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

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Contains all logic associated with the <see cref="GetAllAccountAnalytics"/> Endpoint.
    /// </summary>
    public static class GetAllAccountAnalytics
    {
        /// <summary>
        /// Query parameters for the <see cref="GetAllAccountAnalytics"/> endpoint.
        /// </summary>
        /// <param name="RecordedDate"></param>
        /// <param name="TimePeriod"></param>
        public record Request(DateTimeOffset? RecordedDate, TimePeriod TimePeriod);

        /// <summary>
        /// Response Dto for the <see cref="GetAllAccountAnalytics"/> endpoint.
        /// </summary>
        /// <param name="CurrentPeriodBreakdown"></param>
        /// <param name="PreviousPeriodBreakdown"></param>
        public record Response(TotalBalanceBreakdown CurrentPeriodBreakdown, TotalBalanceBreakdown PreviousPeriodBreakdown);

        /// <summary>
        /// Dto representing the total balance breakdown for a given period.
        /// </summary>
        /// <param name="Balance"></param>
        /// <param name="AccountTypeBreakdowns"></param>
        public record TotalBalanceBreakdown(decimal Balance, Dictionary<string, AccountTypeBreakdown> AccountTypeBreakdowns);

        /// <summary>
        /// Dto representing the breakdown of balances for a specific account type.
        /// </summary>
        /// <param name="Balance"></param>
        /// <param name="PercentageOfTotalBalance"></param>
        /// <param name="Accounts"></param>
        public record AccountTypeBreakdown(decimal Balance, decimal PercentageOfTotalBalance, List<AccountDto> Accounts);

        /// <summary>
        /// Dto representing an individual account record.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="AccountTypeCode"></param>
        /// <param name="Institution"></param>
        /// <param name="AccountName"></param>
        /// <param name="Balance"></param>
        /// <param name="PercentageOfTotalBalance"></param>
        /// <param name="PercentageOfAccountTypeBalance"></param>
        public record AccountDto(
            Guid Id,
            string AccountTypeCode,
            string Institution,
            string AccountName,
            decimal Balance,
            decimal PercentageOfTotalBalance,
            decimal PercentageOfAccountTypeBalance);

        /// <summary>
        /// Internal dto representing account data used for calculations and breakdowns.
        /// </summary>
        /// <param name="AccountId"></param>
        /// <param name="AccountTypeCode"></param>
        /// <param name="Institution"></param>
        /// <param name="AccountName"></param>
        /// <param name="Balance"></param>
        /// <param name="AccountType"></param>
        /// <param name="BalanceRecordedDate"></param>
        private sealed record AccountData(
            Guid AccountId,
            string AccountTypeCode,
            string Institution,
            string AccountName,
            decimal Balance,
            AccountType AccountType,
            DateTimeOffset BalanceRecordedDate);

        /// <summary>
        /// Retrieves the account balance summary for the authenticated user.
        /// </summary>
        /// <param name="request">The request containing the recorded date and time period.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with net worth summary when successful.
        /// </returns>
        [Authorize]
        [WolverineGet("api/accounts/balanceSummary")]
        public static async Task<Ok<Response>> HandleAsync(
            [FromQuery] Request request,
            AuthenticatedUser user,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var recordedDate = request.RecordedDate ?? new DateTimeOffset(2025, 11, 25, 13, 26, 30, TimeSpan.Zero);
            var currentRange = recordedDate.ToPeriodRange(request.TimePeriod);
            var previousRange = recordedDate.ToPeriodRange(request.TimePeriod, -1);

            var currentPeriodAccountData = await dbContext.Accounts
                .AsNoTracking()
                .Where(a => a.UserId == user.Id)
                .Where(a => a.IsActive)
                .Where(a => a.BalanceRecordedDate >= currentRange.RangeStartDate && a.BalanceRecordedDate < currentRange.RangeEndDate)
                .Select(a => new AccountData(
                    a.AccountId,
                    a.AccountType.AccountTypeCode,
                    a.Institution,
                    a.AccountName,
                    a.Balance,
                    (AccountType)a.AccountTypeId,
                    a.BalanceRecordedDate
                ))
                .ToListAsync(cancellationToken);

            var latestDatesQuery = dbContext.AccountBalanceHistories
                .AsNoTracking()
                .Where(h => h.IsActive &&
                            h.BalanceRecordedDate >= previousRange.RangeStartDate &&
                            h.BalanceRecordedDate < previousRange.RangeEndDate &&
                            h.Account.UserId == user.Id &&
                            h.Account.IsActive)
                .GroupBy(h => h.AccountId)
                .Select(g => new { AccountId = g.Key, MaxDate = g.Max(h => h.BalanceRecordedDate) });

            var previousPeriodAccountData = await dbContext.AccountBalanceHistories
                .AsNoTracking()
                .Join(
                    latestDatesQuery,
                    h => new { h.AccountId, h.BalanceRecordedDate },
                    ld => new { ld.AccountId, BalanceRecordedDate = ld.MaxDate },
                    (h, ld) => h)
                .Select(h => new AccountData(
                    h.AccountId,
                    h.Account.AccountType.AccountTypeCode,
                    h.Account.Institution,
                    h.Account.AccountName,
                    h.Balance,
                    (AccountType)h.Account.AccountTypeId,
                    h.BalanceRecordedDate
                ))
                .ToListAsync(cancellationToken);

            var currentBreakdown = BuildBreakdown(currentPeriodAccountData);
            var previousBreakdown = BuildBreakdown(previousPeriodAccountData);

            return TypedResults.Ok(new Response(currentBreakdown, previousBreakdown));
        }

        /// <summary>
        /// Builds a total balance breakdown from a list of account data.
        /// </summary>
        /// <param name="accounts"></param>
        /// <returns></returns>
        private static TotalBalanceBreakdown BuildBreakdown(List<AccountData> accounts)
        {
            if (accounts.Count == 0)
            {
                return new(0m, []);
            }

            var netWorth = accounts.Sum(a => IsLiability(a.AccountType) ? -a.Balance : a.Balance);

            var breakdowns = accounts
                .GroupBy(a => a.AccountTypeCode)
                .Select(g =>
                {
                    var typeBalance = g.Sum(a => IsLiability(a.AccountType) ? -a.Balance : a.Balance);
                    var typeAbsoluteBalance = g.Sum(a => a.Balance);
                    var percentageOfTotal = netWorth != 0 ? (typeBalance / netWorth) * 100m : 0m;

                    var accountList = g.Select(a =>
                    {
                        var signedBalance = IsLiability(a.AccountType) ? -a.Balance : a.Balance;
                        var pctOfTotal = netWorth != 0 ? Math.Round((signedBalance / netWorth) * 100m, 0) : 0m;
                        var pctOfType = typeAbsoluteBalance != 0 ? Math.Round((a.Balance / typeAbsoluteBalance) * 100m, 0) : 0m;
                        return new AccountDto(
                            a.AccountId,
                            a.AccountTypeCode,
                            a.Institution,
                            a.AccountName,
                            a.Balance,
                            pctOfTotal,
                            pctOfType
                        );
                    }).ToList();

                    return (Key: g.Key, Breakdown: new AccountTypeBreakdown(typeBalance, percentageOfTotal, accountList));
                })
                .ToDictionary(x => x.Key, x => x.Breakdown);

            return new(netWorth, breakdowns);
        }

        /// <summary>
        /// Determines if the given account type is considered a liability.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool IsLiability(AccountType type) => type == AccountType.CreditCard;
    }
}
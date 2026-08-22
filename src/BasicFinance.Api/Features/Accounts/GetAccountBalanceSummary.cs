using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using BasicFinance.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Contains all logic associated with the <see cref="GetAccountBalanceSummary"/> Endpoint.
    /// </summary>
    public static class GetAccountBalanceSummary
    {
        /// <summary>
        /// Query parameters for the <see cref="GetAccountBalanceSummary"/> endpoint.
        /// </summary>
        /// <param name="RecordedDate">The anchor date used to resolve period boundaries. Defaults to now (UTC) when omitted.</param>
        /// <param name="TimePeriod">The period mode. Defaults to <see cref="TimePeriod.Monthly"/> when omitted. Invalid values result in a 400 response.</param>
        public record Request(DateTimeOffset? RecordedDate, TimePeriod? TimePeriod = null);

        /// <summary>
        /// Response Dto for the <see cref="GetAccountBalanceSummary"/> endpoint.
        /// </summary>
        /// <param name="CurrentPeriodBreakdown"></param>
        /// <param name="PreviousPeriodBreakdown"></param>
        /// <param name="CurrentPeriodStart">The first day of the current period (inclusive).</param>
        /// <param name="CurrentPeriodEnd">The first day excluded from the current period (exclusive).</param>
        /// <param name="PreviousPeriodStart">The first day of the previous period (inclusive).</param>
        /// <param name="PreviousPeriodEnd">The first day excluded from the previous period (exclusive).</param>
        public record Response(
            TotalBalanceBreakdown CurrentPeriodBreakdown,
            TotalBalanceBreakdown PreviousPeriodBreakdown,
            DateOnly CurrentPeriodStart,
            DateOnly CurrentPeriodEnd,
            DateOnly PreviousPeriodStart,
            DateOnly PreviousPeriodEnd);

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
        /// Retrieves the account balance summary for the authenticated user.
        /// Both periods are sourced from the most recent active balance history
        /// on or before each period's end (last known balances are carried forward).
        /// </summary>
        /// <param name="request">The request containing the recorded date and time period.</param>
        /// <param name="httpContext">The HTTP context used to inspect the raw time period query value.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="timeProvider">Time provider for consistent date calculations.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with net worth summary when successful,
        /// or <see cref="BadRequest{TValue}"/> when the time period is invalid.
        /// </returns>
        [Authorize]
        [WolverineGet("api/accounts/balanceSummary")]
        public static async Task<Results<Ok<Response>, BadRequest<string>>> HandleAsync(
            [FromQuery] Request request,
            HttpContext httpContext,
            AuthenticatedUser user,
            TimeProvider timeProvider,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            if (httpContext.Request.Query.TryGetValue("timePeriod", out var rawTimePeriod) &&
                !Enum.TryParse<TimePeriod>(rawTimePeriod, out _))
            {
                return TypedResults.BadRequest("Invalid time period.");
            }

            var resolution = (request.TimePeriod ?? TimePeriod.Monthly).ToPeriodResolution(request.RecordedDate, timeProvider.GetUtcNow());

            var scopedHistories = dbContext.AccountBalanceHistories
                .AsNoTracking()
                .Where(h => h.IsActive &&
                            h.Account.UserId == user.Id &&
                            h.Account.IsActive);

            var currentSnapshots = await AccountBalanceHistoryQueries.GetLatestSnapshotsOnOrBeforeAsync(
                scopedHistories,
                resolution.CurrentRange.RangeEndDate,
                cancellationToken);
            var previousSnapshots = await AccountBalanceHistoryQueries.GetLatestSnapshotsOnOrBeforeAsync(
                scopedHistories,
                resolution.PreviousRange.RangeEndDate,
                cancellationToken);

            var currentBreakdown = BuildBreakdown(currentSnapshots);
            var previousBreakdown = BuildBreakdown(previousSnapshots);

            return TypedResults.Ok(new Response(
                currentBreakdown,
                previousBreakdown,
                resolution.CurrentStart,
                resolution.CurrentEnd,
                resolution.PreviousStart,
                resolution.PreviousEnd));
        }

        /// <summary>
        /// Builds a total balance breakdown from a list of account snapshots.
        /// </summary>
        /// <param name="accounts"></param>
        /// <returns></returns>
        private static TotalBalanceBreakdown BuildBreakdown(List<AccountBalanceHistoryQueries.Snapshot> accounts)
        {
            if (accounts.Count == 0)
            {
                return new(0m, []);
            }

            var netWorth = accounts.Sum(a => AccountBalanceHistoryQueries.IsLiability(a.AccountType) ? -a.Balance : a.Balance);

            var breakdowns = accounts
                .GroupBy(a => a.AccountTypeCode)
                .Select(g =>
                {
                    var typeBalance = g.Sum(a => AccountBalanceHistoryQueries.IsLiability(a.AccountType) ? -a.Balance : a.Balance);
                    var typeAbsoluteBalance = g.Sum(a => a.Balance);
                    var percentageOfTotal = netWorth != 0 ? (typeBalance / netWorth) * 100m : 0m;

                    var accountList = g.Select(a =>
                    {
                        var signedBalance = AccountBalanceHistoryQueries.IsLiability(a.AccountType) ? -a.Balance : a.Balance;
                        var pctOfTotal = netWorth != 0 ? Math.Round((signedBalance / netWorth) * 100m, 0) : 0m;
                        var pctOfType = typeAbsoluteBalance != 0 ? Math.Round((a.Balance / typeAbsoluteBalance) * 100m, 0) : 0m;
                        return new AccountDto(
                            a.AccountId,
                            a.AccountTypeCode,
                            a.InstitutionName,
                            a.AccountName,
                            a.Balance,
                            pctOfTotal,
                            pctOfType
                        );
                    }).ToList();

                    return (g.Key, Breakdown: new AccountTypeBreakdown(typeBalance, percentageOfTotal, accountList));
                })
                .ToDictionary(x => x.Key, x => x.Breakdown);

            return new(netWorth, breakdowns);
        }
    }
}

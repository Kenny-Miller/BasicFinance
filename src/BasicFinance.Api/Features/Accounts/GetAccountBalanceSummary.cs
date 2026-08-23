using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using BasicFinance.Domain.Internal;
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
        /// Query parameters shared by the account summary endpoints.
        /// </summary>
        /// <param name="RecordedDate">The anchor date used to resolve period boundaries.</param>
        /// <param name="TimePeriod">The period mode.</param>
        public record Request(DateTimeOffset? RecordedDate = null, TimePeriod? TimePeriod = null);

        /// <summary>
        /// Response Dto for the <see cref="GetAccountBalanceSummary"/> endpoint.
        /// </summary>
        /// <param name="CurrentPeriodBreakdown"></param>
        /// <param name="PreviousPeriodBreakdown"></param>
        /// <param name="CurrentPeriodStart">The first day of the current period (inclusive).</param>
        /// <param name="CurrentPeriodEnd">The first day after the current period (exclusive).</param>
        /// <param name="PreviousPeriodStart">The first day of the previous period (inclusive).</param>
        /// <param name="PreviousPeriodEnd">The first day after the previous period (exclusive).</param>
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
        public record AccountTypeBreakdown(decimal Balance, decimal PercentageOfTotalBalance, List<AccountBalanceDto> Accounts);

        /// <summary>
        /// Dto representing an individual account's balance.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="AccountTypeCode"></param>
        /// <param name="Institution"></param>
        /// <param name="AccountName"></param>
        /// <param name="Balance"></param>
        /// <param name="PercentageOfTotalBalance"></param>
        /// <param name="PercentageOfAccountTypeBalance"></param>
        public record AccountBalanceDto(
            Guid Id,
            string AccountTypeCode,
            string Institution,
            string AccountName,
            decimal Balance,
            decimal PercentageOfTotalBalance,
            decimal PercentageOfAccountTypeBalance);

        /// <summary>
        /// Retrieves the account balance summary for the authenticated user.
        /// Both periods are sourced from the most recent balance ledger entry
        /// on or before each period's end (last known balances are carried forward),
        /// considering only accounts that were active at some point during each period.
        /// </summary>
        /// <param name="request">The request containing the recorded date and time period.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="timeProvider">Time provider for consistent date calculations.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with the account balance summary when successful.
        /// </returns>
        [Authorize]
        [WolverineGet("api/accounts/balanceSummary")]
        public static async Task<Ok<Response>> HandleAsync(
            [FromQuery] Request request,
            AuthenticatedUser user,
            TimeProvider timeProvider,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var timePeriod = request.TimePeriod ?? TimePeriod.Monthly;
            var recordedDate = request.RecordedDate ?? timeProvider.GetUtcNow();

            var currentRange = recordedDate.ToPeriodRange(timePeriod);
            var previousRange = recordedDate.ToPeriodRange(timePeriod, -1);

            var currentSnapshots = await PeriodSnapshots(dbContext, user.Id, currentRange)
                .ToListAsync(cancellationToken);
            var previousSnapshots = await PeriodSnapshots(dbContext, user.Id, previousRange)
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(new Response(
                BuildBreakdown(currentSnapshots),
                BuildBreakdown(previousSnapshots),
                DateOnly.FromDateTime(recordedDate.ToStartOfPeriod(timePeriod).Date),
                DateOnly.FromDateTime(recordedDate.ToStartOfPeriod(timePeriod, 1).Date),
                DateOnly.FromDateTime(recordedDate.ToStartOfPeriod(timePeriod, -1).Date),
                DateOnly.FromDateTime(recordedDate.ToStartOfPeriod(timePeriod).Date)));
        }

        /// <summary>
        /// Projects one snapshot per account that was active at some point
        /// within <paramref name="range"/>, carrying forward the most recent
        /// ledger entry on or before the range's end.
        /// </summary>
        private static IQueryable<Snapshot> PeriodSnapshots(
            AppDbContext dbContext,
            string userId,
            DateTimeOffsetRange range)
        {
            return dbContext.Accounts
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Where(x => x.Institution.IsActive)
                .Where(x => x.SystemCreatedDate < range.RangeEndDate &&
                            (x.IsActive ||
                             (x.SystemModifiedDate != null &&
                              x.SystemModifiedDate > range.RangeStartDate)))
                .Select(x => new Snapshot(
                    x.AccountId,
                    x.AccountName,
                    x.AccountType.AccountTypeCode,
                    x.Institution.Name,
                    x.Ledger.Where(l => l.BalanceRecordedDate <= range.RangeEndDate)
                        .OrderByDescending(l => l.BalanceRecordedDate)
                        .ThenByDescending(l => l.SystemCreatedDate)
                        .Select(l => l.Balance)
                        .FirstOrDefault(),
                    x.Ledger.Where(l => l.BalanceRecordedDate <= range.RangeEndDate)
                        .OrderByDescending(l => l.BalanceRecordedDate)
                        .ThenByDescending(l => l.SystemCreatedDate)
                        .Select(l => l.BalanceRecordedDate)
                        .FirstOrDefault()));
        }

        /// <summary>
        /// Builds a total balance breakdown from a list of account snapshots.
        /// </summary>
        /// <param name="snapshots">The account snapshots of a single period.</param>
        /// <returns>A total balance breakdown including account type breakdowns.</returns>
        private static TotalBalanceBreakdown BuildBreakdown(List<Snapshot> snapshots)
        {
            if (snapshots.Count == 0)
            {
                return new(0m, []);
            }

            var netWorth = snapshots.Sum(s => s.LatestBalance ?? 0m);

            var breakdowns = snapshots
                .GroupBy(s => s.AccountTypeCode)
                .Select(g =>
                {
                    var typeBalance = g.Sum(s => s.LatestBalance ?? 0m);
                    var percentageOfTotal = netWorth != 0 ? Math.Round((typeBalance / netWorth) * 100m, 0) : 0m;

                    var accountList = g.Select(s =>
                    {
                        var balance = s.LatestBalance ?? 0m;
                        var pctOfTotal = netWorth != 0 ? Math.Round((balance / netWorth) * 100m, 0) : 0m;
                        var pctOfType = typeBalance != 0 ? Math.Round((balance / typeBalance) * 100m, 0) : 0m;
                        return new AccountBalanceDto(
                            s.AccountId,
                            s.AccountTypeCode,
                            s.InstitutionName,
                            s.AccountName,
                            balance,
                            pctOfTotal,
                            pctOfType);
                    }).ToList();

                    return (g.Key, Breakdown: new AccountTypeBreakdown(typeBalance, percentageOfTotal, accountList));
                })
                .ToDictionary(x => x.Key, x => x.Breakdown);

            return new(netWorth, breakdowns);
        }

        private sealed record Snapshot(
            Guid AccountId,
            string AccountName,
            string AccountTypeCode,
            string InstitutionName,
            decimal? LatestBalance,
            DateTimeOffset? LatestBalanceRecordedDate);
    }
}

using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
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
        /// <param name="CurrentPeriodBreakdown">Total balances and breakdown for the period containing <paramref name="CurrentPeriodStart"/>.</param>
        /// <param name="PreviousPeriodBreakdown">Total balances and breakdown for the period immediately preceding <paramref name="CurrentPeriodStart"/>.</param>
        /// <param name="CurrentPeriodStart">Inclusive start date of the current period.</param>
        /// <param name="CurrentPeriodEnd">Exclusive end date of the current period.</param>
        /// <param name="PreviousPeriodStart">Inclusive start date of the previous period.</param>
        /// <param name="PreviousPeriodEnd">Exclusive end date of the previous period.</param>
        public record Response(
            TotalBalanceBreakdown CurrentPeriodBreakdown,
            TotalBalanceBreakdown PreviousPeriodBreakdown,
            DateOnly CurrentPeriodStart,
            DateOnly CurrentPeriodEnd,
            DateOnly PreviousPeriodStart,
            DateOnly PreviousPeriodEnd);

        /// <summary>
        /// Total balance and breakdown by account type for a period.
        /// </summary>
        /// <param name="Balance">Total balance across all accounts for the period.</param>
        /// <param name="AccountTypeBreakdowns">Breakdown of the total balance by account type, keyed by account type code.</param>
        public record TotalBalanceBreakdown(decimal Balance, Dictionary<string, AccountTypeBreakdown> AccountTypeBreakdowns);

        /// <summary>
        /// Balance and breakdown for a single account type within a period.
        /// </summary>
        /// <param name="Balance">Total balance of all accounts of this type for the period.</param>
        /// <param name="PercentageOfTotalBalance">This type's balance as a percentage of the total balance for the period.</param>
        /// <param name="Accounts">Breakdown of the type's balance by individual account.</param>
        public record AccountTypeBreakdown(decimal Balance, decimal PercentageOfTotalBalance, List<AccountBalanceDto> Accounts);

        /// <summary>
        /// Balance and percentage breakdown for a single account within a period.
        /// </summary>
        /// <param name="Id">The unique identifier of the account.</param>
        /// <param name="AccountTypeCode">The code of the account type.</param>
        /// <param name="Institution">The name of the institution the account belongs to.</param>
        /// <param name="AccountName">The name of the account.</param>
        /// <param name="Balance">The balance of the account on or before the period end.</param>
        /// <param name="PercentageOfTotalBalance">This account's balance as a percentage of the total balance for the period.</param>
        /// <param name="PercentageOfAccountTypeBalance">This account's balance as a percentage of the total balance for its account type.</param>
        public record AccountBalanceDto(
            Guid Id,
            string AccountTypeCode,
            string Institution,
            string AccountName,
            decimal Balance,
            decimal PercentageOfTotalBalance,
            decimal PercentageOfAccountTypeBalance);

        /// <summary>
        /// Retrieves the total balance breakdown for the user's active accounts for the current and
        /// previous period.
        /// Period totals are sourced from the most recent balance ledger entry
        /// on or before each period's end (last known balances are carried forward).
        /// Accounts without a ledger entry that far back are omitted from that period's breakdown.
        /// </summary>
        /// <param name="request">The request containing the recorded date and time period.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="timeProvider">Time provider for consistent date calculations.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with the balance summary when successful.
        /// Unrecognized time period values fall back to <see cref="TimePeriod.Monthly"/>.
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
            var anchor = request.RecordedDate ?? timeProvider.GetUtcNow();
            var recordedDate = new DateTimeOffset(anchor.Year, anchor.Month, anchor.Day, 0, 0, 0, TimeSpan.Zero);
            var currentPeriod = recordedDate.ToPeriodRange(timePeriod);
            var previousPeriod = recordedDate.ToPeriodRange(timePeriod, -1);

            var baseAccountsQuery = dbContext.Accounts
                .AsNoTracking()
                .Include(x => x.Institution)
                .Include(x => x.AccountType)
                .Where(x => x.Institution.IsActive)
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive);

            var currentPeriodQuery = baseAccountsQuery
                .Include(x => x.Ledger.Where(l => l.BalanceRecordedDate <= currentPeriod.RangeEndDate)
                    .OrderByDescending(l => l.BalanceRecordedDate)
                    .ThenByDescending(l => l.SystemCreatedDate)
                    .Take(1));

            var previousPeriodQuery = baseAccountsQuery
                .Include(x => x.Ledger.Where(l => l.BalanceRecordedDate <= previousPeriod.RangeEndDate)
                    .OrderByDescending(l => l.BalanceRecordedDate)
                    .ThenByDescending(l => l.SystemCreatedDate)
                    .Take(1));

            var results = await currentPeriodQuery
                .Join(
                    previousPeriodQuery,
                    current => current.AccountId,
                    previous => previous.AccountId,
                    (current, previous) => new { Current = current, Previous = previous })
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(new Response(
                BuildPeriodBreakdown(results.Select(r => ProjectPeriodSnapshot(r.Current))),
                BuildPeriodBreakdown(results.Select(r => ProjectPeriodSnapshot(r.Previous))),
                DateOnly.FromDateTime(recordedDate.ToStartOfPeriod(timePeriod).Date),
                DateOnly.FromDateTime(recordedDate.ToStartOfPeriod(timePeriod, 1).Date),
                DateOnly.FromDateTime(recordedDate.ToStartOfPeriod(timePeriod, -1).Date),
                DateOnly.FromDateTime(recordedDate.ToStartOfPeriod(timePeriod).Date)));
        }

        /// <summary>
        /// Period-level snapshot of a single account carrying the latest balance on or before the period
        /// end, as loaded by the filtered ledger include.
        /// </summary>
        /// <param name="AccountId">The unique identifier of the account.</param>
        /// <param name="AccountName">The name of the account.</param>
        /// <param name="AccountTypeCode">The code of the account type.</param>
        /// <param name="InstitutionName">The name of the institution the account belongs to.</param>
        /// <param name="LatestBalance">The latest balance on or before the period end, or <c>null</c> when the account has no ledger row that far back.</param>
        private sealed record PeriodSnapshot(
            Guid AccountId,
            string AccountName,
            string AccountTypeCode,
            string InstitutionName,
            decimal? LatestBalance);

        /// <summary>
        /// An account with a known latest balance on or before the period end.
        /// </summary>
        /// <param name="AccountId">The unique identifier of the account.</param>
        /// <param name="AccountName">The name of the account.</param>
        /// <param name="AccountTypeCode">The code of the account type.</param>
        /// <param name="InstitutionName">The name of the institution the account belongs to.</param>
        /// <param name="Balance">The latest balance on or before the period end.</param>
        private sealed record PeriodAccount(
            Guid AccountId,
            string AccountName,
            string AccountTypeCode,
            string InstitutionName,
            decimal Balance);

        /// <summary>
        /// Projects an account, whose filtered ledger include has loaded at most one row, into a period
        /// snapshot. Accounts with no ledger row on or before the period end project a <c>null</c> balance.
        /// </summary>
        /// <param name="account">The account with at most one ledger row loaded.</param>
        /// <returns>A period snapshot for the account.</returns>
        private static PeriodSnapshot ProjectPeriodSnapshot(Account account)
        {
            return new PeriodSnapshot(
                account.AccountId,
                account.AccountName,
                account.AccountType.AccountTypeCode,
                account.Institution.Name,
                account.Ledger.FirstOrDefault()?.Balance);
        }

        /// <summary>
        /// Aggregates period snapshots into a total balance breakdown by account type. Accounts without a
        /// balance on or before the period end are omitted.
        /// </summary>
        /// <param name="snapshots">The period snapshots of all active accounts.</param>
        /// <returns>The total balance breakdown for the period.</returns>
        private static TotalBalanceBreakdown BuildPeriodBreakdown(IEnumerable<PeriodSnapshot> snapshots)
        {
            var accounts = snapshots
                .Where(s => s.LatestBalance is not null)
                .Select(s => new PeriodAccount(s.AccountId, s.AccountName, s.AccountTypeCode, s.InstitutionName, s.LatestBalance!.Value))
                .ToList();
            if (accounts.Count == 0)
            {
                return new TotalBalanceBreakdown(0m, []);
            }

            var netWorth = accounts.Sum(s => s.Balance);
            var accountTypeBreakdowns = accounts
                .GroupBy(s => s.AccountTypeCode)
                .Select(group =>
                {
                    var typeBalance = group.Sum(s => s.Balance);
                    var typePercentageOfTotalBalance = netWorth != 0 ? typeBalance / netWorth * 100m : 0m;
                    var typeAccounts = group
                        .Select(s =>
                        {
                            var balance = s.Balance;
                            var percentageOfTotalBalance = netWorth != 0 ? Math.Round(balance / netWorth * 100m, 0) : 0m;
                            var percentageOfAccountTypeBalance = typeBalance != 0
                                ? Math.Round(Math.Abs(balance) / Math.Abs(typeBalance) * 100m, 0)
                                : 0m;
                            return new AccountBalanceDto(
                                s.AccountId,
                                s.AccountTypeCode,
                                s.InstitutionName,
                                s.AccountName,
                                balance,
                                percentageOfTotalBalance,
                                percentageOfAccountTypeBalance);
                        })
                        .ToList();
                    return KeyValuePair.Create(group.Key, new AccountTypeBreakdown(typeBalance, typePercentageOfTotalBalance, typeAccounts));
                })
                .ToDictionary();

            return new TotalBalanceBreakdown(netWorth, accountTypeBreakdowns);
        }
    }
}

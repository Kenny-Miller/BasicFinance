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

        private sealed class AccountTypeAccumulator
        {
            public decimal Total { get; set; }
            public List<AccountDto> Accounts { get; } = [];
        }

        /// <summary>
        /// Retrieves the total balance breakdown for the user's active accounts for the current and
        /// previous period.
        /// Period totals are sourced from the most recent balance ledger entry
        /// on or before each period's end (last known balances are carried forward).
        /// Accounts without a ledger entry that far back appear in that period's
        /// breakdown with a zero balance.
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
                .Where(x => x.AccountType.IsActive)
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive)
                .ProjectToComparison(currentPeriod.RangeEndDate, previousPeriod.RangeEndDate);

            var results = await baseAccountsQuery.ToListAsync(cancellationToken);
            var currentItems = new List<AccountDto>();
            var previousItems = new List<AccountDto>();
            foreach (var res in results)
            {
                currentItems.Add(Queries.ToAccountDto(res.Account, res.CurrentPeriodLatestLedger, currentPeriod.RangeEndDate));
                previousItems.Add(Queries.ToAccountDto(res.Account, res.PreviousPeriodLatestLedger, previousPeriod.RangeEndDate));
            }

            var accountTypeCodes = await dbContext.AccountTypes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => x.AccountTypeCode)
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(new Response(
                BuildPeriodBreakdown(currentItems, accountTypeCodes),
                BuildPeriodBreakdown(previousItems, accountTypeCodes),
                DateOnly.FromDateTime(currentPeriod.RangeStartDate.Date),
                DateOnly.FromDateTime(recordedDate.ToStartOfPeriod(timePeriod, 1).Date),
                DateOnly.FromDateTime(previousPeriod.RangeStartDate.Date),
                DateOnly.FromDateTime(currentPeriod.RangeStartDate.Date)));
        }

        /// <summary>
        /// Aggregates period snapshots into a total balance breakdown by account type. Every active
        /// account type code is included, with zero balances for types that have no account snapshots.
        /// </summary>
        /// <param name="accounts">Account snapshots with their latest balance at the specified point in time.</param>
        /// <param name="accountTypeCodes"></param>
        /// <returns>The total balance breakdown for the period.</returns>
        private static TotalBalanceBreakdown BuildPeriodBreakdown(List<AccountDto> accounts, List<string> accountTypeCodes)
        {
            var totalNetworth = 0m;
            var accountTypeCodeDict = accountTypeCodes.ToDictionary(x => x, x => new AccountTypeAccumulator());
            foreach (var account in accounts)
            {
                var accountTypeCodeEntry = accountTypeCodeDict[account.AccountTypeCode];
                accountTypeCodeEntry.Accounts.Add(account);
                accountTypeCodeEntry.Total += account.LatestBalance;
                totalNetworth += account.LatestBalance;
            }

            var results = accountTypeCodeDict.ToDictionary(x => x.Key, x =>
            {
                var typePercentageOfTotalBalance = totalNetworth != 0 ? x.Value.Total / totalNetworth * 100m : 0m;
                var balanceDtos = x.Value.Accounts.Select(y =>
                {
                    var percentageOfTotalBalance = totalNetworth != 0 ? Math.Round(Math.Abs(y.LatestBalance / totalNetworth * 100m), 0) : 0m;
                    var percentageOfAccountTypeBalance = x.Value.Total != 0 ? Math.Round(Math.Abs(y.LatestBalance / x.Value.Total * 100m), 0) : 0m;

                    return new AccountBalanceDto(
                                y.Id,
                                y.AccountTypeCode,
                                y.InstitutionName,
                                y.Name,
                                y.LatestBalance,
                                percentageOfTotalBalance,
                                percentageOfAccountTypeBalance);
                });

                return new AccountTypeBreakdown(x.Value.Total, typePercentageOfTotalBalance, [.. balanceDtos]);
            });

            return new TotalBalanceBreakdown(totalNetworth, results);
        }
    }
}

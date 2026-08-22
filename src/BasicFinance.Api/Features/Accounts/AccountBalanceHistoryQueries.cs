using BasicFinance.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using AccountType = BasicFinance.Infrastructure.Enums.AccountType;

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Shared query helpers for <see cref="AccountBalanceHistory"/> data used
    /// by balance summary endpoints.
    /// </summary>
    internal static class AccountBalanceHistoryQueries
    {
        /// <summary>
        /// A snapshot of an account's balance as of a period boundary.
        /// </summary>
        /// <param name="AccountId"></param>
        /// <param name="AccountTypeCode"></param>
        /// <param name="InstitutionName"></param>
        /// <param name="AccountName"></param>
        /// <param name="Balance"></param>
        /// <param name="AccountType"></param>
        /// <param name="BalanceRecordedDate"></param>
        public sealed record Snapshot(
            Guid AccountId,
            string AccountTypeCode,
            string InstitutionName,
            string AccountName,
            decimal Balance,
            AccountType AccountType,
            DateTimeOffset BalanceRecordedDate);

        /// <summary>
        /// Projects the most recent active balance history row on or before
        /// <paramref name="rangeEnd"/> for each account in
        /// <paramref name="scopedHistories"/>. Accounts with no matching
        /// history row are excluded.
        /// </summary>
        /// <param name="scopedHistories">
        /// A pre-scoped query over <see cref="AccountBalanceHistory"/> rows owned by the caller (e.g. filtered by user or account ids).
        /// </param>
        /// <param name="rangeEnd">The upper bound (inclusive) for <see cref="AccountBalanceHistory.BalanceRecordedDate"/>.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// One <see cref="Snapshot"/> per account that has an active history row on or before <paramref name="rangeEnd"/>.
        /// </returns>
        public static async Task<List<Snapshot>> GetLatestSnapshotsOnOrBeforeAsync(
            IQueryable<AccountBalanceHistory> scopedHistories,
            DateTimeOffset rangeEnd,
            CancellationToken cancellationToken)
        {
            var historiesOnOrBeforeEnd = scopedHistories.Where(h => h.BalanceRecordedDate <= rangeEnd);
            var latestDateByAccount = historiesOnOrBeforeEnd
                .GroupBy(h => h.AccountId)
                .Select(g => new { AccountId = g.Key, MaxDate = g.Max(h => h.BalanceRecordedDate) });

            return await historiesOnOrBeforeEnd
                .Join(
                    latestDateByAccount,
                    h => new { h.AccountId, h.BalanceRecordedDate },
                    ld => new { ld.AccountId, BalanceRecordedDate = ld.MaxDate },
                    (h, _) => h)
                .Select(h => new Snapshot(
                    h.AccountId,
                    h.Account.AccountType.AccountTypeCode,
                    h.Account.Institution.Name,
                    h.Account.AccountName,
                    h.Balance,
                    (AccountType)h.Account.AccountTypeId,
                    h.BalanceRecordedDate))
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Determines if the given account type is considered a liability.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsLiability(AccountType type) => type == AccountType.CreditCard;
    }
}

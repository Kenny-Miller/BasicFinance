using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using BasicFinance.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using AccountType = BasicFinance.Infrastructure.Enums.AccountType;

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Shared query helpers for <see cref="AccountLedger"/> data used
    /// by balance summary endpoints.
    /// </summary>
    internal static class AccountLedgerQueries
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
        /// Projects the most recent ledger row on or before
        /// <paramref name="rangeEnd"/> for each account in
        /// <paramref name="scopedLedger"/> (last known balances are carried
        /// forward). Accounts with no matching ledger row are excluded.
        /// </summary>
        /// <param name="scopedLedger">
        /// A pre-scoped query over <see cref="AccountLedger"/> rows owned by the caller (e.g. filtered by user or account ids).
        /// </param>
        /// <param name="rangeEnd">The upper bound (inclusive) for <see cref="AccountLedger.BalanceRecordedDate"/>.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// One <see cref="Snapshot"/> per account that has a ledger row on or before <paramref name="rangeEnd"/>.
        /// </returns>
        public static async Task<List<Snapshot>> GetLatestSnapshotsOnOrBeforeAsync(
            IQueryable<AccountLedger> scopedLedger,
            DateTimeOffset rangeEnd,
            CancellationToken cancellationToken)
        {
            return await scopedLedger
                .WithLatestAsOf(rangeEnd)
                .Select(l => new Snapshot(
                    l.AccountId,
                    l.Account.AccountType.AccountTypeCode,
                    l.Account.Institution.Name,
                    l.Account.AccountName,
                    l.Balance,
                    (AccountType)l.Account.AccountTypeId,
                    l.BalanceRecordedDate))
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Projects the most recent <see cref="AccountLedger"/> entry per account
        /// for all of the given user's accounts.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="userId"></param>
        public static IQueryable<AccountLedger> LatestPerAccountForUser(AppDbContext dbContext, string userId)
        {
            return dbContext.AccountLedgers
                .AsNoTracking()
                .Where(l => l.Account.UserId == userId)
                .WithLatestPerAccount();
        }

        /// <summary>
        /// Left-joins each account to its most recent ledger entry,
        /// defaulting the balance fields when the account has no ledger entry yet.
        /// </summary>
        /// <param name="accounts"></param>
        /// <param name="latestLedger"></param>
        public static IQueryable<AccountListItem> WithLatestLedger(this IQueryable<Account> accounts, IQueryable<AccountLedger> latestLedger)
        {
            return
                from a in accounts
                join lg in latestLedger on a.AccountId equals lg.AccountId into joined
                from lg in joined.DefaultIfEmpty()
                select new AccountListItem(
                    a.AccountId,
                    a.AccountName,
                    a.AccountType.AccountTypeCode,
                    a.Institution.Name,
                    lg != null ? lg.Balance : 0m,
                    lg != null ? lg.BalanceRecordedDate : DateTimeOffset.MinValue);
        }

        /// <summary>
        /// Determines if the given account type is considered a liability.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsLiability(AccountType type) => type == AccountType.CreditCard;
    }
}

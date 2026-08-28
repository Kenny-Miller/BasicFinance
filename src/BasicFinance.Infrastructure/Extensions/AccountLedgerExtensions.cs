using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Infrastructure.Extensions
{
    /// <summary>
    /// Shared query helpers for <see cref="AccountLedger"/> (balance log) scoping.
    /// </summary>
    public static class AccountLedgerExtensions
    {
        extension(IQueryable<AccountLedger> source)
        {
            /// <summary>
            /// Projects the single most recent entry per account,
            /// ordered by <see cref="AccountLedger.BalanceRecordedDate"/> with
            /// <see cref="AccountLedger.SystemCreatedDate"/> as a tie-breaker.
            /// </summary>
            public IQueryable<AccountLedger> WithLatestPerAccount()
            {
                return source.Where(l => !source.Any(l2 =>
                    l2.AccountId == l.AccountId &&
                    (l2.BalanceRecordedDate > l.BalanceRecordedDate ||
                     (l2.BalanceRecordedDate == l.BalanceRecordedDate &&
                      l2.SystemCreatedDate > l.SystemCreatedDate))));
            }
        }
    }
}

using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Infrastructure.Extensions
{
    /// <summary>
    /// Shared query helpers for <see cref="AccountLedger"/> (balance log) scoping
    /// and carry-forward "latest value" queries.
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

            /// <summary>
            /// Projects the single most recent entry per account on or before
            /// <paramref name="asOf"/>, so last known balances are carried forward
            /// when no entry exists for the exact date.
            /// </summary>
            /// <param name="asOf">The upper bound (inclusive) for <see cref="AccountLedger.BalanceRecordedDate"/>.</param>
            public IQueryable<AccountLedger> WithLatestAsOf(DateTimeOffset asOf)
            {
                var onOrBefore = source.Where(l => l.BalanceRecordedDate <= asOf);

                return onOrBefore.Where(l => !onOrBefore.Any(l2 =>
                    l2.AccountId == l.AccountId &&
                    (l2.BalanceRecordedDate > l.BalanceRecordedDate ||
                     (l2.BalanceRecordedDate == l.BalanceRecordedDate &&
                      l2.SystemCreatedDate > l.SystemCreatedDate))));
            }

            /// <summary>
            /// Narrows ledger entries to those whose account was active at some point
            /// within the period (half-open range
            /// [<paramref name="rangeStart"/>, <paramref name="rangeEnd"/>)).
            /// </summary>
            /// <param name="rangeStart">The inclusive period start.</param>
            /// <param name="rangeEnd">The exclusive period end.</param>
            public IQueryable<AccountLedger> WhereAccountActiveDuring(DateTimeOffset rangeStart, DateTimeOffset rangeEnd)
            {
                return source.Where(l => l.Account.SystemCreatedDate < rangeEnd &&
                                         (l.Account.IsActive ||
                                          (l.Account.SystemModifiedDate != null &&
                                           l.Account.SystemModifiedDate > rangeStart)));
            }
        }
    }
}

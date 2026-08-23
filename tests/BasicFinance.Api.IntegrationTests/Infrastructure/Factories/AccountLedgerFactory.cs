using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Api.IntegrationTests.Infrastructure.Factories;

public static class AccountLedgerFactory
{
    /// <summary>
    /// Creates an <see cref="AccountLedger"/> entry for the given account.
    /// </summary>
    /// <param name="account">The account the balance entry belongs to.</param>
    /// <param name="balance">The balance of the account.</param>
    /// <param name="balanceRecordedDate">The date the balance reflects account state as of. Defaults to now (UTC).</param>
    /// <param name="systemCreatedDate">
    /// When the entry was created. Defaults to a deterministic moment after the account's
    /// existing ledger entries, so a seeded entry is always the latest regardless of clock resolution.
    /// Pass explicit, distinct values when seeding multiple entries for one account
    /// and the creation order between them matters.
    /// </param>
    public static AccountLedger CreateFor(
        Account account,
        decimal balance,
        DateTimeOffset? balanceRecordedDate = null,
        DateTimeOffset? systemCreatedDate = null)
    {
        return new AccountLedger(account, balance, balanceRecordedDate ?? DateTimeOffset.UtcNow)
        {
            SystemCreatedDate = systemCreatedDate
                ?? account.Ledger.Max(l => l.SystemCreatedDate).AddMilliseconds(1)
        };
    }
}

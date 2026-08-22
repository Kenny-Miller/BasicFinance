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
    public static AccountLedger CreateFor(
        Account account,
        decimal balance,
        DateTimeOffset? balanceRecordedDate = null)
    {
        return new AccountLedger(account, balance, balanceRecordedDate ?? DateTimeOffset.UtcNow);
    }
}

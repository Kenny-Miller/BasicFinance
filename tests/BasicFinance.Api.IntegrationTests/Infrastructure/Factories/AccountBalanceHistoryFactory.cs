using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Api.IntegrationTests.Infrastructure.Factories;

public static class AccountBalanceHistoryFactory
{
    /// <summary>
    /// Creates an <see cref="AccountBalanceHistory"/> row for the given account.
    /// Defaults to the account's current balance and balance recorded date.
    /// </summary>
    public static AccountBalanceHistory CreateFor(
        Account account,
        decimal? balance = null,
        DateTimeOffset? balanceRecordedDate = null)
    {
        var history = new AccountBalanceHistory(account);

        if (balance.HasValue)
        {
            history.Balance = balance.Value;
        }

        if (balanceRecordedDate.HasValue)
        {
            history.BalanceRecordedDate = balanceRecordedDate.Value;
        }

        return history;
    }
}

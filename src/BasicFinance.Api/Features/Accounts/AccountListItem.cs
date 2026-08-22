namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Projection of an <see cref="Infrastructure.Entities.Account"/> joined to its
    /// most recent <see cref="Infrastructure.Entities.AccountLedger"/> entry,
    /// used as the sortable basis for account listing endpoints.
    /// </summary>
    /// <param name="AccountId">The unique identifier of the account.</param>
    /// <param name="AccountName">The name of the account.</param>
    /// <param name="AccountTypeCode">The account type code.</param>
    /// <param name="Institution">The name of the associated institution.</param>
    /// <param name="Balance">The most recent balance (zero when no ledger entry exists yet).</param>
    /// <param name="BalanceRecordedDate">The recorded date of the most recent ledger entry.</param>
    internal sealed record AccountListItem(
        Guid AccountId,
        string AccountName,
        string AccountTypeCode,
        string Institution,
        decimal Balance,
        DateTimeOffset BalanceRecordedDate);
}

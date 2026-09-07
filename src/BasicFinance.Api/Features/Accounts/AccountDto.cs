using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Dto containing <see cref="Account"/> data.
    /// </summary>
    /// <param name="Id">The unique identifier of the account.</param>
    /// <param name="Name">The name of the account.</param>
    /// <param name="AccountTypeCode">The code of the account type.</param>
    /// <param name="AccountTypeName">The display name of the account type.</param>
    /// <param name="InstitutionCode">The code of the institution.</param>
    /// <param name="InstitutionName">The display name of the institution.</param>
    /// <param name="Currency">The ISO 4217 currency code of the account.</param>
    /// <param name="IsLiability">Whether the account type is a liability (balances are stored as negative values).</param>
    /// <param name="LatestBalance">The latest ledger balance on or before the reference date, or <c>0</c> when the account has no ledger entry that far back.</param>
    /// <param name="LatestBalanceRecordedDate">The recorded date of the latest ledger balance on or before the reference date, or the fallback reference date when the account has no ledger entry that far back.</param>
    public record AccountDto(
        Guid Id,
        string Name,
        string AccountTypeCode,
        string AccountTypeName,
        string InstitutionCode,
        string InstitutionName,
        string Currency,
        bool IsLiability,
        decimal LatestBalance,
        DateTimeOffset LatestBalanceRecordedDate);
}

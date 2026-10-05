using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Api.Features.AccountTypes
{
    /// <summary>
    /// Dto containing <see cref="AccountType"/> data.
    /// </summary>
    /// <param name="Id">The unique identifier of the account type.</param>
    /// <param name="Code">The code of the account type.</param>
    /// <param name="Name">The display name of the account type.</param>
    /// <param name="IsLiability">Whether the account type is a liability (balances are stored as negative values).</param>
    public record AccountTypeDto(
        int Id,
        string Code,
        string Name,
        bool IsLiability);
}

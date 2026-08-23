using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Dto containing <see cref="Account"/> data.
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="Name"></param>
    /// <param name="AccountTypeCode"></param>
    /// <param name="InstitutionCode"></param>
    /// <param name="LatestBalance"></param>
    /// <param name="LatestBalanceRecordedDate"></param>
    public record AccountDto(Guid Id, string Name, string AccountTypeCode, string InstitutionCode, decimal LatestBalance, DateTimeOffset LatestBalanceRecordedDate);
}

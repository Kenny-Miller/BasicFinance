using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Dto containing <see cref="Account"/> data.
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="Name"></param>
    /// <param name="AccountTypeCode"></param>
    /// <param name="Institution"></param>
    /// <param name="Balance"></param>
    /// <param name="BalanceRecordedDate"></param>
    public record AccountDto(Guid Id, string Name, string AccountTypeCode, string Institution, decimal Balance, DateTimeOffset BalanceRecordedDate);
}

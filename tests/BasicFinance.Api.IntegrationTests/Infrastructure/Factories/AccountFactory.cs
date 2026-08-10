using BasicFinance.Infrastructure.Entities;
using AccountTypeEnum = BasicFinance.Infrastructure.Enums.AccountType;

namespace BasicFinance.Api.IntegrationTests.Infrastructure.Factories;

public static class AccountFactory
{
    private static readonly Guid TestUserGoogleSpreadsheetId = new("00000000-0000-0000-0000-000000000001");

    public static Account Create(
        string userId,
        AccountTypeEnum accountType = AccountTypeEnum.Checking,
        string accountName = "Test Checking",
        decimal balance = 1000.00m,
        string currency = "USD",
        string? notes = null,
        int institutionId = 1,
        Guid? financialAccountId = null,
        DateTimeOffset? balanceRecordedDate = null)
    {
        return new Account(
            TestUserGoogleSpreadsheetId,
            accountType,
            userId,
            accountName,
            balance,
            currency,
            notes ?? string.Empty,
            institutionId,
            financialAccountId ?? Guid.NewGuid(),
            balanceRecordedDate ?? DateTimeOffset.UtcNow);
    }
}

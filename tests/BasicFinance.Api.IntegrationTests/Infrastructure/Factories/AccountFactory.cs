using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Infrastructure.Entities;
using AccountTypeEnum = BasicFinance.Infrastructure.Enums.AccountType;

namespace BasicFinance.Api.IntegrationTests.Infrastructure.Factories;

public static class AccountFactory
{
    public static Account Create(
        string userId,
        AccountTypeEnum accountType = AccountTypeEnum.Checking,
        string accountName = "Test Checking",
        string currency = "USD",
        string? notes = null,
        int institutionId = TestConstants.WellsFargoInstitutionId,
        Guid? financialAccountId = null,
        DateTimeOffset? systemCreatedDate = null,
        decimal openingBalance = 0m,
        DateTimeOffset? openingBalanceRecordedDate = null)
    {
        return new Account(
            TestConstants.TestUserGoogleSpreadsheetId,
            accountType,
            userId,
            accountName,
            currency,
            notes ?? string.Empty,
            institutionId,
            financialAccountId ?? Guid.NewGuid(),
            openingBalance,
            // Default to a date before any seeded ledger history so that
            // explicitly seeded ledger entries remain the latest.
            openingBalanceRecordedDate ?? DateTimeOffset.UnixEpoch)
        {
            SystemCreatedDate = systemCreatedDate ?? DateTimeOffset.UtcNow
        };
    }

    public static IEnumerable<Account> CreateBatch(
        int count,
        string userId,
        string namePrefix = "Account",
        AccountTypeEnum accountType = AccountTypeEnum.Checking,
        int institutionId = TestConstants.WellsFargoInstitutionId)
    {
        for (var i = 0; i < count; i++)
        {
            yield return Create(
                userId,
                accountType: accountType,
                accountName: $"{namePrefix} {i}",
                institutionId: institutionId);
        }
    }
}

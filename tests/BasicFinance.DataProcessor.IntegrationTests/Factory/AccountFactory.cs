using BasicFinance.Infrastructure.Entities;
using AccountTypeEnum = BasicFinance.Infrastructure.Enums.AccountType;

namespace BasicFinance.DataProcessor.IntegrationTests.Factory
{
    public static class AccountFactory
    {
        public static Account Create(
            Guid userGoogleSpreadsheetId,
            AccountTypeEnum accountType = AccountTypeEnum.Checking,
            string userId = "test-user-id",
            string accountName = "Test Checking",
            decimal balance = 1000.00m,
            string currency = "USD",
            string? notes = null,
            int institutionId = 1,
            Guid financialAccountId = default,
            DateTimeOffset? balanceRecordedDate = null)
        {
            return new Account(
                userGoogleSpreadsheetId,
                accountType,
                userId,
                accountName,
                balance,
                currency,
                notes ?? string.Empty,
                institutionId,
                financialAccountId == default ? Guid.NewGuid() : financialAccountId,
                balanceRecordedDate ?? DateTimeOffset.UtcNow);
        }
    }
}
using BasicFinance.Infrastructure.Entities;
using AccountTypeEnum = BasicFinance.Infrastructure.Enums.AccountType;

namespace BasicFinance.DataProcessor.IntegrationTests.Helpers
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
            Guid institutionId = default,
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
                institutionId == default ? Guid.NewGuid() : institutionId,
                financialAccountId == default ? Guid.NewGuid() : financialAccountId,
                balanceRecordedDate ?? DateTimeOffset.UtcNow);
        }
    }
}

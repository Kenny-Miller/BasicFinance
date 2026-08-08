using BasicFinance.Infrastructure.Entities;
using TransactionCategoryEnum = BasicFinance.Infrastructure.Enums.TransactionCategory;
using TransactionTypeEnum = BasicFinance.Infrastructure.Enums.TransactionType;

namespace BasicFinance.DataProcessor.IntegrationTests.Factory
{
    public static class TransactionFactory
    {
        public static Transaction Create(
            string userId = "test-user-id",
            Guid accountId = default,
            long financialTransactionId = 0,
            TransactionTypeEnum transactionType = TransactionTypeEnum.Debit,
            TransactionCategoryEnum transactionCategory = TransactionCategoryEnum.Uncategorized,
            DateTimeOffset? date = null,
            decimal amount = 100.00m,
            string description = "Test Transaction")
        {
            return new Transaction(
                userId,
                accountId == default ? Guid.NewGuid() : accountId,
                financialTransactionId == 0 ? 1234567890L : financialTransactionId,
                transactionType,
                transactionCategory,
                date ?? DateTimeOffset.UtcNow,
                amount,
                description);
        }
    }
}
using BasicFinance.Infrastructure.Entities;
using TransactionCategoryEnum = BasicFinance.Infrastructure.Enums.TransactionCategory;
using TransactionTypeEnum = BasicFinance.Infrastructure.Enums.TransactionType;

namespace BasicFinance.Api.IntegrationTests.Infrastructure.Factories;

public static class TransactionFactory
{
    public static Transaction Create(
        string userId,
        Guid accountId,
        TransactionTypeEnum transactionType = TransactionTypeEnum.Debit,
        TransactionCategoryEnum transactionCategory = TransactionCategoryEnum.Uncategorized,
        DateTimeOffset? date = null,
        decimal amount = 100.00m,
        string description = "Test Transaction")
    {
        return new Transaction(
            userId,
            accountId,
            1234567890L,
            transactionType,
            transactionCategory,
            date ?? DateTimeOffset.UtcNow,
            amount,
            description);
    }
}

using BasicFinance.Infrastructure.Entities;
using TransactionCategoryEnum = BasicFinance.Infrastructure.Enums.TransactionCategory;
using TransactionTypeEnum = BasicFinance.Infrastructure.Enums.TransactionType;

namespace BasicFinance.Api.IntegrationTests.Infrastructure.Factories;

public static class TransactionFactory
{
    /// <summary>
    /// Creates a new <see cref="Transaction"/> entity.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="accountId"></param>
    /// <param name="transactionType"></param>
    /// <param name="transactionCategory"></param>
    /// <param name="date"></param>
    /// <param name="amount"></param>
    /// <param name="description"></param>
    /// <returns></returns>
    public static Transaction Create(
        string userId,
        Guid accountId,
        TransactionTypeEnum transactionType = TransactionTypeEnum.Debit,
        TransactionCategoryEnum transactionCategory = TransactionCategoryEnum.Uncategorized,
        DateTimeOffset? date = null,
        decimal amount = 100.00m,
        string description = "Test Transaction")
    {
        return new(
            userId,
            accountId,
            1234567890L,
            transactionType,
            transactionCategory,
            date ?? DateTimeOffset.UtcNow,
            amount,
            description);
    }

    /// <summary>
    /// Creates an <see cref="IEnumerable{T}"/> of <see cref="Transaction"/>s yielding <paramref name="amount"/> items.
    /// </summary>
    /// <param name="count"></param>
    /// <param name="userId"></param>
    /// <param name="accountId"></param>
    /// <param name="descriptionPrefix"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public static IEnumerable<Transaction> CreateBatch(
        int count,
        string userId,
        Guid accountId,
        string descriptionPrefix = "Transaction",
        decimal amount = 100.00m)
    {
        for (var i = 0; i < count; i++)
        {
            yield return Create(
                userId,
                accountId,
                description: $"{descriptionPrefix} {i}",
                amount: amount);
        }
    }
}

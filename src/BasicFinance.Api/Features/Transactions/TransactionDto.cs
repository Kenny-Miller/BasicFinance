using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Api.Features.Transactions
{
    /// <summary>
    /// Dto containing <see cref="Transaction"/> data.
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="TransactionTypeName"></param>
    /// <param name="TransactionCategoryName"></param>
    /// <param name="AccountName"></param>
    /// <param name="Date"></param>
    /// <param name="Amount"></param>
    /// <param name="Description"></param>
    public record TransactionDto(
        Guid Id,
        string TransactionTypeName,
        string TransactionCategoryName,
        string AccountName,
        DateTimeOffset Date,
        decimal Amount,
        string Description);
}

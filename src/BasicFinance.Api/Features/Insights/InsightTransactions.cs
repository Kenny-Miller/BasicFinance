using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using TransactionCategoryEnum = BasicFinance.Infrastructure.Enums.TransactionCategory;

namespace BasicFinance.Api.Features.Insights;

/// <summary>
/// Shared transaction queries used by the cash flow and spending insights endpoints.
/// </summary>
internal static class InsightTransactions
{
    /// <summary>
    /// Categories that move money between the user's own accounts (or are explicitly ignored). They are neither
    /// income nor spend, and would otherwise be counted twice (once leaving one account, once arriving in another).
    /// </summary>
    public static readonly int[] ExcludedCategoryIds =
    [
        (int)TransactionCategoryEnum.InternalTransfer,
        (int)TransactionCategoryEnum.SavingsTransfer,
        (int)TransactionCategoryEnum.CreditCardPayment,
        (int)TransactionCategoryEnum.Ignore,
    ];

    /// <summary>
    /// Returns the user's active transactions on active accounts at active institutions, excluding transfer
    /// categories, optionally limited to a single institution.
    /// </summary>
    /// <param name="dbContext">Application <see cref="AppDbContext"/>.</param>
    /// <param name="userId">The id of the user who owns the transactions.</param>
    /// <param name="institutionId">Optionally limits the transactions to accounts at a single institution.</param>
    /// <returns>An untracked, filterable query of the matching transactions.</returns>
    public static IQueryable<Transaction> ForUser(AppDbContext dbContext, string userId, int? institutionId) =>
        dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Where(t => t.IsActive)
            .Where(t => t.Account.IsActive)
            .Where(t => t.Account.Institution.IsActive)
            .Where(t => !institutionId.HasValue || t.Account.InstitutionId == institutionId.Value)
            .Where(t => !ExcludedCategoryIds.Contains(t.TransactionCategoryId));
}

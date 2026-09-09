using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.TransactionCategories
{
    /// <summary>
    /// Contains all logic associated with the <see cref="ListTransactionCategories"/> Endpoint.
    /// </summary>
    public static class ListTransactionCategories
    {
        /// <summary>
        /// Retrieves the active <see cref="TransactionCategory"/>s.
        /// </summary>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a list of <see cref="TransactionCategoryDto"/> when successful.
        /// </returns>
        [Authorize]
        [WolverineGet("api/transaction-categories/")]
        public static async Task<Ok<List<TransactionCategoryDto>>> HandleAsync(
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var transactionCategories = await dbContext.TransactionCategories
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.TransactionCategoryName)
                .Select(x => new TransactionCategoryDto(x.TransactionCategoryId, x.TransactionCategoryCode, x.TransactionCategoryName))
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(transactionCategories);
        }
    }
}

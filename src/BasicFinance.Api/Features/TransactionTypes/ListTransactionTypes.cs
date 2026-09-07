using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.TransactionTypes
{
    /// <summary>
    /// Contains all logic associated with the <see cref="ListTransactionTypes"/> Endpoint.
    /// </summary>
    public static class ListTransactionTypes
    {
        /// <summary>
        /// Retrieves the active <see cref="TransactionType"/>s.
        /// </summary>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with a list of <see cref="TransactionTypeDto"/> when successful.
        /// </returns>
        [Authorize]
        [WolverineGet("api/transaction-types/")]
        public static async Task<Ok<List<TransactionTypeDto>>> HandleAsync(
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var transactionTypes = await dbContext.TransactionTypes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.TransactionTypeName)
                .Select(x => new TransactionTypeDto(x.TransactionTypeId, x.TransactionTypeCode, x.TransactionTypeName))
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(transactionTypes);
        }
    }
}

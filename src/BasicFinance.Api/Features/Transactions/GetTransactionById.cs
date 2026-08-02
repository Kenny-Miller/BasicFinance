using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Transactions;

/// <summary>
/// Contains all logic associated with the <see cref="GetTransactionById"/> Endpoint.
/// </summary>
public class GetTransactionById
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

    /// <summary>
    /// Gets a <see cref="Transaction"/>s associated with the authenticated user and the specified Id.
    /// </summary>
    /// <param name="transactionId">The request query parameters.</param>
    /// <param name="user">The authenticated user performing the request.</param>
    /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted spreadsheets.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// Returns <see cref="Ok{TValue}"/> when successful,
    /// or <see cref="BadRequest"/> on failure.
    /// </returns>
    [Authorize]
    [WolverineGet("api/transactions/{transactionId:guid}")]
    public static async Task<Results<Ok<TransactionDto>, BadRequest<string>>> HandleAsync(
        [FromRoute] Guid transactionId,
        AuthenticatedUser user,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.TransactionId == transactionId)
            .Where(x => x.UserId == user.Id)
            .Where(x => x.IsActive)
            .Select(x => new TransactionDto(
                x.TransactionId,
                x.TransactionType.TransactionTypeName,
                x.TransactionCategory.TransactionCategoryName,
                x.Account.AccountName,
                x.Date,
                x.Amount,
                x.Description))
            .SingleOrDefaultAsync(cancellationToken);

        return transaction != null
            ? TypedResults.Ok(transaction)
            : TypedResults.BadRequest("Transaction with the specified Id was not found");
    }
}

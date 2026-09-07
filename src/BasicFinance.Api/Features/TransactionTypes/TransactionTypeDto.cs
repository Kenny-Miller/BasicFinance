using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Api.Features.TransactionTypes
{
    /// <summary>
    /// Dto containing <see cref="TransactionType"/> data.
    /// </summary>
    /// <param name="Id">The unique identifier of the transaction type.</param>
    /// <param name="Code">The code of the transaction type.</param>
    /// <param name="Name">The display name of the transaction type.</param>
    public record TransactionTypeDto(
        int Id,
        string Code,
        string Name);
}

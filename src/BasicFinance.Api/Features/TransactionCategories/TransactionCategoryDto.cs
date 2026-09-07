using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Api.Features.TransactionCategories
{
    /// <summary>
    /// Dto containing <see cref="TransactionCategory"/> data.
    /// </summary>
    /// <param name="Id">The unique identifier of the transaction category.</param>
    /// <param name="Code">The code of the transaction category.</param>
    /// <param name="Name">The display name of the transaction category.</param>
    public record TransactionCategoryDto(
        int Id,
        string Code,
        string Name);
}

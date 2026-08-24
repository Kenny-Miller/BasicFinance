using System.Linq.Expressions;
using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Shared query helpers for the <see cref="Account"/> endpoints.
    /// </summary>
    public static class Queries
    {
        /// <summary>
        /// Projection from <see cref="Account"/> to <see cref="AccountDto"/>,
        /// taking the latest ledger entry (by balance recorded date, with system
        /// created date as a tie-breaker) for the balance fields. The balance
        /// fields are <c>null</c> when the account has no ledger entries.
        /// </summary>
        public static readonly Expression<Func<Account, AccountDto>> ToAccountDtoExpression =
            x => new AccountDto(
                x.AccountId,
                x.AccountName,
                x.AccountType.AccountTypeCode,
                x.AccountType.AccountTypeName,
                x.Institution.InstitutionCode,
                x.Institution.Name,
                x.Currency,
                x.AccountType.IsLiability,
                x.Ledger.OrderByDescending(l => l.BalanceRecordedDate)
                    .ThenByDescending(l => l.SystemCreatedDate)
                    .Select(l => (decimal?)l.Balance)
                    .FirstOrDefault(),
                x.Ledger.OrderByDescending(l => l.BalanceRecordedDate)
                    .ThenByDescending(l => l.SystemCreatedDate)
                    .Select(l => (DateTimeOffset?)l.BalanceRecordedDate)
                    .FirstOrDefault());

        public static readonly Func<Account, AccountDto> ToAccountDtoFunc = ToAccountDtoExpression.Compile();

        /// <summary>
        /// Projects <see cref="Account"/> entities to <see cref="AccountDto"/>,
        /// taking the latest ledger entry (by balance recorded date, with system
        /// created date as a tie-breaker) for the balance fields.
        /// </summary>
        /// <param name="source">An <see cref="IQueryable{T}"/> of <see cref="Account"/> entities.</param>
        /// <returns>A query of <see cref="AccountDto"/> projections.</returns>
        public static IQueryable<AccountDto> ToAccountDto(this IQueryable<Account> source) => source.Select(ToAccountDtoExpression);
    }
}

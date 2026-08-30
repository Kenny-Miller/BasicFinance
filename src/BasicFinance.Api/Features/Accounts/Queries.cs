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
        /// created date as a tie-breaker) for the balance fields.
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
                    .Select(l => l.Balance)
                    .First(),
                x.Ledger.OrderByDescending(l => l.BalanceRecordedDate)
                    .ThenByDescending(l => l.SystemCreatedDate)
                    .Select(l => l.BalanceRecordedDate)
                    .First());

        /// <summary>
        /// Maps an <see cref="Account"/> to <see cref="AccountDto"/>, with the balance fields
        /// taken from the given latest ledger entry, or from a zero balance with the fallback
        /// recorded date when the entry is <c>null</c>.
        /// </summary>
        /// <param name="account">The account entity.</param>
        /// <param name="latestLedger">The account's latest ledger entry on or before the period end, or <c>null</c>.</param>
        /// <param name="fallbackRecordedDate">The recorded date to use when <paramref name="latestLedger"/> is <c>null</c>.</param>
        /// <returns>The <see cref="AccountDto"/> mapping.</returns>
        public static AccountDto ToAccountDto(Account account, AccountLedger? latestLedger, DateTimeOffset fallbackRecordedDate) => new(
            account.AccountId,
            account.AccountName,
            account.AccountType.AccountTypeCode,
            account.AccountType.AccountTypeName,
            account.Institution.InstitutionCode,
            account.Institution.Name,
            account.Currency,
            account.AccountType.IsLiability,
            latestLedger?.Balance ?? 0m,
            latestLedger?.BalanceRecordedDate ?? fallbackRecordedDate);


        /// <summary>
        /// Projects <see cref="Account"/> entities to <see cref="AccountDto"/>,
        /// taking the latest ledger entry (by balance recorded date, with system
        /// created date as a tie-breaker) for the balance fields.
        /// </summary>
        /// <param name="source">An <see cref="IQueryable{T}"/> of <see cref="Account"/> entities.</param>
        /// <returns>A query of <see cref="AccountDto"/> projections.</returns>
        public static IQueryable<AccountDto> ToAccountDto(this IQueryable<Account> source) => source.Select(ToAccountDtoExpression);

        /// <summary>
        /// Projects <see cref="Account"/> entities to <see cref="AccountPeriodComparisonDto"/>,
        /// selecting the most recent ledger entry (by balance recorded date, with system
        /// created date as a tie-breaker) on or before each of the two period end dates.
        /// </summary>
        /// <param name="source">An <see cref="IQueryable{T}"/> of <see cref="Account"/> entities.</param>
        /// <param name="currentPeriodEndDate">The last instant of the current period (inclusive).</param>
        /// <param name="previousPeriodEndDate">The last instant of the previous period (inclusive).</param>
        /// <returns>A query of <see cref="AccountPeriodComparisonDto"/> projections, one account at a time.</returns>
        internal static IQueryable<AccountPeriodComparisonDto> ProjectToComparison(
            this IQueryable<Account> source,
            DateTimeOffset currentPeriodEndDate,
            DateTimeOffset previousPeriodEndDate) =>
            source.Select(x => new AccountPeriodComparisonDto(
                x,
                x.Ledger.Where(l => l.BalanceRecordedDate <= currentPeriodEndDate)
                    .OrderByDescending(l => l.BalanceRecordedDate)
                    .ThenByDescending(l => l.SystemCreatedDate)
                    .FirstOrDefault(),
                x.Ledger.Where(l => l.BalanceRecordedDate <= previousPeriodEndDate)
                    .OrderByDescending(l => l.BalanceRecordedDate)
                    .ThenByDescending(l => l.SystemCreatedDate)
                    .FirstOrDefault()));

    }
}

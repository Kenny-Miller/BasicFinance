using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Api.Features.Accounts;

/// <summary>
/// An <see cref="Account"/> paired with its latest ledger balance on or before the
/// end of the current and previous comparison periods.
/// </summary>
/// <param name="Account">The account entity.</param>
/// <param name="CurrentPeriodLatestLedger">The latest ledger entry on or before the current period end, or <c>null</c>.</param>
/// <param name="PreviousPeriodLatestLedger">The latest ledger entry on or before the previous period end, or <c>null</c>.</param>
internal sealed record AccountPeriodComparisonDto(
    Account Account,
    AccountLedger? CurrentPeriodLatestLedger,
    AccountLedger? PreviousPeriodLatestLedger);

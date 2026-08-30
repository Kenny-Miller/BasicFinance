using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using BasicFinance.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Accounts
{
    /// <summary>
    /// Contains all logic associated with the <see cref="GetInstitutionSummary"/> Endpoint.
    /// </summary>
    public static class GetInstitutionSummary
    {
        /// <summary>
        /// Query parameters for the <see cref="GetInstitutionSummary"/> endpoint.
        /// </summary>
        /// <param name="RecordedDate">The anchor date used to resolve period boundaries.</param>
        /// <param name="TimePeriod">The period mode.</param>
        public record Request(DateTimeOffset? RecordedDate = null, TimePeriod? TimePeriod = null);

        /// <summary>
        /// Response Dto for the <see cref="GetInstitutionSummary"/> endpoint.
        /// </summary>
        /// <param name="InstitutionId">The Id of the institution.</param>
        /// <param name="InstitutionName">The full name of the institution.</param>
        /// <param name="Accounts">The user's active accounts at the institution, with their latest balance on or before the current period end.</param>
        /// <param name="AccountTypeTotals">Sum of the latest balances per account type for the current period, zero-filled for every active account type.</param>
        /// <param name="AccountTypePreviousTotals">Sum of the latest balances per account type for the previous period, zero-filled for every active account type.</param>
        /// <param name="CurrentPeriodStart">The first day of the current period (inclusive).</param>
        /// <param name="CurrentPeriodEnd">The first day excluded from the current period (exclusive).</param>
        /// <param name="PreviousPeriodStart">The first day of the previous period (inclusive).</param>
        /// <param name="PreviousPeriodEnd">The first day excluded from the previous period (exclusive).</param>
        public record InstitutionSummaryResponse(
            int InstitutionId,
            string InstitutionName,
            IEnumerable<AccountDto> Accounts,
            Dictionary<string, decimal> AccountTypeTotals,
            Dictionary<string, decimal> AccountTypePreviousTotals,
            DateTime CurrentPeriodStart,
            DateTime CurrentPeriodEnd,
            DateTime PreviousPeriodStart,
            DateTime PreviousPeriodEnd);

        /// <summary>
        /// Retrieves account-level summary for a specific institution.
        /// Period totals are sourced from the most recent balance ledger entry
        /// on or before each period's end (last known balances are carried forward;
        /// accounts without a ledger entry that far back carry a zero balance).
        /// The per-account-type totals are zero-filled for every active account type.
        /// If the institution is deactivated no accounts are returned, but the
        /// institution name and zero-filled totals are.
        /// </summary>
        /// <param name="institutionId">The unique identifier of the institution.</param>
        /// <param name="request">The request containing the recorded date and time period.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="timeProvider">Time provider for consistent date calculations.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with the institution summary.
        /// Unrecognized time period values fall back to <see cref="TimePeriod.Monthly"/>.
        /// A <see cref="BadRequest"/> is returned when the institution does not exist.
        /// </returns>
        [Authorize]
        [WolverineGet("api/accounts/institution/{institutionId:int}/summary")]
        public static async Task<Results<Ok<InstitutionSummaryResponse>, BadRequest<string>>> HandleAsync(
            [FromRoute] int institutionId,
            [FromQuery] Request request,
            AuthenticatedUser user,
            TimeProvider timeProvider,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var timePeriod = request.TimePeriod ?? TimePeriod.Monthly;
            var anchor = request.RecordedDate ?? timeProvider.GetUtcNow();
            var recordedDate = new DateTimeOffset(anchor.Year, anchor.Month, anchor.Day, 0, 0, 0, TimeSpan.Zero);

            var currentRange = recordedDate.ToPeriodRange(timePeriod);
            var previousRange = recordedDate.ToPeriodRange(timePeriod, -1);

            var institution = await dbContext.Institutions
                .AsNoTracking()
                .Where(x => x.InstitutionId == institutionId)
                .FirstOrDefaultAsync(cancellationToken);

            if (institution is null)
            {
                return TypedResults.BadRequest("Institution with the specified Id was not found");
            }

            var results = await dbContext.Accounts
                .AsNoTracking()
                .Include(x => x.Institution)
                .Include(x => x.AccountType)
                .Where(x => x.InstitutionId == institutionId)
                .Where(x => x.Institution.IsActive)
                .Where(x => x.AccountType.IsActive)
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive)
                .ProjectToComparison(currentRange.RangeEndDate, previousRange.RangeEndDate)
                .ToListAsync(cancellationToken);

            var accountTypeCodes = await dbContext.AccountTypes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => x.AccountTypeCode)
                .ToListAsync(cancellationToken);

            var accounts = results
                .Select(x => Queries.ToAccountDto(x.Account, x.CurrentPeriodLatestLedger, currentRange.RangeStartDate))
                .ToList();
            var previousAccounts = results
                .Select(x => Queries.ToAccountDto(x.Account, x.PreviousPeriodLatestLedger, previousRange.RangeStartDate))
                .ToList();

            var response = new InstitutionSummaryResponse(
                institutionId,
                institution.Name,
                accounts,
                BuildAccountTypeTotals(accounts, accountTypeCodes),
                BuildAccountTypeTotals(previousAccounts, accountTypeCodes),
                currentRange.RangeStartDate.UtcDateTime,
                currentRange.RangeEndDate.UtcDateTime,
                previousRange.RangeStartDate.UtcDateTime,
                previousRange.RangeEndDate.UtcDateTime
            );

            return TypedResults.Ok(response);
        }

        /// <summary>
        /// Builds a total per account type, initialized to <c>0</c> for every active
        /// account type code and accumulated from the given accounts' balances.
        /// </summary>
        /// <param name="accounts">The mapped accounts to sum per account type.</param>
        /// <param name="accountTypeCodes">The active account type codes to include.</param>
        /// <returns>A total per account type where every active account type is present.</returns>
        private static Dictionary<string, decimal> BuildAccountTypeTotals(List<AccountDto> accounts, List<string> accountTypeCodes)
        {
            var totals = accountTypeCodes.ToDictionary(x => x, _ => 0m);
            foreach (var account in accounts)
            {
                totals[account.AccountTypeCode] += account.LatestBalance;
            }

            return totals;
        }
    }
}

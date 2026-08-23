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
        /// <param name="Accounts">The user's active accounts at the institution with their latest balances.</param>
        /// <param name="AccountTypeTotals">Sum of the latest balances per account type for the current period.</param>
        /// <param name="AccountTypePreviousTotals">Sum of the latest balances per account type for the previous period.</param>
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
        /// Validates that the authenticated user owns at least one active account at the institution.
        /// Period totals are sourced from the most recent balance ledger entry
        /// on or before each period's end (last known balances are carried forward).
        /// </summary>
        /// <param name="institutionId">The unique identifier of the institution.</param>
        /// <param name="request">The request containing the recorded date and time period.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="timeProvider">Time provider for consistent date calculations.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with the institution summary when successful,
        /// or <see cref="BadRequest{TValue}"/> when the institution does not exist
        /// or the authenticated user has no active accounts at that institution.
        /// Unrecognized time period values fall back to <see cref="TimePeriod.Monthly"/>.
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
                .Where(x => x.IsActive)
                .Where(x => x.InstitutionId == institutionId)
                .Where(x => x.Accounts.Any(y => y.UserId == user.Id && y.IsActive))
                .SingleOrDefaultAsync(cancellationToken);

            if (institution == null)
            {
                return TypedResults.BadRequest("Institution not found or you have no accounts at this institution.");
            }

            var baseAccountsInInstitutionQuery = dbContext.Accounts
                .AsNoTracking()
                .Include(x => x.Institution)
                .Include(x => x.AccountType)
                .Where(x => x.Institution.IsActive && x.Institution.InstitutionId == institutionId)
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive);

            var currentPeriodQuery = baseAccountsInInstitutionQuery
                .Include(x => x.Ledger
                    .Where(l => l.BalanceRecordedDate <= currentRange.RangeEndDate)
                    .OrderByDescending(l => l.BalanceRecordedDate)
                    .ThenByDescending(l => l.SystemCreatedDate)
                    .Take(1));

            var previousPeriodQuery = baseAccountsInInstitutionQuery
                .Include(x => x.Ledger
                    .Where(l => l.BalanceRecordedDate <= previousRange.RangeEndDate)
                    .OrderByDescending(l => l.BalanceRecordedDate)
                    .ThenByDescending(l => l.SystemCreatedDate)
                    .Take(1));

            // Project the most recent ledger entry on or before each period's end, per account.
            var results = await currentPeriodQuery
                .Join(previousPeriodQuery,
                    current => current.AccountId,
                    previous => previous.AccountId,
                    (current, previous) => new
                    {
                        CurrentAccount = current,
                        PreviousAccount = previous,
                    })
                .ToListAsync(cancellationToken);

            var processedData = new
            {
                CurrentAccountData = results
                    .Select(x => x.CurrentAccount)
                    .Select(Queries.ToAccountDtoFunc)
                    .ToList(),
                AccountTypeTotals = results
                    .Select(x => x.CurrentAccount)
                    .Select(Queries.ToAccountDtoFunc)
                    .GroupBy(s => s.AccountTypeCode)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.LatestBalance)),
                PreviousAccountTypeTotals = results
                    .Select(x => x.PreviousAccount)
                    .Select(Queries.ToAccountDtoFunc)
                    .GroupBy(s => s.AccountTypeCode)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.LatestBalance)),
            };

            var response = new InstitutionSummaryResponse(
                institutionId,
                institution.Name,
                processedData.CurrentAccountData,
                processedData.AccountTypeTotals,
                processedData.PreviousAccountTypeTotals,
                currentRange.RangeStartDate.UtcDateTime,
                currentRange.RangeEndDate.UtcDateTime,
                previousRange.RangeStartDate.UtcDateTime,
                previousRange.RangeEndDate.UtcDateTime
            );

            return TypedResults.Ok(response);
        }
    }
}

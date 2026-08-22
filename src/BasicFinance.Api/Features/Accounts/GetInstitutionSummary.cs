using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Enums;
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
        /// <param name="RecordedDate">The anchor date used to resolve period boundaries. Defaults to now (UTC) when omitted.</param>
        /// <param name="TimePeriod">The period mode. Defaults to <see cref="TimePeriod.Monthly"/> when omitted. Invalid values result in a 400 response.</param>
        public record Request(DateTimeOffset? RecordedDate, TimePeriod? TimePeriod = null);

        /// <summary>
        /// Response Dto for the <see cref="GetInstitutionSummary"/> endpoint.
        /// </summary>
        /// <param name="InstitutionId"></param>
        /// <param name="InstitutionName"></param>
        /// <param name="Accounts"></param>
        /// <param name="AccountTypeTotals"></param>
        /// <param name="AccountTypePreviousTotals"></param>
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
            DateOnly CurrentPeriodStart,
            DateOnly CurrentPeriodEnd,
            DateOnly PreviousPeriodStart,
            DateOnly PreviousPeriodEnd);

        /// <summary>
        /// Retrieves account-level summary for a specific institution.
        /// Validates that the authenticated user owns at least one active account at the institution.
        /// Period totals are sourced from the most recent active balance history
        /// on or before each period's end (last known balances are carried forward).
        /// </summary>
        /// <param name="institutionId">The unique identifier of the institution.</param>
        /// <param name="request">The request containing the recorded date and time period.</param>
        /// <param name="httpContext">The HTTP context used to inspect the raw time period query value.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="timeProvider">Time provider for consistent date calculations.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with institution summary when successful,
        /// or <see cref="BadRequest{TValue}"/> if the time period is invalid,
        /// or the institution is not found or the user has no accounts.
        /// </returns>
        [Authorize]
        [WolverineGet("api/accounts/institution/{institutionId:int}/summary")]
        public static async Task<Results<Ok<InstitutionSummaryResponse>, BadRequest<string>>> HandleAsync(
            [FromRoute] int institutionId,
            [FromQuery] Request request,
            HttpContext httpContext,
            AuthenticatedUser user,
            TimeProvider timeProvider,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            if (httpContext.Request.Query.TryGetValue("timePeriod", out var rawTimePeriod) &&
                !Enum.TryParse<TimePeriod>(rawTimePeriod, out _))
            {
                return TypedResults.BadRequest("Invalid time period.");
            }

            var resolution = (request.TimePeriod ?? TimePeriod.Monthly).ToPeriodResolution(request.RecordedDate, timeProvider.GetUtcNow());

            var institution = await dbContext.Institutions
                .AsNoTracking()
                .Where(i => i.InstitutionId == institutionId)
                .Select(i => new
                {
                    InstitutionName = i.Name,
                    Accounts = i.Accounts
                        .Where(a => a.UserId == user.Id && a.IsActive)
                        .Select(a => new
                        {
                            a.AccountId,
                            a.AccountName,
                            a.AccountTypeId,
                            a.AccountType.AccountTypeCode,
                            a.Balance,
                            a.BalanceRecordedDate
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (institution == null || institution.Accounts.Count == 0)
            {
                return TypedResults.BadRequest("Institution not found or you have no accounts at this institution.");
            }

            var accountIds = institution.Accounts.Select(a => a.AccountId).ToHashSet();

            var accountDetails = institution.Accounts
                .Select(a => new AccountDto(
                    a.AccountId,
                    a.AccountName,
                    a.AccountTypeCode,
                    institution.InstitutionName,
                    AccountBalanceHistoryQueries.IsLiability((AccountType)a.AccountTypeId) ? -a.Balance : a.Balance,
                    a.BalanceRecordedDate
                ))
                .ToList();

            var scopedHistories = dbContext.AccountBalanceHistories
                .AsNoTracking()
                .Where(h => h.IsActive &&
                            accountIds.Contains(h.AccountId));

            var currentSnapshots = await AccountBalanceHistoryQueries.GetLatestSnapshotsOnOrBeforeAsync(
                scopedHistories,
                resolution.CurrentRange.RangeEndDate,
                cancellationToken);
            var previousSnapshots = await AccountBalanceHistoryQueries.GetLatestSnapshotsOnOrBeforeAsync(
                scopedHistories,
                resolution.PreviousRange.RangeEndDate,
                cancellationToken);

            var response = new InstitutionSummaryResponse(
                institutionId,
                institution.InstitutionName,
                accountDetails,
                CalculateTypeTotals(currentSnapshots),
                CalculateTypeTotals(previousSnapshots),
                resolution.CurrentStart,
                resolution.CurrentEnd,
                resolution.PreviousStart,
                resolution.PreviousEnd
            );

            return TypedResults.Ok(response);
        }

        /// <summary>
        /// Aggregates account-type totals from a list of account snapshots.
        /// Credit card balances are treated as liabilities (negative).
        /// </summary>
        static Dictionary<string, decimal> CalculateTypeTotals(List<AccountBalanceHistoryQueries.Snapshot> snapshots)
        {
            if (snapshots.Count == 0)
            {
                return [];
            }

            return snapshots
                .GroupBy(s => s.AccountTypeCode)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => AccountBalanceHistoryQueries.IsLiability(s.AccountType) ? -s.Balance : s.Balance)
                );
        }
    }
}

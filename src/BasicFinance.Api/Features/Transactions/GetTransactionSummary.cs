using BasicFinance.Api.Common.Authentication;
using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using TransactionTypeEnum = BasicFinance.Infrastructure.Enums.TransactionType;

namespace BasicFinance.Api.Features.Transactions
{
    /// <summary>
    /// Contains all logic associated with the <see cref="GetTransactionSummary"/> endpoint.
    /// </summary>
    public static class GetTransactionSummary
    {
        /// <summary>
        /// Query parameters for the <see cref="GetTransactionSummary"/> endpoint.
        /// </summary>
        /// <param name="RecordedDate">The anchor date used to resolve period boundaries. Falls back to the current date when absent.</param>
        /// <param name="TimePeriod">The period mode. Falls back to <see cref="TimePeriod.Monthly"/> when absent or unrecognized.</param>
        /// <param name="AccountId">Optional account id to restrict the summary to.</param>
        /// <param name="InstitutionId">Optional institution id to restrict the summary to the user's active accounts at the institution.</param>
        public record Request(
            DateTimeOffset? RecordedDate = null,
            TimePeriod? TimePeriod = null,
            Guid? AccountId = null,
            int? InstitutionId = null);

        /// <summary>
        /// Response Dto for the <see cref="GetTransactionSummary"/> endpoint.
        /// </summary>
        /// <param name="CurrentStart">The first day of the current period (inclusive).</param>
        /// <param name="CurrentEnd">The first day excluded from the current period (exclusive).</param>
        /// <param name="PreviousStart">The first day of the previous period (inclusive).</param>
        /// <param name="PreviousEnd">The first day excluded from the previous period (exclusive).</param>
        /// <param name="CurrentPeriod">Headline aggregates for the current period.</param>
        /// <param name="PreviousPeriod">Headline aggregates for the previous period.</param>
        public record TransactionSummaryResponse(
            DateOnly CurrentStart,
            DateOnly CurrentEnd,
            DateOnly PreviousStart,
            DateOnly PreviousEnd,
            TransactionPeriodSummary CurrentPeriod,
            TransactionPeriodSummary PreviousPeriod);

        /// <summary>
        /// Headline aggregates for a single period.
        /// </summary>
        /// <param name="TotalCount">Count of all transaction types in the period.</param>
        /// <param name="TotalSpend">Sum of debit amounts in the period.</param>
        /// <param name="TotalIncome">Sum of credit amounts in the period.</param>
        /// <param name="NetFlow">Total income minus total spend; negative when spend exceeds income.</param>
        public record TransactionPeriodSummary(int TotalCount, decimal TotalSpend, decimal TotalIncome, decimal NetFlow);

        /// <summary>
        /// Period-level aggregates computed by the database over grouped transaction rows.
        /// </summary>
        /// <param name="IsCurrentPeriod">Whether the row aggregates transactions in the current period.</param>
        /// <param name="TotalCount">Count of all transaction types in the period.</param>
        /// <param name="TotalSpend">Sum of debit amounts in the period.</param>
        /// <param name="TotalIncome">Sum of credit amounts in the period.</param>
        private sealed record PeriodAggregateRow(bool IsCurrentPeriod, int TotalCount, decimal TotalSpend, decimal TotalIncome);

        /// <summary>
        /// Retrieves headline transaction aggregates (count, spend, income, net flow) for the user's
        /// active accounts for the current and previous period. The period is resolved from
        /// <c>timePeriod</c> plus <c>recordedDate</c>.
        /// </summary>
        /// <param name="request">The request containing period parameters and optional account or institution filters.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="timeProvider">Time provider for consistent date calculations.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with the transaction summary. Zeroed aggregates are returned when
        /// no transactions match the requested period and filters.
        /// Unrecognized time period values fall back to <see cref="TimePeriod.Monthly"/>.
        /// </returns>
        [Authorize]
        [WolverineGet("api/transactions/summary")]
        public static async Task<Ok<TransactionSummaryResponse>> HandleAsync(
            [FromQuery] Request request,
            AuthenticatedUser user,
            TimeProvider timeProvider,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var timePeriod = request.TimePeriod ?? TimePeriod.Monthly;
            var anchor = request.RecordedDate ?? timeProvider.GetUtcNow();
            var recordedDate = new DateTimeOffset(anchor.Year, anchor.Month, anchor.Day, 0, 0, 0, TimeSpan.Zero);
            var currentPeriod = recordedDate.ToPeriodRange(timePeriod);
            var previousPeriod = recordedDate.ToPeriodRange(timePeriod, -1);

            var baseQuery = dbContext.Transactions
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive);

            if (request.AccountId is { } accountId)
            {
                baseQuery = baseQuery.Where(x => x.AccountId == accountId);
            }

            if (request.InstitutionId is { } institutionId)
            {
                baseQuery = baseQuery
                    .Where(x => x.Account.InstitutionId == institutionId)
                    .Where(x => x.Account.IsActive)
                    .Where(x => x.Account.Institution.IsActive);
            }

            var aggregatesByPeriod = await baseQuery
                .Where(x => x.Date >= previousPeriod.RangeStartDate && x.Date <= currentPeriod.RangeEndDate)
                .GroupBy(x => x.Date >= currentPeriod.RangeStartDate)
                .Select(g => new PeriodAggregateRow(
                    g.Key,
                    g.Count(),
                    g.Sum(r => r.TransactionTypeId == (int)TransactionTypeEnum.Debit ? r.Amount : 0m),
                    g.Sum(r => r.TransactionTypeId == (int)TransactionTypeEnum.Credit ? r.Amount : 0m)))
                .ToListAsync(cancellationToken);

            var emptyAggregates = new PeriodAggregateRow(false, 0, 0m, 0m);
            var currentAggregates = aggregatesByPeriod.FirstOrDefault(a => a.IsCurrentPeriod, emptyAggregates);
            var previousAggregates = aggregatesByPeriod.FirstOrDefault(a => !a.IsCurrentPeriod, emptyAggregates);

            return TypedResults.Ok(new TransactionSummaryResponse(
                DateOnly.FromDateTime(currentPeriod.RangeStartDate.Date),
                DateOnly.FromDateTime(recordedDate.ToStartOfPeriod(timePeriod, 1).Date),
                DateOnly.FromDateTime(previousPeriod.RangeStartDate.Date),
                DateOnly.FromDateTime(currentPeriod.RangeStartDate.Date),
                ToPeriodSummary(currentAggregates),
                ToPeriodSummary(previousAggregates)));
        }

        /// <summary>
        /// Projects per-period aggregates into a wire summary, deriving net flow as income minus spend.
        /// </summary>
        /// <param name="aggregates">The period aggregates.</param>
        /// <returns>The wire summary for the period.</returns>
        private static TransactionPeriodSummary ToPeriodSummary(PeriodAggregateRow aggregates) =>
            new(aggregates.TotalCount, aggregates.TotalSpend, aggregates.TotalIncome, aggregates.TotalIncome - aggregates.TotalSpend);
    }
}

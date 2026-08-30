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
    /// Contains all logic associated with the <see cref="GetDailyTransactionSummary"/> endpoint.
    /// </summary>
    public static class GetDailyTransactionSummary
    {
        /// <summary>
        /// Query parameters for the <see cref="GetDailyTransactionSummary"/> endpoint.
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
        /// Response Dto for the <see cref="GetDailyTransactionSummary"/> endpoint.
        /// </summary>
        /// <param name="CurrentStart">The first day of the current period (inclusive).</param>
        /// <param name="CurrentEnd">The first day excluded from the current period (exclusive).</param>
        /// <param name="PreviousStart">The first day of the previous period (inclusive).</param>
        /// <param name="PreviousEnd">The first day excluded from the previous period (exclusive).</param>
        /// <param name="CurrentPeriod">One zero-filled spend point per calendar day of the current period.</param>
        /// <param name="PreviousPeriod">One zero-filled spend point per calendar day of the previous period.</param>
        public record DailySummaryResponse(
            DateOnly CurrentStart,
            DateOnly CurrentEnd,
            DateOnly PreviousStart,
            DateOnly PreviousEnd,
            List<DailyTransactionSummary> CurrentPeriod,
            List<DailyTransactionSummary> PreviousPeriod);

        /// <summary>
        /// Spend and transaction count for a single calendar day.
        /// </summary>
        /// <param name="Date">The day (UTC).</param>
        /// <param name="TotalSpend">Sum of debit amounts for the day.</param>
        /// <param name="TransactionCount">Count of all transaction types for the day.</param>
        public record DailyTransactionSummary(DateOnly Date, decimal TotalSpend, int TransactionCount);

        /// <summary>
        /// Daily aggregates computed by the database over grouped transaction rows.
        /// </summary>
        /// <param name="TotalSpend">Sum of debit amounts for the day.</param>
        /// <param name="TransactionCount">Count of all transaction types for the day.</param>
        private sealed record DailyAggregation(decimal TotalSpend, int TransactionCount);

        /// <summary>
        /// Retrieves per-calendar-day spend points for the user's active accounts for the current and
        /// previous period. The period is resolved from <c>timePeriod</c> plus <c>recordedDate</c>.
        /// Days without activity are zero-filled.
        /// </summary>
        /// <param name="request">The request containing period parameters and optional account or institution filters.</param>
        /// <param name="user">The authenticated user performing the request.</param>
        /// <param name="timeProvider">Time provider for consistent date calculations.</param>
        /// <param name="dbContext">Application <see cref="AppDbContext"/> used to query persisted data.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        /// <returns>
        /// Returns <see cref="Ok{TValue}"/> with the daily summary. Zero-filled points are returned for days
        /// with no activity, including when no transactions match the requested filters.
        /// Unrecognized time period values fall back to <see cref="TimePeriod.Monthly"/>.
        /// </returns>
        [Authorize]
        [WolverineGet("api/transactions/dailySummary")]
        public static async Task<Ok<DailySummaryResponse>> HandleAsync(
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

            var pointsByDate = await baseQuery
                .Where(x => x.Date >= previousPeriod.RangeStartDate && x.Date <= currentPeriod.RangeEndDate)
                .GroupBy(x => DateOnly.FromDateTime(x.Date.Date))
                .ToDictionaryAsync(
                    x => x.Key,
                    x => new DailyAggregation(
                        x.Sum(r => r.TransactionTypeId == (int)TransactionTypeEnum.Debit ? r.Amount : 0m),
                        x.Count()),
                    cancellationToken);

            var currentStart = DateOnly.FromDateTime(currentPeriod.RangeStartDate.Date);
            var currentEnd = DateOnly.FromDateTime(recordedDate.ToStartOfPeriod(timePeriod, 1).Date);
            var previousStart = DateOnly.FromDateTime(previousPeriod.RangeStartDate.Date);

            return TypedResults.Ok(new DailySummaryResponse(
                currentStart,
                currentEnd,
                previousStart,
                currentStart,
                BuildDailyPoints(currentStart, currentEnd, pointsByDate),
                BuildDailyPoints(previousStart, currentStart, pointsByDate)));
        }

        /// <summary>
        /// Builds one zero-filled point per calendar day across [start, end), sourcing spend and count
        /// from the aggregated points where present.
        /// </summary>
        /// <param name="start">The inclusive first day of the range.</param>
        /// <param name="end">The exclusive first day after the range.</param>
        /// <param name="pointsByDate">Aggregated points keyed by day.</param>
        /// <returns>Daily points for every calendar day in the range, zero-filled where no activity exists.</returns>
        private static List<DailyTransactionSummary> BuildDailyPoints(
            DateOnly start,
            DateOnly end,
            IReadOnlyDictionary<DateOnly, DailyAggregation> pointsByDate)
        {
            var points = new List<DailyTransactionSummary>(end.DayNumber - start.DayNumber);
            for (var date = start; date < end; date = date.AddDays(1))
            {
                points.Add(pointsByDate.TryGetValue(date, out var point)
                    ? new DailyTransactionSummary(date, point.TotalSpend, point.TransactionCount)
                    : new DailyTransactionSummary(date, 0m, 0));
            }

            return points;
        }
    }
}

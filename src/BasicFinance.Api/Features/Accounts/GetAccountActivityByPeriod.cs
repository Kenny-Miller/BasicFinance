using BasicFinance.Api.Common.Authentication;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace BasicFinance.Api.Features.Accounts
{
    public static class GetAccountActivityByPeriod
    {
        public record Request(DateTime StartDate, AccountPeriod AccountPeriod = AccountPeriod.Monthly);

        public enum AccountPeriod
        {
            Weekly,
            Monthly,
            Quarterly,
            Yearly
        }

        public record AccountSnapshotDto(Guid Id, string Institution, string AccountName, decimal Balance);

        public record AccountTypeSummaryDto(
            string AccountTypeCode,
            decimal CurrentTotal,
            decimal PreviousTotal,
            double PercentOfTotal,
            List<AccountSnapshotDto> Accounts);

        public record AccountsByPeriodResponse(
            DateTimeOffset CurrentPeriodStart,
            DateTimeOffset CurrentPeriodEnd,
            DateTimeOffset PreviousPeriodStart,
            DateTimeOffset PreviousPeriodEnd,
            decimal TotalNetWorth,
            List<AccountTypeSummaryDto> AccountTypes);

        [Authorize]
        [WolverineGet("api/accounts/activityByPeriod")]
        public static async Task<Ok<AccountsByPeriodResponse>> HandleAsync(
            [FromQuery] Request request,
            AuthenticatedUser user,
            TimeProvider timeProvider,
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var startDate = new DateTimeOffset(request.StartDate.Year, request.StartDate.Month, request.StartDate.Day, 0, 0, 0, TimeSpan.Zero);
            var now = new DateTimeOffset(2025, 11, 25, 13, 26, 30, TimeSpan.Zero);
            var (currentStart, currentEnd) = GetPeriodBoundaries(startDate, request.AccountPeriod, now);
            var (previousStart, previousEnd) = GetPreviousPeriodBoundaries(request.AccountPeriod, currentStart);

            var userAccountIds = await dbContext.Accounts
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive)
                .Select(x => x.AccountId)
                .ToListAsync(cancellationToken);

            var currentAccounts = await dbContext.Accounts
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Where(x => x.IsActive)
                .Where(x => x.BalanceRecordedDate >= currentStart)
                .Where(x => x.BalanceRecordedDate < currentEnd)
                .ToListAsync(cancellationToken);

            var previousHistories = await dbContext.AccountBalanceHistories
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Where(x => userAccountIds.Contains(x.AccountId))
                .Where(x => x.BalanceRecordedDate >= previousStart)
                .Where(x => x.BalanceRecordedDate < previousEnd)
                .ToListAsync(cancellationToken);

            var latestPreviousByAccount = previousHistories
                .GroupBy(h => h.AccountId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(h => h.BalanceRecordedDate).First());

            var accountTypes = await dbContext.AccountTypes
                .AsNoTracking()
                .Where(t => t.IsActive)
                .ToListAsync(cancellationToken);

            var currentByType = currentAccounts
                .GroupBy(a => a.AccountTypeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            decimal assetTotal = 0m;
            decimal liabilityTotal = 0m;

            var typeSummaries = new List<AccountTypeSummaryDto>();

            foreach (var accountType in accountTypes)
            {
                var accountId = accountType.AccountTypeId;
                var accounts = currentByType.TryGetValue(accountId, out var accountsList) ? accountsList : [];

                var currentTotal = accounts.Sum(a => a.Balance);
                var previousTotal = accounts.Sum(a =>
                    latestPreviousByAccount.TryGetValue(a.AccountId, out var history) ? history.Balance : 0m);

                if (accountType.AccountTypeId == (int)AccountType.CreditCard)
                {
                    liabilityTotal += currentTotal;
                }
                else
                {
                    assetTotal += currentTotal;
                }

                typeSummaries.Add(new AccountTypeSummaryDto(
                    accountType.AccountTypeCode,
                    currentTotal,
                    previousTotal,
                    0,
                    accounts.OrderBy(a => a.AccountName)
                        .Select(a => new AccountSnapshotDto(
                            a.AccountId,
                            a.Institution,
                            a.AccountName,
                            a.Balance))
                        .ToList()));
            }

            var netWorth = assetTotal - liabilityTotal;

            var resultSummaries = typeSummaries.Select(summary =>
            {
                var isLiability = summary.AccountTypeCode == "CRD";
                var percent = netWorth == 0 ? 0 : (double)(summary.CurrentTotal / Math.Abs(netWorth) * 100) * (isLiability ? -1 : 1);
                return summary with { PercentOfTotal = percent };
            }).ToList();

            return TypedResults.Ok(new AccountsByPeriodResponse(
                currentStart,
                currentEnd,
                previousStart,
                previousEnd,
                netWorth,
                resultSummaries));
        }

        private static (DateTimeOffset Start, DateTimeOffset End) GetPeriodBoundaries(DateTimeOffset startDate, AccountPeriod period, DateTimeOffset now)
        {
            var start = period switch
            {
                AccountPeriod.Weekly => startDate.AddDays(-(int)startDate.DayOfWeek),
                AccountPeriod.Monthly => new DateTimeOffset(startDate.Year, startDate.Month, 1, 0, 0, 0, TimeSpan.Zero),
                AccountPeriod.Quarterly => new DateTimeOffset(startDate.Year, (startDate.Month - 1) / 3 * 3 + 1, 1, 0, 0, 0, TimeSpan.Zero),
                AccountPeriod.Yearly => new DateTimeOffset(startDate.Year, 1, 1, 0, 0, 0, TimeSpan.Zero),
                _ => throw new ArgumentException("Invalid account period", nameof(period))
            };

            var end = period switch
            {
                AccountPeriod.Weekly => start.AddDays(7),
                AccountPeriod.Monthly => start.AddMonths(1),
                AccountPeriod.Quarterly => start.AddMonths(3),
                AccountPeriod.Yearly => start.AddYears(1),
                _ => throw new ArgumentException("Invalid account period", nameof(period))
            };

            if (end > now)
            {
                end = now;
            }

            return (start, end);
        }

        private static (DateTimeOffset Start, DateTimeOffset End) GetPreviousPeriodBoundaries(AccountPeriod period, DateTimeOffset currentStart)
        {
            var previousStart = period switch
            {
                AccountPeriod.Weekly => currentStart.AddDays(-7),
                AccountPeriod.Monthly => currentStart.AddMonths(-1),
                AccountPeriod.Quarterly => currentStart.AddMonths(-3),
                AccountPeriod.Yearly => currentStart.AddYears(-1),
                _ => throw new ArgumentException("Invalid account period", nameof(period))
            };

            return (previousStart, currentStart);
        }
    }
}

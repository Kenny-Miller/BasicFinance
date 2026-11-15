using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;

namespace BasicFinance.Api.IntegrationTests.Features.Spending;

public class GetSpendingActivityByPeriodTests : ApiTestFixtureBase
{
    private const string Endpoint = "/api/spending/activity-by-period";

    public GetSpendingActivityByPeriodTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetSpendingActivityByPeriod_QuarterlyPeriod_UsesQuarterBoundaries()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId);
        await DbContext.SeedAsync(account, CancellationToken);
        var transactions = new[]
        {
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), amount: 40.00m),
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero), amount: 60.00m),
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero), amount: 500.00m),
        };
        await DbContext.SeedRangeAsync(transactions, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<SpendingByPeriodDto>($"{Endpoint}?StartDate=2026-08-05&TimePeriod=Quarterly", CancellationToken);

        // Assert
        Assert.Equal(new DateTime(2026, 7, 1), result.PeriodStartDate);
        Assert.Equal(new DateTime(2026, 10, 1), result.PeriodEndDate);
        Assert.Equal(100.00m, result.TotalSpend);
    }

    [Fact]
    public async Task GetSpendingActivityByPeriod_WeeklyPeriod_StartsOnMonday()
    {
        // Act: 2026-08-05 is a Wednesday.
        var result = await HttpClient.GetResultAsync<SpendingByPeriodDto>($"{Endpoint}?StartDate=2026-08-05&TimePeriod=Weekly", CancellationToken);

        // Assert
        Assert.Equal(new DateTime(2026, 8, 3), result.PeriodStartDate);
        Assert.Equal(new DateTime(2026, 8, 10), result.PeriodEndDate);
    }

    [Fact]
    public async Task GetSpendingActivityByPeriod_CurrentPeriod_IncludesTodayAndEndsTomorrow()
    {
        // Arrange
        var utcNow = DateTimeOffset.UtcNow;
        var today = utcNow.UtcDateTime.Date;
        var account = AccountFactory.Create(AuthenticatedUserId);
        await DbContext.SeedAsync(account, CancellationToken);
        var todaysTransaction = TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: utcNow, amount: 25.00m);
        await DbContext.SeedAsync(todaysTransaction, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<SpendingByPeriodDto>($"{Endpoint}?StartDate={today:yyyy-MM-dd}&TimePeriod=Yearly", CancellationToken);

        // Assert: the end is exclusive, so it is capped at tomorrow and today's spend is counted.
        Assert.Equal(new DateTime(today.Year, 1, 1), result.PeriodStartDate);
        Assert.Equal(today.AddDays(1), result.PeriodEndDate);
        Assert.Equal(25.00m, result.TotalSpend);
    }

    private sealed record SpendingByPeriodDto(
        DateTime PeriodStartDate,
        DateTime PeriodEndDate,
        decimal TotalSpend,
        decimal TotalIncome);
}

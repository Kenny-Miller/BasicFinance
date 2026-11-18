using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using BasicFinance.Infrastructure.Entities;
using Xunit;
using TransactionCategoryEnum = BasicFinance.Infrastructure.Enums.TransactionCategory;
using TransactionTypeEnum = BasicFinance.Infrastructure.Enums.TransactionType;

namespace BasicFinance.Api.IntegrationTests.Features.Insights;

public class GetSpendingOverTimeTests : ApiTestFixtureBase
{
    private const string Endpoint = "/api/insights/spending-over-time";

    private const string MonthlyQuery = "timePeriod=Monthly";

    private const string OtherUserId = "11111111-1111-1111-1111-111111111111";

    public GetSpendingOverTimeTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    /// <summary>
    /// Gets the start (UTC midnight) of the current monthly period. Seeded transaction dates are expressed relative to it.
    /// </summary>
    private static DateTimeOffset PeriodStart => DateTimeOffset.UtcNow.ToPeriodRange(TimePeriod.Monthly).RangeStartDate;

    /// <summary>
    /// Gets the start (UTC midnight) of the previous monthly period.
    /// </summary>
    private static DateTimeOffset PreviousPeriodStart => DateTimeOffset.UtcNow.ToPeriodRange(TimePeriod.Monthly, -1).RangeStartDate;

    private static int DaysIn(DateTimeOffset monthStart) => DateTime.DaysInMonth(monthStart.Year, monthStart.Month);

    [Theory]
    [InlineData(TimePeriod.Weekly)]
    [InlineData(TimePeriod.Monthly)]
    [InlineData(TimePeriod.Quarterly)]
    [InlineData(TimePeriod.Yearly)]
    public async Task GetSpendingOverTime_NoTransactions_ReturnsZeroTimelinesSizedToLongerPeriod(TimePeriod timePeriod)
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var current = now.ToPeriodWindow(timePeriod);
        var previous = now.ToPeriodWindow(timePeriod, -1);
        var expectedDays = Math.Max(current.Days, previous.Days);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsSpendingOverTimeResponseDto>($"{Endpoint}?timePeriod={timePeriod}", CancellationToken);

        // Assert
        Assert.Equal(previous.Start.UtcDateTime, result.PreviousPeriodStart);
        Assert.Equal(current.Start.UtcDateTime, result.Page.WindowStart);
        Assert.Equal(current.LastDay.UtcDateTime, result.Page.WindowEnd);
        Assert.Null(result.Page.PreviousAsOf);
        Assert.Null(result.Page.NextAsOf);
        Assert.Equal(Enumerable.Range(1, expectedDays), result.CurrentTimeline.Select(x => x.Day));
        Assert.Equal(Enumerable.Range(1, expectedDays), result.PreviousTimeline.Select(x => x.Day));
        Assert.All(result.CurrentTimeline.Concat(result.PreviousTimeline), x => Assert.Equal(0m, x.Amount));
        Assert.Equal(0m, result.TotalSpend);
        Assert.Equal(0m, result.PreviousTotalSpend);
        Assert.Equal(0m, result.SpendChange);
    }

    [Fact]
    public async Task GetSpendingOverTime_SpendAcrossBothPeriods_AccumulatesAndCarriesTotalsForward()
    {
        // Arrange
        var account = await SeedAccountAsync();
        var previousLastDay = DaysIn(PreviousPeriodStart);
        await DbContext.SeedRangeAsync(
            [
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: PreviousPeriodStart.AddHours(9), amount: 30m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: PreviousPeriodStart.AddDays(previousLastDay - 1).AddHours(9), amount: 70m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: PeriodStart.AddHours(9), amount: 10m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: PeriodStart.AddHours(18), amount: 5m),
            ],
            CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsSpendingOverTimeResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(15m, result.CurrentTimeline[0].Amount);
        Assert.All(result.CurrentTimeline, x => Assert.Equal(15m, x.Amount));
        Assert.Equal(30m, result.PreviousTimeline[0].Amount);
        Assert.Equal(30m, result.PreviousTimeline[previousLastDay - 2].Amount);
        Assert.Equal(100m, result.PreviousTimeline[previousLastDay - 1].Amount);
        Assert.Equal(100m, result.PreviousTimeline[^1].Amount);
        Assert.Equal(15m, result.TotalSpend);
        Assert.Equal(100m, result.PreviousTotalSpend);
        Assert.Equal(-85m, result.SpendChange);
    }

    [Fact]
    public async Task GetSpendingOverTime_SpendBeforePreviousPeriod_IsExcluded()
    {
        // Arrange
        var account = await SeedAccountAsync();
        await DbContext.SeedRangeAsync(
            [
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: PreviousPeriodStart.AddDays(-1), amount: 500m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: PreviousPeriodStart.AddHours(1), amount: 20m),
            ],
            CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsSpendingOverTimeResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(20m, result.PreviousTotalSpend);
        Assert.Equal(0m, result.TotalSpend);
    }

    [Theory]
    [InlineData(TransactionCategoryEnum.InternalTransfer)]
    [InlineData(TransactionCategoryEnum.SavingsTransfer)]
    [InlineData(TransactionCategoryEnum.CreditCardPayment)]
    [InlineData(TransactionCategoryEnum.Ignore)]
    public async Task GetSpendingOverTime_CreditsAndTransferCategories_AreExcluded(TransactionCategoryEnum category)
    {
        // Arrange
        var account = await SeedAccountAsync();
        await DbContext.SeedRangeAsync(
            [
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, TransactionTypeEnum.Debit, category, PeriodStart.AddHours(1), 500m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, TransactionTypeEnum.Credit, TransactionCategoryEnum.Income, PeriodStart.AddHours(1), 900m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, TransactionTypeEnum.Debit, TransactionCategoryEnum.Groceries, PeriodStart.AddHours(2), 25m),
            ],
            CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsSpendingOverTimeResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(25m, result.TotalSpend);
    }

    [Fact]
    public async Task GetSpendingOverTime_InstitutionFilter_OnlyIncludesAccountsAtInstitution()
    {
        // Arrange
        var wellsFargo = await SeedAccountAsync();
        var chase = await SeedAccountAsync(TestConstants.ChaseInstitutionId);
        await DbContext.SeedRangeAsync(
            [
                TransactionFactory.Create(AuthenticatedUserId, wellsFargo.AccountId, date: PeriodStart.AddHours(1), amount: 100m),
                TransactionFactory.Create(AuthenticatedUserId, chase.AccountId, date: PeriodStart.AddHours(1), amount: 7m),
            ],
            CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsSpendingOverTimeResponseDto>($"{Endpoint}?{MonthlyQuery}&institutionId={TestConstants.ChaseInstitutionId}", CancellationToken);

        // Assert
        Assert.Equal(7m, result.TotalSpend);
    }

    [Fact]
    public async Task GetSpendingOverTime_OtherUsersAndInactiveData_AreExcluded()
    {
        // Arrange
        var ownAccount = await SeedAccountAsync();
        var otherAccount = AccountFactory.Create(OtherUserId);
        var inactiveAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Inactive");
        inactiveAccount.SetIsActive(false);
        await DbContext.SeedRangeAsync([otherAccount, inactiveAccount], CancellationToken);
        var inactiveTransaction = TransactionFactory.Create(AuthenticatedUserId, ownAccount.AccountId, date: PeriodStart.AddHours(1), amount: 300m);
        inactiveTransaction.IsActive = false;
        await DbContext.SeedRangeAsync(
            [
                TransactionFactory.Create(AuthenticatedUserId, ownAccount.AccountId, date: PeriodStart.AddHours(1), amount: 10m),
                TransactionFactory.Create(OtherUserId, otherAccount.AccountId, date: PeriodStart.AddHours(1), amount: 999m),
                TransactionFactory.Create(AuthenticatedUserId, inactiveAccount.AccountId, date: PeriodStart.AddHours(1), amount: 200m),
                inactiveTransaction,
            ],
            CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsSpendingOverTimeResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(10m, result.TotalSpend);
    }

    [Fact]
    public async Task GetSpendingOverTime_AccountsFromInactiveInstitution_AreExcluded()
    {
        // Arrange
        var inactiveInstitution = InstitutionFactory.Create(name: "Inactive Bank", institutionCode: "INACT");
        await DbContext.SeedAsync(inactiveInstitution, CancellationToken);
        inactiveInstitution.IsActive = false;
        await DbContext.SaveChangesAsync(CancellationToken);
        var account = await SeedAccountAsync(inactiveInstitution.InstitutionId);
        await DbContext.SeedAsync(TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: PeriodStart.AddHours(1), amount: 10m), CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsSpendingOverTimeResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(0m, result.TotalSpend);
    }

    [Fact]
    public async Task GetSpendingOverTime_PastAsOf_ComparesThatPeriodWithTheOneBeforeIt()
    {
        // Arrange
        var account = await SeedAccountAsync();
        await DbContext.SeedRangeAsync(
            [
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: Utc(2026, 1, 5), amount: 500m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: Utc(2026, 2, 10), amount: 30m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: Utc(2026, 3, 5), amount: 10m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: new DateTimeOffset(2026, 3, 31, 23, 59, 59, 999, TimeSpan.Zero), amount: 4m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: Utc(2026, 4, 1), amount: 99m),
            ],
            CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsSpendingOverTimeResponseDto>($"{Endpoint}?timePeriod=Monthly&asOf=2026-03-15", CancellationToken);

        // Assert
        Assert.Equal(new DateTime(2026, 2, 1), result.PreviousPeriodStart);
        Assert.Equal(31, result.CurrentTimeline.Count);
        Assert.Equal(31, result.PreviousTimeline.Count);
        Assert.Equal(0m, result.CurrentTimeline[3].Amount);
        Assert.Equal(10m, result.CurrentTimeline[4].Amount);
        Assert.Equal(14m, result.CurrentTimeline[30].Amount);
        Assert.Equal(30m, result.PreviousTimeline[9].Amount);
        Assert.Equal(14m, result.TotalSpend);
        Assert.Equal(30m, result.PreviousTotalSpend);
        Assert.Equal(new DateTime(2026, 3, 1), result.Page.WindowStart);
        Assert.Equal(new DateTime(2026, 3, 31), result.Page.WindowEnd);
        Assert.Equal(new DateTime(2026, 2, 1), result.Page.PreviousAsOf);
        Assert.Equal(new DateTime(2026, 4, 1), result.Page.NextAsOf);
    }

    [Fact]
    public async Task GetSpendingOverTime_FollowingPreviousThenNextAsOf_ReturnsToTheSamePeriod()
    {
        // Arrange
        var account = await SeedAccountAsync();
        await DbContext.SeedAsync(TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: Utc(2025, 12, 23), amount: 5m), CancellationToken);

        // Act
        var page = await HttpClient.GetResultAsync<InsightsSpendingOverTimeResponseDto>($"{Endpoint}?timePeriod=Weekly&asOf=2026-01-01", CancellationToken);
        var older = await HttpClient.GetResultAsync<InsightsSpendingOverTimeResponseDto>($"{Endpoint}?timePeriod=Weekly&asOf={page.Page.PreviousAsOf:yyyy-MM-dd}", CancellationToken);
        var backAgain = await HttpClient.GetResultAsync<InsightsSpendingOverTimeResponseDto>($"{Endpoint}?timePeriod=Weekly&asOf={older.Page.NextAsOf:yyyy-MM-dd}", CancellationToken);

        // Assert
        Assert.Equal(new DateTime(2025, 12, 29), page.Page.WindowStart);
        Assert.Equal(new DateTime(2026, 1, 4), page.Page.WindowEnd);
        Assert.Equal(new DateTime(2025, 12, 22), older.Page.WindowStart);
        Assert.Null(older.Page.PreviousAsOf);
        Assert.Equal(page.Page.WindowStart, backAgain.Page.WindowStart);
    }

    [Fact]
    public async Task GetSpendingOverTime_FutureAsOf_IsClampedToCurrentPeriod()
    {
        // Arrange
        var future = DateTimeOffset.UtcNow.AddDays(400);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsSpendingOverTimeResponseDto>($"{Endpoint}?timePeriod=Monthly&asOf={future:yyyy-MM-dd}", CancellationToken);

        // Assert
        Assert.Equal(PeriodStart.UtcDateTime, result.Page.WindowStart);
        Assert.Null(result.Page.NextAsOf);
    }

    private static DateTimeOffset Utc(int year, int month, int day) => new(year, month, day, 12, 0, 0, TimeSpan.Zero);

    private async Task<Account> SeedAccountAsync(int institutionId = TestConstants.WellsFargoInstitutionId)
    {
        var account = AccountFactory.Create(AuthenticatedUserId, institutionId: institutionId);
        return await DbContext.SeedAsync(account, CancellationToken);
    }
}

using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;
using AccountTypeEnum = BasicFinance.Infrastructure.Enums.AccountType;

namespace BasicFinance.Api.IntegrationTests.Features.Insights;

public class GetNetworthTests : ApiTestFixtureBase
{
    private const string Endpoint = "/api/insights/networth";

    /// <summary>
    /// A one month lookback ending on a fixed past day, so the window is always 1–31 March 2026 whatever
    /// day the tests run on.
    /// </summary>
    private const string MonthlyQuery = "range=OneMonth&asOf=2026-03-31";

    private const int DaysInPeriod = 31;

    private const string OtherUserId = "11111111-1111-1111-1111-111111111111";

    /// <summary>
    /// The start (UTC midnight) of the window requested by <see cref="MonthlyQuery"/>. Seeded ledger dates are
    /// expressed relative to this value.
    /// </summary>
    private static readonly DateTimeOffset PeriodStart = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

    public GetNetworthTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    private static DateTime Today => DateTimeOffset.UtcNow.UtcDateTime.Date;

    [Fact]
    public async Task GetNetworth_AsOfOmitted_TimelineEndsTodayWithNoNextPage()
    {
        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?range=OneMonth", CancellationToken);

        // Assert
        Assert.Equal(Today, result.NetworthTimeline[^1].Date);
        Assert.Equal(Today, result.Page.WindowEnd);
        Assert.Null(result.Page.NextAsOf);
    }

    [Fact]
    public async Task GetNetworth_FutureAsOf_IsClampedToToday()
    {
        // Arrange
        var future = Today.AddDays(400);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?range=OneMonth&asOf={future:yyyy-MM-dd}", CancellationToken);

        // Assert
        Assert.Equal(Today, result.NetworthTimeline[^1].Date);
        Assert.Null(result.Page.NextAsOf);
    }

    [Fact]
    public async Task GetNetworth_AsOfWithTimeOfDay_UsesWholeDay()
    {
        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?range=OneMonth&asOf=2026-03-31T17:45:00", CancellationToken);

        // Assert
        Assert.Equal(DaysInPeriod, result.NetworthTimeline.Count);
        Assert.Equal(new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Unspecified), result.NetworthTimeline[^1].Date);
    }

    [Theory]
    [InlineData("OneMonth", "2026-03-01")]
    [InlineData("ThreeMonths", "2026-01-01")]
    [InlineData("SixMonths", "2025-10-01")]
    [InlineData("OneYear", "2025-04-01")]
    public async Task GetNetworth_EachRange_LooksBackThatFarFromAsOf(string range, string expectedStart)
    {
        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?range={range}&asOf=2026-03-31", CancellationToken);

        // Assert
        Assert.Equal(DateTime.Parse(expectedStart, System.Globalization.CultureInfo.InvariantCulture), result.Page.WindowStart);
        Assert.Equal(result.Page.WindowStart, result.NetworthTimeline[0].Date);
        Assert.Equal(new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Unspecified), result.Page.WindowEnd);
    }

    [Fact]
    public async Task GetNetworth_AllRange_StartsAtEarliestEntryWithZeroOpeningAndNoPreviousPage()
    {
        // Arrange
        var checking = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "All Checking",
            openingBalance: 100m,
            openingBalanceRecordedDate: new DateTimeOffset(2025, 1, 10, 15, 0, 0, TimeSpan.Zero));
        await DbContext.SeedAsync(checking, CancellationToken);
        await DbContext.SeedAsync(AccountLedgerFactory.CreateFor(checking, 400m, new DateTimeOffset(2025, 3, 5, 9, 0, 0, TimeSpan.Zero)), CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?range=All&asOf=2025-03-31", CancellationToken);

        // Assert
        Assert.Equal(81, result.NetworthTimeline.Count);
        Assert.Equal(new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Unspecified), result.NetworthTimeline[0].Date);
        Assert.Equal(100m, result.NetworthTimeline[0].Networth);
        Assert.Equal(new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Unspecified), result.NetworthTimeline[^1].Date);
        Assert.Equal(400m, result.TotalNetworth);
        Assert.Equal(400m, result.NetworthChange);
        Assert.Equal(new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Unspecified), result.Page.WindowStart);
        Assert.Null(result.Page.PreviousAsOf);
    }

    [Fact]
    public async Task GetNetworth_AllRangeWithNoEntries_ReturnsSingleZeroDay()
    {
        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?range=All&asOf=2025-03-31", CancellationToken);

        // Assert
        var point = Assert.Single(result.NetworthTimeline);
        Assert.Equal(new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Unspecified), point.Date);
        Assert.Equal(0m, point.Networth);
        Assert.Null(result.Page.PreviousAsOf);
    }

    [Fact]
    public async Task GetNetworth_PastPageWithEarlierEntries_PreviousAndNextAsOfStepOneRangeEachWay()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Paging Checking");
        await DbContext.SeedAsync(checking, CancellationToken);
        await DbContext.SeedAsync(AccountLedgerFactory.CreateFor(checking, 1000m, PeriodStart.AddDays(-40)), CancellationToken);

        // Act
        var page = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);
        var older = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?range=OneMonth&asOf={page.Page.PreviousAsOf:yyyy-MM-dd}", CancellationToken);
        var backAgain = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?range=OneMonth&asOf={older.Page.NextAsOf:yyyy-MM-dd}", CancellationToken);

        // Assert
        Assert.Equal(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Unspecified), page.Page.PreviousAsOf);
        Assert.Equal(new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Unspecified), page.Page.NextAsOf);
        Assert.Equal(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Unspecified), older.Page.WindowStart);
        Assert.Equal(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Unspecified), older.Page.WindowEnd);
        Assert.Equal(page.Page.WindowStart, backAgain.Page.WindowStart);
        Assert.Equal(page.Page.WindowEnd, backAgain.Page.WindowEnd);
    }

    [Fact]
    public async Task GetNetworth_NoEntriesBeforeWindow_PreviousAsOfIsNull()
    {
        // Arrange
        var checking = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "New Checking",
            openingBalance: 250m,
            openingBalanceRecordedDate: PeriodStart.AddDays(4));
        await DbContext.SeedAsync(checking, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Null(result.Page.PreviousAsOf);
        Assert.Equal(250m, result.TotalNetworth);
    }

    [Fact]
    public async Task GetNetworth_NoAccounts_ReturnsZeroTimelineForEveryDayOfPeriod()
    {
        // Arrange
        var periodStart = PeriodStart;

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(DaysInPeriod, result.NetworthTimeline.Count);
        Assert.Equal(periodStart.UtcDateTime.Date, result.NetworthTimeline[0].Date);
        Assert.Equal(periodStart.AddDays(DaysInPeriod - 1).UtcDateTime.Date, result.NetworthTimeline[^1].Date);
        Assert.All(result.NetworthTimeline, point => Assert.Equal(0m, point.Networth));
        Assert.Equal(0m, result.TotalNetworth);
        Assert.Equal(0m, result.NetworthChange);
        Assert.Equal(0m, result.NetworthChangePercentage);
    }

    [Fact]
    public async Task GetNetworth_LedgersBeforePeriod_AreCarriedForwardAcrossTimeline()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Carry Checking");
        var savings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Carry Savings");
        await DbContext.SeedRangeAsync([checking, savings], CancellationToken);
        var checkingLedger = AccountLedgerFactory.CreateFor(checking, 1000m, PeriodStart.AddDays(-10));
        // Older than the previous period: must still count toward the opening balance.
        var savingsLedger = AccountLedgerFactory.CreateFor(savings, 5000m, PeriodStart.AddDays(-75));
        await DbContext.SeedRangeAsync([checkingLedger, savingsLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(DaysInPeriod, result.NetworthTimeline.Count);
        Assert.All(result.NetworthTimeline, point => Assert.Equal(6000m, point.Networth));
        Assert.Equal(6000m, result.TotalNetworth);
        Assert.Equal(0m, result.NetworthChange);
        Assert.Equal(0m, result.NetworthChangePercentage);
    }

    [Fact]
    public async Task GetNetworth_OneAccountUpdatedMidPeriod_OtherAccountsKeepLastKnownBalance()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Updated Checking");
        var savings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Unchanged Savings");
        await DbContext.SeedRangeAsync([checking, savings], CancellationToken);
        var openingChecking = AccountLedgerFactory.CreateFor(checking, 1000m, PeriodStart.AddDays(-5));
        var openingSavings = AccountLedgerFactory.CreateFor(savings, 5000m, PeriodStart.AddDays(-5));
        var updatedChecking = AccountLedgerFactory.CreateFor(checking, 1200m, PeriodStart.AddDays(2).AddHours(9));
        await DbContext.SeedRangeAsync([openingChecking, openingSavings, updatedChecking], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(6000m, result.NetworthTimeline[0].Networth);
        Assert.Equal(6000m, result.NetworthTimeline[1].Networth);
        Assert.Equal(6200m, result.NetworthTimeline[2].Networth);
        Assert.Equal(6200m, result.NetworthTimeline[^1].Networth);
        Assert.Equal(6200m, result.TotalNetworth);
        Assert.Equal(200m, result.NetworthChange);
        Assert.Equal(3.33m, result.NetworthChangePercentage);
    }

    [Fact]
    public async Task GetNetworth_MultipleEntriesOnSameDay_UsesLatestRecordedEntry()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Same Day Checking");
        await DbContext.SeedAsync(checking, CancellationToken);
        var morningLedger = AccountLedgerFactory.CreateFor(checking, 500m, PeriodStart.AddDays(1).AddHours(8));
        var eveningLedger = AccountLedgerFactory.CreateFor(checking, 700m, PeriodStart.AddDays(1).AddHours(20));
        await DbContext.SeedRangeAsync([eveningLedger, morningLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(0m, result.NetworthTimeline[0].Networth);
        Assert.Equal(700m, result.NetworthTimeline[1].Networth);
        Assert.Equal(700m, result.TotalNetworth);
    }

    [Fact]
    public async Task GetNetworth_EntriesWithSameRecordedDate_UsesLatestSystemCreatedDate()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Tie Break Checking");
        await DbContext.SeedAsync(checking, CancellationToken);
        var recordedDate = PeriodStart.AddDays(3).AddHours(12);
        var olderLedger = AccountLedgerFactory.CreateFor(checking, 900m, recordedDate, systemCreatedDate: recordedDate.AddMinutes(1));
        var newerLedger = AccountLedgerFactory.CreateFor(checking, 950m, recordedDate, systemCreatedDate: recordedDate.AddMinutes(5));
        await DbContext.SeedRangeAsync([newerLedger, olderLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(950m, result.NetworthTimeline[3].Networth);
        Assert.Equal(950m, result.TotalNetworth);
    }

    [Fact]
    public async Task GetNetworth_AccountWithFirstEntryMidPeriod_ContributesZeroUntilFirstEntry()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Existing Checking");
        var savings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "New Savings");
        await DbContext.SeedRangeAsync([checking, savings], CancellationToken);
        var checkingLedger = AccountLedgerFactory.CreateFor(checking, 1000m, PeriodStart.AddDays(-1));
        var savingsLedger = AccountLedgerFactory.CreateFor(savings, 300m, PeriodStart.AddDays(3));
        await DbContext.SeedRangeAsync([checkingLedger, savingsLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(1000m, result.NetworthTimeline[2].Networth);
        Assert.Equal(1300m, result.NetworthTimeline[3].Networth);
        Assert.Equal(1300m, result.TotalNetworth);
        Assert.Equal(300m, result.NetworthChange);
        Assert.Equal(30m, result.NetworthChangePercentage);
    }

    [Fact]
    public async Task GetNetworth_EntryAtPeriodStart_ReplacesOpeningBalanceOnFirstDay()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Boundary Checking");
        await DbContext.SeedAsync(checking, CancellationToken);
        var openingLedger = AccountLedgerFactory.CreateFor(checking, 1000m, PeriodStart.AddMilliseconds(-1));
        var periodStartLedger = AccountLedgerFactory.CreateFor(checking, 1500m, PeriodStart);
        await DbContext.SeedRangeAsync([openingLedger, periodStartLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(1500m, result.NetworthTimeline[0].Networth);
        Assert.Equal(1500m, result.TotalNetworth);
        Assert.Equal(500m, result.NetworthChange);
        Assert.Equal(50m, result.NetworthChangePercentage);
    }

    [Fact]
    public async Task GetNetworth_NetworthDecreased_ReturnsNegativeChangeAndPercentage()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Decrease Checking");
        await DbContext.SeedAsync(checking, CancellationToken);
        var openingLedger = AccountLedgerFactory.CreateFor(checking, 1000m, PeriodStart.AddDays(-3));
        var currentLedger = AccountLedgerFactory.CreateFor(checking, 800m, PeriodStart.AddDays(4));
        await DbContext.SeedRangeAsync([openingLedger, currentLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(800m, result.TotalNetworth);
        Assert.Equal(-200m, result.NetworthChange);
        Assert.Equal(-20m, result.NetworthChangePercentage);
    }

    [Fact]
    public async Task GetNetworth_NegativeOpeningNetworthImproves_ReturnsPositivePercentage()
    {
        // Arrange
        var creditCard = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.CreditCard, accountName: "Liability Card");
        await DbContext.SeedAsync(creditCard, CancellationToken);
        var openingLedger = AccountLedgerFactory.CreateFor(creditCard, -1000m, PeriodStart.AddDays(-3));
        var currentLedger = AccountLedgerFactory.CreateFor(creditCard, -500m, PeriodStart.AddDays(4));
        await DbContext.SeedRangeAsync([openingLedger, currentLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(-1000m, result.NetworthTimeline[0].Networth);
        Assert.Equal(-500m, result.TotalNetworth);
        Assert.Equal(500m, result.NetworthChange);
        Assert.Equal(50m, result.NetworthChangePercentage);
    }

    [Fact]
    public async Task GetNetworth_InstitutionFilter_OnlyIncludesAccountsAtInstitution()
    {
        // Arrange
        var wellsFargoChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "WF Checking");
        var chaseSavings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Chase Savings", institutionId: TestConstants.ChaseInstitutionId);
        await DbContext.SeedRangeAsync([wellsFargoChecking, chaseSavings], CancellationToken);
        var wellsFargoLedger = AccountLedgerFactory.CreateFor(wellsFargoChecking, 1000m, PeriodStart.AddDays(-1));
        var chaseLedger = AccountLedgerFactory.CreateFor(chaseSavings, 4000m, PeriodStart.AddDays(-1));
        await DbContext.SeedRangeAsync([wellsFargoLedger, chaseLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}&institutionId={TestConstants.WellsFargoInstitutionId}", CancellationToken);

        // Assert
        Assert.All(result.NetworthTimeline, point => Assert.Equal(1000m, point.Networth));
        Assert.Equal(1000m, result.TotalNetworth);
    }

    [Fact]
    public async Task GetNetworth_AccountsFromOtherUsers_AreExcluded()
    {
        // Arrange
        var ownAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Own Checking");
        var otherAccount = AccountFactory.Create(OtherUserId, accountName: "Other Checking");
        await DbContext.SeedRangeAsync([ownAccount, otherAccount], CancellationToken);
        var ownLedger = AccountLedgerFactory.CreateFor(ownAccount, 100m, PeriodStart.AddDays(1));
        var otherLedger = AccountLedgerFactory.CreateFor(otherAccount, 900m, PeriodStart.AddDays(1));
        await DbContext.SeedRangeAsync([ownLedger, otherLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(100m, result.NetworthTimeline[1].Networth);
        Assert.Equal(100m, result.TotalNetworth);
    }

    [Fact]
    public async Task GetNetworth_InactiveAccounts_AreExcluded()
    {
        // Arrange
        var activeChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "Active Checking");
        var inactiveSavings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Inactive Savings");
        inactiveSavings.SetIsActive(false);
        await DbContext.SeedRangeAsync([activeChecking, inactiveSavings], CancellationToken);
        var activeLedger = AccountLedgerFactory.CreateFor(activeChecking, 1000m, PeriodStart.AddDays(-1));
        var inactiveLedger = AccountLedgerFactory.CreateFor(inactiveSavings, 500m, PeriodStart.AddDays(-1));
        await DbContext.SeedRangeAsync([activeLedger, inactiveLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.All(result.NetworthTimeline, point => Assert.Equal(1000m, point.Networth));
        Assert.Equal(1000m, result.TotalNetworth);
    }

    [Fact]
    public async Task GetNetworth_AccountsFromInactiveInstitution_AreExcluded()
    {
        // Arrange
        var inactiveInstitution = InstitutionFactory.Create(name: "Inactive Bank", institutionCode: "INACT");
        await DbContext.SeedAsync(inactiveInstitution, CancellationToken);
        inactiveInstitution.IsActive = false;
        await DbContext.SaveChangesAsync(CancellationToken);
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: "Inactive Bank Checking", institutionId: inactiveInstitution.InstitutionId);
        await DbContext.SeedAsync(account, CancellationToken);
        var ledger = AccountLedgerFactory.CreateFor(account, 750m, PeriodStart.AddDays(-1));
        await DbContext.SeedAsync(ledger, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.All(result.NetworthTimeline, point => Assert.Equal(0m, point.Networth));
        Assert.Equal(0m, result.TotalNetworth);
    }
}

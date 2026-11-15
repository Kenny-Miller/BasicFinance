using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using Xunit;
using AccountTypeEnum = BasicFinance.Infrastructure.Enums.AccountType;

namespace BasicFinance.Api.IntegrationTests.Features.Insights;

public class GetMyNetworthTests : ApiTestFixtureBase
{
    private const string Endpoint = "/api/insights/networth";

    private const string MonthlyQuery = "timePeriod=Monthly";

    private const string OtherUserId = "11111111-1111-1111-1111-111111111111";

    public GetMyNetworthTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    /// <summary>
    /// Gets the start (UTC midnight) of the current monthly period. The endpoint always reports the period
    /// containing today, so seeded ledger dates are expressed relative to this value. Offsets of up to 27 days
    /// always fall inside the period.
    /// </summary>
    private static DateTimeOffset PeriodStart => DateTimeOffset.UtcNow.ToPeriodRange(TimePeriod.Monthly).RangeStartDate;

    private static int DaysInPeriod => DateTime.DaysInMonth(PeriodStart.Year, PeriodStart.Month);

    [Fact]
    public async Task GetMyNetworth_NoAccounts_ReturnsZeroTimelineForEveryDayOfPeriod()
    {
        // Arrange
        var periodStart = PeriodStart;

        // Act
        var result = await HttpClient.GetResultAsync<MyNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

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
    public async Task GetMyNetworth_LedgersBeforePeriod_AreCarriedForwardAcrossTimeline()
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
        var result = await HttpClient.GetResultAsync<MyNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(DaysInPeriod, result.NetworthTimeline.Count);
        Assert.All(result.NetworthTimeline, point => Assert.Equal(6000m, point.Networth));
        Assert.Equal(6000m, result.TotalNetworth);
        Assert.Equal(0m, result.NetworthChange);
        Assert.Equal(0m, result.NetworthChangePercentage);
    }

    [Fact]
    public async Task GetMyNetworth_OneAccountUpdatedMidPeriod_OtherAccountsKeepLastKnownBalance()
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
        var result = await HttpClient.GetResultAsync<MyNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

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
    public async Task GetMyNetworth_MultipleEntriesOnSameDay_UsesLatestRecordedEntry()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Same Day Checking");
        await DbContext.SeedAsync(checking, CancellationToken);
        var morningLedger = AccountLedgerFactory.CreateFor(checking, 500m, PeriodStart.AddDays(1).AddHours(8));
        var eveningLedger = AccountLedgerFactory.CreateFor(checking, 700m, PeriodStart.AddDays(1).AddHours(20));
        await DbContext.SeedRangeAsync([eveningLedger, morningLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<MyNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(0m, result.NetworthTimeline[0].Networth);
        Assert.Equal(700m, result.NetworthTimeline[1].Networth);
        Assert.Equal(700m, result.TotalNetworth);
    }

    [Fact]
    public async Task GetMyNetworth_EntriesWithSameRecordedDate_UsesLatestSystemCreatedDate()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Tie Break Checking");
        await DbContext.SeedAsync(checking, CancellationToken);
        var recordedDate = PeriodStart.AddDays(3).AddHours(12);
        var olderLedger = AccountLedgerFactory.CreateFor(checking, 900m, recordedDate, systemCreatedDate: recordedDate.AddMinutes(1));
        var newerLedger = AccountLedgerFactory.CreateFor(checking, 950m, recordedDate, systemCreatedDate: recordedDate.AddMinutes(5));
        await DbContext.SeedRangeAsync([newerLedger, olderLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<MyNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(950m, result.NetworthTimeline[3].Networth);
        Assert.Equal(950m, result.TotalNetworth);
    }

    [Fact]
    public async Task GetMyNetworth_AccountWithFirstEntryMidPeriod_ContributesZeroUntilFirstEntry()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Existing Checking");
        var savings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "New Savings");
        await DbContext.SeedRangeAsync([checking, savings], CancellationToken);
        var checkingLedger = AccountLedgerFactory.CreateFor(checking, 1000m, PeriodStart.AddDays(-1));
        var savingsLedger = AccountLedgerFactory.CreateFor(savings, 300m, PeriodStart.AddDays(3));
        await DbContext.SeedRangeAsync([checkingLedger, savingsLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<MyNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(1000m, result.NetworthTimeline[2].Networth);
        Assert.Equal(1300m, result.NetworthTimeline[3].Networth);
        Assert.Equal(1300m, result.TotalNetworth);
        Assert.Equal(300m, result.NetworthChange);
        Assert.Equal(30m, result.NetworthChangePercentage);
    }

    [Fact]
    public async Task GetMyNetworth_EntryAtPeriodStart_ReplacesOpeningBalanceOnFirstDay()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Boundary Checking");
        await DbContext.SeedAsync(checking, CancellationToken);
        var openingLedger = AccountLedgerFactory.CreateFor(checking, 1000m, PeriodStart.AddMilliseconds(-1));
        var periodStartLedger = AccountLedgerFactory.CreateFor(checking, 1500m, PeriodStart);
        await DbContext.SeedRangeAsync([openingLedger, periodStartLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<MyNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(1500m, result.NetworthTimeline[0].Networth);
        Assert.Equal(1500m, result.TotalNetworth);
        Assert.Equal(500m, result.NetworthChange);
        Assert.Equal(50m, result.NetworthChangePercentage);
    }

    [Fact]
    public async Task GetMyNetworth_NetworthDecreased_ReturnsNegativeChangeAndPercentage()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Decrease Checking");
        await DbContext.SeedAsync(checking, CancellationToken);
        var openingLedger = AccountLedgerFactory.CreateFor(checking, 1000m, PeriodStart.AddDays(-3));
        var currentLedger = AccountLedgerFactory.CreateFor(checking, 800m, PeriodStart.AddDays(4));
        await DbContext.SeedRangeAsync([openingLedger, currentLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<MyNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(800m, result.TotalNetworth);
        Assert.Equal(-200m, result.NetworthChange);
        Assert.Equal(-20m, result.NetworthChangePercentage);
    }

    [Fact]
    public async Task GetMyNetworth_NegativeOpeningNetworthImproves_ReturnsPositivePercentage()
    {
        // Arrange
        var creditCard = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.CreditCard, accountName: "Liability Card");
        await DbContext.SeedAsync(creditCard, CancellationToken);
        var openingLedger = AccountLedgerFactory.CreateFor(creditCard, -1000m, PeriodStart.AddDays(-3));
        var currentLedger = AccountLedgerFactory.CreateFor(creditCard, -500m, PeriodStart.AddDays(4));
        await DbContext.SeedRangeAsync([openingLedger, currentLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<MyNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(-1000m, result.NetworthTimeline[0].Networth);
        Assert.Equal(-500m, result.TotalNetworth);
        Assert.Equal(500m, result.NetworthChange);
        Assert.Equal(50m, result.NetworthChangePercentage);
    }

    [Fact]
    public async Task GetMyNetworth_InstitutionFilter_OnlyIncludesAccountsAtInstitution()
    {
        // Arrange
        var wellsFargoChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "WF Checking");
        var chaseSavings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Chase Savings", institutionId: TestConstants.ChaseInstitutionId);
        await DbContext.SeedRangeAsync([wellsFargoChecking, chaseSavings], CancellationToken);
        var wellsFargoLedger = AccountLedgerFactory.CreateFor(wellsFargoChecking, 1000m, PeriodStart.AddDays(-1));
        var chaseLedger = AccountLedgerFactory.CreateFor(chaseSavings, 4000m, PeriodStart.AddDays(-1));
        await DbContext.SeedRangeAsync([wellsFargoLedger, chaseLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<MyNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}&institutionId={TestConstants.WellsFargoInstitutionId}", CancellationToken);

        // Assert
        Assert.All(result.NetworthTimeline, point => Assert.Equal(1000m, point.Networth));
        Assert.Equal(1000m, result.TotalNetworth);
    }

    [Fact]
    public async Task GetMyNetworth_AccountsFromOtherUsers_AreExcluded()
    {
        // Arrange
        var ownAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Own Checking");
        var otherAccount = AccountFactory.Create(OtherUserId, accountName: "Other Checking");
        await DbContext.SeedRangeAsync([ownAccount, otherAccount], CancellationToken);
        var ownLedger = AccountLedgerFactory.CreateFor(ownAccount, 100m, PeriodStart.AddDays(1));
        var otherLedger = AccountLedgerFactory.CreateFor(otherAccount, 900m, PeriodStart.AddDays(1));
        await DbContext.SeedRangeAsync([ownLedger, otherLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<MyNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(100m, result.NetworthTimeline[1].Networth);
        Assert.Equal(100m, result.TotalNetworth);
    }

    [Fact]
    public async Task GetMyNetworth_InactiveAccounts_AreExcluded()
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
        var result = await HttpClient.GetResultAsync<MyNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.All(result.NetworthTimeline, point => Assert.Equal(1000m, point.Networth));
        Assert.Equal(1000m, result.TotalNetworth);
    }

    [Fact]
    public async Task GetMyNetworth_AccountsFromInactiveInstitution_AreExcluded()
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
        var result = await HttpClient.GetResultAsync<MyNetworthResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.All(result.NetworthTimeline, point => Assert.Equal(0m, point.Networth));
        Assert.Equal(0m, result.TotalNetworth);
    }
}

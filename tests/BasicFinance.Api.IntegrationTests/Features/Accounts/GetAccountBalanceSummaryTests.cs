using System.Net;
using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;
using AccountTypeEnum = BasicFinance.Infrastructure.Enums.AccountType;

namespace BasicFinance.Api.IntegrationTests.Features.Accounts;

public class GetAccountBalanceSummaryTests : ApiTestFixtureBase
{
    private const string EndpointUrl = "/api/accounts/balanceSummary";

    private static readonly DateTimeOffset AnchorDate = new(2026, 8, 7, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CurrentMonthRecordedDate = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PreviousMonthRecordedDate = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PreviousQuarterRecordedDate = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public GetAccountBalanceSummaryTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetAllAccountAnalytics_NoQueryParameters_ReturnsMonthlyPeriodWithCurrentPeriodBalances()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: "Monthly Default Checking", balance: 1000m);
        var history = AccountBalanceHistoryFactory.CreateFor(account);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedAsync(history, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<BalanceSummaryResponseDto>(EndpointUrl, CancellationToken);

        // Assert
        var monthStart = new DateOnly(now.Year, now.Month, 1);
        Assert.Equal(1000m, result.CurrentPeriodBreakdown.Balance);
        Assert.Equal(0m, result.PreviousPeriodBreakdown.Balance);
        Assert.Equal(monthStart, result.CurrentPeriodStart);
        Assert.Equal(monthStart.AddMonths(1), result.CurrentPeriodEnd);
        Assert.Equal(monthStart.AddMonths(-1), result.PreviousPeriodStart);
        Assert.Equal(monthStart, result.PreviousPeriodEnd);
        Assert.Single(result.CurrentPeriodBreakdown.AccountTypeBreakdowns);
        Assert.Equal(1000m, result.CurrentPeriodBreakdown.AccountTypeBreakdowns["CHK"].Balance);
        Assert.Equal(account.AccountId, result.CurrentPeriodBreakdown.AccountTypeBreakdowns["CHK"].Accounts.Single().Id);
    }

    [Fact]
    public async Task GetAllAccountAnalytics_LatestHistoryOnOrBeforePeriodEnd_ReturnsLatestBalancesPerPeriod()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: "Current Checking", balance: 1200m);
        var previousHistory = AccountBalanceHistoryFactory.CreateFor(account, balance: 1500m, balanceRecordedDate: PreviousMonthRecordedDate);
        var currentHistory = AccountBalanceHistoryFactory.CreateFor(account, balance: 1200m, balanceRecordedDate: CurrentMonthRecordedDate);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedRangeAsync([previousHistory, currentHistory], CancellationToken);

        // Act
        var result = await GetResultAsync(AnchorDate, "Monthly");

        // Assert
        Assert.Equal(1200m, result.CurrentPeriodBreakdown.Balance);
        Assert.Equal(1500m, result.PreviousPeriodBreakdown.Balance);
        Assert.Equal(new DateOnly(2026, 8, 1), result.CurrentPeriodStart);
        Assert.Equal(new DateOnly(2026, 9, 1), result.CurrentPeriodEnd);
        Assert.Equal(new DateOnly(2026, 7, 1), result.PreviousPeriodStart);
        Assert.Equal(new DateOnly(2026, 8, 1), result.PreviousPeriodEnd);
    }

    [Fact]
    public async Task GetAllAccountAnalytics_NoHistoryInCurrentPeriod_CarriesForwardLastKnownBalance()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: "Carried Forward Checking", balance: 900m);
        var history = AccountBalanceHistoryFactory.CreateFor(account, balance: 900m, balanceRecordedDate: PreviousMonthRecordedDate);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedAsync(history, CancellationToken);

        // Act
        var result = await GetResultAsync(AnchorDate, "Monthly");

        // Assert
        Assert.Equal(900m, result.CurrentPeriodBreakdown.Balance);
        Assert.Equal(900m, result.PreviousPeriodBreakdown.Balance);
    }

    [Fact]
    public async Task GetAllAccountAnalytics_QuarterlyPeriod_ReturnsQuarterlyBoundariesAndBalances()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: "Quarterly Checking", balance: 1300m);
        var previousQuarterHistory = AccountBalanceHistoryFactory.CreateFor(account, balance: 700m, balanceRecordedDate: PreviousQuarterRecordedDate);
        var currentQuarterHistory = AccountBalanceHistoryFactory.CreateFor(account, balance: 1300m, balanceRecordedDate: CurrentMonthRecordedDate);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedRangeAsync([previousQuarterHistory, currentQuarterHistory], CancellationToken);

        // Act
        var result = await GetResultAsync(AnchorDate, "Quarterly");

        // Assert
        Assert.Equal(1300m, result.CurrentPeriodBreakdown.Balance);
        Assert.Equal(700m, result.PreviousPeriodBreakdown.Balance);
        Assert.Equal(new DateOnly(2026, 7, 1), result.CurrentPeriodStart);
        Assert.Equal(new DateOnly(2026, 10, 1), result.CurrentPeriodEnd);
        Assert.Equal(new DateOnly(2026, 4, 1), result.PreviousPeriodStart);
        Assert.Equal(new DateOnly(2026, 7, 1), result.PreviousPeriodEnd);
    }

    [Fact]
    public async Task GetAllAccountAnalytics_InactiveAccountHistory_IsExcluded()
    {
        // Arrange
        var activeAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Active Checking", balance: 500m);
        var inactiveAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Inactive Checking", balance: 999m);
        inactiveAccount.IsActive = false;
        var activeHistory = AccountBalanceHistoryFactory.CreateFor(activeAccount, balance: 500m, balanceRecordedDate: CurrentMonthRecordedDate);
        var inactiveHistory = AccountBalanceHistoryFactory.CreateFor(inactiveAccount, balance: 999m, balanceRecordedDate: CurrentMonthRecordedDate);
        await DbContext.SeedRangeAsync([activeAccount, inactiveAccount], CancellationToken);
        await DbContext.SeedRangeAsync([activeHistory, inactiveHistory], CancellationToken);

        // Act
        var result = await GetResultAsync(AnchorDate, "Monthly");

        // Assert
        Assert.Equal(500m, result.CurrentPeriodBreakdown.Balance);
        Assert.Single(result.CurrentPeriodBreakdown.AccountTypeBreakdowns["CHK"].Accounts);
        Assert.Equal(activeAccount.AccountId, result.CurrentPeriodBreakdown.AccountTypeBreakdowns["CHK"].Accounts.Single().Id);
    }

    [Fact]
    public async Task GetAllAccountAnalytics_AnotherUsersHistory_IsExcluded()
    {
        // Arrange
        var otherUserId = Guid.NewGuid().ToString();
        var myAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Mine Checking", balance: 500m);
        var otherAccount = AccountFactory.Create(otherUserId, accountName: "Other Checking", balance: 777m);
        var myHistory = AccountBalanceHistoryFactory.CreateFor(myAccount, balance: 500m, balanceRecordedDate: CurrentMonthRecordedDate);
        var otherHistory = AccountBalanceHistoryFactory.CreateFor(otherAccount, balance: 777m, balanceRecordedDate: CurrentMonthRecordedDate);
        await DbContext.SeedRangeAsync([myAccount, otherAccount], CancellationToken);
        await DbContext.SeedRangeAsync([myHistory, otherHistory], CancellationToken);

        // Act
        var result = await GetResultAsync(AnchorDate, "Monthly");

        // Assert
        Assert.Equal(500m, result.CurrentPeriodBreakdown.Balance);
        Assert.Single(result.CurrentPeriodBreakdown.AccountTypeBreakdowns["CHK"].Accounts);
        Assert.Equal(myAccount.AccountId, result.CurrentPeriodBreakdown.AccountTypeBreakdowns["CHK"].Accounts.Single().Id);
    }

    [Fact]
    public async Task GetAllAccountAnalytics_CreditCardBalance_IsTreatedAsLiability()
    {
        // Arrange
        var checkingAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Checking Liability", balance: 500m);
        var creditAccount = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.CreditCard, accountName: "Credit Liability", balance: 200m);
        var checkingHistory = AccountBalanceHistoryFactory.CreateFor(checkingAccount, balance: 500m, balanceRecordedDate: CurrentMonthRecordedDate);
        var creditHistory = AccountBalanceHistoryFactory.CreateFor(creditAccount, balance: 200m, balanceRecordedDate: CurrentMonthRecordedDate);
        await DbContext.SeedRangeAsync([checkingAccount, creditAccount], CancellationToken);
        await DbContext.SeedRangeAsync([checkingHistory, creditHistory], CancellationToken);

        // Act
        var result = await GetResultAsync(AnchorDate, "Monthly");

        // Assert
        var current = result.CurrentPeriodBreakdown;
        Assert.Equal(300m, current.Balance);
        Assert.Equal(500m, current.AccountTypeBreakdowns["CHK"].Balance);
        Assert.Equal(-200m, current.AccountTypeBreakdowns["CC"].Balance);
        Assert.Equal(200m, current.AccountTypeBreakdowns["CC"].Accounts.Single().Balance);
        Assert.Equal(-67m, current.AccountTypeBreakdowns["CC"].Accounts.Single().PercentageOfTotalBalance);
        Assert.Equal(167m, current.AccountTypeBreakdowns["CHK"].Accounts.Single().PercentageOfTotalBalance);
        Assert.Equal(0m, result.PreviousPeriodBreakdown.Balance);
    }

    [Fact]
    public async Task GetAllAccountAnalytics_InvalidTimePeriod_ReturnsBadRequest()
    {
        // Act
        var response = await HttpClient.GetAsync($"{EndpointUrl}?TimePeriod=EveryBlueMoon", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<BalanceSummaryResponseDto> GetResultAsync(DateTimeOffset recordedDate, string timePeriod = "Monthly") =>
        HttpClient.GetResultAsync<BalanceSummaryResponseDto>($"{EndpointUrl}?recordedDate={recordedDate:O}&timePeriod={timePeriod}", CancellationToken);
}

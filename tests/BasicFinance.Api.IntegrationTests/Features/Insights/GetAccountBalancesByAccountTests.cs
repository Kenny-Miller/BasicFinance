using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using BasicFinance.Domain.Enums;
using BasicFinance.Domain.Extensions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AccountTypeEnum = BasicFinance.Infrastructure.Enums.AccountType;

namespace BasicFinance.Api.IntegrationTests.Features.Insights;

public class GetAccountBalancesByAccountTests : ApiTestFixtureBase
{
    private const string Endpoint = "/api/insights/account-balances/by-account";

    private const string MonthlyQuery = "timePeriod=Monthly";

    private const string OtherUserId = "11111111-1111-1111-1111-111111111111";

    public GetAccountBalancesByAccountTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    /// <summary>
    /// Gets the start (UTC midnight) of the current monthly period. The endpoint measures change from the start
    /// of the period containing today, so seeded ledger dates are expressed relative to this value.
    /// </summary>
    private static DateTimeOffset PeriodStart => DateTimeOffset.UtcNow.ToPeriodRange(TimePeriod.Monthly).RangeStartDate;

    [Fact]
    public async Task GetAccountBalancesByAccount_NoAccounts_ReturnsEmptyList()
    {
        // Arrange
        const string parameters = MonthlyQuery;

        // Act
        var result = await GetAccountsAsync(parameters);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_AssetGrew_ReturnsAccountDetailsLatestBalanceAndPositivePercent()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Everyday Checking");
        await DbContext.SeedAsync(checking, CancellationToken);
        var openingLedger = AccountLedgerFactory.CreateFor(checking, 1000m, PeriodStart.AddDays(-5));
        var latestLedger = AccountLedgerFactory.CreateFor(checking, 1031m, PeriodStart.AddDays(1));
        await DbContext.SeedRangeAsync([openingLedger, latestLedger], CancellationToken);

        // Act
        var result = await GetAccountsAsync(MonthlyQuery);

        // Assert
        var account = Assert.Single(result);
        Assert.Equal(checking.AccountId, account.AccountId);
        Assert.Equal("Everyday Checking", account.AccountName);
        Assert.Equal("CHK", account.AccountTypeCode);
        Assert.Equal("Checking", account.AccountTypeName);
        Assert.Equal("Wells Fargo", account.InstitutionName);
        Assert.False(account.IsLiability);
        Assert.Equal(1031m, account.Balance);
        Assert.Equal(3.1m, account.PercentChange);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_AssetShrank_ReturnsNegativePercent()
    {
        // Arrange
        var savings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Shrinking Savings");
        await DbContext.SeedAsync(savings, CancellationToken);
        var openingLedger = AccountLedgerFactory.CreateFor(savings, 2000m, PeriodStart.AddDays(-5));
        var latestLedger = AccountLedgerFactory.CreateFor(savings, 1500m, PeriodStart.AddDays(1));
        await DbContext.SeedRangeAsync([openingLedger, latestLedger], CancellationToken);

        // Act
        var result = await GetAccountsAsync(MonthlyQuery);

        // Assert
        var account = Assert.Single(result);
        Assert.Equal(1500m, account.Balance);
        Assert.Equal(-25m, account.PercentChange);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_LiabilityDebtGrew_ReturnsPositivePercent()
    {
        // Arrange
        var creditCard = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.CreditCard, accountName: "Growing Card");
        await DbContext.SeedAsync(creditCard, CancellationToken);
        var openingLedger = AccountLedgerFactory.CreateFor(creditCard, -2000m, PeriodStart.AddDays(-5));
        var latestLedger = AccountLedgerFactory.CreateFor(creditCard, -2240m, PeriodStart.AddDays(1));
        await DbContext.SeedRangeAsync([openingLedger, latestLedger], CancellationToken);

        // Act
        var result = await GetAccountsAsync(MonthlyQuery);

        // Assert
        var account = Assert.Single(result);
        Assert.True(account.IsLiability);
        Assert.Equal("CC", account.AccountTypeCode);
        Assert.Equal(-2240m, account.Balance);
        Assert.Equal(12m, account.PercentChange);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_LiabilityPaidDown_ReturnsNegativePercent()
    {
        // Arrange
        var creditCard = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.CreditCard, accountName: "Paid Down Card");
        await DbContext.SeedAsync(creditCard, CancellationToken);
        var openingLedger = AccountLedgerFactory.CreateFor(creditCard, -2000m, PeriodStart.AddDays(-5));
        var latestLedger = AccountLedgerFactory.CreateFor(creditCard, -1800m, PeriodStart.AddDays(1));
        await DbContext.SeedRangeAsync([openingLedger, latestLedger], CancellationToken);

        // Act
        var result = await GetAccountsAsync(MonthlyQuery);

        // Assert
        var account = Assert.Single(result);
        Assert.Equal(-1800m, account.Balance);
        Assert.Equal(-10m, account.PercentChange);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_NoEntryBeforePeriod_ReturnsLatestBalanceAndZeroPercent()
    {
        // Arrange
        var checking = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "New Checking",
            openingBalance: 500m,
            openingBalanceRecordedDate: PeriodStart.AddDays(1));
        await DbContext.SeedAsync(checking, CancellationToken);

        // Act
        var result = await GetAccountsAsync(MonthlyQuery);

        // Assert
        var account = Assert.Single(result);
        Assert.Equal(500m, account.Balance);
        Assert.Equal(0m, account.PercentChange);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_EntriesWithSameRecordedDate_UsesLatestSystemCreatedDate()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Tie Break Checking");
        await DbContext.SeedAsync(checking, CancellationToken);
        var recordedDate = PeriodStart.AddDays(1).AddHours(12);
        var olderLedger = AccountLedgerFactory.CreateFor(checking, 900m, recordedDate, systemCreatedDate: recordedDate.AddMinutes(1));
        var newerLedger = AccountLedgerFactory.CreateFor(checking, 950m, recordedDate, systemCreatedDate: recordedDate.AddMinutes(5));
        await DbContext.SeedRangeAsync([newerLedger, olderLedger], CancellationToken);

        // Act
        var result = await GetAccountsAsync(MonthlyQuery);

        // Assert
        var account = Assert.Single(result);
        Assert.Equal(950m, account.Balance);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_MultipleAccountsOfSameType_ReturnsEachAccountSeparately()
    {
        // Arrange
        var firstChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "A Checking");
        var secondChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "B Checking");
        await DbContext.SeedRangeAsync([firstChecking, secondChecking], CancellationToken);
        var firstLedger = AccountLedgerFactory.CreateFor(firstChecking, 100m, PeriodStart.AddDays(-1));
        var secondLedger = AccountLedgerFactory.CreateFor(secondChecking, 200m, PeriodStart.AddDays(-1));
        await DbContext.SeedRangeAsync([firstLedger, secondLedger], CancellationToken);

        // Act
        var result = await GetAccountsAsync(MonthlyQuery);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(100m, result.Single(x => x.AccountId == firstChecking.AccountId).Balance);
        Assert.Equal(200m, result.Single(x => x.AccountId == secondChecking.AccountId).Balance);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_MixedAccounts_OrdersByAccountTypeThenName()
    {
        // Arrange
        var investment = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Investment, accountName: "A Brokerage");
        var creditCard = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.CreditCard, accountName: "A Card");
        var zuluChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "Z Checking");
        var savings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "A Savings");
        var alphaChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "B Checking");
        await DbContext.SeedRangeAsync([investment, creditCard, zuluChecking, savings, alphaChecking], CancellationToken);
        string[] expectedOrder = ["B Checking", "Z Checking", "A Savings", "A Card", "A Brokerage"];

        // Act
        var result = await GetAccountsAsync(MonthlyQuery);

        // Assert
        Assert.Equal(expectedOrder, result.Select(x => x.AccountName));
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_InstitutionFilter_OnlyIncludesAccountsAtInstitution()
    {
        // Arrange
        var wellsFargoChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "WF Checking");
        var chaseSavings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Chase Savings", institutionId: TestConstants.ChaseInstitutionId);
        await DbContext.SeedRangeAsync([wellsFargoChecking, chaseSavings], CancellationToken);

        // Act
        var result = await GetAccountsAsync($"{MonthlyQuery}&institutionId={TestConstants.ChaseInstitutionId}");

        // Assert
        var account = Assert.Single(result);
        Assert.Equal(chaseSavings.AccountId, account.AccountId);
        Assert.Equal("Chase", account.InstitutionName);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_AccountsFromOtherUsers_AreExcluded()
    {
        // Arrange
        var ownAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Own Checking");
        var otherAccount = AccountFactory.Create(OtherUserId, accountName: "Other Checking");
        await DbContext.SeedRangeAsync([ownAccount, otherAccount], CancellationToken);

        // Act
        var result = await GetAccountsAsync(MonthlyQuery);

        // Assert
        var account = Assert.Single(result);
        Assert.Equal(ownAccount.AccountId, account.AccountId);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_InactiveAccounts_AreExcluded()
    {
        // Arrange
        var activeChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "Active Checking");
        var inactiveSavings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Inactive Savings");
        inactiveSavings.SetIsActive(false);
        await DbContext.SeedRangeAsync([activeChecking, inactiveSavings], CancellationToken);

        // Act
        var result = await GetAccountsAsync(MonthlyQuery);

        // Assert
        var account = Assert.Single(result);
        Assert.Equal(activeChecking.AccountId, account.AccountId);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_AccountsWithInactiveAccountType_AreExcluded()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Active Type Checking");
        var investment = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Investment, accountName: "Inactive Type Brokerage");
        await DbContext.SeedRangeAsync([checking, investment], CancellationToken);
        var investmentType = await DbContext.AccountTypes.SingleAsync(x => x.AccountTypeCode == "INV", CancellationToken);
        investmentType.IsActive = false;
        await DbContext.SaveChangesAsync(CancellationToken);

        // Act
        var result = await GetAccountsAsync(MonthlyQuery);

        // Assert
        var account = Assert.Single(result);
        Assert.Equal(checking.AccountId, account.AccountId);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_AccountsFromInactiveInstitution_AreExcluded()
    {
        // Arrange
        var inactiveInstitution = InstitutionFactory.Create(name: "Inactive Bank", institutionCode: "INACT");
        await DbContext.SeedAsync(inactiveInstitution, CancellationToken);
        inactiveInstitution.IsActive = false;
        await DbContext.SaveChangesAsync(CancellationToken);
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: "Inactive Bank Checking", institutionId: inactiveInstitution.InstitutionId);
        await DbContext.SeedAsync(account, CancellationToken);

        // Act
        var result = await GetAccountsAsync(MonthlyQuery);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_PastAsOf_MeasuresThatPeriodAndIgnoresLaterEntries()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "History Checking");
        await DbContext.SeedAsync(checking, CancellationToken);
        await DbContext.SeedRangeAsync(
            [
                AccountLedgerFactory.CreateFor(checking, 1000m, new DateTimeOffset(2026, 2, 20, 9, 0, 0, TimeSpan.Zero)),
                AccountLedgerFactory.CreateFor(checking, 1100m, new DateTimeOffset(2026, 3, 31, 23, 59, 59, 999, TimeSpan.Zero)),
                AccountLedgerFactory.CreateFor(checking, 2000m, new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)),
            ],
            CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsAccountBalancesByAccountResponseDto>($"{Endpoint}?timePeriod=Monthly&asOf=2026-03-15", CancellationToken);

        // Assert
        var account = Assert.Single(result.Accounts);
        Assert.Equal(1100m, account.Balance);
        Assert.Equal(10m, account.PercentChange);
        Assert.Equal(new DateTime(2026, 3, 1), result.Page.WindowStart);
        Assert.Equal(new DateTime(2026, 3, 31), result.Page.WindowEnd);
        Assert.Equal(new DateTime(2026, 2, 1), result.Page.PreviousAsOf);
        Assert.Equal(new DateTime(2026, 4, 1), result.Page.NextAsOf);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_CurrentPeriodWithNoEarlierEntries_HasNoNeighbouringPages()
    {
        // Arrange
        var checking = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "Brand New Checking",
            openingBalance: 500m,
            openingBalanceRecordedDate: PeriodStart);
        await DbContext.SeedAsync(checking, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsAccountBalancesByAccountResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(PeriodStart.UtcDateTime, result.Page.WindowStart);
        Assert.Null(result.Page.PreviousAsOf);
        Assert.Null(result.Page.NextAsOf);
    }

    [Fact]
    public async Task GetAccountBalancesByAccount_FutureAsOf_IsClampedToCurrentPeriod()
    {
        // Arrange
        var future = DateTimeOffset.UtcNow.AddDays(400);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsAccountBalancesByAccountResponseDto>($"{Endpoint}?timePeriod=Monthly&asOf={future:yyyy-MM-dd}", CancellationToken);

        // Assert
        Assert.Equal(PeriodStart.UtcDateTime, result.Page.WindowStart);
        Assert.Null(result.Page.NextAsOf);
    }

    private async Task<List<InsightsAccountBalanceDto>> GetAccountsAsync(string query)
    {
        var response = await HttpClient.GetResultAsync<InsightsAccountBalancesByAccountResponseDto>($"{Endpoint}?{query}", CancellationToken);
        return response.Accounts;
    }
}

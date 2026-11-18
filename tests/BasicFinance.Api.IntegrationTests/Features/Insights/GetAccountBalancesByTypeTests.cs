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

public class GetAccountBalancesByTypeTests : ApiTestFixtureBase
{
    private const string Endpoint = "/api/insights/account-balances/by-type";

    private const string MonthlyQuery = "timePeriod=Monthly";

    private const string OtherUserId = "11111111-1111-1111-1111-111111111111";

    // Active account types seeded by DbSeedHelper.SeedGlobalDataAsync. The endpoint returns an entry for each
    // of these types even when the user has no accounts for it.
    private static readonly string[] ActiveAccountTypeCodes = ["CHK", "SAV", "CC", "INV"];

    public GetAccountBalancesByTypeTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    /// <summary>
    /// Gets the start (UTC midnight) of the current monthly period. The endpoint measures change from the start
    /// of the period containing today, so seeded ledger dates are expressed relative to this value.
    /// </summary>
    private static DateTimeOffset PeriodStart => DateTimeOffset.UtcNow.ToPeriodRange(TimePeriod.Monthly).RangeStartDate;

    [Fact]
    public async Task GetAccountBalancesByType_NoAccounts_ReturnsZeroForEveryActiveAccountType()
    {
        // Arrange
        const string parameters = MonthlyQuery;

        // Act
        var result = await HttpClient.GetResultAsync<InsightsAccountTypeBalancesResponseDto>($"{Endpoint}?{parameters}", CancellationToken);

        // Assert
        Assert.Equal(ActiveAccountTypeCodes.Order(), result.AccountTypeBalances.Select(x => x.AccountTypeCode).Order());
        Assert.All(result.AccountTypeBalances, x =>
        {
            Assert.Equal(0m, x.Balance);
            Assert.Equal(0m, x.PercentChanged);
        });
        Assert.Equal(0m, result.TotalPositiveBalance);
        Assert.Equal(0m, result.TotalNegativeBalance);
    }

    [Fact]
    public async Task GetAccountBalancesByType_AccountsAcrossTypes_ReturnsBalanceAndPercentPerTypeAndTotals()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Checking");
        var savings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Savings");
        var creditCard = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.CreditCard, accountName: "Card");
        await DbContext.SeedRangeAsync([checking, savings, creditCard], CancellationToken);
        var checkingOpening = AccountLedgerFactory.CreateFor(checking, 1000m, PeriodStart.AddDays(-5));
        var checkingLatest = AccountLedgerFactory.CreateFor(checking, 1031m, PeriodStart.AddDays(1));
        var savingsOpening = AccountLedgerFactory.CreateFor(savings, 5000m, PeriodStart.AddDays(-40));
        var cardOpening = AccountLedgerFactory.CreateFor(creditCard, -2000m, PeriodStart.AddDays(-5));
        var cardLatest = AccountLedgerFactory.CreateFor(creditCard, -2240m, PeriodStart.AddDays(1));
        await DbContext.SeedRangeAsync([checkingOpening, checkingLatest, savingsOpening, cardOpening, cardLatest], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsAccountTypeBalancesResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        var byCode = result.AccountTypeBalances.ToDictionary(x => x.AccountTypeCode);
        Assert.Equal(ActiveAccountTypeCodes.Length, byCode.Count);
        Assert.Equal(1031m, byCode["CHK"].Balance);
        Assert.Equal(3.1m, byCode["CHK"].PercentChanged);
        Assert.Equal(5000m, byCode["SAV"].Balance);
        Assert.Equal(0m, byCode["SAV"].PercentChanged);
        Assert.Equal(-2240m, byCode["CC"].Balance);
        Assert.Equal(12m, byCode["CC"].PercentChanged);
        Assert.Equal(0m, byCode["INV"].Balance);
        Assert.Equal(0m, byCode["INV"].PercentChanged);
        Assert.Equal(6031m, result.TotalPositiveBalance);
        Assert.Equal(-2240m, result.TotalNegativeBalance);
    }

    [Fact]
    public async Task GetAccountBalancesByType_MultipleAccountsOfSameType_SumsBalancesBeforeCalculatingPercent()
    {
        // Arrange
        var firstChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "First Checking");
        var secondChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "Second Checking");
        await DbContext.SeedRangeAsync([firstChecking, secondChecking], CancellationToken);
        var firstOpening = AccountLedgerFactory.CreateFor(firstChecking, 1000m, PeriodStart.AddDays(-5));
        var firstLatest = AccountLedgerFactory.CreateFor(firstChecking, 1100m, PeriodStart.AddDays(1));
        var secondOpening = AccountLedgerFactory.CreateFor(secondChecking, 1000m, PeriodStart.AddDays(-5));
        await DbContext.SeedRangeAsync([firstOpening, firstLatest, secondOpening], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsAccountTypeBalancesResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        var checking = result.AccountTypeBalances.Single(x => x.AccountTypeCode == "CHK");
        Assert.Equal(2100m, checking.Balance);
        Assert.Equal(5m, checking.PercentChanged);
        Assert.Equal(2100m, result.TotalPositiveBalance);
    }

    [Fact]
    public async Task GetAccountBalancesByType_LiabilityPaidDown_ReturnsNegativePercent()
    {
        // Arrange
        var creditCard = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.CreditCard, accountName: "Paid Down Card");
        await DbContext.SeedAsync(creditCard, CancellationToken);
        var openingLedger = AccountLedgerFactory.CreateFor(creditCard, -2000m, PeriodStart.AddDays(-5));
        var latestLedger = AccountLedgerFactory.CreateFor(creditCard, -1800m, PeriodStart.AddDays(1));
        await DbContext.SeedRangeAsync([openingLedger, latestLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsAccountTypeBalancesResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        var card = result.AccountTypeBalances.Single(x => x.AccountTypeCode == "CC");
        Assert.Equal(-1800m, card.Balance);
        Assert.Equal(-10m, card.PercentChanged);
        Assert.Equal(-1800m, result.TotalNegativeBalance);
    }

    [Fact]
    public async Task GetAccountBalancesByType_AccountOpenedMidPeriod_CountsTowardBalanceWithZeroOpening()
    {
        // Arrange
        var existingChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "Existing Checking");
        var newChecking = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "New Checking",
            openingBalance: 500m,
            openingBalanceRecordedDate: PeriodStart.AddDays(1));
        await DbContext.SeedRangeAsync([existingChecking, newChecking], CancellationToken);
        var existingLedger = AccountLedgerFactory.CreateFor(existingChecking, 1000m, PeriodStart.AddDays(-5));
        await DbContext.SeedAsync(existingLedger, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsAccountTypeBalancesResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        var checking = result.AccountTypeBalances.Single(x => x.AccountTypeCode == "CHK");
        Assert.Equal(1500m, checking.Balance);
        Assert.Equal(50m, checking.PercentChanged);
    }

    [Fact]
    public async Task GetAccountBalancesByType_EntriesWithSameRecordedDate_UsesLatestSystemCreatedDate()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Tie Break Checking");
        await DbContext.SeedAsync(checking, CancellationToken);
        var recordedDate = PeriodStart.AddDays(1).AddHours(12);
        var olderLedger = AccountLedgerFactory.CreateFor(checking, 900m, recordedDate, systemCreatedDate: recordedDate.AddMinutes(1));
        var newerLedger = AccountLedgerFactory.CreateFor(checking, 950m, recordedDate, systemCreatedDate: recordedDate.AddMinutes(5));
        await DbContext.SeedRangeAsync([newerLedger, olderLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsAccountTypeBalancesResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        var checkingBalance = result.AccountTypeBalances.Single(x => x.AccountTypeCode == "CHK");
        Assert.Equal(950m, checkingBalance.Balance);
    }

    [Fact]
    public async Task GetAccountBalancesByType_InstitutionFilter_OnlyIncludesAccountsAtInstitution()
    {
        // Arrange
        var wellsFargoChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "WF Checking");
        var chaseChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "Chase Checking", institutionId: TestConstants.ChaseInstitutionId);
        await DbContext.SeedRangeAsync([wellsFargoChecking, chaseChecking], CancellationToken);
        var wellsFargoLedger = AccountLedgerFactory.CreateFor(wellsFargoChecking, 1000m, PeriodStart.AddDays(-1));
        var chaseLedger = AccountLedgerFactory.CreateFor(chaseChecking, 4000m, PeriodStart.AddDays(-1));
        await DbContext.SeedRangeAsync([wellsFargoLedger, chaseLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsAccountTypeBalancesResponseDto>($"{Endpoint}?{MonthlyQuery}&institutionId={TestConstants.ChaseInstitutionId}", CancellationToken);

        // Assert
        var checking = result.AccountTypeBalances.Single(x => x.AccountTypeCode == "CHK");
        Assert.Equal(4000m, checking.Balance);
        Assert.Equal(4000m, result.TotalPositiveBalance);
    }

    [Fact]
    public async Task GetAccountBalancesByType_AccountsFromOtherUsers_AreExcluded()
    {
        // Arrange
        var ownAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Own Checking");
        var otherAccount = AccountFactory.Create(OtherUserId, accountName: "Other Checking");
        await DbContext.SeedRangeAsync([ownAccount, otherAccount], CancellationToken);
        var ownLedger = AccountLedgerFactory.CreateFor(ownAccount, 100m, PeriodStart.AddDays(-1));
        var otherLedger = AccountLedgerFactory.CreateFor(otherAccount, 900m, PeriodStart.AddDays(-1));
        await DbContext.SeedRangeAsync([ownLedger, otherLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsAccountTypeBalancesResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        var checking = result.AccountTypeBalances.Single(x => x.AccountTypeCode == "CHK");
        Assert.Equal(100m, checking.Balance);
        Assert.Equal(100m, result.TotalPositiveBalance);
    }

    [Fact]
    public async Task GetAccountBalancesByType_InactiveAccounts_AreExcluded()
    {
        // Arrange
        var activeChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "Active Checking");
        var inactiveChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "Inactive Checking");
        inactiveChecking.SetIsActive(false);
        await DbContext.SeedRangeAsync([activeChecking, inactiveChecking], CancellationToken);
        var activeLedger = AccountLedgerFactory.CreateFor(activeChecking, 1000m, PeriodStart.AddDays(-1));
        var inactiveLedger = AccountLedgerFactory.CreateFor(inactiveChecking, 500m, PeriodStart.AddDays(-1));
        await DbContext.SeedRangeAsync([activeLedger, inactiveLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsAccountTypeBalancesResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        var checking = result.AccountTypeBalances.Single(x => x.AccountTypeCode == "CHK");
        Assert.Equal(1000m, checking.Balance);
        Assert.Equal(1000m, result.TotalPositiveBalance);
    }

    [Fact]
    public async Task GetAccountBalancesByType_InactiveAccountType_IsOmittedAndExcludedFromTotals()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Checking");
        var investment = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Investment, accountName: "Brokerage");
        await DbContext.SeedRangeAsync([checking, investment], CancellationToken);
        var checkingLedger = AccountLedgerFactory.CreateFor(checking, 1000m, PeriodStart.AddDays(-1));
        var investmentLedger = AccountLedgerFactory.CreateFor(investment, 3000m, PeriodStart.AddDays(-1));
        await DbContext.SeedRangeAsync([checkingLedger, investmentLedger], CancellationToken);
        var investmentType = await DbContext.AccountTypes.SingleAsync(x => x.AccountTypeCode == "INV", CancellationToken);
        investmentType.IsActive = false;
        await DbContext.SaveChangesAsync(CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsAccountTypeBalancesResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.DoesNotContain(result.AccountTypeBalances, x => x.AccountTypeCode == "INV");
        Assert.Equal(ActiveAccountTypeCodes.Length - 1, result.AccountTypeBalances.Count);
        Assert.Equal(1000m, result.TotalPositiveBalance);
    }

    [Fact]
    public async Task GetAccountBalancesByType_AccountsFromInactiveInstitution_AreExcluded()
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
        var result = await HttpClient.GetResultAsync<InsightsAccountTypeBalancesResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.All(result.AccountTypeBalances, x => Assert.Equal(0m, x.Balance));
        Assert.Equal(0m, result.TotalPositiveBalance);
        Assert.Equal(0m, result.TotalNegativeBalance);
    }

    [Fact]
    public async Task GetAccountBalancesByType_PastAsOf_MeasuresThatPeriodAndIgnoresLaterEntries()
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
        var result = await HttpClient.GetResultAsync<InsightsAccountTypeBalancesResponseDto>($"{Endpoint}?timePeriod=Monthly&asOf=2026-03-15", CancellationToken);

        // Assert
        var checkingBalance = result.AccountTypeBalances.Single(x => x.AccountTypeCode == "CHK");
        Assert.Equal(1100m, checkingBalance.Balance);
        Assert.Equal(10m, checkingBalance.PercentChanged);
        Assert.Equal(1100m, result.TotalPositiveBalance);
        Assert.Equal(new DateTime(2026, 3, 1), result.Page.WindowStart);
        Assert.Equal(new DateTime(2026, 3, 31), result.Page.WindowEnd);
        Assert.Equal(new DateTime(2026, 2, 1), result.Page.PreviousAsOf);
        Assert.Equal(new DateTime(2026, 4, 1), result.Page.NextAsOf);
    }

    [Fact]
    public async Task GetAccountBalancesByType_CurrentPeriodWithNoAccounts_HasNoNeighbouringPages()
    {
        // Act
        var result = await HttpClient.GetResultAsync<InsightsAccountTypeBalancesResponseDto>($"{Endpoint}?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(PeriodStart.UtcDateTime, result.Page.WindowStart);
        Assert.Null(result.Page.PreviousAsOf);
        Assert.Null(result.Page.NextAsOf);
    }
}

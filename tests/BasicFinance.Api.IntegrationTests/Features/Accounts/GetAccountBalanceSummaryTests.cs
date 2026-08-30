using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;
using AccountTypeEnum = BasicFinance.Infrastructure.Enums.AccountType;

namespace BasicFinance.Api.IntegrationTests.Features.Accounts;

public class GetAccountBalanceSummaryTests : ApiTestFixtureBase
{
    private const string MonthlyQuery = "recordedDate=2026-08-05&timePeriod=Monthly";

    private const string OtherUserId = "11111111-1111-1111-1111-111111111111";

    // Active account types seeded by DbSeedHelper.SeedGlobalDataAsync. The balance summary
    // returns an entry for each of these types even when the user has no accounts for it.
    private static readonly string[] ActiveAccountTypeCodes = ["CHK", "SAV", "CC", "INV"];

    public GetAccountBalanceSummaryTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetAccountBalanceSummary_MonthlyPeriod_ReturnsBreakdownByAccountType()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Balance Checking");
        var savings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Balance Savings");
        await DbContext.SeedRangeAsync([checking, savings], CancellationToken);
        var currentCheckingLedger = AccountLedgerFactory.CreateFor(checking, 1000m, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero));
        var previousCheckingLedger = AccountLedgerFactory.CreateFor(checking, 1000m, new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero));
        var currentSavingsLedger = AccountLedgerFactory.CreateFor(savings, 3000m, new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero));
        var previousSavingsLedger = AccountLedgerFactory.CreateFor(savings, 1500m, new DateTimeOffset(2026, 7, 20, 0, 0, 0, TimeSpan.Zero));
        await DbContext.SeedRangeAsync([currentCheckingLedger, previousCheckingLedger, currentSavingsLedger, previousSavingsLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<AccountBalanceSummaryResponseDto>($"/api/accounts/balanceSummary?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(4000m, result.CurrentPeriodBreakdown.Balance);
        Assert.Equal(ActiveAccountTypeCodes.Length, result.CurrentPeriodBreakdown.AccountTypeBreakdowns.Count);
        var currentChecking = result.CurrentPeriodBreakdown.AccountTypeBreakdowns["CHK"];
        Assert.Equal(1000m, currentChecking.Balance);
        Assert.Equal(25m, currentChecking.PercentageOfTotalBalance);
        Assert.Single(currentChecking.Accounts);
        var currentCheckingAccount = currentChecking.Accounts[0];
        Assert.Equal(checking.AccountId, currentCheckingAccount.Id);
        Assert.Equal("CHK", currentCheckingAccount.AccountTypeCode);
        Assert.Equal("Wells Fargo", currentCheckingAccount.Institution);
        Assert.Equal(checking.AccountName, currentCheckingAccount.AccountName);
        Assert.Equal(1000m, currentCheckingAccount.Balance);
        Assert.Equal(25m, currentCheckingAccount.PercentageOfTotalBalance);
        Assert.Equal(100m, currentCheckingAccount.PercentageOfAccountTypeBalance);
        var currentSavings = result.CurrentPeriodBreakdown.AccountTypeBreakdowns["SAV"];
        Assert.Equal(3000m, currentSavings.Balance);
        Assert.Equal(75m, currentSavings.PercentageOfTotalBalance);
        Assert.Single(currentSavings.Accounts);
        var currentSavingsAccount = currentSavings.Accounts[0];
        Assert.Equal(savings.AccountId, currentSavingsAccount.Id);
        Assert.Equal(3000m, currentSavingsAccount.Balance);
        Assert.Equal(75m, currentSavingsAccount.PercentageOfTotalBalance);
        Assert.Equal(100m, currentSavingsAccount.PercentageOfAccountTypeBalance);
        var currentCreditCard = result.CurrentPeriodBreakdown.AccountTypeBreakdowns["CC"];
        Assert.Equal(0m, currentCreditCard.Balance);
        Assert.Equal(0m, currentCreditCard.PercentageOfTotalBalance);
        Assert.Empty(currentCreditCard.Accounts);
        var currentInvestment = result.CurrentPeriodBreakdown.AccountTypeBreakdowns["INV"];
        Assert.Equal(0m, currentInvestment.Balance);
        Assert.Equal(0m, currentInvestment.PercentageOfTotalBalance);
        Assert.Empty(currentInvestment.Accounts);

        Assert.Equal(2500m, result.PreviousPeriodBreakdown.Balance);
        Assert.Equal(ActiveAccountTypeCodes.Length, result.PreviousPeriodBreakdown.AccountTypeBreakdowns.Count);
        var previousChecking = result.PreviousPeriodBreakdown.AccountTypeBreakdowns["CHK"];
        Assert.Equal(1000m, previousChecking.Balance);
        Assert.Equal(40m, previousChecking.PercentageOfTotalBalance);
        Assert.Equal(1000m, previousChecking.Accounts[0].Balance);
        Assert.Equal(40m, previousChecking.Accounts[0].PercentageOfTotalBalance);
        var previousSavings = result.PreviousPeriodBreakdown.AccountTypeBreakdowns["SAV"];
        Assert.Equal(1500m, previousSavings.Balance);
        Assert.Equal(60m, previousSavings.PercentageOfTotalBalance);
        Assert.Equal(1500m, previousSavings.Accounts[0].Balance);
        Assert.Equal(60m, previousSavings.Accounts[0].PercentageOfTotalBalance);
        var previousCreditCard = result.PreviousPeriodBreakdown.AccountTypeBreakdowns["CC"];
        Assert.Equal(0m, previousCreditCard.Balance);
        Assert.Equal(0m, previousCreditCard.PercentageOfTotalBalance);
        Assert.Empty(previousCreditCard.Accounts);
        var previousInvestment = result.PreviousPeriodBreakdown.AccountTypeBreakdowns["INV"];
        Assert.Equal(0m, previousInvestment.Balance);
        Assert.Equal(0m, previousInvestment.PercentageOfTotalBalance);
        Assert.Empty(previousInvestment.Accounts);

        Assert.Equal(new DateOnly(2026, 8, 1), result.CurrentPeriodStart);
        Assert.Equal(new DateOnly(2026, 9, 1), result.CurrentPeriodEnd);
        Assert.Equal(new DateOnly(2026, 7, 1), result.PreviousPeriodStart);
        Assert.Equal(new DateOnly(2026, 8, 1), result.PreviousPeriodEnd);
    }

    [Fact]
    public async Task GetAccountBalanceSummary_LatestLedgerBeforePeriodStart_IsCarriedForward()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Carry Forward Checking");
        var savings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Current Only Savings");
        await DbContext.SeedRangeAsync([checking, savings], CancellationToken);
        var checkingLedger = AccountLedgerFactory.CreateFor(checking, 800m, new DateTimeOffset(2026, 7, 28, 0, 0, 0, TimeSpan.Zero));
        var savingsLedger = AccountLedgerFactory.CreateFor(savings, 200m, new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero));
        await DbContext.SeedRangeAsync([checkingLedger, savingsLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<AccountBalanceSummaryResponseDto>($"/api/accounts/balanceSummary?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(1000m, result.CurrentPeriodBreakdown.Balance);
        Assert.Equal(ActiveAccountTypeCodes.Length, result.CurrentPeriodBreakdown.AccountTypeBreakdowns.Count);
        var currentChecking = result.CurrentPeriodBreakdown.AccountTypeBreakdowns["CHK"];
        Assert.Equal(800m, currentChecking.Balance);
        Assert.Equal(80m, currentChecking.PercentageOfTotalBalance);
        Assert.Equal(800m, currentChecking.Accounts[0].Balance);
        var currentSavings = result.CurrentPeriodBreakdown.AccountTypeBreakdowns["SAV"];
        Assert.Equal(200m, currentSavings.Balance);
        Assert.Equal(20m, currentSavings.PercentageOfTotalBalance);
        Assert.Equal(200m, currentSavings.Accounts[0].Balance);

        Assert.Equal(800m, result.PreviousPeriodBreakdown.Balance);
        Assert.Equal(ActiveAccountTypeCodes.Length, result.PreviousPeriodBreakdown.AccountTypeBreakdowns.Count);
        var previousChecking = result.PreviousPeriodBreakdown.AccountTypeBreakdowns["CHK"];
        Assert.Equal(800m, previousChecking.Balance);
        Assert.Equal(100m, previousChecking.PercentageOfTotalBalance);
        Assert.Equal(800m, previousChecking.Accounts[0].Balance);
        var previousSavings = result.PreviousPeriodBreakdown.AccountTypeBreakdowns["SAV"];
        Assert.Equal(0m, previousSavings.Balance);
        Assert.Equal(0m, previousSavings.PercentageOfTotalBalance);
        var previousSavingsAccount = previousSavings.Accounts[0];
        Assert.Equal(savings.AccountId, previousSavingsAccount.Id);
        Assert.Equal(0m, previousSavingsAccount.Balance);
        Assert.Equal(0m, previousSavingsAccount.PercentageOfTotalBalance);
        Assert.Equal(0m, previousSavingsAccount.PercentageOfAccountTypeBalance);
    }

    [Fact]
    public async Task GetAccountBalanceSummary_LedgerEntryOnNextPeriodStart_IsExcludedFromCurrentPeriod()
    {
        // Arrange
        var checking = AccountFactory.Create(AuthenticatedUserId, accountName: "Boundary Checking");
        var savings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Boundary Savings");
        await DbContext.SeedRangeAsync([checking, savings], CancellationToken);
        var checkingLedger = AccountLedgerFactory.CreateFor(checking, 1000m, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero));
        var nextPeriodLedger = AccountLedgerFactory.CreateFor(checking, 4000m, new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        var savingsLedger = AccountLedgerFactory.CreateFor(savings, 3000m, new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero));
        await DbContext.SeedRangeAsync([checkingLedger, nextPeriodLedger, savingsLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<AccountBalanceSummaryResponseDto>($"/api/accounts/balanceSummary?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(4000m, result.CurrentPeriodBreakdown.Balance);
        Assert.Equal(ActiveAccountTypeCodes.Length, result.CurrentPeriodBreakdown.AccountTypeBreakdowns.Count);
        var currentChecking = result.CurrentPeriodBreakdown.AccountTypeBreakdowns["CHK"];
        Assert.Equal(1000m, currentChecking.Balance);
        Assert.Equal(1000m, currentChecking.Accounts[0].Balance);
        var currentSavings = result.CurrentPeriodBreakdown.AccountTypeBreakdowns["SAV"];
        Assert.Equal(3000m, currentSavings.Balance);
        Assert.Equal(75m, currentSavings.PercentageOfTotalBalance);
    }

    [Fact]
    public async Task GetAccountBalanceSummary_NoAccounts_ReturnsZeroEntriesForAllAccountTypes()
    {
        // Arrange
        const string parameters = MonthlyQuery;

        // Act
        var result = await HttpClient.GetResultAsync<AccountBalanceSummaryResponseDto>($"/api/accounts/balanceSummary?{parameters}", CancellationToken);

        // Assert
        Assert.Equal(0m, result.CurrentPeriodBreakdown.Balance);
        Assert.Equal(ActiveAccountTypeCodes.Length, result.CurrentPeriodBreakdown.AccountTypeBreakdowns.Count);
        Assert.All(ActiveAccountTypeCodes, code =>
        {
            var currentEntry = result.CurrentPeriodBreakdown.AccountTypeBreakdowns[code];
            Assert.Equal(0m, currentEntry.Balance);
            Assert.Equal(0m, currentEntry.PercentageOfTotalBalance);
            Assert.Empty(currentEntry.Accounts);
        });
        Assert.Equal(0m, result.PreviousPeriodBreakdown.Balance);
        Assert.Equal(ActiveAccountTypeCodes.Length, result.PreviousPeriodBreakdown.AccountTypeBreakdowns.Count);
        Assert.All(ActiveAccountTypeCodes, code =>
        {
            var previousEntry = result.PreviousPeriodBreakdown.AccountTypeBreakdowns[code];
            Assert.Equal(0m, previousEntry.Balance);
            Assert.Equal(0m, previousEntry.PercentageOfTotalBalance);
            Assert.Empty(previousEntry.Accounts);
        });
        Assert.Equal(new DateOnly(2026, 8, 1), result.CurrentPeriodStart);
        Assert.Equal(new DateOnly(2026, 9, 1), result.CurrentPeriodEnd);
        Assert.Equal(new DateOnly(2026, 7, 1), result.PreviousPeriodStart);
        Assert.Equal(new DateOnly(2026, 8, 1), result.PreviousPeriodEnd);
    }

    [Fact]
    public async Task GetAccountBalanceSummary_InactiveAccounts_AreExcludedFromBreakdowns()
    {
        // Arrange
        var activeChecking = AccountFactory.Create(AuthenticatedUserId, accountName: "Active Checking");
        var activeSavings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Active Savings");
        var inactiveSavings = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Inactive Savings");
        inactiveSavings.SetIsActive(false);
        await DbContext.SeedRangeAsync([activeChecking, activeSavings, inactiveSavings], CancellationToken);
        var activeCheckingLedger = AccountLedgerFactory.CreateFor(activeChecking, 1000m, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero));
        var activeSavingsLedger = AccountLedgerFactory.CreateFor(activeSavings, 3000m, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero));
        var inactiveSavingsLedger = AccountLedgerFactory.CreateFor(inactiveSavings, 500m, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero));
        await DbContext.SeedRangeAsync([activeCheckingLedger, activeSavingsLedger, inactiveSavingsLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<AccountBalanceSummaryResponseDto>($"/api/accounts/balanceSummary?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(4000m, result.CurrentPeriodBreakdown.Balance);
        Assert.Equal(ActiveAccountTypeCodes.Length, result.CurrentPeriodBreakdown.AccountTypeBreakdowns.Count);
        var currentSavings = result.CurrentPeriodBreakdown.AccountTypeBreakdowns["SAV"];
        Assert.Equal(3000m, currentSavings.Balance);
        Assert.Single(currentSavings.Accounts);
        Assert.Equal(activeSavings.AccountId, currentSavings.Accounts[0].Id);
        Assert.NotEqual(inactiveSavings.AccountId, currentSavings.Accounts[0].Id);
    }

    [Fact]
    public async Task GetAccountBalanceSummary_AccountsFromOtherUsers_AreExcludedFromBreakdowns()
    {
        // Arrange
        var ownAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Own Checking");
        var otherAccount = AccountFactory.Create(OtherUserId, accountType: AccountTypeEnum.Savings, accountName: "Other Savings");
        await DbContext.SeedRangeAsync([ownAccount, otherAccount], CancellationToken);
        var ownLedger = AccountLedgerFactory.CreateFor(ownAccount, 100m, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero));
        var otherLedger = AccountLedgerFactory.CreateFor(otherAccount, 900m, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero));
        await DbContext.SeedRangeAsync([ownLedger, otherLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<AccountBalanceSummaryResponseDto>($"/api/accounts/balanceSummary?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(100m, result.CurrentPeriodBreakdown.Balance);
        Assert.Equal(ActiveAccountTypeCodes.Length, result.CurrentPeriodBreakdown.AccountTypeBreakdowns.Count);
        var currentChecking = result.CurrentPeriodBreakdown.AccountTypeBreakdowns["CHK"];
        Assert.Single(currentChecking.Accounts);
        Assert.Equal(ownAccount.AccountId, currentChecking.Accounts[0].Id);
        Assert.Equal(100m, currentChecking.Accounts[0].Balance);
        var currentSavings = result.CurrentPeriodBreakdown.AccountTypeBreakdowns["SAV"];
        Assert.Equal(0m, currentSavings.Balance);
        Assert.Empty(currentSavings.Accounts);
    }

    [Fact]
    public async Task GetAccountBalanceSummary_AccountsFromInactiveInstitution_AreExcludedFromBreakdowns()
    {
        // Arrange
        var inactiveInstitution = InstitutionFactory.Create(name: "Inactive Bank", institutionCode: "INACT");
        await DbContext.SeedAsync(inactiveInstitution, CancellationToken);
        inactiveInstitution.IsActive = false;
        await DbContext.SaveChangesAsync(CancellationToken);
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: "Inactive Bank Checking", institutionId: inactiveInstitution.InstitutionId);
        await DbContext.SeedAsync(account, CancellationToken);
        var ledger = AccountLedgerFactory.CreateFor(account, 750m, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero));
        await DbContext.SeedAsync(ledger, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<AccountBalanceSummaryResponseDto>($"/api/accounts/balanceSummary?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(0m, result.CurrentPeriodBreakdown.Balance);
        Assert.Equal(ActiveAccountTypeCodes.Length, result.CurrentPeriodBreakdown.AccountTypeBreakdowns.Count);
        Assert.All(ActiveAccountTypeCodes, code =>
        {
            var currentEntry = result.CurrentPeriodBreakdown.AccountTypeBreakdowns[code];
            Assert.Equal(0m, currentEntry.Balance);
            Assert.Equal(0m, currentEntry.PercentageOfTotalBalance);
            Assert.Empty(currentEntry.Accounts);
        });
        Assert.Equal(0m, result.PreviousPeriodBreakdown.Balance);
        Assert.Equal(ActiveAccountTypeCodes.Length, result.PreviousPeriodBreakdown.AccountTypeBreakdowns.Count);
        Assert.All(ActiveAccountTypeCodes, code =>
        {
            var previousEntry = result.PreviousPeriodBreakdown.AccountTypeBreakdowns[code];
            Assert.Equal(0m, previousEntry.Balance);
            Assert.Equal(0m, previousEntry.PercentageOfTotalBalance);
            Assert.Empty(previousEntry.Accounts);
        });
    }

    [Fact]
    public async Task GetAccountBalanceSummary_UnrecognizedTimePeriod_FallsBackToMonthly()
    {
        // Arrange
        const string parameters = "recordedDate=2026-08-05&timePeriod=Annualish";
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: "Fallback Checking");
        await DbContext.SeedAsync(account, CancellationToken);
        var ledger = AccountLedgerFactory.CreateFor(account, 1000m, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero));
        await DbContext.SeedAsync(ledger, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<AccountBalanceSummaryResponseDto>($"/api/accounts/balanceSummary?{parameters}", CancellationToken);

        // Assert
        Assert.Equal(1000m, result.CurrentPeriodBreakdown.Balance);
        Assert.Equal(new DateOnly(2026, 8, 1), result.CurrentPeriodStart);
        Assert.Equal(new DateOnly(2026, 9, 1), result.CurrentPeriodEnd);
        Assert.Equal(new DateOnly(2026, 7, 1), result.PreviousPeriodStart);
        Assert.Equal(new DateOnly(2026, 8, 1), result.PreviousPeriodEnd);
    }
}

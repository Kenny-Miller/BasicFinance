using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;

namespace BasicFinance.Api.IntegrationTests.Features.Accounts;

public class GetInstitutionSummaryTests : ApiTestFixtureBase
{
    private const string MonthlyQuery = "recordedDate=2026-08-05&timePeriod=Monthly";

    public GetInstitutionSummaryTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetInstitutionSummary_ActiveAccounts_ReturnsRosterAndTypeTotals()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: "Chase Checking", institutionId: TestConstants.ChaseInstitutionId);
        await DbContext.SeedAsync(account, CancellationToken);
        var currentPeriodLedger = AccountLedgerFactory.CreateFor(account, 1000m, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero));
        var previousPeriodLedger = AccountLedgerFactory.CreateFor(account, 900m, new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero));
        await DbContext.SeedRangeAsync([currentPeriodLedger, previousPeriodLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>($"/api/accounts/institution/{TestConstants.ChaseInstitutionId}/summary?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(TestConstants.ChaseInstitutionId, result.InstitutionId);
        Assert.Equal("Chase", result.InstitutionName);
        Assert.Single(result.Accounts);
        var rosterAccount = result.Accounts.First();
        Assert.Equal("Chase Checking", rosterAccount.Name);
        Assert.Equal("CHK", rosterAccount.AccountTypeCode);
        Assert.Equal("CHASE", rosterAccount.InstitutionCode);
        Assert.Equal("Chase", rosterAccount.InstitutionName);
        Assert.Equal(1000m, rosterAccount.LatestBalance);
        Assert.Equal(1000m, result.AccountTypeTotals["CHK"]);
        Assert.Equal(900m, result.AccountTypePreviousTotals["CHK"]);
        Assert.Equal(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), result.CurrentPeriodStart);
        Assert.Equal(new DateTimeOffset(2026, 8, 31, 23, 59, 59, 999, TimeSpan.Zero), result.CurrentPeriodEnd);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), result.PreviousPeriodStart);
        Assert.Equal(new DateTimeOffset(2026, 7, 31, 23, 59, 59, 999, TimeSpan.Zero), result.PreviousPeriodEnd);
    }

    [Fact]
    public async Task GetInstitutionSummary_InstitutionWithNoAccounts_ReturnsZeroFilledResponse()
    {
        // Arrange
        const string parameters = MonthlyQuery;

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>($"/api/accounts/institution/{TestConstants.SchwabInstitutionId}/summary?{parameters}", CancellationToken);

        // Assert
        Assert.Equal(TestConstants.SchwabInstitutionId, result.InstitutionId);
        Assert.Equal("Charles Schwab", result.InstitutionName);
        Assert.Empty(result.Accounts);
        Assert.Empty(result.AccountTypeTotals);
        Assert.Empty(result.AccountTypePreviousTotals);
    }

    [Fact]
    public async Task GetInstitutionSummary_NonExistentInstitution_ReturnsZeroFilledResponse()
    {
        // Arrange
        const string parameters = MonthlyQuery;

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>($"/api/accounts/institution/{TestConstants.NonExistentInstitutionId}/summary?{parameters}", CancellationToken);

        // Assert
        Assert.Equal(TestConstants.NonExistentInstitutionId, result.InstitutionId);
        Assert.Equal(string.Empty, result.InstitutionName);
        Assert.Empty(result.Accounts);
        Assert.Empty(result.AccountTypeTotals);
        Assert.Empty(result.AccountTypePreviousTotals);
    }

    [Fact]
    public async Task GetInstitutionSummary_InactiveAccounts_AreExcludedFromRosterAndTotals()
    {
        // Arrange
        var activeAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Active Wells Fargo", institutionId: TestConstants.WellsFargoInstitutionId);
        var inactiveAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Inactive Wells Fargo", institutionId: TestConstants.WellsFargoInstitutionId);
        inactiveAccount.SetIsActive(false);
        await DbContext.SeedRangeAsync([activeAccount, inactiveAccount], CancellationToken);
        var activeLedger = AccountLedgerFactory.CreateFor(activeAccount, 1000m, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero));
        var inactiveLedger = AccountLedgerFactory.CreateFor(inactiveAccount, 500m, new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero));
        await DbContext.SeedRangeAsync([activeLedger, inactiveLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>($"/api/accounts/institution/{TestConstants.WellsFargoInstitutionId}/summary?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Single(result.Accounts);
        Assert.Equal("Active Wells Fargo", result.Accounts.First().Name);
        Assert.Equal(1000m, result.AccountTypeTotals["CHK"]);
    }

    [Fact]
    public async Task GetInstitutionSummary_InactiveInstitution_ReturnsZeroFilledResponse()
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
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>($"/api/accounts/institution/{inactiveInstitution.InstitutionId}/summary?{MonthlyQuery}", CancellationToken);

        // Assert
        Assert.Equal(inactiveInstitution.InstitutionId, result.InstitutionId);
        Assert.Equal("Inactive Bank", result.InstitutionName);
        Assert.Empty(result.Accounts);
        Assert.Empty(result.AccountTypeTotals);
        Assert.Empty(result.AccountTypePreviousTotals);
    }
}

using System.Net;
using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;
using AccountTypeEnum = BasicFinance.Infrastructure.Enums.AccountType;

namespace BasicFinance.Api.IntegrationTests.Features.Accounts;

public class GetAccountInstitutionSummaryTests : ApiTestFixtureBase
{
    private static readonly DateTimeOffset AnchorDate = new(2026, 8, 7, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CurrentMonthRecordedDate = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PreviousMonthRecordedDate = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Account creation date before the previous period.
    /// </summary>
    private static readonly DateTimeOffset PreviousPeriodAccountCreatedDate = new(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Date the account is deactivated, inside the previous period. Only active
    /// accounts are summarized, so the closed account is excluded from both periods.
    /// </summary>
    private static readonly DateTimeOffset AccountDeactivationDate = new(2026, 7, 20, 0, 0, 0, TimeSpan.Zero);

    public GetAccountInstitutionSummaryTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetAccountInstitutionSummary_UserHasAccountsAtInstitution_ReturnsInstitutionSummary()
    {
        // Arrange
        var account = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "Institution Checking",
            institutionId: TestConstants.WellsFargoInstitutionId);
        var ledger = AccountLedgerFactory.CreateFor(account, balance: 1000m, balanceRecordedDate: CurrentMonthRecordedDate);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedAsync(ledger, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>(EndpointFor(TestConstants.WellsFargoInstitutionId), CancellationToken);

        // Assert
        Assert.Equal(TestConstants.WellsFargoInstitutionId, result.InstitutionId);
        Assert.Equal("Wells Fargo", result.InstitutionName);
        var detail = result.Accounts.Single();
        Assert.Equal(account.AccountId, detail.Id);
        Assert.Equal("Institution Checking", detail.Name);
        Assert.Equal("CHK", detail.AccountTypeCode);
        Assert.Equal(1000m, detail.LatestBalance);
        Assert.Equal(CurrentMonthRecordedDate, detail.LatestBalanceRecordedDate);
        Assert.Equal(1000m, result.AccountTypeTotals["CHK"]);
        Assert.Equal(0m, result.AccountTypePreviousTotals["CHK"]);
        Assert.Equal(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), result.CurrentPeriodStart);
        Assert.Equal(new DateTime(2026, 8, 31, 23, 59, 59, 999, DateTimeKind.Utc), result.CurrentPeriodEnd);
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), result.PreviousPeriodStart);
        Assert.Equal(new DateTime(2026, 7, 31, 23, 59, 59, 999, DateTimeKind.Utc), result.PreviousPeriodEnd);
    }

    [Fact]
    public async Task GetAccountInstitutionSummary_CreditCardAccount_RosterAndTotalsAreNegative()
    {
        // Arrange
        var account = AccountFactory.Create(
            AuthenticatedUserId,
            accountType: AccountTypeEnum.CreditCard,
            accountName: "Institution Credit",
            institutionId: TestConstants.WellsFargoInstitutionId);
        var ledger = AccountLedgerFactory.CreateFor(account, balance: -250m, balanceRecordedDate: CurrentMonthRecordedDate);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedAsync(ledger, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>(EndpointFor(TestConstants.WellsFargoInstitutionId), CancellationToken);

        // Assert
        Assert.Equal(-250m, result.Accounts.Single().LatestBalance);
        Assert.Equal(-250m, result.AccountTypeTotals["CC"]);
    }

    [Fact]
    public async Task GetAccountInstitutionSummary_MultipleHistoryRows_ReturnsLatestBalanceOnOrBeforeEachPeriodEnd()
    {
        // Arrange
        var account = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "History Checking",
            institutionId: TestConstants.WellsFargoInstitutionId,
            systemCreatedDate: PreviousPeriodAccountCreatedDate);
        var previousLedger = AccountLedgerFactory.CreateFor(account, balance: 100m, balanceRecordedDate: PreviousMonthRecordedDate);
        var latestLedger = AccountLedgerFactory.CreateFor(account, balance: 200m, balanceRecordedDate: CurrentMonthRecordedDate);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedRangeAsync([previousLedger, latestLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>(EndpointFor(TestConstants.WellsFargoInstitutionId), CancellationToken);

        // Assert
        Assert.Equal(200m, result.AccountTypeTotals["CHK"]);
        Assert.Equal(100m, result.AccountTypePreviousTotals["CHK"]);
    }

    [Fact]
    public async Task GetAccountInstitutionSummary_AccountDeactivatedDuringPreviousPeriod_IsExcludedFromPreviousPeriodTotals()
    {
        // Arrange
        var activeAccount = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "Ongoing Checking",
            institutionId: TestConstants.WellsFargoInstitutionId);
        var activeLedger = AccountLedgerFactory.CreateFor(activeAccount, balance: 100m, balanceRecordedDate: CurrentMonthRecordedDate);
        var closedAccount = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "Former Checking",
            institutionId: TestConstants.WellsFargoInstitutionId,
            systemCreatedDate: PreviousPeriodAccountCreatedDate);
        var closedLedger = AccountLedgerFactory.CreateFor(closedAccount, balance: 500m, balanceRecordedDate: PreviousMonthRecordedDate);
        closedAccount.IsActive = false;
        closedAccount.SystemModifiedDate = AccountDeactivationDate;
        await DbContext.SeedRangeAsync([activeAccount, closedAccount], CancellationToken);
        await DbContext.SeedRangeAsync([activeLedger, closedLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>(EndpointFor(TestConstants.WellsFargoInstitutionId), CancellationToken);

        // Assert
        var detail = Assert.Single(result.Accounts);
        Assert.Equal(activeAccount.AccountId, detail.Id);
        Assert.Equal(100m, result.AccountTypeTotals["CHK"]);
        Assert.Equal(0m, result.AccountTypePreviousTotals["CHK"]);
    }

    [Fact]
    public async Task GetAccountInstitutionSummary_OnlyOpeningLedgerEntry_ReturnsZeroBalanceAndZeroCurrentTotals()
    {
        // Arrange
        var account = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "Opening Ledger Checking",
            institutionId: TestConstants.WellsFargoInstitutionId);
        await DbContext.SeedAsync(account, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>(EndpointFor(TestConstants.WellsFargoInstitutionId), CancellationToken);

        // Assert
        Assert.Single(result.Accounts);
        Assert.Equal(0m, result.Accounts.Single().LatestBalance);
        Assert.Equal(0m, result.AccountTypeTotals["CHK"]);
        Assert.Equal(0m, result.AccountTypePreviousTotals["CHK"]);
    }

    [Fact]
    public async Task GetAccountInstitutionSummary_AnotherUsersAccountAtInstitution_IsExcluded()
    {
        // Arrange
        var otherUserId = Guid.NewGuid().ToString();
        var myAccount = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "Mine Checking",
            institutionId: TestConstants.WellsFargoInstitutionId);
        var otherAccount = AccountFactory.Create(
            otherUserId,
            accountName: "Other Checking",
            institutionId: TestConstants.WellsFargoInstitutionId);
        var myLedger = AccountLedgerFactory.CreateFor(myAccount, balance: 200m, balanceRecordedDate: CurrentMonthRecordedDate);
        var otherLedger = AccountLedgerFactory.CreateFor(otherAccount, balance: 900m, balanceRecordedDate: CurrentMonthRecordedDate);
        await DbContext.SeedRangeAsync([myAccount, otherAccount], CancellationToken);
        await DbContext.SeedRangeAsync([myLedger, otherLedger], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>(EndpointFor(TestConstants.WellsFargoInstitutionId), CancellationToken);

        // Assert
        Assert.Single(result.Accounts);
        Assert.Equal(myAccount.AccountId, result.Accounts.Single().Id);
        Assert.Equal(200m, result.AccountTypeTotals["CHK"]);
    }

    [Fact]
    public async Task GetAccountInstitutionSummary_NonExistentInstitution_ReturnsBadRequest()
    {
        // Act
        var response = await HttpClient.GetAsync(EndpointFor(TestConstants.NonExistentInstitutionId), CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountInstitutionSummary_AnotherUserOnlyAtInstitution_ReturnsBadRequest()
    {
        // Arrange
        var otherAccount = AccountFactory.Create(
            Guid.NewGuid().ToString(),
            accountName: "Other Chase Checking",
            institutionId: TestConstants.ChaseInstitutionId);
        await DbContext.SeedAsync(otherAccount, CancellationToken);

        // Act
        var response = await HttpClient.GetAsync(EndpointFor(TestConstants.ChaseInstitutionId), CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string EndpointFor(int institutionId) =>
        $"/api/accounts/institution/{institutionId}/summary?recordedDate={AnchorDate:O}&timePeriod=Monthly";
}

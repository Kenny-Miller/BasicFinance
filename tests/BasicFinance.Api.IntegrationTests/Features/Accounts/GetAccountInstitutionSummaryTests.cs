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
            balance: 1000m,
            institutionId: TestConstants.WellsFargoInstitutionId,
            balanceRecordedDate: CurrentMonthRecordedDate);
        var history = AccountBalanceHistoryFactory.CreateFor(account, balance: 1000m, balanceRecordedDate: CurrentMonthRecordedDate);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedAsync(history, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>(EndpointFor(TestConstants.WellsFargoInstitutionId), CancellationToken);

        // Assert
        Assert.Equal(TestConstants.WellsFargoInstitutionId, result.InstitutionId);
        Assert.Equal("Wells Fargo", result.InstitutionName);
        var detail = result.Accounts.Single();
        Assert.Equal(account.AccountId, detail.Id);
        Assert.Equal("Institution Checking", detail.Name);
        Assert.Equal("CHK", detail.AccountTypeCode);
        Assert.Equal(1000m, detail.Balance);
        Assert.Equal(CurrentMonthRecordedDate, detail.BalanceRecordedDate);
        Assert.Equal(1000m, result.AccountTypeTotals["CHK"]);
        Assert.Empty(result.AccountTypePreviousTotals);
        Assert.Equal(new DateOnly(2026, 8, 1), result.CurrentPeriodStart);
        Assert.Equal(new DateOnly(2026, 9, 1), result.CurrentPeriodEnd);
        Assert.Equal(new DateOnly(2026, 7, 1), result.PreviousPeriodStart);
        Assert.Equal(new DateOnly(2026, 8, 1), result.PreviousPeriodEnd);
    }

    [Fact]
    public async Task GetAccountInstitutionSummary_CreditCardAccount_RosterAndTotalsAreNegative()
    {
        // Arrange
        var account = AccountFactory.Create(
            AuthenticatedUserId,
            accountType: AccountTypeEnum.CreditCard,
            accountName: "Institution Credit",
            balance: 250m,
            institutionId: TestConstants.WellsFargoInstitutionId,
            balanceRecordedDate: CurrentMonthRecordedDate);
        var history = AccountBalanceHistoryFactory.CreateFor(account, balance: 250m, balanceRecordedDate: CurrentMonthRecordedDate);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedAsync(history, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>(EndpointFor(TestConstants.WellsFargoInstitutionId), CancellationToken);

        // Assert
        Assert.Equal(-250m, result.Accounts.Single().Balance);
        Assert.Equal(-250m, result.AccountTypeTotals["CC"]);
    }

    [Fact]
    public async Task GetAccountInstitutionSummary_MultipleHistoryRows_ReturnsLatestBalanceOnOrBeforeEachPeriodEnd()
    {
        // Arrange
        var account = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "History Checking",
            balance: 200m,
            institutionId: TestConstants.WellsFargoInstitutionId,
            balanceRecordedDate: CurrentMonthRecordedDate);
        var earliestHistory = AccountBalanceHistoryFactory.CreateFor(account, balance: 100m, balanceRecordedDate: PreviousMonthRecordedDate);
        var latestHistory = AccountBalanceHistoryFactory.CreateFor(account, balance: 200m, balanceRecordedDate: CurrentMonthRecordedDate);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedRangeAsync([earliestHistory, latestHistory], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>(EndpointFor(TestConstants.WellsFargoInstitutionId), CancellationToken);

        // Assert
        Assert.Equal(200m, result.AccountTypeTotals["CHK"]);
        Assert.Equal(100m, result.AccountTypePreviousTotals["CHK"]);
    }

    [Fact]
    public async Task GetAccountInstitutionSummary_NoHistoryRows_ReturnsEmptyTotalsWithAccountsListed()
    {
        // Arrange
        var account = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "No History Checking",
            balance: 100m,
            institutionId: TestConstants.WellsFargoInstitutionId);
        await DbContext.SeedAsync(account, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionSummaryResponseDto>(EndpointFor(TestConstants.WellsFargoInstitutionId), CancellationToken);

        // Assert
        Assert.Single(result.Accounts);
        Assert.Equal(100m, result.Accounts.Single().Balance);
        Assert.Empty(result.AccountTypeTotals);
        Assert.Empty(result.AccountTypePreviousTotals);
    }

    [Fact]
    public async Task GetAccountInstitutionSummary_AnotherUsersAccountAtInstitution_IsExcluded()
    {
        // Arrange
        var otherUserId = Guid.NewGuid().ToString();
        var myAccount = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "Mine Checking",
            balance: 200m,
            institutionId: TestConstants.WellsFargoInstitutionId,
            balanceRecordedDate: CurrentMonthRecordedDate);
        var otherAccount = AccountFactory.Create(
            otherUserId,
            accountName: "Other Checking",
            balance: 900m,
            institutionId: TestConstants.WellsFargoInstitutionId,
            balanceRecordedDate: CurrentMonthRecordedDate);
        var myHistory = AccountBalanceHistoryFactory.CreateFor(myAccount, balance: 200m, balanceRecordedDate: CurrentMonthRecordedDate);
        var otherHistory = AccountBalanceHistoryFactory.CreateFor(otherAccount, balance: 900m, balanceRecordedDate: CurrentMonthRecordedDate);
        await DbContext.SeedRangeAsync([myAccount, otherAccount], CancellationToken);
        await DbContext.SeedRangeAsync([myHistory, otherHistory], CancellationToken);

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
            balance: 100m,
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

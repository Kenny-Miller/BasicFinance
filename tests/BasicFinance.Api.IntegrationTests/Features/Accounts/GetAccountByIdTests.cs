using System.Net;
using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;
using AccountDto = BasicFinance.Api.IntegrationTests.Helpers.AccountDto;

namespace BasicFinance.Api.IntegrationTests.Features.Accounts;

public class GetAccountByIdTests : ApiTestFixtureBase
{
    private static readonly DateTimeOffset MostRecentRecordedDate = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset BackfilledRecordedDate = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

    public GetAccountByIdTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetAccountById_ExistingAccount_ReturnsOk()
    {
        // Arrange
        const string accountName = "My Account";
        const decimal balance = 7500m;
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: accountName);
        var ledger = AccountLedgerFactory.CreateFor(account, balance, balanceRecordedDate: MostRecentRecordedDate);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedAsync(ledger, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<AccountDto>($"/api/Accounts/{account.AccountId}", CancellationToken);

        // Assert
        Assert.Equal(account.AccountId, result.Id);
        Assert.Equal(accountName, result.Name);
        Assert.Equal(balance, result.LatestBalance);
        Assert.Equal(MostRecentRecordedDate, result.LatestBalanceRecordedDate);
        Assert.Equal("CHK", result.AccountTypeCode);
        Assert.Equal("WF", result.InstitutionCode);
    }

    [Fact]
    public async Task GetAccountById_LedgerRecordedDatesOutOfCreationOrder_ReturnsLatestRecordedDateBalance()
    {
        // Arrange: the later-created ledger entry carries an older BalanceRecordedDate
        // than the earlier-created entry, so ordering must be by BalanceRecordedDate
        // rather than SystemCreatedDate.
        var account = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "Backfilled Ledger Checking");
        var mostRecent = AccountLedgerFactory.CreateFor(
            account,
            balance: 1000m,
            balanceRecordedDate: MostRecentRecordedDate);
        var backdated = AccountLedgerFactory.CreateFor(
            account,
            balance: 4321m,
            balanceRecordedDate: BackfilledRecordedDate);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedRangeAsync([mostRecent, backdated], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<AccountDto>($"/api/Accounts/{account.AccountId}", CancellationToken);

        // Assert
        Assert.Equal(1000m, result.LatestBalance);
        Assert.Equal(MostRecentRecordedDate, result.LatestBalanceRecordedDate);
    }

    [Fact]
    public async Task GetAccountById_TiedLedgerRecordedDates_ReturnsBalanceFromNewestCreatedEntry()
    {
        // Arrange: both entries share the same BalanceRecordedDate, so the
        // SystemCreatedDate tie-breaker must select the newer entry.
        var account = AccountFactory.Create(
            AuthenticatedUserId,
            accountName: "Tied Ledger Checking",
            systemCreatedDate: new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));
        var recordedDate = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        var earlierCreated = AccountLedgerFactory.CreateFor(
            account,
            balance: 100m,
            balanceRecordedDate: recordedDate,
            systemCreatedDate: new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero));
        var laterCreated = AccountLedgerFactory.CreateFor(
            account,
            balance: 900m,
            balanceRecordedDate: recordedDate,
            systemCreatedDate: new DateTimeOffset(2026, 8, 1, 11, 0, 0, TimeSpan.Zero));
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedRangeAsync([earlierCreated, laterCreated], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<AccountDto>($"/api/Accounts/{account.AccountId}", CancellationToken);

        // Assert
        Assert.Equal(900m, result.LatestBalance);
        Assert.Equal(recordedDate, result.LatestBalanceRecordedDate);
    }

    [Fact]
    public async Task GetAccountById_AnotherUsersAccount_ReturnsBadRequest()
    {
        // Arrange
        var account = AccountFactory.Create(Guid.NewGuid().ToString(), accountName: "Other Account");
        await DbContext.SeedAsync(account, CancellationToken);

        // Act
        var response = await HttpClient.GetAsync($"/api/Accounts/{account.AccountId}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountById_NonExistentAccount_ReturnsBadRequest()
    {
        // Act
        var response = await HttpClient.GetAsync($"/api/Accounts/{TestConstants.ZeroGuid}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountById_DeactivatedAccount_ReturnsBadRequest()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId);
        account.IsActive = false;
        await DbContext.SeedAsync(account, CancellationToken);

        // Act
        var response = await HttpClient.GetAsync($"/api/Accounts/{account.AccountId}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

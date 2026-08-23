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
        var ledger = AccountLedgerFactory.CreateFor(account, balance);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedAsync(ledger, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<AccountDto>($"/api/Accounts/{account.AccountId}", CancellationToken);

        // Assert
        Assert.Equal(accountName, result.Name);
        Assert.Equal(balance, result.LatestBalance);
        Assert.Equal("CHK", result.AccountTypeCode);
        Assert.Equal("WF", result.InstitutionCode);
    }

    [Fact]
    public async Task GetAccountById_LedgerEntryBackdatedToEarlierPeriod_ReturnsMostRecentRecordedDateBalance()
    {
        // Arrange: a later-created ledger entry carries an older BalanceRecordedDate
        // than the prior entry, so the SystemCreatedDate tie-breaker would select it
        // under the legacy ordering.
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

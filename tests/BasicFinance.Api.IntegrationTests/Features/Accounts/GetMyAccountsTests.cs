using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;
using AccountDto = BasicFinance.Api.IntegrationTests.Helpers.AccountDto;

namespace BasicFinance.Api.IntegrationTests.Features.Accounts;

public class GetMyAccountsTests : ApiTestFixtureBase
{
    public GetMyAccountsTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetMyAccounts_UserHasAccounts_ReturnsAccountList()
    {
        // Arrange
        const string accountName = "My Checking";
        const decimal balance = 5000m;
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: accountName, balance: balance);
        await DbContext.SeedAsync(account, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<AccountDto>>("/api/my/accounts", CancellationToken);

        // Assert
        Assert.Contains(result, a => a.Name == accountName);
        Assert.Contains(result, a => a.Balance == balance);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetMyAccounts_UserHasNoAccounts_ReturnsEmptyList()
    {
        // Act
        var result = await HttpClient.GetResultAsync<List<AccountDto>>("/api/my/accounts", CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMyAccounts_OnlyActiveAccountsIncluded_ExcludesInactive()
    {
        // Arrange
        var activeAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Active Account");
        var inactiveAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Inactive Account");
        inactiveAccount.IsActive = false;
        await DbContext.SeedRangeAsync([activeAccount, inactiveAccount], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<AccountDto>>("/api/my/accounts", CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal("Active Account", result[0].Name);
    }
}

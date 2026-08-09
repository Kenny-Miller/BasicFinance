using BasicFinance.Api.IntegrationTests.Factory;
using BasicFinance.Api.IntegrationTests.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using Xunit;
using AccountDto = BasicFinance.Api.IntegrationTests.Helpers.AccountDto;
using AccountTypeEnum = BasicFinance.Infrastructure.Enums.AccountType;

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
        var account = AccountFactory.Create(TestUserId, accountName: "My Checking", balance: 5000m);
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.GetAsync("/api/my/accounts", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<AccountDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Contains(result, a => a.AccountName == "My Checking");
        Assert.Contains(result, a => a.Balance == 5000m);
    }

    [Fact]
    public async Task GetMyAccounts_UserHasNoAccounts_ReturnsEmptyList()
    {
        // Arrange

        // Act
        var response = await HttpClient.GetAsync("/api/my/accounts", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<AccountDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMyAccounts_OnlyActiveAccountsIncluded_ExcludesInactive()
    {
        // Arrange
        var activeAccount = AccountFactory.Create(TestUserId, accountName: "Active Account");
        var inactiveAccount = AccountFactory.Create(TestUserId, accountName: "Inactive Account");
        inactiveAccount.IsActive = false;

        DbContext.Accounts.AddRange(activeAccount, inactiveAccount);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.GetAsync("/api/my/accounts", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<AccountDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Active Account", result[0].AccountName);
    }
}

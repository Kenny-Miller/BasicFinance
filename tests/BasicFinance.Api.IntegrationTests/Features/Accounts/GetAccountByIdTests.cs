using System.Net.Http.Json;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;
using AccountDto = BasicFinance.Api.IntegrationTests.Helpers.AccountDto;

namespace BasicFinance.Api.IntegrationTests.Features.Accounts;

public class GetAccountByIdTests : ApiTestFixtureBase
{
    public GetAccountByIdTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetAccountById_ExistingAccount_ReturnsOk()
    {
        // Arrange
        var account = AccountFactory.Create(TestUserId, accountName: "My Account", balance: 7500m);
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var accountId = account.AccountId;

        // Act
        var response = await HttpClient.GetAsync($"/api/Accounts/{accountId}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AccountDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("My Account", result.AccountName);
        Assert.Equal(7500m, result.Balance);
    }

    [Fact]
    public async Task GetAccountById_NonExistentAccount_ReturnsBadRequest()
    {
        // Act
        var response = await HttpClient.GetAsync("/api/Accounts/00000000-0000-0000-0000-000000000000", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountById_DeactivatedAccount_ReturnsBadRequest()
    {
        // Arrange
        var account = AccountFactory.Create(TestUserId, accountName: "Inactive Account");
        account.IsActive = false;
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var accountId = account.AccountId;

        // Act
        var response = await HttpClient.GetAsync($"/api/Accounts/{accountId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}

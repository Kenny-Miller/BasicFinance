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
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: accountName, balance: balance);
        await DbContext.SeedAsync(account, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<AccountDto>($"/api/Accounts/{account.AccountId}", CancellationToken);

        // Assert
        Assert.Equal(accountName, result.Name);
        Assert.Equal(balance, result.Balance);
        Assert.Equal("CHK", result.AccountTypeCode);
        Assert.Equal("Wells Fargo", result.Institution);
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

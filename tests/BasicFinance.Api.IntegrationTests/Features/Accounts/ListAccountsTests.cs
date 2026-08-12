using System.Net.Http.Json;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using BasicFinance.Domain.Queries;
using Xunit;
using AccountDto = BasicFinance.Api.IntegrationTests.Helpers.AccountDto;
using AccountTypeEnum = BasicFinance.Infrastructure.Enums.AccountType;

namespace BasicFinance.Api.IntegrationTests.Features.Accounts;

public class ListAccountsTests : ApiTestFixtureBase
{
    public ListAccountsTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task ListAccounts_UserHasAccounts_ReturnsAccountList()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: "Test Account", balance: 2500m);
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.GetAsync("/api/Accounts/", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResult<AccountDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Items.Any());
        Assert.Contains(result.Items, a => a.AccountName == "Test Account");
    }

    [Fact]
    public async Task ListAccounts_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        for (var i = 0; i < 5; i++)
        {
            DbContext.Accounts.Add(AccountFactory.Create(AuthenticatedUserId, accountName: $"Account {i}"));
        }

        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.GetAsync("/api/Accounts/?page=1&pageSize=2", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResult<AccountDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task ListAccounts_FilterByAccountTypeCode_ReturnsFilteredResults()
    {
        // Arrange
        DbContext.Accounts.Add(AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Checking, accountName: "Checking Account"));
        DbContext.Accounts.Add(AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings, accountName: "Savings Account"));
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.GetAsync("/api/Accounts/?accountTypeCode=CHK", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResult<AccountDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        foreach (var account in result.Items)
        {
            Assert.Equal("CHK", account.AccountTypeCode);
        }
    }

    [Fact]
    public async Task ListAccounts_FilterByInstitution_ReturnsFilteredResults()
    {
        // Arrange
        DbContext.Accounts.Add(AccountFactory.Create(AuthenticatedUserId, institutionId: 1, accountName: "WF Account"));
        DbContext.Accounts.Add(AccountFactory.Create(AuthenticatedUserId, institutionId: 2, accountName: "Chase Account"));
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.GetAsync("/api/Accounts/?institution=Wells+Fargo", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResult<AccountDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        foreach (var account in result.Items)
        {
            Assert.Equal("Wells Fargo", account.Institution);
        }
    }

    [Fact]
    public async Task ListAccounts_UserHasNoAccounts_ReturnsEmptyList()
    {
        // Act
        var response = await HttpClient.GetAsync("/api/Accounts/", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResult<AccountDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }
}

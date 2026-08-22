using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
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
        const string accountName = "Test Account";
        const decimal balance = 2500m;
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: accountName, balance: balance);
        await DbContext.SeedAsync(account, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<AccountDto>>("/api/Accounts/", CancellationToken);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(QueryConstants.DefaultPageSize, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageCount);
        Assert.Single(result.Items);
        Assert.Contains(result.Items, a => a.Name == accountName);
        Assert.Contains(result.Items, a => a.Balance == balance);
        Assert.Contains(result.Items, a => a.AccountTypeCode == "CHK");
    }

    [Fact]
    public async Task ListAccounts_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var accounts = AccountFactory.CreateBatch(5, AuthenticatedUserId).ToList();
        await DbContext.SeedRangeAsync(accounts, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<AccountDto>>("/api/Accounts/?page=1&pageSize=2", CancellationToken);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.PageCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task ListAccounts_FilterByAccountTypeCode_ReturnsFilteredResults()
    {
        // Arrange
        var checkingAccount = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Checking);
        var savingsAccount = AccountFactory.Create(AuthenticatedUserId, accountType: AccountTypeEnum.Savings);
        await DbContext.SeedRangeAsync([checkingAccount, savingsAccount], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<AccountDto>>("/api/Accounts/?accountTypeCode=CHK", CancellationToken);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageCount);
        Assert.All(result.Items, account => Assert.Equal("CHK", account.AccountTypeCode));
    }

    [Fact]
    public async Task ListAccounts_FilterByInstitution_ReturnsFilteredResults()
    {
        // Arrange
        var wfAccount = AccountFactory.Create(AuthenticatedUserId, institutionId: TestConstants.WellsFargoInstitutionId);
        var chaseAccount = AccountFactory.Create(AuthenticatedUserId, institutionId: TestConstants.ChaseInstitutionId);
        await DbContext.SeedRangeAsync([wfAccount, chaseAccount], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<AccountDto>>("/api/Accounts/?institution=Wells+Fargo", CancellationToken);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageCount);
        Assert.All(result.Items, account => Assert.Equal("Wells Fargo", account.Institution));
    }

    [Fact]
    public async Task ListAccounts_UserHasNoAccounts_ReturnsEmptyList()
    {
        // Act
        var result = await HttpClient.GetResultAsync<ListResult<AccountDto>>("/api/Accounts/", CancellationToken);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(QueryConstants.DefaultPageSize, result.PageSize);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.PageCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task ListAccounts_WithSorting_SortsByAccountNameAsc()
    {
        // Arrange
        var zebraAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Zebra Account");
        var alphaAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Alpha Account");
        var middleAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Middle Account");
        await DbContext.SeedRangeAsync([zebraAccount, alphaAccount, middleAccount], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<AccountDto>>("/api/Accounts/?sortField=Name&sortDirection=Asc", CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);
        var names = result.Items.Select(a => a.Name).ToList();
        Assert.Equal("Alpha Account", names[0]);
        Assert.Equal("Middle Account", names[1]);
        Assert.Equal("Zebra Account", names[2]);
    }

    [Fact]
    public async Task ListAccounts_WithSorting_SortsByBalanceDesc()
    {
        // Arrange
        var lowBalance = AccountFactory.Create(AuthenticatedUserId, balance: 100m);
        var highBalance = AccountFactory.Create(AuthenticatedUserId, balance: 10000m);
        var midBalance = AccountFactory.Create(AuthenticatedUserId, balance: 5000m);
        await DbContext.SeedRangeAsync([lowBalance, highBalance, midBalance], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<AccountDto>>("/api/Accounts/?sortField=Balance&sortDirection=Desc", CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);
        var balances = result.Items.Select(a => a.Balance).ToList();
        Assert.Equal(10000m, balances[0]);
        Assert.Equal(5000m, balances[1]);
        Assert.Equal(100m, balances[2]);
    }
}

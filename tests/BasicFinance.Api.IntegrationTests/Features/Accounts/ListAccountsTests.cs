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
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: accountName);
        var ledger = AccountLedgerFactory.CreateFor(account, balance);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedAsync(ledger, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<AccountDto>>("/api/Accounts/", CancellationToken);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(QueryConstants.DefaultPageSize, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageCount);
        Assert.Single(result.Items);
        Assert.Contains(result.Items, a => a.Name == accountName);
        Assert.Contains(result.Items, a => a.LatestBalance == balance);
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
        var names = result.Items.Select(a => a.Name).ToList();
        Assert.Equal("Account 0", names[0]);
        Assert.Equal("Account 1", names[1]);
    }

    [Fact]
    public async Task ListAccounts_SecondPage_ReturnsRemainingItems()
    {
        // Arrange
        var accounts = AccountFactory.CreateBatch(3, AuthenticatedUserId).ToList();
        await DbContext.SeedRangeAsync(accounts, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<AccountDto>>("/api/Accounts/?page=2&pageSize=2", CancellationToken);

        // Assert
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.PageCount);
        var names = result.Items.Select(a => a.Name).ToList();
        Assert.Single(names);
        Assert.Equal("Account 2", names[0]);
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
        Assert.All(result.Items, account => Assert.Equal("WF", account.InstitutionCode));
    }

    [Fact]
    public async Task ListAccounts_WithCombinedFilters_ReturnsOnlyMatchingAccounts()
    {
        // Arrange
        var matchingAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "WF Checking", institutionId: TestConstants.WellsFargoInstitutionId);
        var otherInstitutionAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Chase Checking", institutionId: TestConstants.ChaseInstitutionId);
        var otherTypeAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "WF Savings", accountType: AccountTypeEnum.Savings, institutionId: TestConstants.WellsFargoInstitutionId);
        await DbContext.SeedRangeAsync([matchingAccount, otherInstitutionAccount, otherTypeAccount], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<AccountDto>>("/api/Accounts/?accountTypeCode=CHK&institution=Wells+Fargo", CancellationToken);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(QueryConstants.DefaultPageSize, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageCount);
        var detail = Assert.Single(result.Items);
        Assert.Equal("WF Checking", detail.Name);
        Assert.Equal("CHK", detail.AccountTypeCode);
        Assert.Equal("WF", detail.InstitutionCode);
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
        var lowBalance = AccountFactory.Create(AuthenticatedUserId, accountName: "Low Balance Account");
        var highBalance = AccountFactory.Create(AuthenticatedUserId, accountName: "High Balance Account");
        var midBalance = AccountFactory.Create(AuthenticatedUserId, accountName: "Mid Balance Account");
        await DbContext.SeedRangeAsync([lowBalance, highBalance, midBalance], CancellationToken);
        await DbContext.SeedAsync(AccountLedgerFactory.CreateFor(lowBalance, 100m), CancellationToken);
        await DbContext.SeedAsync(AccountLedgerFactory.CreateFor(highBalance, 10000m), CancellationToken);
        await DbContext.SeedAsync(AccountLedgerFactory.CreateFor(midBalance, 5000m), CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<AccountDto>>("/api/Accounts/?sortField=LatestBalance&sortDirection=Desc", CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);
        var balances = result.Items.Select(a => a.LatestBalance).ToList();
        Assert.Equal(10000m, balances[0]);
        Assert.Equal(5000m, balances[1]);
        Assert.Equal(100m, balances[2]);
    }

    [Fact]
    public async Task ListAccounts_UnknownSortField_FallsBackToNameAscending()
    {
        // Arrange
        var zebraAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Zebra Account");
        var alphaAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Alpha Account");
        var middleAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Middle Account");
        await DbContext.SeedRangeAsync([zebraAccount, alphaAccount, middleAccount], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<AccountDto>>("/api/Accounts/?sortField=Bogus", CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);
        var names = result.Items.Select(a => a.Name).ToList();
        Assert.Equal("Alpha Account", names[0]);
        Assert.Equal("Middle Account", names[1]);
        Assert.Equal("Zebra Account", names[2]);
    }

    [Fact]
    public async Task ListAccounts_AnotherUserHasAccounts_ExcludesTheirAccounts()
    {
        // Arrange
        var myAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "My Account");
        var otherAccount = AccountFactory.Create(Guid.NewGuid().ToString(), accountName: "Other Account");
        await DbContext.SeedRangeAsync([myAccount, otherAccount], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<AccountDto>>("/api/Accounts/", CancellationToken);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(QueryConstants.DefaultPageSize, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageCount);
        Assert.Equal("My Account", result.Items.Single().Name);
    }

    [Fact]
    public async Task ListAccounts_UserHasDeactivatedAccount_ExcludesDeactivatedAccount()
    {
        // Arrange
        var activeAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Active Account");
        var deactivatedAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Deactivated Account");
        deactivatedAccount.IsActive = false;
        await DbContext.SeedRangeAsync([activeAccount, deactivatedAccount], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<AccountDto>>("/api/Accounts/", CancellationToken);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(QueryConstants.DefaultPageSize, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageCount);
        Assert.Equal("Active Account", result.Items.Single().Name);
    }
}

using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using BasicFinance.Domain.Queries;
using Xunit;
using TransactionDto = BasicFinance.Api.IntegrationTests.Helpers.TransactionDto;

namespace BasicFinance.Api.IntegrationTests.Features.Transactions;

public class ListTransactionsTests : ApiTestFixtureBase
{
    public ListTransactionsTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task ListTransactions_UserHasTransactions_ReturnsTransactionList()
    {
        // Arrange
        const string description = "Test Purchase";
        const decimal amount = 42.50m;
        var account = AccountFactory.Create(AuthenticatedUserId);
        await DbContext.SeedAsync(account, CancellationToken);
        var transaction = TransactionFactory.Create(AuthenticatedUserId, account.AccountId, description: description, amount: amount);
        await DbContext.SeedAsync(transaction, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<TransactionDto>>("/api/transactions/", CancellationToken);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(QueryConstants.DefaultPageSize, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageCount);
        Assert.Single(result.Items);
        Assert.Contains(result.Items, t => t.Description == description);
        Assert.Contains(result.Items, t => t.Amount == amount);
        Assert.Contains(result.Items, t => t.TransactionTypeName == "Debit");
        Assert.Contains(result.Items, t => t.AccountName == account.AccountName);
    }

    [Fact]
    public async Task ListTransactions_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId);
        await DbContext.SeedAsync(account, CancellationToken);
        var transactions = TransactionFactory.CreateBatch(5, AuthenticatedUserId, account.AccountId).ToList();
        await DbContext.SeedRangeAsync(transactions, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<TransactionDto>>("/api/transactions/?page=1&pageSize=2", CancellationToken);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.PageCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task ListTransactions_FilterByAccountId_ReturnsFilteredResults()
    {
        // Arrange
        var account1 = AccountFactory.Create(AuthenticatedUserId, accountName: "Account 1");
        var account2 = AccountFactory.Create(AuthenticatedUserId, accountName: "Account 2");
        await DbContext.SeedRangeAsync([account1, account2], CancellationToken);
        var tx1 = TransactionFactory.Create(AuthenticatedUserId, account1.AccountId, description: "Tx for Account 1");
        var tx2 = TransactionFactory.Create(AuthenticatedUserId, account2.AccountId, description: "Tx for Account 2");
        await DbContext.SeedRangeAsync([tx1, tx2], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<TransactionDto>>($"/api/transactions/?accountId={account1.AccountId}", CancellationToken);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageCount);
        Assert.All(result.Items, transaction => Assert.Equal("Account 1", transaction.AccountName));
    }

    [Fact]
    public async Task ListTransactions_FilterByMinAmount_ReturnsFilteredResults()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId);
        await DbContext.SeedAsync(account, CancellationToken);
        var smallTx = TransactionFactory.Create(AuthenticatedUserId, account.AccountId, amount: 50m);
        var largeTx = TransactionFactory.Create(AuthenticatedUserId, account.AccountId, amount: 250m);
        await DbContext.SeedRangeAsync([smallTx, largeTx], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<TransactionDto>>("/api/transactions/?minAmount=100", CancellationToken);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageCount);
        Assert.All(result.Items, transaction => Assert.True(transaction.Amount >= 100m));
        Assert.Contains(result.Items, t => t.Amount == 250m);
    }

    [Fact]
    public async Task ListTransactions_UserHasNoTransactions_ReturnsEmptyList()
    {
        // Act
        var result = await HttpClient.GetResultAsync<ListResult<TransactionDto>>("/api/transactions/", CancellationToken);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(QueryConstants.DefaultPageSize, result.PageSize);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.PageCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task ListTransactions_WithSorting_SortsByDateAsc()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId);
        await DbContext.SeedAsync(account, CancellationToken);

        var oldestDate = DateTimeOffset.UtcNow.AddDays(-10);
        var newestDate = DateTimeOffset.UtcNow;
        var middleDate = DateTimeOffset.UtcNow.AddDays(-5);

        var oldestTx = TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: oldestDate, description: "Oldest");
        var newestTx = TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: newestDate, description: "Newest");
        var middleTx = TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: middleDate, description: "Middle");

        await DbContext.SeedRangeAsync([oldestTx, newestTx, middleTx], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<TransactionDto>>("/api/transactions/?sortField=Date&sortDirection=Asc", CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);
        var descriptions = result.Items.Select(t => t.Description).ToList();
        Assert.Equal("Oldest", descriptions[0]);
        Assert.Equal("Middle", descriptions[1]);
        Assert.Equal("Newest", descriptions[2]);
    }

    [Fact]
    public async Task ListTransactions_WithSorting_SortsByAmountDesc()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId);
        await DbContext.SeedAsync(account, CancellationToken);

        var lowAmount = TransactionFactory.Create(AuthenticatedUserId, account.AccountId, amount: 10m, description: "Low");
        var highAmount = TransactionFactory.Create(AuthenticatedUserId, account.AccountId, amount: 1000m, description: "High");
        var midAmount = TransactionFactory.Create(AuthenticatedUserId, account.AccountId, amount: 500m, description: "Mid");

        await DbContext.SeedRangeAsync([lowAmount, highAmount, midAmount], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<TransactionDto>>("/api/transactions/?sortField=Amount&sortDirection=Desc", CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);
        var amounts = result.Items.Select(t => t.Amount).ToList();
        Assert.Equal(1000m, amounts[0]);
        Assert.Equal(500m, amounts[1]);
        Assert.Equal(10m, amounts[2]);
    }
}

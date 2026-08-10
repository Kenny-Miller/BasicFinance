using System.Net.Http.Json;
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
        var account = AccountFactory.Create(TestUserId);
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transaction = TransactionFactory.Create(TestUserId, account.AccountId, description: "Test Purchase");
        DbContext.Transactions.Add(transaction);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.GetAsync("/api/transactions/", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResult<TransactionDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Items.Any());
        Assert.Contains(result.Items, t => t.Description == "Test Purchase");
    }

    [Fact]
    public async Task ListTransactions_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var account = AccountFactory.Create(TestUserId);
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        for (var i = 0; i < 5; i++)
        {
            DbContext.Transactions.Add(TransactionFactory.Create(TestUserId, account.AccountId, description: $"Transaction {i}"));
        }

        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.GetAsync("/api/transactions/?page=1&pageSize=2", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResult<TransactionDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task ListTransactions_FilterByAccountId_ReturnsFilteredResults()
    {
        // Arrange
        var account1 = AccountFactory.Create(TestUserId, accountName: "Account 1");
        var account2 = AccountFactory.Create(TestUserId, accountName: "Account 2");
        DbContext.Accounts.AddRange(account1, account2);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        DbContext.Transactions.Add(TransactionFactory.Create(TestUserId, account1.AccountId, description: "Tx for Account 1"));
        DbContext.Transactions.Add(TransactionFactory.Create(TestUserId, account2.AccountId, description: "Tx for Account 2"));
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.GetAsync($"/api/transactions/?accountId={account1.AccountId}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResult<TransactionDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        foreach (var transaction in result.Items)
        {
            Assert.Equal("Account 1", transaction.AccountName);
        }
    }

    [Fact]
    public async Task ListTransactions_FilterByMinAmount_ReturnsFilteredResults()
    {
        // Arrange
        var account = AccountFactory.Create(TestUserId);
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        DbContext.Transactions.Add(TransactionFactory.Create(TestUserId, account.AccountId, amount: 50m, description: "Small Purchase"));
        DbContext.Transactions.Add(TransactionFactory.Create(TestUserId, account.AccountId, amount: 250m, description: "Large Purchase"));
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.GetAsync("/api/transactions/?minAmount=100", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResult<TransactionDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        foreach (var transaction in result.Items)
        {
            Assert.True(transaction.Amount >= 100m);
        }
    }

    [Fact]
    public async Task ListTransactions_UserHasNoTransactions_ReturnsEmptyList()
    {
        // Act
        var response = await HttpClient.GetAsync("/api/transactions/", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResult<TransactionDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }
}

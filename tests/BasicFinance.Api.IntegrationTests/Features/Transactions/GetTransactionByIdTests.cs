using BasicFinance.Api.IntegrationTests.Factory;
using BasicFinance.Api.IntegrationTests.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using System.Net.Http.Json;
using Xunit;
using TransactionDto = BasicFinance.Api.IntegrationTests.Helpers.TransactionDto;

namespace BasicFinance.Api.IntegrationTests.Features.Transactions;

public class GetTransactionByIdTests : ApiTestFixtureBase
{
    public GetTransactionByIdTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetTransactionById_ExistingTransaction_ReturnsOk()
    {
        // Arrange
        var account = AccountFactory.Create(TestUserId);
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transaction = TransactionFactory.Create(TestUserId, account.AccountId, description: "Specific Purchase", amount: 42.50m);
        DbContext.Transactions.Add(transaction);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transactionId = transaction.TransactionId;

        // Act
        var response = await HttpClient.GetAsync($"/api/transactions/{transactionId}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TransactionDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("Specific Purchase", result.Description);
        Assert.Equal(42.50m, result.Amount);
    }

    [Fact]
    public async Task GetTransactionById_NonExistentTransaction_ReturnsBadRequest()
    {
        // Act
        var response = await HttpClient.GetAsync("/api/transactions/00000000-0000-0000-0000-000000000000", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactionById_DeactivatedTransaction_ReturnsBadRequest()
    {
        // Arrange
        var account = AccountFactory.Create(TestUserId);
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transaction = TransactionFactory.Create(TestUserId, account.AccountId);
        transaction.IsActive = false;
        DbContext.Transactions.Add(transaction);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transactionId = transaction.TransactionId;

        // Act
        var response = await HttpClient.GetAsync($"/api/transactions/{transactionId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}

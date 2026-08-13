using System.Net;
using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
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
        const string description = "Specific Purchase";
        const decimal amount = 42.50m;
        var account = AccountFactory.Create(AuthenticatedUserId);
        await DbContext.SeedAsync(account, CancellationToken);
        var transaction = TransactionFactory.Create(AuthenticatedUserId, account.AccountId, description: description, amount: amount);
        await DbContext.SeedAsync(transaction, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<TransactionDto>($"/api/transactions/{transaction.TransactionId}", CancellationToken);

        // Assert
        Assert.Equal(description, result.Description);
        Assert.Equal(amount, result.Amount);
        Assert.Equal("Debit", result.TransactionTypeName);
        Assert.Equal(account.AccountName, result.AccountName);
    }

    [Fact]
    public async Task GetTransactionById_NonExistentTransaction_ReturnsBadRequest()
    {
        // Act
        var response = await HttpClient.GetAsync($"/api/transactions/{TestConstants.ZeroGuid}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactionById_DeactivatedTransaction_ReturnsBadRequest()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId);
        await DbContext.SeedAsync(account, CancellationToken);
        var transaction = TransactionFactory.Create(AuthenticatedUserId, account.AccountId);
        transaction.IsActive = false;
        await DbContext.SeedAsync(transaction, CancellationToken);

        // Act
        var response = await HttpClient.GetAsync($"/api/transactions/{transaction.TransactionId}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

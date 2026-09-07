using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using BasicFinance.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BasicFinance.Api.IntegrationTests.Features.TransactionTypes;

public class ListTransactionTypesTests : ApiTestFixtureBase
{
    public ListTransactionTypesTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    private static readonly string[] SeededTransactionTypeCodes =
    [
        "CR",
        "DR"
    ];

    [Fact]
    public async Task ListTransactionTypes_SeededReferenceData_ReturnsAllActiveTransactionTypes()
    {
        // Act
        var result = await HttpClient.GetResultAsync<List<TransactionTypeDto>>("/api/transaction-types/", CancellationToken);

        // Assert
        Assert.Equal(SeededTransactionTypeCodes.Length, result.Count);
        Assert.All(result, x => Assert.Contains(x.Code, SeededTransactionTypeCodes));
        Assert.Contains(result, x => x.Code == "CR" && x.Name == "Credit");
        Assert.Contains(result, x => x.Code == "DR" && x.Name == "Debit");
    }

    [Fact]
    public async Task ListTransactionTypes_InactiveTransactionType_ReturnsOnlyActiveTransactionTypes()
    {
        // Arrange
        var inactiveTransactionType = await DbContext.TransactionTypes.SingleAsync(x => x.TransactionTypeCode == "DR", CancellationToken);
        inactiveTransactionType.IsActive = false;
        await DbContext.SaveChangesAsync(CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<TransactionTypeDto>>("/api/transaction-types/", CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal("CR", result[0].Code);
    }

    [Fact]
    public async Task ListTransactionTypes_NoActiveTransactionTypes_ReturnsEmptyList()
    {
        // Arrange
        await DbContext.TransactionTypes
            .Where(x => x.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsActive, false), CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<TransactionTypeDto>>("/api/transaction-types/", CancellationToken);

        // Assert
        Assert.Empty(result);
    }
}

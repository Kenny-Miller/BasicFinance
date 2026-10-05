using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using BasicFinance.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BasicFinance.Api.IntegrationTests.Features.TransactionCategories;

public class ListTransactionCategoriesTests : ApiTestFixtureBase
{
    public ListTransactionCategoriesTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    private static readonly string[] SeededTransactionCategoryCodes =
    [
        "CR",
        "DR",
        "UNC",
        "AUTO",
        "BILLS",
        "BUSINESS",
        "CASH",
        "DONATIONS",
        "DINING",
        "EDUCATION",
        "ENTERTAINMENT",
        "FAMILY",
        "FEES",
        "GIFTS",
        "GROCERIES",
        "HEALTH",
        "HOME",
        "LEGAL",
        "LOAN",
        "MEDICAL",
        "PERSONAL",
        "PETS",
        "SHOPPING",
        "SOFTWARE",
        "TAXES",
        "TRAVEL",
        "INCOME",
        "INVESTMENT",
        "CREDIT",
        "IGNORE",
        "TRANSFER",
        "REIMBURSEMENT",
        "SAVINGS"
    ];

    [Fact]
    public async Task ListTransactionCategories_SeededReferenceData_ReturnsAllActiveTransactionCategories()
    {
        // Act
        var result = await HttpClient.GetResultAsync<List<TransactionCategoryDto>>("/api/transaction-categories/", CancellationToken);

        // Assert
        Assert.Equal(SeededTransactionCategoryCodes.Length, result.Count);
        Assert.All(result, x => Assert.Contains(x.Code, SeededTransactionCategoryCodes));
        Assert.Contains(result, x => x.Code == "UNC" && x.Name == "Uncategorized");
        Assert.Contains(result, x => x.Code == "GROCERIES" && x.Name == "Groceries");
        Assert.Contains(result, x => x.Code == "INCOME" && x.Name == "Income");
    }

    [Fact]
    public async Task ListTransactionCategories_InactiveTransactionCategory_ReturnsOnlyActiveTransactionCategories()
    {
        // Arrange
        var inactiveTransactionCategory = await DbContext.TransactionCategories.SingleAsync(x => x.TransactionCategoryCode == "IGNORE", CancellationToken);
        inactiveTransactionCategory.IsActive = false;
        await DbContext.SaveChangesAsync(CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<TransactionCategoryDto>>("/api/transaction-categories/", CancellationToken);

        // Assert
        Assert.Equal(SeededTransactionCategoryCodes.Length - 1, result.Count);
        Assert.DoesNotContain(result, x => x.Code == "IGNORE");
    }

    [Fact]
    public async Task ListTransactionCategories_NoActiveTransactionCategories_ReturnsEmptyList()
    {
        // Arrange
        await DbContext.TransactionCategories
            .Where(x => x.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsActive, false), CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<TransactionCategoryDto>>("/api/transaction-categories/", CancellationToken);

        // Assert
        Assert.Empty(result);
    }
}

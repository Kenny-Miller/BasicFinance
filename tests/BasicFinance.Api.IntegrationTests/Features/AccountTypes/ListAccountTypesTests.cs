using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using BasicFinance.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BasicFinance.Api.IntegrationTests.Features.AccountTypes;

public class ListAccountTypesTests : ApiTestFixtureBase
{
    public ListAccountTypesTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    private static readonly string[] SeededAccountTypeCodes =
    [
        "CHK",
        "SAV",
        "CC",
        "INV"
    ];

    [Fact]
    public async Task ListAccountTypes_SeededReferenceData_ReturnsAllActiveAccountTypes()
    {
        // Act
        var result = await HttpClient.GetResultAsync<List<AccountTypeDto>>("/api/account-types/", CancellationToken);

        // Assert
        Assert.Equal(SeededAccountTypeCodes.Length, result.Count);
        Assert.All(result, x => Assert.Contains(x.Code, SeededAccountTypeCodes));
        Assert.Contains(result, x => x.Code == "CHK" && x.Name == "Checking" && !x.IsLiability);
        Assert.Contains(result, x => x.Code == "SAV" && x.Name == "Savings" && !x.IsLiability);
        Assert.Contains(result, x => x.Code == "CC" && x.Name == "Credit Card" && x.IsLiability);
        Assert.Contains(result, x => x.Code == "INV" && x.Name == "Investment" && !x.IsLiability);
        var orderedNames = result.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal).ToList();
        Assert.Equal(orderedNames, result.Select(x => x.Name).ToList());
    }

    [Fact]
    public async Task ListAccountTypes_InactiveAccountType_ReturnsOnlyActiveAccountTypes()
    {
        // Arrange
        var inactiveAccountType = await DbContext.AccountTypes.SingleAsync(x => x.AccountTypeCode == "INV", CancellationToken);
        inactiveAccountType.IsActive = false;
        await DbContext.SaveChangesAsync(CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<AccountTypeDto>>("/api/account-types/", CancellationToken);

        // Assert
        Assert.Equal(SeededAccountTypeCodes.Length - 1, result.Count);
        Assert.DoesNotContain(result, x => x.Code == "INV");
    }

    [Fact]
    public async Task ListAccountTypes_NoActiveAccountTypes_ReturnsEmptyList()
    {
        // Arrange
        await DbContext.AccountTypes
            .Where(x => x.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsActive, false), CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<AccountTypeDto>>("/api/account-types/", CancellationToken);

        // Assert
        Assert.Empty(result);
    }
}

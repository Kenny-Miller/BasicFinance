using System.Net.Http.Json;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;
using MyInstitutionDto = BasicFinance.Api.IntegrationTests.Helpers.MyInstitutionDto;

namespace BasicFinance.Api.IntegrationTests.Features.Institutions;

public class GetMyInstitutionsTests : ApiTestFixtureBase
{
    public GetMyInstitutionsTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetMyInstitutions_UserHasAccounts_ReturnsAssociatedInstitutions()
    {
        // Arrange
        var account = AccountFactory.Create(TestUserId, institutionId: 1);
        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.GetAsync("/api/my/institutions", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<MyInstitutionDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Contains(result, i => i.InstitutionId == 1);
    }

    [Fact]
    public async Task GetMyInstitutions_UserHasNoAccounts_ReturnsEmptyList()
    {
        // Arrange

        // Act
        var response = await HttpClient.GetAsync("/api/my/institutions", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<MyInstitutionDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMyInstitutions_OnlyActiveAccountsCount_ReturnsCorrectInstitutions()
    {
        // Arrange
        var activeAccount = AccountFactory.Create(TestUserId, institutionId: 1);
        var inactiveAccount = AccountFactory.Create(TestUserId, institutionId: 2);
        inactiveAccount.IsActive = false;

        DbContext.Accounts.AddRange(activeAccount, inactiveAccount);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.GetAsync("/api/my/institutions", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<MyInstitutionDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(1, result[0].InstitutionId);
    }
}

using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;
using InstitutionDto = BasicFinance.Api.IntegrationTests.Helpers.InstitutionDto;

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
        var account = AccountFactory.Create(AuthenticatedUserId, institutionId: TestConstants.WellsFargoInstitutionId);
        await DbContext.SeedAsync(account, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<InstitutionDto>>("/api/my/institutions", CancellationToken);

        // Assert
        var institution = result.Single();
        Assert.Equal(TestConstants.WellsFargoInstitutionId, institution.Id);
        Assert.Equal("WF", institution.Code);
        Assert.Equal("Wells Fargo", institution.Name);
        Assert.Null(institution.LogoUrl);
    }

    [Fact]
    public async Task GetMyInstitutions_UserHasNoAccounts_ReturnsEmptyList()
    {
        // Act
        var result = await HttpClient.GetResultAsync<List<InstitutionDto>>("/api/my/institutions", CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMyInstitutions_OnlyActiveAccountsCount_ReturnsCorrectInstitutions()
    {
        // Arrange
        var activeAccount = AccountFactory.Create(AuthenticatedUserId, institutionId: TestConstants.WellsFargoInstitutionId);
        var inactiveAccount = AccountFactory.Create(AuthenticatedUserId, institutionId: TestConstants.ChaseInstitutionId);
        inactiveAccount.IsActive = false;
        await DbContext.SeedRangeAsync([activeAccount, inactiveAccount], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<InstitutionDto>>("/api/my/institutions", CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal(TestConstants.WellsFargoInstitutionId, result[0].Id);
        Assert.Equal("WF", result[0].Code);
        Assert.Equal("Wells Fargo", result[0].Name);
        Assert.Null(result[0].LogoUrl);
    }
}

using System.Net;
using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;
using InstitutionDto = BasicFinance.Api.IntegrationTests.Helpers.InstitutionDto;

namespace BasicFinance.Api.IntegrationTests.Features.Institutions;

public class GetInstitutionByIdTests : ApiTestFixtureBase
{
    public GetInstitutionByIdTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetInstitutionById_ExistingInstitution_ReturnsOk()
    {
        // Arrange
        var institution = InstitutionFactory.Create("Test Institution", "TEST");
        await DbContext.SeedAsync(institution, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InstitutionDto>($"/api/institutions/{institution.InstitutionId}", CancellationToken);

        // Assert
        Assert.Equal(institution.InstitutionId, result.Id);
        Assert.Equal("TEST", result.Code);
        Assert.Equal("Test Institution", result.Name);
    }

    [Fact]
    public async Task GetInstitutionById_NonExistentInstitution_ReturnsBadRequest()
    {
        // Act
        var response = await HttpClient.GetAsync($"/api/institutions/{TestConstants.NonExistentInstitutionId}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetInstitutionById_DeactivatedInstitution_ReturnsBadRequest()
    {
        // Arrange
        var institution = InstitutionFactory.Create("Inactive Institution", "INACTIVE");
        institution.IsActive = false;
        await DbContext.SeedAsync(institution, CancellationToken);

        // Act
        var response = await HttpClient.GetAsync($"/api/institutions/{institution.InstitutionId}", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

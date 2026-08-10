using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
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
        DbContext.Institutions.Add(institution);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var institutionId = institution.InstitutionId;

        // Act
        var response = await HttpClient.GetAsync($"/api/institutions/{institutionId}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<InstitutionDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("TEST", result.InstitutionCode);
        Assert.Equal("Test Institution", result.Name);
    }

    [Fact]
    public async Task GetInstitutionById_NonExistentInstitution_ReturnsBadRequest()
    {
        // Act
        var response = await HttpClient.GetAsync("/api/institutions/99999", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetInstitutionById_DeactivatedInstitution_ReturnsBadRequest()
    {
        // Arrange
        var institution = InstitutionFactory.Create("Inactive Institution", "INACTIVE");
        institution.IsActive = false;
        DbContext.Institutions.Add(institution);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var institutionId = institution.InstitutionId;

        // Act
        var response = await HttpClient.GetAsync($"/api/institutions/{institutionId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}

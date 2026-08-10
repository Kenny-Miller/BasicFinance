using System.Net.Http.Json;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using BasicFinance.Domain.Queries;
using Xunit;
using InstitutionDto = BasicFinance.Api.IntegrationTests.Helpers.InstitutionDto;

namespace BasicFinance.Api.IntegrationTests.Features.Institutions;

public class ListInstitutionsTests : ApiTestFixtureBase
{
    public ListInstitutionsTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task ListInstitutions_NoFilter_ReturnsAllActiveInstitutions()
    {
        // Arrange
        DbContext.Institutions.Add(InstitutionFactory.Create("Test Institution", "TEST"));
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.GetAsync("/api/institutions/", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResult<InstitutionDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Items.Count() >= 4);
    }

    [Fact]
    public async Task ListInstitutions_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        DbContext.Institutions.Add(InstitutionFactory.Create("Another Institution", "ANOTHER"));
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.GetAsync("/api/institutions/?page=1&pageSize=2", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResult<InstitutionDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task ListInstitutions_WithSorting_SortsByName()
    {
        // Arrange

        // Act
        var response = await HttpClient.GetAsync("/api/institutions/?sortField=Name&sortDirection=Asc", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResult<InstitutionDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        var names = result.Items.Select(i => i.Name).ToList();
        var sortedNames = names.Order().ToList();
        Assert.Equal(sortedNames, names);
    }
}

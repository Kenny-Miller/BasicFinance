using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
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
        var institution = InstitutionFactory.Create("Test Institution", "TEST");
        await DbContext.SeedAsync(institution, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<InstitutionDto>>("/api/institutions/", CancellationToken);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(QueryConstants.DefaultPageSize, result.PageSize);
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(1, result.PageCount);
        Assert.Equal(4, result.Items.Count());
        Assert.Contains(result.Items, i => i.Name == "Test Institution");
    }

    [Fact]
    public async Task ListInstitutions_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var institution = InstitutionFactory.Create("Another Institution", "ANOTHER");
        await DbContext.SeedAsync(institution, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<InstitutionDto>>("/api/institutions/?page=1&pageSize=2", CancellationToken);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(2, result.PageCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task ListInstitutions_WithSorting_SortsByNameAsc()
    {
        // Arrange
        var zebraInstitution = InstitutionFactory.Create("Zebra Bank", "ZEB");
        var alphaInstitution = InstitutionFactory.Create("Alpha Bank", "ALP");
        await DbContext.SeedRangeAsync([zebraInstitution, alphaInstitution], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<InstitutionDto>>("/api/institutions/?sortField=Name&sortDirection=Asc", CancellationToken);

        // Assert
        Assert.Equal(5, result.TotalCount);
        var names = result.Items.Select(i => i.Name).ToList();
        Assert.Equal("Alpha Bank", names[0]);
    }

    [Fact]
    public async Task ListInstitutions_WithSorting_SortsByNameDesc()
    {
        // Arrange
        var zebraInstitution = InstitutionFactory.Create("Zebra Bank", "ZEB");
        var alphaInstitution = InstitutionFactory.Create("Alpha Bank", "ALP");
        await DbContext.SeedRangeAsync([zebraInstitution, alphaInstitution], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<ListResult<InstitutionDto>>("/api/institutions/?sortField=Name&sortDirection=Desc", CancellationToken);

        // Assert
        Assert.Equal(5, result.TotalCount);
        var names = result.Items.Select(i => i.Name).ToList();
        Assert.Equal("Zebra Bank", names[0]);
    }
}

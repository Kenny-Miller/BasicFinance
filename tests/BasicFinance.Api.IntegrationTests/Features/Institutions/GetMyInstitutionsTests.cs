using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using MyInstitutionResponseDto = BasicFinance.Api.IntegrationTests.Helpers.MyInstitutionResponseDto;

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
        const decimal openingBalance = 500m;
        const decimal latestBalance = 1250.75m;
        const decimal amexBalance = 750m;
        const string amexLogoUrl = "https://example.com/amex-logo.png";
        var openingDate = DateTimeOffset.UtcNow.AddDays(-2);
        var latestDate = DateTimeOffset.UtcNow;
        var account = AccountFactory.Create(AuthenticatedUserId, openingBalance: openingBalance, openingBalanceRecordedDate: openingDate);
        var ledger = AccountLedgerFactory.CreateFor(account, latestBalance, latestDate);
        var amexInstitution = InstitutionFactory.Create(name: "Amex", institutionCode: "AMEX", logoUrl: amexLogoUrl);
        await DbContext.SeedAsync(account, CancellationToken);
        await DbContext.SeedAsync(ledger, CancellationToken);
        await DbContext.SeedAsync(amexInstitution, CancellationToken);
        var amexAccount = AccountFactory.Create(AuthenticatedUserId, institutionId: amexInstitution.InstitutionId, openingBalance: amexBalance);
        await DbContext.SeedAsync(amexAccount, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<MyInstitutionResponseDto>>("/api/my/institutions", CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        var wellsFargo = result.Single(i => i.InstitutionId == TestConstants.WellsFargoInstitutionId);
        Assert.Equal("WF", wellsFargo.InstitutionCode);
        Assert.Equal("Wells Fargo", wellsFargo.Name);
        Assert.Null(wellsFargo.LogoUrl);
        Assert.Equal(1, wellsFargo.ActiveAccounts);
        Assert.Equal(latestBalance, wellsFargo.TotalBalance);
        var amex = result.Single(i => i.InstitutionId == amexInstitution.InstitutionId);
        Assert.Equal("AMEX", amex.InstitutionCode);
        Assert.Equal("Amex", amex.Name);
        Assert.Equal(amexLogoUrl, amex.LogoUrl);
        Assert.Equal(1, amex.ActiveAccounts);
        Assert.Equal(amexBalance, amex.TotalBalance);
    }

    [Fact]
    public async Task GetMyInstitutions_UserHasNoAccounts_ReturnsEmptyList()
    {
        // Act
        var result = await HttpClient.GetResultAsync<List<MyInstitutionResponseDto>>("/api/my/institutions", CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMyInstitutions_OnlyActiveAccountsCount_ReturnsCorrectInstitutions()
    {
        // Arrange
        const decimal activeBalance = 1000m;
        const decimal inactiveBalance = 3000m;
        var activeAccount = AccountFactory.Create(AuthenticatedUserId, institutionId: TestConstants.WellsFargoInstitutionId, openingBalance: activeBalance);
        var inactiveAccount = AccountFactory.Create(AuthenticatedUserId, institutionId: TestConstants.ChaseInstitutionId, openingBalance: inactiveBalance);
        inactiveAccount.IsActive = false;
        await DbContext.SeedRangeAsync([activeAccount, inactiveAccount], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<MyInstitutionResponseDto>>("/api/my/institutions", CancellationToken);

        // Assert
        var institution = result.Single();
        Assert.Equal(TestConstants.WellsFargoInstitutionId, institution.InstitutionId);
        Assert.Equal("WF", institution.InstitutionCode);
        Assert.Equal("Wells Fargo", institution.Name);
        Assert.Null(institution.LogoUrl);
        Assert.Equal(1, institution.ActiveAccounts);
        Assert.Equal(activeBalance, institution.TotalBalance);
    }

    [Fact]
    public async Task GetMyInstitutions_MultipleActiveAccounts_ReturnsCountAndTotal()
    {
        // Arrange
        const decimal firstBalance = 1000m;
        const decimal secondBalance = 2000m;
        const decimal chaseBalance = 500m;
        var firstAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Checking One", openingBalance: firstBalance);
        var secondAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Checking Two", openingBalance: secondBalance);
        var chaseAccount = AccountFactory.Create(AuthenticatedUserId, institutionId: TestConstants.ChaseInstitutionId, accountName: "Chase Checking", openingBalance: chaseBalance);
        await DbContext.SeedRangeAsync([firstAccount, secondAccount, chaseAccount], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<MyInstitutionResponseDto>>("/api/my/institutions", CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        var wellsFargo = result.Single(i => i.InstitutionId == TestConstants.WellsFargoInstitutionId);
        var chase = result.Single(i => i.InstitutionId == TestConstants.ChaseInstitutionId);
        Assert.Equal(2, wellsFargo.ActiveAccounts);
        Assert.Equal(firstBalance + secondBalance, wellsFargo.TotalBalance);
        Assert.Equal(1, chase.ActiveAccounts);
        Assert.Equal(chaseBalance, chase.TotalBalance);
    }

    [Fact]
    public async Task GetMyInstitutions_OtherUsersAccounts_NotIncluded()
    {
        // Arrange
        const decimal myBalance = 1000m;
        const decimal otherUserBalance = 9999m;
        const decimal myChaseBalance = 500m;
        var otherUserId = Guid.NewGuid().ToString();
        var myAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "My Checking", openingBalance: myBalance);
        var otherAccount = AccountFactory.Create(otherUserId, accountName: "Other Checking", openingBalance: otherUserBalance);
        var myChaseAccount = AccountFactory.Create(AuthenticatedUserId, institutionId: TestConstants.ChaseInstitutionId, accountName: "My Chase", openingBalance: myChaseBalance);
        await DbContext.SeedRangeAsync([myAccount, otherAccount, myChaseAccount], CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<MyInstitutionResponseDto>>("/api/my/institutions", CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        var wellsFargo = result.Single(i => i.InstitutionId == TestConstants.WellsFargoInstitutionId);
        var chase = result.Single(i => i.InstitutionId == TestConstants.ChaseInstitutionId);
        Assert.Equal(1, wellsFargo.ActiveAccounts);
        Assert.Equal(myBalance, wellsFargo.TotalBalance);
        Assert.Equal(1, chase.ActiveAccounts);
        Assert.Equal(myChaseBalance, chase.TotalBalance);
    }

    [Fact]
    public async Task GetMyInstitutions_InactiveInstitution_Excluded()
    {
        // Arrange
        const decimal balance = 1000m;
        var schwabInstitution = await DbContext.Institutions
            .SingleAsync(i => i.InstitutionId == TestConstants.SchwabInstitutionId, CancellationToken);
        schwabInstitution.IsActive = false;
        var schwabAccount = AccountFactory.Create(AuthenticatedUserId, institutionId: TestConstants.SchwabInstitutionId, openingBalance: balance);
        // Persisting the account also commits the institution's IsActive change.
        await DbContext.SeedAsync(schwabAccount, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<List<MyInstitutionResponseDto>>("/api/my/institutions", CancellationToken);

        // Assert
        Assert.DoesNotContain(result, i => i.InstitutionId == TestConstants.SchwabInstitutionId);
    }
}

using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Api.IntegrationTests.Infrastructure.Factories;

public static class InstitutionFactory
{
    public static Institution Create(
        string name = "Wells Fargo",
        string institutionCode = "WF",
        string? logoUrl = null)
    {
        return new Institution(institutionCode, name, logoUrl);
    }
}

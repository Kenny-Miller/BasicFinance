using BasicFinance.Infrastructure.Entities;
using BasicFinance.Infrastructure.Enums;

namespace BasicFinance.DataProcessor.IntegrationTests.Helpers
{
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
}

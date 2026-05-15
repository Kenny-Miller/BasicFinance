namespace BasicFinance.Infrastructure.VendorModels.Exports
{
    public record WellsFargoAccountExport(
        string AccountId,
        string AccountNumberDisplay,
        string AccountType,
        float AvailableBalance,
        string BalanceType,
        Currency Currency,
        float CurrentBalance,
        string Description,
        FiAttribute[] FiAttributes,
        string LineOfBusiness,
        string Nickname,
        string ProductName,
        string Status) : IAccountExport;
    public record Currency(string CurrencyCode);
    public record FiAttribute(string Name, string Value);
}

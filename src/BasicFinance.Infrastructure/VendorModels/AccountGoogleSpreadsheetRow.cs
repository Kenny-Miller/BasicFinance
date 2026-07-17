namespace BasicFinance.Infrastructure.VendorModels
{
    public record AccountGoogleSpreadsheetRow(string AccountName, decimal Balance, string Currency, string Notes, DateTime LastUpdateDated, string Institution, Guid FinancialAccountId, string RawDataJson);
}
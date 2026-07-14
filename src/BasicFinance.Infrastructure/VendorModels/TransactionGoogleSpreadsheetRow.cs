namespace BasicFinance.Infrastructure.VendorModels
{
    public record TransactionGoogleSpreadsheetRow(DateTimeOffset Date, decimal Amount, string Description, string Category, string Account, string RawDataJson);
}
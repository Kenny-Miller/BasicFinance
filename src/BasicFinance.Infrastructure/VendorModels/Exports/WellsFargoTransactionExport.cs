namespace BasicFinance.Infrastructure.VendorModels.Exports
{
    public record WellsFargoTransactionExport(
        Guid AccountId,
        string TransactionId,
        string PostedTimestamp,
        string TransactionTimestamp,
        string Description,
        string DebitCreditMemo,
        string Category,
        string SubCategory,
        string Reference,
        string Status,
        decimal Amount,
        string ForeignCurrency,
        string TransactionType,
        string Payee
    ) : ITransactionExport;
}
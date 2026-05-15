namespace BasicFinance.Infrastructure.VendorModels.Exports
{
    public interface ITransactionExport
    {
        public Guid AccountId { get; }
        public string TransactionType { get; }
        public string Category { get; }
        public string SubCategory { get; }
    }
}

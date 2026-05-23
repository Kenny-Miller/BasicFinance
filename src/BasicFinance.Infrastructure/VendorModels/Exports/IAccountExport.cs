namespace BasicFinance.Infrastructure.VendorModels.Exports
{
    public interface IAccountExport
    {
        public string AccountType { get; }
        public string BalanceType { get; }
    }
}

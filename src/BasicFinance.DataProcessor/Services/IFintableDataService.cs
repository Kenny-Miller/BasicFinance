using BasicFinance.Infrastructure.Entities;
using Transaction = BasicFinance.Infrastructure.Entities.Transaction;

namespace BasicFinance.DataProcessor.Services
{
    public interface IFintableDataService
    {
        Task<List<Account>> GetAccountsAsync();
        Task<List<Transaction>> GetTransactionsAsync();
    }
}

using BasicFinance.Domain.Entities;
using Transaction = BasicFinance.Domain.Entities.Transaction;

namespace BasicFinance.DataProcessor.Services
{
    public interface IFintableDataService
    {
        Task<List<Account>> GetAccountsAsync();
        Task<List<Transaction>> GetTransactionsAsync();
    }
}

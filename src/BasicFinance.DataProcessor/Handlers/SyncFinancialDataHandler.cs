using BasicFinance.Domain.Commands;

namespace BasicFinance.DataProcessor.Handlers
{
    public class SyncFinancialDataHandler()
    {
        public async Task Handle(SyncFinancialData message)
        {
            await Task.CompletedTask;
        }
    }
}

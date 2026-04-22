namespace BasicFinance.Domain.Commands
{
    public record SyncFinancialData(string UserId, string GoogleSheetId);
}

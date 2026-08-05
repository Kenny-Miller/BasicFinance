using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.DataProcessor.IntegrationTests.Helpers
{
    public static class UserGoogleSpreadsheetFactory
    {
        public static UserGoogleSpreadsheet Create(
            string userId = "test-user-id",
            string googleSheetId = "test-sheet-id",
            string googleSheetName = "Test Sheet")
        {
            return new UserGoogleSpreadsheet(userId, googleSheetId, googleSheetName);
        }
    }
}

using Newtonsoft.Json;

namespace BasicFinance.DataProcessor.IntegrationTests.Factory;

public static class GoogleSpreadsheetExportFactory
{
    public static string CreateAccountExportJson(
        string accountType = "Checking",
        string balanceType = "CurrentBalance")
    {
        var export = new
        {
            AccountId = Guid.NewGuid().ToString(),
            AccountNumberDisplay = "****1234",
            AccountType = accountType,
            AvailableBalance = 1000.0f,
            BalanceType = balanceType,
            Currency = new { CurrencyCode = "USD" },
            CurrentBalance = 1000.0f,
            Description = "Test Account",
            FinancialAccountMetadata = Array.Empty<object>(),
            LineOfBusiness = "Consumer",
            Nickname = "Test",
            ProductName = "Checking",
            Status = "Active"
        };
        return JsonConvert.SerializeObject(export);
    }

    public static string CreateTransactionExportJson(
        long transactionId,
        string transactionType = "Debit",
        string category = "Uncategorized",
        string subCategory = "")
    {
        var export = new
        {
            AccountId = Guid.NewGuid().ToString(),
            TransactionId = transactionId,
            PostedTimestamp = DateTime.UtcNow.ToString("o"),
            TransactionTimestamp = DateTime.UtcNow.ToString("o"),
            Description = "Test Transaction",
            DebitCreditMemo = transactionType,
            Category = category,
            SubCategory = subCategory,
            Reference = "REF123",
            Status = "Posted",
            Amount = 100.00m,
            ForeignCurrency = "",
            TransactionType = transactionType,
            Payee = "Test Payee"
        };
        return JsonConvert.SerializeObject(export);
    }
}
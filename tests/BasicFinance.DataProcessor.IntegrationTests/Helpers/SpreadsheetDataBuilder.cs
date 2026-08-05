using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;

namespace BasicFinance.DataProcessor.IntegrationTests.Helpers
{
    public class SpreadsheetDataBuilder
    {
        private readonly List<object[]> _accountRows =
        [
            ["Account Name", "Balance", "Currency", "Notes", "LastUpdatedDate", "Institution", "FinancialAccountId", "RawData", "AvailableBalance"]
        ];

        private readonly List<object[]> _transactionRows =
        [
            ["Date", "Amount", "Description", "Category", "Account", "Attachment", "RawData", "Extra"]
        ];

        public SpreadsheetDataBuilder AddAccountRow(
            string accountName,
            decimal balance,
            string currency,
            string notes,
            DateTime lastUpdatedDate,
            string institution,
            Guid financialAccountId,
            string rawDataJson)
        {
            _accountRows.Add([
                accountName,
                balance.ToString(System.Globalization.CultureInfo.InvariantCulture),
                currency,
                notes,
                lastUpdatedDate,
                institution,
                financialAccountId.ToString(),
                rawDataJson,
                ""
            ]);
            return this;
        }

        public SpreadsheetDataBuilder AddTransactionRow(
            DateTime date,
            decimal amount,
            string description,
            string category,
            string account,
            string rawDataJson)
        {
            _transactionRows.Add([
                date.ToString(System.Globalization.CultureInfo.InvariantCulture),
                amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                description,
                category,
                account,
                "",
                "",
                rawDataJson
            ]);
            return this;
        }

        public BatchGetValuesResponse Build()
        {
            return new BatchGetValuesResponse
            {
                ValueRanges =
                [
                    new ValueRange
                    {
                        Range = "Accounts!A1:I100",
                        Values = _accountRows.Cast<IList<object>>().ToList()
                    },
                    new ValueRange
                    {
                        Range = "Transactions!A1:G100",
                        Values = _transactionRows.Cast<IList<object>>().ToList()
                    }
                ]
            };
        }
    }

    public static class WellsFargoExportHelpers
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
}

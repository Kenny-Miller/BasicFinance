using Google.Apis.Sheets.v4.Data;

namespace BasicFinance.DataProcessor.IntegrationTests.Factory;

public class SpreadsheetDataFactory
{
    readonly List<object[]> _accountRows =
    [
        ["Account Name", "Balance", "Currency", "Notes", "LastUpdatedDate", "Institution", "FinancialAccountId", "RawData", "AvailableBalance"]
    ];

    readonly List<object[]> _transactionRows =
    [
        ["Date", "Amount", "Description", "Category", "Account", "Attachment", "RawData", "Extra"]
    ];

    public SpreadsheetDataFactory AddAccountRow(
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

    public SpreadsheetDataFactory AddTransactionRow(
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
                    Values = [.. _accountRows.Cast<IList<object>>()]
                },
                new ValueRange
                {
                    Range = "Transactions!A1:G100",
                    Values = [.. _transactionRows.Cast<IList<object>>()]
                }
            ]
        };
    }
}
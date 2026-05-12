using BasicFinance.Infrastructure.VendorModels;

namespace BasicFinance.Infrastructure.Extensions
{
    public static class GoogleSpreadsheetExtensions
    {
        extension(IEnumerable<IList<object>> source)
        {
            /// <summary>
            /// Maps the untyped rows retrieved from the Google Spreadsheet's Account subsheet to a list of <see cref="AccountGoogleSpreadsheetRow"/> objects.
            /// </summary>
            /// <remarks>
            /// Expects the following column order: Account Name, Balance, Currency, Notes, LastUpdatedDate, Institution, and FinancialAccountId.
            public List<AccountGoogleSpreadsheetRow> MapToAccountRows() =>
                [.. source
                    .Where(x => x.Count >= 7)
                    .Select(x => new AccountGoogleSpreadsheetRow(
                        (string)x[0],
                        decimal.Parse((string)x[1]),
                        (string)x[2],
                        (string)x[3],
                        DateTime.SpecifyKind(DateTime.Parse((string)x[4]), DateTimeKind.Utc),
                        (string)x[5],
                        Guid.Parse((string)x[6])))
                ];

            /// <summary>
            /// Maps the untyped rows retrieved from the Google Spreadsheet's Transaction subsheet to a list of <see cref="TransactionGoogleSpreadsheetRow"/> objects.
            /// </summary>
            /// <remarks>
            /// Expects the following column order in the Transactions subsheet: Date, Amount, Description, and Category.
            /// </remarks>
            public List<TransactionGoogleSpreadsheetRow> MapToTransactionRows() =>
                [.. source
                    .Where(x => x.Count >= 5)
                    .Select(x => new TransactionGoogleSpreadsheetRow(
                        DateTime.Parse((string)x[0]),
                        decimal.Parse((string)x[1]),
                        (string)x[2],
                        (string)x[3],
                        (string)x[4]))
                 ];
        }
    }
}

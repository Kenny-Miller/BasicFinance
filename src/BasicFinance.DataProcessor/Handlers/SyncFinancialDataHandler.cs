using System.Collections.Frozen;
using System.Globalization;
using BasicFinance.Domain.Commands;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using BasicFinance.Infrastructure.Entities;
using BasicFinance.Infrastructure.Helpers;
using BasicFinance.Infrastructure.VendorModels;
using BasicFinance.Infrastructure.VendorModels.Exports;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using AccountType = BasicFinance.Infrastructure.Enums.AccountType;
using TransactionCategory = BasicFinance.Infrastructure.Enums.TransactionCategory;
using TransactionType = BasicFinance.Infrastructure.Enums.TransactionType;

namespace BasicFinance.DataProcessor.Handlers
{
    public static partial class SyncFinancialDataHandler
    {
        private static readonly IReadOnlyList<string> _subSpreadSheetNames = ["Accounts", "Transactions"];

        private static readonly FrozenDictionary<string, Type> _accountExportToTypeDict = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase){
            { "Wells Fargo", typeof(WellsFargoAccountExport) },
        }.ToFrozenDictionary();

        private static readonly FrozenDictionary<string, Type> _accountExportToTransactionExportDict = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase){
            {  "Wells Fargo", typeof(WellsFargoTransactionExport) },
        }.ToFrozenDictionary();

        /// <summary>
        /// Handles the synchronization of financial data between a Google Spreadsheet and the database for a specific <see cref="UserGoogleSpreadsheet"/>. 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="googleClient"></param>
        /// <param name="dbContext"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static async Task HandleAsync(SyncFinancialData message, GoogleServiceAccountClient googleClient, AppDbContext dbContext, ILogger logger)
        {
            // Retrieve the Google Spreadsheet associated with the UserGoogleSpreadsheetId specified in the message.
            var userSpreadsheet = await dbContext.UserGoogleSpreadsheets.SingleAsync(x => x.UserGoogleSpreadsheetId == message.UserGoogleSpreadsheetId);
            var subsheets = await googleClient.GetSubSpreadsheetsAsync(userSpreadsheet.GoogleSheetId, _subSpreadSheetNames);
            if (subsheets == null)
            {
                LogGoogleSheetNotFound(logger, userSpreadsheet.UserId);
                return;
            }

            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync();

                var accountRows = MapToAccountRows(subsheets.ValueRanges[0].Values.Skip(1));
                await SyncAccounts(dbContext, logger, userSpreadsheet.UserId, userSpreadsheet.UserGoogleSpreadsheetId, accountRows);

                var transactionRows = MapToTransactionRows(subsheets.ValueRanges[1].Values.Skip(1));
                await SyncTransactionsAsync(dbContext, logger, userSpreadsheet.UserId, userSpreadsheet.UserGoogleSpreadsheetId, transactionRows);

                await transaction.CommitAsync();
            });
        }

        /// <summary>
        /// Syncs the user's existing accounts with the account retrieved from the Google Spreadsheet.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="logger"></param>
        /// <param name="userId"></param>
        /// <param name="userGoogleSpreadsheetId"></param>
        /// <param name="accountRows"></param>
        /// <returns></returns>
        private static async Task SyncAccounts(AppDbContext dbContext, ILogger logger, string userId, Guid userGoogleSpreadsheetId, List<AccountGoogleSpreadsheetRow> accountRows)
        {
            var existingAccountsDict = await dbContext.Accounts
                   .Where(x => x.UserGoogleSpreadsheetId == userGoogleSpreadsheetId)
                   .Where(x => x.UserId == userId)
                   .Where(x => x.IsActive)
                   .ToDictionaryAsync(x => new { x.FinancialAccountId, x.Institution });

            var accountRowDict = accountRows.ToDictionary(x => new { x.FinancialAccountId, x.Institution });

            // Update existing accounts and remove accounts that no longer exist in the spreadsheet
            foreach (var (key, existingAccount) in existingAccountsDict)
            {
                if (accountRowDict.TryGetValue(key, out var accountRow))
                {
                    if (accountRow.Balance != existingAccount.Balance)
                    {
                        dbContext.AccountBalanceHistories.Add(new(existingAccount));
                        existingAccount.UpdateBalance(accountRow.Balance, accountRow.LastUpdateDated);
                    }
                }
                else
                {
                    dbContext.Remove(existingAccount);
                }

                accountRowDict.Remove(key);
            }

            // Add new accounts that exist in the spreadsheet but not in the database
            var newAccounts = new List<Account>();
            foreach (var accountRow in accountRowDict.Values)
            {
                var accountExportType = _accountExportToTypeDict.TryGetValue(accountRow.Institution, out var exportType) ? exportType : null;
                if (accountExportType == null)
                {
                    LogExportTypeNotFound(logger, userId, accountRow.Institution, accountRow.AccountName);
                    continue;
                }

                var deserializedAccountJson = JsonConvert.DeserializeObject(accountRow.RawDataJson, accountExportType);
                if (deserializedAccountJson is not IAccountExport export)
                {
                    LogAccountExportTypeDeserialilzationFailed(logger, userId, accountRow.Institution, accountRow.AccountName);
                    continue;
                }

                var accountType = export.AccountType.ToLowerInvariant() switch
                {
                    "checking" => AccountType.Checking,
                    "savings" => AccountType.Savings,
                    "credit" => AccountType.CreditCard,
                    "investment" => AccountType.Investment,
                    _ => (AccountType?)null
                };

                if (accountType == null)
                {
                    continue;
                }

                var accountToCreate = new Account(
                   userGoogleSpreadsheetId,
                   accountType.Value,
                   userId,
                   accountRow.AccountName,
                   accountRow.Balance,
                   accountRow.Currency,
                   accountRow.Notes,
                   accountRow.Institution,
                   accountRow.FinancialAccountId,
                   accountRow.LastUpdateDated);
                newAccounts.Add(accountToCreate);
            }

            dbContext.Accounts.AddRange(newAccounts);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Creates transactions in the database for the specified user and <see cref="UserGoogleSpreadsheet"/> based on the provided list of <see cref="TransactionGoogleSpreadsheetRow"/> retrieved from the Google Spreadsheet.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="logger"></param>
        /// <param name="userId"></param>
        /// <param name="userGoogleSpreadsheetId"></param>
        /// <param name="transactionRows"></param>
        /// <returns></returns>
        private static async Task SyncTransactionsAsync(AppDbContext dbContext, ILogger logger, string userId, Guid userGoogleSpreadsheetId, List<TransactionGoogleSpreadsheetRow> transactionRows)
        {
            var existingAccountsDict = await dbContext.Accounts
                .Where(x => x.UserGoogleSpreadsheetId == userGoogleSpreadsheetId)
                .Where(x => x.UserId == userId)
                .Where(x => x.IsActive)
                .ToDictionaryAsync(x => x.AccountName);

            var existingTransactionsDict = await dbContext.Transactions
                .Where(x => x.UserId == userId)
                .Where(x => x.IsActive)
                .ToDictionaryAsync(x => new { x.Account.FinancialAccountId, x.FinancialTransactionId });

            // Add new transactions that exist in the spreadsheet but not in the database
            var newTransactions = new List<Transaction>();
            foreach (var transactionRow in transactionRows)
            {
                var account = existingAccountsDict.TryGetValue(transactionRow.Account, out var accountMatch) ? accountMatch : null;
                if (account == null)
                {
                    LogAccountForTransactionNotFound(logger, userId, transactionRow.Account, transactionRow.Description, transactionRow.Date, transactionRow.Amount);
                    continue;
                }

                var transactionExportType = _accountExportToTransactionExportDict.TryGetValue(account.Institution, out var exportType) ? exportType : null;
                if (transactionExportType == null)
                {
                    LogTransactionExportTypeNotFound(logger, userId, account.Institution, transactionRow.Account, transactionRow.Description, transactionRow.Date, transactionRow.Amount);
                    continue;
                }

                var deserializedAccountJson = JsonConvert.DeserializeObject(transactionRow.RawDataJson, transactionExportType);
                if (deserializedAccountJson is not ITransactionExport export)
                {
                    LogTransactionExportTypeDeserializationFailed(logger, userId, account.Institution, transactionRow.Account, transactionRow.Description, transactionRow.Date, transactionRow.Amount);
                    continue;
                }

                var transactionType = export.TransactionType.ToLower(System.Globalization.CultureInfo.CurrentCulture) switch
                {
                    "debit" => TransactionType.Debit,
                    "posdebit" => TransactionType.Debit,
                    "deposit" => TransactionType.Credit,
                    "directdeposit" => TransactionType.Credit,
                    "credit" => TransactionType.Credit,
                    "interest" => TransactionType.Credit,
                    _ => throw new InvalidOperationException("Unknown transaction type")
                };

                var transactionCategory = (export.Category?.ToLower(System.Globalization.CultureInfo.CurrentCulture), export.SubCategory?.ToLower(System.Globalization.CultureInfo.CurrentCulture)) switch
                {
                    (_, "card payments (non-wf)") => TransactionCategory.CreditCardPayment,
                    (_, "direct deposits") => TransactionCategory.Income,
                    (_, "other deposits") => TransactionCategory.Income,
                    (_, "interest") => TransactionCategory.Income,
                    (_, "Other Outgoing Transfers") => TransactionCategory.InternalTransfer,
                    (_, "other insurance/financial") => TransactionCategory.Investment,
                    (_, "rent") => TransactionCategory.BillsAndUtilities,
                    ("do not display", _) => TransactionCategory.Ignore,
                    ("deposits", _) => TransactionCategory.Income,
                    ("outgoing transfers", _) => TransactionCategory.InternalTransfer,
                    ("credit card/loan payments", _) => TransactionCategory.CreditCardPayment,
                    ("bills/utilities", _) => TransactionCategory.BillsAndUtilities,
                    ("insurance/financial", _) => TransactionCategory.Investment,
                    ("taxes", _) => TransactionCategory.Taxes,
                    ("income", _) => TransactionCategory.Income,
                    ("investment", _) => TransactionCategory.Investment,
                    ("credit card", _) => TransactionCategory.CreditCardPayment,
                    ("transfer", _) => TransactionCategory.InternalTransfer,
                    _ => TransactionCategory.Uncategorized
                };

                var normalizedDescription = StringHelper.NormalizeWhiteSpace(transactionRow.Description);
                var transactionToCreate = new Transaction(
                    userId,
                    existingAccountsDict[transactionRow.Account].AccountId,
                    export.TransactionId,
                    transactionType,
                    transactionCategory,
                    transactionRow.Date,
                    transactionRow.Amount,
                    normalizedDescription)
                {
                    // Load account so that we can use below when removing transactions that are not found
                    Account = existingAccountsDict[transactionRow.Account]
                };

                newTransactions.Add(transactionToCreate);
            }

            // Remove transactions that no longer exist in the spreadsheet
            var transactionRowGroupDict = newTransactions.ToDictionary(x => new { x.Account.FinancialAccountId, x.FinancialTransactionId });
            foreach (var (key, transaction) in existingTransactionsDict)
            {
                if (!transactionRowGroupDict.ContainsKey(key))
                {
                    dbContext.Remove(transaction);
                }
            }

            dbContext.Transactions.AddRange(newTransactions);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Maps the untyped rows retrieved from the Google Spreadsheet's Account subsheet to a list of <see cref="AccountGoogleSpreadsheetRow"/> objects.
        /// </summary>
        /// <remarks>
        /// Expects the following column order: Account Name, Balance, Currency, Notes, LastUpdatedDate, Institution, FinancialAccountId, Raw Data, and Available Balance.
        /// Avaiable Balance is generally blank so skip mapping.
        /// </remarks>
        /// <param name="source"></param>
        /// <returns></returns>
        private static List<AccountGoogleSpreadsheetRow> MapToAccountRows(IEnumerable<IList<object>> source) =>
            [.. source
                    .Where(x => x.Count >= 8)
                    .Select(x => new AccountGoogleSpreadsheetRow(
                        (string)x[0],
                        decimal.Parse((string)x[1], CultureInfo.InvariantCulture),
                        (string)x[2],
                        (string)x[3],
                        DateTime.SpecifyKind((DateTime)x[4], DateTimeKind.Utc),
                        (string)x[5],
                        Guid.Parse((string)x[6]),
                        (string)x[7]
                    ))
            ];

        /// <summary>
        /// Maps the untyped rows retrieved from the Google Spreadsheet's Transaction subsheet to a list of <see cref="TransactionGoogleSpreadsheetRow"/> objects.
        /// </summary>
        /// <remarks>
        /// Expects the following column order in the Transactions subsheet: Date, Amount, Description, Category, Account, Attachment, Raw Data.
        /// Attachment is generally blank so skip mapping.
        /// </remarks>
        /// <param name="source"></param>
        /// <returns></returns>
        private static List<TransactionGoogleSpreadsheetRow> MapToTransactionRows(IEnumerable<IList<object>> source) =>
            [.. source
                    .Where(x => x.Count >= 7)
                    .Select(x => new TransactionGoogleSpreadsheetRow(
                        DateTime.SpecifyKind(DateTime.Parse((string)x[0], CultureInfo.InvariantCulture), DateTimeKind.Utc),
                        decimal.Parse((string)x[1],CultureInfo.InvariantCulture),
                        (string)x[2],
                        (string)x[3],
                        (string)x[4],
                        (string)x[7]))
             ];

        [LoggerMessage(
            EventName = nameof(LogGoogleSheetNotFound),
            Level = LogLevel.Error,
            Message = "Google sheet for user {UserId} was not found during data process")]
        private static partial void LogGoogleSheetNotFound(ILogger logger, string userId);

        [LoggerMessage(
            EventName = nameof(LogExportTypeNotFound),
            Level = LogLevel.Error,
            Message = "No export type found for institution {Institution} for user {UserId}. AccountRow {Institution}-{AccountName} will be skipped.")]
        private static partial void LogExportTypeNotFound(ILogger logger, string userId, string institution, string accountName);

        [LoggerMessage(
            EventName = nameof(LogAccountExportTypeDeserialilzationFailed),
            Level = LogLevel.Error,
            Message = "Failed to deserialize account export for institution {Institution} for user {UserId}. AccountRow {Institution}-{AccountName} will be skipped.")]
        private static partial void LogAccountExportTypeDeserialilzationFailed(ILogger logger, string userId, string institution, string accountName);

        [LoggerMessage(
           EventName = nameof(LogAccountForTransactionNotFound),
           Level = LogLevel.Error,
           Message = "No account found for transaction {Account}-{Description}-{Date}-{Amount} for user {UserId}. Transaction will be skipped.")]
        private static partial void LogAccountForTransactionNotFound(ILogger logger, string userId, string account, string description, DateTimeOffset date, decimal amount);

        [LoggerMessage(
            EventName = nameof(LogTransactionExportTypeNotFound),
            Level = LogLevel.Error,
            Message = "No export type found for institution {Institution} for transaction {Account}-{Description}-{Date}-{Amount} for user {UserId}. Transaction will be skipped.")]
        private static partial void LogTransactionExportTypeNotFound(ILogger logger, string userId, string institution, string account, string description, DateTimeOffset date, decimal amount);

        [LoggerMessage(
            EventName = nameof(LogTransactionExportTypeDeserializationFailed),
            Level = LogLevel.Error,
            Message = "Failed to deserialize transaction export for institution {Institution} for transaction {Account}-{Description}-{Date}-{Amount} for user {UserId}. Transaction will be skipped.")]
        private static partial void LogTransactionExportTypeDeserializationFailed(ILogger logger, string userId, string institution, string account, string description, DateTimeOffset date, decimal amount);
    }
}
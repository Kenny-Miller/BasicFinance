using System.Collections.Frozen;
using BasicFinance.Domain.Commands;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using BasicFinance.Infrastructure.Entities;
using BasicFinance.Infrastructure.Extensions;
using BasicFinance.Infrastructure.VendorModels;
using BasicFinance.Infrastructure.VendorModels.Exports;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using AccountType = BasicFinance.Infrastructure.Enums.AccountType;
using TransactionCategory = BasicFinance.Infrastructure.Enums.TransactionCategory;
using TransactionType = BasicFinance.Infrastructure.Enums.TransactionType;

namespace BasicFinance.DataProcessor.Handlers
{
    public static class SyncFinancialDataHandler
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
                logger.LogWarning("Google sheet for user {UserId} was not found during data process", userSpreadsheet.UserId);
                return;
            }

            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync();

                // Wipe transactions whose account is associated with the specified Google Spreadsheet as the transaction sheet will act as the source of truth
                await dbContext.Transactions
                   .Where(x => x.UserId == userSpreadsheet.UserId)
                   .Where(x => x.Account.UserGoogleSpreadsheet.UserGoogleSpreadsheetId == userSpreadsheet.UserGoogleSpreadsheetId)
                   .ExecuteDeleteAsync();

                var accountDictionary = subsheets.ValueRanges[0].Values
                    .Skip(1)
                    .MapToAccountRows()
                    .ToDictionary(x => x.AccountName);

                await SyncAccounts(dbContext, logger, userSpreadsheet.UserId, userSpreadsheet.UserGoogleSpreadsheetId, accountDictionary);

                var transactionRows = subsheets.ValueRanges[1].Values
                   .Skip(1)
                   .MapToTransactionRows();

                await CreateTransactionsAsync(dbContext, logger, userSpreadsheet.UserId, userSpreadsheet.UserGoogleSpreadsheetId, transactionRows);
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
        /// <param name="accountRowDictionary"></param>
        /// <returns></returns>
        private static async Task SyncAccounts(AppDbContext dbContext, ILogger logger, string userId, Guid userGoogleSpreadsheetId, Dictionary<string, AccountGoogleSpreadsheetRow> accountRowDictionary)
        {
            var existingAccounts = await dbContext.Accounts
                   .Where(x => x.UserGoogleSpreadsheetId == userGoogleSpreadsheetId)
                   .Where(x => x.IsActive)
                   .ToListAsync();

            foreach (var account in existingAccounts)
            {
                if (accountRowDictionary.TryGetValue(account.AccountName, out var accountRow))
                {
                    if (accountRow.Balance != account.Balance)
                    {
                        dbContext.AccountBalanceHistories.Add(new(account));
                        account.UpdateBalance(accountRow.Balance, accountRow.LastUpdateDated);
                    }
                }
                else
                {
                    dbContext.Remove(account);
                }

                accountRowDictionary.Remove(account.AccountName);
            }

            foreach (var account in accountRowDictionary.Values)
            {
                var accountExportType = _accountExportToTypeDict.TryGetValue(account.Institution, out var exportType) ? exportType : null;
                if (accountExportType == null)
                {
                    logger.LogError("No export type found for institution {Institution} for user {UserId}. Account {AccountName} will be skipped.", account.Institution, userId, account.AccountName);
                    continue;
                }

                var deserializedAccountJson = JsonConvert.DeserializeObject(account.RawDataJson, accountExportType);
                if (deserializedAccountJson is not IAccountExport export)
                {
                    logger.LogError("Failed to deserialize account export for institution {Institution} for user {UserId}. Account {AccountName} will be skipped.", account.Institution, userId, account.AccountName);
                    continue;
                }

                var accountType = export.AccountType.ToLower() switch
                {
                    "checking" => AccountType.Checking,
                    "savings" => AccountType.Savings,
                    "credit" => AccountType.CreditCard,
                    "investment" => AccountType.Investment,
                    _ => throw new InvalidOperationException("Unknown account type")
                };

                var accountToCreate = new Account(
                   userGoogleSpreadsheetId,
                   accountType,
                   userId,
                   account.AccountName,
                   account.Balance,
                   account.Currency,
                   account.Notes,
                   account.Institution,
                   account.FinancialAccountId,
                   account.LastUpdateDated);
                dbContext.Accounts.Add(accountToCreate);
            }

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
        private static async Task CreateTransactionsAsync(AppDbContext dbContext, ILogger logger, string userId, Guid userGoogleSpreadsheetId, List<TransactionGoogleSpreadsheetRow> transactionRows)
        {
            var accountDict = await dbContext.Accounts
                .Where(x => x.UserGoogleSpreadsheetId == userGoogleSpreadsheetId)
                .Where(x => x.UserId == userId)
                .Where(x => x.IsActive)
                .ToDictionaryAsync(x => x.AccountName, x => x);

            foreach (var transaction in transactionRows)
            {
                var account = accountDict.TryGetValue(transaction.Account, out var accountMatch) ? accountMatch : null;
                if (account == null)
                {
                    logger.LogError("No account found for transaction {Transaction} for user {UserId}. Transaction will be skipped.", transaction.Account + transaction.Description, userId);
                    continue;
                }

                var transactionExportType = _accountExportToTransactionExportDict.TryGetValue(account.Institution, out var exportType) ? exportType : null;
                if (transactionExportType == null)
                {
                    logger.LogError("No export type found for institution {Institution} for user {UserId}. Transaction {Transaction} will be skipped.", account.Institution, userId, transaction.Account + transaction.Description);
                    continue;
                }

                var deserializedAccountJson = JsonConvert.DeserializeObject(transaction.RawDataJson, transactionExportType);
                if (deserializedAccountJson is not ITransactionExport export)
                {
                    logger.LogError("Failed to deserialize transaction export for institution {Institution} for user {UserId}. Account {AccountName} will be skipped.", account.Institution, userId, account.AccountName);
                    continue;
                }

                var transactionType = export.TransactionType.ToLower() switch
                {
                    "debit" => TransactionType.Debit,
                    "posdebit" => TransactionType.Debit,
                    "deposit" => TransactionType.Credit,
                    "directedeposit" => TransactionType.Credit,
                    "credit" => TransactionType.Credit,
                    _ => throw new InvalidOperationException("Unknown transaction type")
                };

                var transactionCategory = (export.Category.ToLower(), export.SubCategory.ToLower()) switch
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

                var transactionToCreate = new Transaction(
                    userId,
                    accountDict[transaction.Account].AccountId,
                    transactionType,
                    transactionCategory,
                    transaction.Date,
                    transaction.Amount,
                    transaction.Description,
                    transaction.Category);
                dbContext.Transactions.Add(transactionToCreate);
            }
            await dbContext.SaveChangesAsync();
        }
    }
}

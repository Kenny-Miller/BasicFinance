using BasicFinance.Domain.Commands;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using BasicFinance.Infrastructure.Entities;
using BasicFinance.Infrastructure.Extensions;
using BasicFinance.Infrastructure.VendorModels;
using Microsoft.EntityFrameworkCore;

namespace BasicFinance.DataProcessor.Handlers
{
    public static class SyncFinancialDataHandler
    {
        private static readonly IReadOnlyList<string> _subSpreadSheetNames = ["Accounts", "Transactions"];

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

                await SyncAccounts(dbContext, userSpreadsheet.UserId, userSpreadsheet.UserGoogleSpreadsheetId, accountDictionary);

                var transactionRows = subsheets.ValueRanges[1].Values
                   .Skip(1)
                   .MapToTransactionRows();

                await CreateTransactionsAsync(dbContext, userSpreadsheet.UserId, userSpreadsheet.UserGoogleSpreadsheetId, transactionRows);
            });
        }

        /// <summary>
        /// Syncs the user's existing accounts with the account retrieved from the Google Spreadsheet.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="userId"></param>
        /// <param name="userGoogleSpreadsheetId"></param>
        /// <param name="accountRowDictionary"></param>
        /// <returns></returns>
        private static async Task SyncAccounts(AppDbContext dbContext, string userId, Guid userGoogleSpreadsheetId, Dictionary<string, AccountGoogleSpreadsheetRow> accountRowDictionary)
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

            var accountsToCreate = accountRowDictionary.Values.Select(x => new Account(
                userGoogleSpreadsheetId,
                userId,
                x.AccountName,
                x.Balance,
                x.Currency,
                x.Notes,
                x.Institution,
                x.FinancialAccountId,
                x.LastUpdateDated));

            dbContext.Accounts.AddRange(accountsToCreate);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Creates transactions in the database for the specified user and <see cref="UserGoogleSpreadsheet"/> based on the provided list of <see cref="TransactionGoogleSpreadsheetRow"/> retrieved from the Google Spreadsheet.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="userId"></param>
        /// <param name="userGoogleSpreadsheetId"></param>
        /// <param name="transactionRows"></param>
        /// <returns></returns>
        private static async Task CreateTransactionsAsync(AppDbContext dbContext, string userId, Guid userGoogleSpreadsheetId, List<TransactionGoogleSpreadsheetRow> transactionRows)
        {
            var accountDict = await dbContext.Accounts
                .Where(x => x.UserGoogleSpreadsheetId == userGoogleSpreadsheetId)
                .Where(x => x.UserId == userId)
                .Where(x => x.IsActive)
                .Select(x => new { x.AccountId, x.AccountName })
                .ToDictionaryAsync(x => x.AccountName, x => x.AccountId);

            var transactions = transactionRows.Select(x => new Transaction(
                userId,
                accountDict[x.Account],
                x.Date,
                x.Amount,
                x.Description,
                x.Category));

            dbContext.Transactions.AddRange(transactions);
            await dbContext.SaveChangesAsync();
        }
    }
}

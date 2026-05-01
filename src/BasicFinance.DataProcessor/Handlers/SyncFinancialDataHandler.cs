using BasicFinance.Domain.Commands;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using BasicFinance.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace BasicFinance.DataProcessor.Handlers
{
    public class SyncFinancialDataHandler()
    {
        private static readonly List<string> _subSpreadSheetNames = ["Accounts", "Transactions"];

        public async Task Handle(SyncFinancialData message, GoogleServiceAccountClient googleClient, AppDbContext dbContext, ILogger<SyncFinancialDataHandler> logger)
        {
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

                // Wipe transactions as transaction sheet will act as source of truth
                await dbContext.Transactions.Where(x => x.UserId == userSpreadsheet.UserId).ExecuteDeleteAsync();

                // Determine if we need to create, update and add history, or delete as account sheet contains the current balance of the account and not previous values.                
                var existingAccounts = await dbContext.Accounts
                    .Where(x => x.UserGoogleSpreadsheetId == userSpreadsheet.UserGoogleSpreadsheetId)
                    .Where(x => x.IsActive)
                    .ToListAsync();

                var spreadsheetAccountDict = MapToAccounts(userSpreadsheet, subsheets.ValueRanges[0].Values)
                    .ToDictionary(x => x.AccountName);

                var accountsToDelete = new List<Guid>();
                foreach (var account in existingAccounts)
                {
                    if (spreadsheetAccountDict.TryGetValue(account.AccountName, out var mappedAccount))
                    {
                        if (account.Balance != mappedAccount.Balance || account.BalanceRecordedDate < mappedAccount.BalanceRecordedDate)
                        {
                            dbContext.AccountBalanceHistories.Add(new(account));
                            account.UpdateBalance(mappedAccount.Balance, mappedAccount.BalanceRecordedDate);
                        }
                        spreadsheetAccountDict.Remove(account.AccountName);
                    }
                    else
                    {
                        accountsToDelete.Add(account.AccountId);
                    }
                }

                await dbContext.AccountBalanceHistories.Where(x => accountsToDelete.Contains(x.AccountId)).ExecuteDeleteAsync();
                await dbContext.Accounts.Where(x => accountsToDelete.Contains(x.AccountId)).ExecuteDeleteAsync();

                var accountsToCreate = spreadsheetAccountDict.Values.ToList();
                dbContext.Accounts.AddRange(accountsToCreate);
                await dbContext.SaveChangesAsync();

                var mappedAccountDict = accountsToCreate.ToDictionary(x => x.AccountName);
                var mappedTransactions = MapToTransactions(userSpreadsheet.UserId, subsheets.ValueRanges[1].Values, mappedAccountDict);
                dbContext.Transactions.AddRange(mappedTransactions);
                await dbContext.SaveChangesAsync();

                await transaction.CommitAsync();
            });
        }

        /// <summary>
        /// Maps the untyped rows retrieved from the Google Spreadsheet's Account subsheet to a list of <see cref="Account"/> entities.
        /// </summary>
        /// <param name="userGoogleSpreadSheet"></param>
        /// <param name="untypedAccountRows"></param>
        /// <returns></returns>
        private static List<Account> MapToAccounts(UserGoogleSpreadsheet userGoogleSpreadSheet, IList<IList<object>> untypedAccountRows)
        {
            return [.. untypedAccountRows
                .Skip(1)
                .Select( x=> new{ AccountName = x[0], Balance = x[1], Currency = x[2], Notes = x[3], LastUpdateDated = x[4], Institution = x[5], FinancialAccountId = x[6]})
                .Select(x => new Account
            {
                UserGoogleSpreadsheetId = userGoogleSpreadSheet.UserGoogleSpreadsheetId,
                UserId = userGoogleSpreadSheet.UserId,
                AccountName = (string)x.AccountName,
                Balance = decimal.Parse((string)x.Balance),
                Currency = (string)x.Currency,
                Notes = (string?)x.Notes,
                BalanceRecordedDate = DateTime.SpecifyKind((DateTime)x.LastUpdateDated, DateTimeKind.Utc),
                Institution = (string)x.Institution,
                FinancialAccountId = Guid.Parse((string)x.FinancialAccountId),
                IsActive = true,
            })];
        }

        /// <summary>
        /// Maps the untyped rows retrieved from the Google Spreadsheet's Transactions subsheet to a list of <see cref="Transaction"/> entities.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="untypedTransactionRows"></param>
        /// <param name="mappedAccountDict"></param>
        /// <returns></returns>
        private static List<Transaction> MapToTransactions(string userId, IList<IList<object>> untypedTransactionRows, Dictionary<string, Account> mappedAccountDict)
        {
            return [.. untypedTransactionRows
                .Skip(1)
                .Select( x=> new{ Date = x[0], Ammount = x[1], Description = x[2], Category = x[3], Account = x[4]})
                .Select(x => new Transaction
            {
                UserId = userId,
                Date = DateTimeOffset.Parse((string)x.Date).ToUniversalTime(),
                Amount = decimal.Parse((string)x.Ammount),
                AccountId = mappedAccountDict[(string)x.Account].AccountId,
                Description = (string)x.Description,
                Category = (string)x.Category,
                IsActive = true
            })];
        }
    }
}

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
            var subsheets = await googleClient.GetSubSpreadsheetsAsync(message.GoogleSheetId, _subSpreadSheetNames);
            if (subsheets == null)
            {
                logger.LogWarning("Google shee for user {UserId} was not found during data process", message.UserId);
                return;
            }

            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync();

                // Preform a total refresh. Finatable sheet will remain the source of true in the event of data loss.
                // Ensure we delete any additional processed data assocaiated with the user to keep things clean.
                await dbContext.Accounts.Where(x => x.UserId == message.UserId).ExecuteDeleteAsync();
                await dbContext.Transactions.Where(x => x.UserId == message.UserId).ExecuteDeleteAsync();

                var mappedAccounts = MapToAccounts(message.UserId, subsheets.ValueRanges[0].Values);
                dbContext.Accounts.AddRange(mappedAccounts);
                await dbContext.SaveChangesAsync();

                var mappedAccountDict = mappedAccounts.ToDictionary(x => x.AccountName);
                var mappedTransactions = MapToTransactions(message.UserId, subsheets.ValueRanges[1].Values, mappedAccountDict);
                dbContext.Transactions.AddRange(mappedTransactions);
                await dbContext.SaveChangesAsync();

                await transaction.CommitAsync();
            });
        }

        /// <summary>
        /// Maps the untyped rows retrieved from the Google Spreadsheet's Account subsheet to a list of <see cref="Account"/> entities.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="untypedAccountRows"></param>
        /// <returns></returns>
        private static List<Account> MapToAccounts(string userId, IList<IList<object>> untypedAccountRows)
        {
            return [.. untypedAccountRows
                .Skip(1)
                .Select( x=> new{ AccountName = x[0], Balance = x[1], Currency = x[2], Notes = x[3], LastUpdateDated = x[4], Institution = x[5], FinancialAccountId = x[6]})
                .Select(x => new Account
            {
                UserId = userId,
                AccountName = (string)x.AccountName,
                Balance = decimal.Parse((string)x.Balance),
                Currency = (string)x.Currency,
                Notes = (string?)x.Notes,
                LastUpdatedDate = DateTime.SpecifyKind((DateTime)x.LastUpdateDated, DateTimeKind.Utc),
                Institution = (string)x.Institution,
                FinancialAccountId = Guid.Parse((string)x.FinancialAccountId)
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
            })];
        }
    }
}

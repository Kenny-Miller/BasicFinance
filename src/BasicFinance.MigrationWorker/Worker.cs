using System.Diagnostics;
using BasicFinance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BasicFinance.MigrationWorker
{
    public partial class Worker(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<Worker> logger) : BackgroundService
    {
        public const string ActivitySourceName = "Migrations";
        private static readonly ActivitySource _activitySource = new(ActivitySourceName);

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var activity = _activitySource.StartActivity("Migrating Database", ActivityKind.Client);

            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                LogMigrationStarted(logger);

                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(stoppingToken);
                if (pendingMigrations.Any())
                {
                    await dbContext.Database.MigrateAsync(stoppingToken);
                }
                else
                {
                    LogNoPendingMigrationsFound(logger);
                }

                await SeedAccountTypesAsync(dbContext, logger, stoppingToken);
                await SeedTransactionCategoriesAsync(dbContext, logger, stoppingToken);
                await SeedTransactionTypesAsync(dbContext, logger, stoppingToken);

                LogMigrationCompleted(logger);
            }
            catch (Exception ex)
            {
                LogMigrationErrored(logger, ex);
                activity?.AddException(ex);
                throw;
            }

            hostApplicationLifetime.StopApplication();
        }

        private static async Task SeedAccountTypesAsync(AppDbContext dbContext, ILogger<Worker> logger, CancellationToken cancellationToken)
        {
            // Seed account types
            var accountTypesCount = await dbContext.AccountTypes.CountAsync(cancellationToken);
            if (accountTypesCount == 0)
            {
                LogDbSetSeedingStarted(logger, nameof(dbContext.AccountTypes));
                dbContext.AccountTypes.AddRange(
                    new("CHK", "Checking"),
                    new("SAV", "Savings"),
                    new("CC", "Credit Card"),
                    new("INV", "Investment"));

                await dbContext.SaveChangesAsync(cancellationToken);
                LogDbSetSeedingCompleted(logger, nameof(dbContext.AccountTypes));
            }
        }

        private static async Task SeedTransactionTypesAsync(AppDbContext dbContext, ILogger<Worker> logger, CancellationToken cancellationToken)
        {
            // Seed transaction types
            var transactionTypesCount = await dbContext.TransactionTypes.CountAsync(cancellationToken);
            if (transactionTypesCount == 0)
            {
                LogDbSetSeedingStarted(logger, nameof(dbContext.TransactionTypes));
                dbContext.TransactionTypes.AddRange(
                    new("CR", "Credit"),
                    new("DR", "Debit"));

                await dbContext.SaveChangesAsync(cancellationToken);
                LogDbSetSeedingCompleted(logger, nameof(dbContext.TransactionTypes));
            }
        }

        private static async Task SeedTransactionCategoriesAsync(AppDbContext dbContext, ILogger<Worker> logger, CancellationToken cancellationToken)
        {
            // Seed transaction categories
            var transactionCategoriesCount = await dbContext.TransactionCategories.CountAsync(cancellationToken);
            if (transactionCategoriesCount == 0)
            {
                LogDbSetSeedingStarted(logger, nameof(dbContext.TransactionCategories));
                dbContext.TransactionCategories.AddRange(
                    new("CR", "Credit"),
                    new("DR", "Debit"),
                    new("UNC", "Uncategorized"),
                    new("AUTO", "Auto and Transport"),
                    new("BILLS", "Bills and Utilities"),
                    new("BUSINESS", "Business"),
                    new("CASH", "Cash & Checks"),
                    new("DONATIONS", "Charitable Donations"),
                    new("DINING", "Dining & Drinks"),
                    new("EDUCATION", "Education"),
                    new("ENTERTAINMENT", "Entertainment & Rec"),
                    new("FAMILY", "Family Care"),
                    new("FEES", "Fees"),
                    new("GIFTS", "Gifts"),
                    new("GROCERIES", "Groceries"),
                    new("HEALTH", "Health & Wellness"),
                    new("HOME", "Home & Garden"),
                    new("LEGAL", "Legal"),
                    new("LOAN", "Loan Payment"),
                    new("MEDICAL", "Medical"),
                    new("PERSONAL", "Personal Care"),
                    new("PETS", "Pets"),
                    new("SHOPPING", "Shopping"),
                    new("SOFTWARE", "Software & Tech"),
                    new("TAXES", "Taxes"),
                    new("TRAVEL", "Travel & Vacation"),
                    new("INCOME", "Income"),
                    new("INVESTMENT", "Investment"),
                    new("CREDIT", "Credit Card Payment"),
                    new("IGNORE", "Ignore"),
                    new("TRANSFER", "Internal Transfer"),
                    new("REIMBURSEMENT", "Reimbursement"),
                    new("SAVINGS", "Savings Transfer"));

                await dbContext.SaveChangesAsync(cancellationToken);
                LogDbSetSeedingCompleted(logger, nameof(dbContext.TransactionCategories));
            }
        }

        [LoggerMessage(
           EventName = nameof(LogMigrationStarted),
           Level = LogLevel.Information,
           Message = "Starting database migration...")]
        private static partial void LogMigrationStarted(ILogger logger);

        [LoggerMessage(
           EventName = nameof(LogNoPendingMigrationsFound),
           Level = LogLevel.Information,
           Message = "No pending migrations found")]
        private static partial void LogNoPendingMigrationsFound(ILogger logger);

        [LoggerMessage(
           EventName = nameof(LogMigrationCompleted),
           Level = LogLevel.Information,
           Message = "Database migration completed")]
        private static partial void LogMigrationCompleted(ILogger logger);

        [LoggerMessage(
           EventName = nameof(LogMigrationErrored),
           Level = LogLevel.Error,
           Message = "Database migration failed")]
        private static partial void LogMigrationErrored(ILogger logger, Exception ex);

        [LoggerMessage(
           EventName = nameof(LogDbSetSeedingStarted),
           Level = LogLevel.Information,
           Message = "Seeding {Dbset}...")]
        private static partial void LogDbSetSeedingStarted(ILogger logger, string dbset);

        [LoggerMessage(
            EventName = nameof(LogDbSetSeedingCompleted),
            Level = LogLevel.Information,
            Message = "{Dbset} seeding completed")]
        private static partial void LogDbSetSeedingCompleted(ILogger logger, string dbset);
    }
}
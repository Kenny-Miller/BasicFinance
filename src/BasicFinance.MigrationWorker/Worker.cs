using System.Diagnostics;
using BasicFinance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BasicFinance.MigrationWorker
{
    /// <summary>
    /// Represents a background service that performs database migrations and seeds initial data into the database
    /// used by the BasicFinance application.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="hostApplicationLifetime"></param>
    /// <param name="logger"></param>
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

                await SeedInstitutionsAsync(dbContext, logger, stoppingToken);
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

        /// <summary>
        /// Seeds the Institutions table in the database with initial data if it is empty.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="logger"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task SeedInstitutionsAsync(AppDbContext dbContext, ILogger<Worker> logger, CancellationToken cancellationToken)
        {
            var institutionsCount = await dbContext.Institutions.CountAsync(cancellationToken);
            if (institutionsCount == 0)
            {
                LogDbSetSeedingStarted(logger, nameof(dbContext.Institutions));
                dbContext.Institutions.AddRange(
                    new("WF", "Wells Fargo", null),
                    new("CHASE", "Chase", null),
                    new("SCHW", "Charles Schwab", null));

                await dbContext.SaveChangesAsync(cancellationToken);
                LogDbSetSeedingCompleted(logger, nameof(dbContext.Institutions));
            }
        }

        /// <summary>
        /// Seeds the AccountTypes table in the database with initial data if it is empty.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="logger"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Seeds the TransactionTypes table in the database with initial data if it is empty.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="logger"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Seeds the TransactionCategories table in the database with initial data if it is empty.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="logger"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Logs the start of the database migration process.
        /// </summary>
        /// <param name="logger"></param>
        [LoggerMessage(
           EventName = nameof(LogMigrationStarted),
           Level = LogLevel.Information,
           Message = "Starting database migration...")]
        private static partial void LogMigrationStarted(ILogger logger);

        /// <summary>
        /// Logs that no pending migrations were found in the database.
        /// </summary>
        /// <param name="logger"></param>
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

        /// <summary>
        /// Logs an error that occurred during the database migration process.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="ex"></param>
        [LoggerMessage(
           EventName = nameof(LogMigrationErrored),
           Level = LogLevel.Error,
           Message = "Database migration failed")]
        private static partial void LogMigrationErrored(ILogger logger, Exception ex);

        /// <summary>
        /// Logs the start of seeding a specific DbSet in the database.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="dbset"></param>
        [LoggerMessage(
           EventName = nameof(LogDbSetSeedingStarted),
           Level = LogLevel.Information,
           Message = "Seeding {Dbset}...")]
        private static partial void LogDbSetSeedingStarted(ILogger logger, string dbset);

        /// <summary>
        /// Logs the completion of seeding a specific DbSet in the database.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="dbset"></param>
        [LoggerMessage(
            EventName = nameof(LogDbSetSeedingCompleted),
            Level = LogLevel.Information,
            Message = "{Dbset} seeding completed")]
        private static partial void LogDbSetSeedingCompleted(ILogger logger, string dbset);
    }
}
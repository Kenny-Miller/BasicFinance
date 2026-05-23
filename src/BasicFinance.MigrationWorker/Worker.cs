using System.Diagnostics;
using BasicFinance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BasicFinance.MigrationWorker
{
    public class Worker(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<Worker> logger) : BackgroundService
    {
        public const string ActivitySourceName = "Migrations";
        private static readonly ActivitySource _activitySource = new(ActivitySourceName);

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("Migrating Database", ActivityKind.Client);

            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                logger.LogInformation("Starting database migration...");

                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
                if (pendingMigrations.Any())
                {
                    await dbContext.Database.MigrateAsync(cancellationToken);
                }
                else
                {
                    logger.LogInformation("No pending migrations found");
                }

                await SeedAccountTypesAsync(dbContext, logger, cancellationToken);
                await SeedTransactionCategoriesAsync(dbContext, logger, cancellationToken);
                await SeedTransactionTypesAsync(dbContext, logger, cancellationToken);

                logger.LogInformation("Database migration completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database migration failed");
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
                logger.LogInformation("Seeding account types...");
                dbContext.AccountTypes.AddRange(
                    new("CHK", "Checking"),
                    new("SAV", "Savings"),
                    new("CC", "Credit Card"),
                    new("INV", "Investment"));

                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Account types seeding completed");
            }
        }

        private static async Task SeedTransactionTypesAsync(AppDbContext dbContext, ILogger<Worker> logger, CancellationToken cancellationToken)
        {
            // Seed transaction types
            var transactionTypesCount = await dbContext.TransactionTypes.CountAsync(cancellationToken);
            if (transactionTypesCount == 0)
            {
                logger.LogInformation("Seeding transaction types...");
                dbContext.TransactionTypes.AddRange(
                    new("CR", "Credit"),
                    new("DR", "Debit"));

                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Transaction types seeding completed");
            }
        }

        private static async Task SeedTransactionCategoriesAsync(AppDbContext dbContext, ILogger<Worker> logger, CancellationToken cancellationToken)
        {
            // Seed transaction categories
            var transactionCategoriesCount = await dbContext.TransactionCategories.CountAsync(cancellationToken);
            if (transactionCategoriesCount == 0)
            {
                logger.LogInformation("Seeding transaction categories...");
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
                logger.LogInformation("Transaction categories seeding completed");
            }
        }
    }
}

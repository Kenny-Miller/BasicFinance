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
        private static readonly ActivitySource _activitySourc = new(ActivitySourceName);

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            using var activity = _activitySourc.StartActivity("Migrating Database", ActivityKind.Client);

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
    }
}

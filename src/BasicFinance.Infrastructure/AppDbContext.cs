using BasicFinance.Infrastructure.Entities;
using BasicFinance.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace BasicFinance.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public DbSet<Account> Accounts { get; init; } = null!;
        public DbSet<AccountType> AccountTypes { get; init; } = null!;
        public DbSet<UserGoogleSpreadsheet> UserGoogleSpreadsheets { get; init; } = null!;
        public DbSet<AccountBalanceHistory> AccountBalanceHistories { get; init; } = null!;
        public DbSet<Transaction> Transactions { get; init; } = null!;
        public DbSet<TransactionCategory> TransactionCategories { get; init; } = null!;
        public DbSet<TransactionType> TransactionTypes { get; init; } = null!;

        /// <summary>
        /// Shared instance of the <see cref="TrimWhitespaceInterceptor"/> to be used by all DbContext instances. Will 
        /// trim leading and trailing whitespace from string properties on entities before they are saved to the database.
        /// </summary>
        private static readonly TrimWhitespaceInterceptor _interceptor = new();

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        /// <inheritdoc/>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.AddInterceptors(_interceptor);
    }
}
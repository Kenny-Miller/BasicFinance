using BasicFinance.Domain.Entities;
using BasicFinance.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace BasicFinanace.Infrastructure
{
    public class AppDbContext: DbContext
    {
        public DbSet<Account> Accounts { get; init; }
        public DbSet<DataSpreadsheet> DataSpreadsheets { get; init; }
        public DbSet<Transaction> Transactions { get; init; }

        /// <summary>
        /// Shared instance of the <see cref="TrimWhitespaceInterceptor"/> to be used by all DbContext instances. Will 
        /// trim leading and trailing whitespace from string properties on entities before they are saved to the database.
        /// </summary>
        private static readonly TrimWhitespaceInterceptor _interceptor = new();

        /// <inheritdoc/>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.AddInterceptors(_interceptor);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BasicFinance.Infrastructure.Interceptors
{
    internal class TrimWhitespaceInterceptor : SaveChangesInterceptor
    {
        /// <inheritdoc />
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            TrimWhitespace(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <inheritdoc />
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            TrimWhitespace(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        /// <summary>
        /// Trims leading and trailing whitespace from all string properties that have been added or modified.
        /// </summary>
        /// <param name="context"></param>
        private static void TrimWhitespace(DbContext? context)
        {
            if (context == null)
            {
                return;
            }

            var entriesToTrim = context.ChangeTracker
                .Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .SelectMany(e => e.Properties);

            foreach (var entry in entriesToTrim)
            {
                if (entry.CurrentValue is string stringValue)
                {
                    entry.CurrentValue = stringValue.Trim();
                }
            }
        }
    }
}

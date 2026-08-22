using Microsoft.EntityFrameworkCore;

namespace BasicFinance.Api.IntegrationTests.Helpers;

public static class DbContextExtensions
{
    public static async Task<TEntity> SeedAsync<TEntity>(
        this DbContext context,
        TEntity entity,
        CancellationToken ct = default) where TEntity : class
    {
        context.Set<TEntity>().Add(entity);
        await context.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task SeedRangeAsync<TEntity>(
        this DbContext context,
        IEnumerable<TEntity> entities,
        CancellationToken ct = default) where TEntity : class
    {
        context.Set<TEntity>().AddRange(entities);
        await context.SaveChangesAsync(ct);
    }


}

using BasicFinance.DataProcessor.IntegrationTests.Helpers;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using BasicFinance.SharedServiceDefaults;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace BasicFinance.DataProcessor.IntegrationTests.Infrastructure;

public class DataProcessorAppFixture : AppFixtureBase
{
    /// <summary>
    /// Gets a shared instance of the PostgreSQL container for the running integration tests.
    /// </summary>
    private static readonly PostgreSqlContainer _sharedPostgreSqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    /// <summary>
    /// Gets a shared instance of the RabbitMQ container for the running integration tests.
    /// </summary>
    private static readonly RabbitMqContainer _sharedRabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .Build();

    /// <summary>
    /// Gets a lazy-initialized task that starts the shared PostgreSQL and RabbitMQ containers for the integration tests.
    /// </summary>
    private static Lazy<Task> LazyStartContainersTask = new(() => Task.WhenAll(
        _sharedPostgreSqlContainer.StartAsync(),
        _sharedRabbitMqContainer.StartAsync()));

    /// <inheritdoc/>
    protected override async Task RunPreFactoryInitializationAsync()
    {
        await LazyStartContainersTask.Value;
    }

    /// <inheritdoc/>
    protected override WebApplicationFactory<Program> RunFactoryInitialization()
    {
        var factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Override Service Discovery connection strings with the ones from the test containers
                config.AddInMemoryCollection(GetTestFixtureInMemoryCollectionSettings());
            });

            builder.ConfigureTestServices(services =>
            {
                // Remove existing services that we need to mock during testing
                services.RemoveAll<IGoogleServiceAccountClient>();
                services.AddSingleton(x => NSubstitute.Substitute.For<IGoogleServiceAccountClient>());
            });
        });

        return factory;
    }

    /// <inheritdoc/>
    protected override async Task RunPostFactoryInitializationAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var migrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (migrations.Any())
        {
            await dbContext.Database.MigrateAsync();
        }

        await DbDataHelper.SeedGlobalDataAsync(dbContext);
    }

    /// <inheritdoc/>
    public override async Task DisposeAsync()
    {
        await _sharedPostgreSqlContainer.DisposeAsync();
        await _sharedRabbitMqContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Gets the in-memory collection settings for the test fixture.
    /// </summary>
    /// <returns></returns>
    private Dictionary<string, string?> GetTestFixtureInMemoryCollectionSettings()
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(_sharedPostgreSqlContainer.GetConnectionString())
        {
            Database = $"DB_{TestFixtureGuid}"
        };
        var postgreSqlConnectionString = builder.ToString();

        return new()
        {
            [$"ConnectionStrings:{ServiceDiscoveryNames.BasicFinanceDb}"] = postgreSqlConnectionString,
            [$"ConnectionStrings:{ServiceDiscoveryNames.RabbitMq}"] = _sharedRabbitMqContainer.GetConnectionString()
        };
    }
}

using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using BasicFinance.SharedServiceDefaults;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using Xunit;

namespace BasicFinance.DataProcessor.IntegrationTests.InfrastructureV2;

public sealed class DataProcessorClassFixture : IAsyncLifetime, IAsyncDisposable
{
    /// <summary>
    /// Gets a unique identifier for the class fixture instance.
    /// </summary>
    public Guid ClassFixtureGuid { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the shared assembly fixture that manages the lifecycle of the PostgreSQL and RabbitMQ containers for the integration tests.
    /// </summary>
    private DataProcessorAssemblyFixture _assemblyFixture = default!;

    /// <summary>
    /// Gets the application factory used to create and configure the test server for integration tests.
    /// </summary>
    private WebApplicationFactory<Program> _factory = default!;

    /// <summary>
    /// Gets the database connection used by the class fixture.
    /// </summary>
    private NpgsqlConnection _classFixtureDbConnection = default!;

    /// <summary>
    /// Gets a Respawner instance used to reset the database state between test runs.
    /// </summary>
    private Respawner _respawner = default!;

    /// <summary>
    /// Creates a new service scope from the application factory's service provider.
    /// </summary>
    /// <returns></returns>
    public IServiceScope CreateServiceScope() => _factory.Services.CreateScope();

    /// <summary>
    /// Reset the db back to it's initial state.
    /// </summary>
    /// <returns></returns>
    public Task ResetDatabaseAsync() => _respawner.ResetAsync(_classFixtureDbConnection);

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _assemblyFixture = await TestContext.Current.GetFixture<DataProcessorAssemblyFixture>()
           ?? throw new InvalidOperationException("Failed to get the assembly fixture.");

        var builder = new NpgsqlConnectionStringBuilder(_assemblyFixture.PostgreSqlConnectionString)
        {
            Database = $"DB_{ClassFixtureGuid}",
            IncludeErrorDetail = true
        };

        _classFixtureDbConnection = new NpgsqlConnection(builder.ConnectionString);

        _factory = CreateClassFixtureFactory();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var migrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (migrations.Any())
        {
            await dbContext.Database.MigrateAsync();
        }

        await _classFixtureDbConnection.OpenAsync();

        var options = new RespawnerOptions() { WithReseed = true };
        _respawner = await Respawner.CreateAsync(_classFixtureDbConnection, options);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _classFixtureDbConnection.DisposeAsync();
        await _factory.DisposeAsync();
    }

    /// <inheritdoc/>
    private WebApplicationFactory<Program> CreateClassFixtureFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webHostBuilder =>
            {
                webHostBuilder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{ServiceDiscoveryNames.BasicFinanceDb}"] = _classFixtureDbConnection.ConnectionString,
                        [$"ConnectionStrings:{ServiceDiscoveryNames.RabbitMq}"] = _assemblyFixture.RabbitMqConnectionString,
                        [$"Wolverine:QueueName"] = $"queue-{ClassFixtureGuid}"
                    });
                });

                webHostBuilder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IGoogleServiceAccountClient>();
                    services.AddSingleton(_ => NSubstitute.Substitute.For<IGoogleServiceAccountClient>());
                });
            });
    }
}

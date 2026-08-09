using BasicFinance.Api.IntegrationTests.Constants;
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

namespace BasicFinance.Api.IntegrationTests.Infrastructure;

public sealed class ApiClassFixture : IAsyncLifetime, IAsyncDisposable
{
    public Guid ClassFixtureGuid { get; } = Guid.NewGuid();

    public string TestUserId { get; } = TestConstants.TestUserId;

    private ApiAssemblyFixture _assemblyFixture = default!;

    private WebApplicationFactory<Program> _factory = default!;

    private NpgsqlConnection _classFixtureDbConnection = default!;

    private Respawner _respawner = default!;

    public IServiceScope CreateServiceScope() => _factory.Services.CreateScope();

    public Task ResetDatabaseAsync() => _respawner.ResetAsync(_classFixtureDbConnection);

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _assemblyFixture = await TestContext.Current.GetFixture<ApiAssemblyFixture>()
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

        var options = new RespawnerOptions { WithReseed = true };
        _respawner = await Respawner.CreateAsync(_classFixtureDbConnection, options);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _classFixtureDbConnection.DisposeAsync();
        await _factory.DisposeAsync();
    }

    /// <summary>
    /// Creates the <see cref="WebApplicationFactory{TEntryPoint}"/> configured with
    /// integration test fixture settings.
    /// </summary>
    /// <returns></returns>
    private WebApplicationFactory<Program> CreateClassFixtureFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webHostBuilder =>
            {
                webHostBuilder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");

                webHostBuilder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{ServiceDiscoveryNames.BasicFinanceDb}"] = _classFixtureDbConnection.ConnectionString,
                        [$"ConnectionStrings:{ServiceDiscoveryNames.RabbitMq}"] = _assemblyFixture.RabbitMqConnectionString,
                        [$"ConnectionStrings:{ServiceDiscoveryNames.Keycloak}"] = _assemblyFixture.KeycloakBaseAddress,
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
    public HttpClient CreateClient()
    {
        return _factory.CreateClient();
    }
}

using BasicFinance.Api.IntegrationTests.Infrastructure.Handlers;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using BasicFinance.SharedServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using Xunit;

namespace BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;

public sealed class ApiClassFixture : IAsyncLifetime, IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier for the class test fixture instance.
    /// </summary>
    public Guid ClassFixtureGuid { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the unique identifier of the authenticated user that is returned
    /// by the <see cref="WebApplicationFactory{T}"/>'s configured <see cref="AuthenticationHandler{T}"/>..
    /// </summary>
    public string AuthenticatedUserId { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The <see cref="ApiAssemblyFixture"/> instance shared by all
    /// <see cref="ApiAssemblyFixture"/>s.
    /// </summary>
    private ApiAssemblyFixture _assemblyFixture = default!;

    /// <summary>
    /// The <see cref="WebApplicationFactory{Program}"/> instance used to serve
    /// the application used by the tests.
    /// </summary>
    private WebApplicationFactory<Program> _factory = default!;

    /// <summary>
    /// The <see cref="NpgsqlConnection"/> instance used to connect to the
    /// <see cref="ApiClassFixture"/>'s provisioned database.
    /// </summary>
    private NpgsqlConnection _classFixtureDbConnection = default!;

    /// <summary>
    /// The <see cref="Respawner"/> instance used by the class fixture to 
    /// reset the database inbetween each test run.
    /// </summary>
    private Respawner _respawner = default!;

    /// <summary>
    /// Creates a new service scope for resolving dependencies used by
    /// the application being tested.
    /// </summary>
    /// <returns></returns>
    public IServiceScope CreateServiceScope() => _factory.Services.CreateScope();

    /// <summary>
    /// Creates a <see cref="HttpClient"/> instance configured to target
    /// the application being tested.
    /// </summary>
    /// <returns></returns>
    public HttpClient CreateClient() => _factory.CreateClient();

    /// <summary>
    /// Resets the <see cref="ApiClassFixture"/>'s database to it's
    /// post-migration state.
    /// </summary>
    /// <returns></returns>
    public Task ResetDatabaseAsync() => _respawner.ResetAsync(_classFixtureDbConnection);

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _assemblyFixture = await TestContext.Current.GetFixture<ApiAssemblyFixture>()
            ?? throw new InvalidOperationException("Failed to get the assembly fixture.");

        await CreateClassFixtureDbConnectionAsync();
        CreateClassFixtureFactory();
        await RunDatabaseMigrationsAsync();

        await _classFixtureDbConnection.OpenAsync();
        await CreateClassFixtureRespawnerAsync();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _classFixtureDbConnection.DisposeAsync();
        await _factory.DisposeAsync();
    }

    /// <summary>
    /// Creates a <see cref="WebApplicationFactory{TEntryPoint}"/> configured with
    /// overrides inorder to connect with test container infrastrcture.
    /// </summary>
    /// <returns></returns>
    private void CreateClassFixtureFactory()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webHostBuilder =>
            {
                webHostBuilder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");

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

                    services.AddAuthentication(ServiceDiscoveryNames.Keycloak)
                        .AddScheme<AuthenticationSchemeOptions, ApiAuthenticationHandler>(ServiceDiscoveryNames.Keycloak, null);
                });
            });
    }

    /// <summary>
    /// Creates a pre-opened <see cref="NpgsqlConnection"/> instance
    /// connected to the <see cref="ApiClassFixture"/>'s database.
    /// </summary>
    /// <returns></returns>
    private async Task CreateClassFixtureDbConnectionAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(_assemblyFixture.PostgreSqlConnectionString)
        {
            Database = $"DB_{ClassFixtureGuid}",
            IncludeErrorDetail = true
        };

        _classFixtureDbConnection = new NpgsqlConnection(builder.ConnectionString);
    }

    /// <summary>
    /// Checks and applies any pending database migrations to the
    /// <see cref="ApiClassFixture"/>'s database.
    /// </summary>
    /// <returns></returns>
    private async Task RunDatabaseMigrationsAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var migrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (migrations.Any())
        {
            await dbContext.Database.MigrateAsync();
        }
    }

    /// <summary>
    /// Creates a <see cref="Respawner"/> instance
    /// connected to the <see cref="ApiClassFixture"/>'s database.
    /// </summary>
    private async Task CreateClassFixtureRespawnerAsync()
    {
        var options = new RespawnerOptions { WithReseed = true };
        _respawner = await Respawner.CreateAsync(_classFixtureDbConnection, options);
    }
}

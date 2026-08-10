using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

[assembly: AssemblyFixture(typeof(ApiAssemblyFixture))]
namespace BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;

public sealed class ApiAssemblyFixture : IAsyncLifetime, IAsyncDisposable
{
    /// <summary>
    /// Gets the provisioned Keycloak user credentials (user ID and access token).
    /// </summary>
    public KeycloakUserDto KeycloakUser { get; private set; } = default!;

    /// <summary>
    /// Gets the connection string to the Postregres container.
    /// </summary>
    public string PostgreSqlConnectionString => _postgreSql.GetConnectionString();

    /// <summary>
    /// Gets the connection string the the RabbitMq container.
    /// </summary>
    public string RabbitMqConnectionString => _rabbitMq.GetConnectionString();

    /// <summary>
    /// Gets the base url of the Keycloak container.
    /// </summary>
    public string KeycloakBaseAddress => _keycloak.GetBaseAddress();

    /// <summary>
    /// Gets the Postres container used by the assembly test fixture.
    /// </summary>
    private readonly PostgreSqlContainer _postgreSql = new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>
    /// Gets the Rabbitmq container used by the assembly test fixture.
    /// </summary>
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management-alpine").Build();

    /// <summary>
    /// Gets the Keycloak container used by the assembly test fixture.
    /// </summary>
    private readonly KeycloakContainer _keycloak = new KeycloakBuilder("quay.io/keycloak/keycloak:26.0")
        .WithRealm(Path.Combine(AppContext.BaseDirectory, "Realms", "IntegrationTestRealm.json"))
        .Build();

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _postgreSql.StartAsync(),
            _rabbitMq.StartAsync(),
            _keycloak.StartAsync());

        try
        {

            KeycloakUser = await
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("For more on this error consult the server log"))
        {
            var logs = await _keycloak.GetLogsAsync();
            throw new InvalidOperationException($"Keycloak container error logs: {logs}");
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _keycloak.DisposeAsync();
        await _postgreSql.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
}

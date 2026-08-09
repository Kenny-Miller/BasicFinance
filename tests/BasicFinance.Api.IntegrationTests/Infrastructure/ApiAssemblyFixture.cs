using BasicFinance.Api.IntegrationTests.Helpers;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

[assembly: AssemblyFixture(typeof(BasicFinance.Api.IntegrationTests.Infrastructure.ApiAssemblyFixture))]
namespace BasicFinance.Api.IntegrationTests.Infrastructure;

public sealed class ApiAssemblyFixture : IAsyncLifetime, IAsyncDisposable
{
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
        .WithUsername("admin")
        .WithPassword("admin")
        .Build();

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _postgreSql.StartAsync(),
            _rabbitMq.StartAsync(),
            _keycloak.StartAsync());

        await KeycloakHelper.ProvisionTestRealmAsync(_keycloak.GetBaseAddress());
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _keycloak.DisposeAsync();
        await _postgreSql.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
}

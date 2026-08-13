using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

[assembly: AssemblyFixture(typeof(ApiAssemblyFixture))]
namespace BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;

public sealed class ApiAssemblyFixture : IAsyncLifetime, IAsyncDisposable
{
    /// <summary>
    /// Gets the connection string used to connect to the <see cref="PostgreSqlContainer"/> instance
    /// used by the integration tests.
    /// </summary>
    public string PostgreSqlConnectionString => _postgreSql.GetConnectionString();

    /// <summary>
    /// Gets the connection string used to connect to the <see cref="RabbitMqContainer"/> instance
    /// used by the integration tests.
    /// </summary>
    public string RabbitMqConnectionString => _rabbitMq.GetConnectionString();

    /// <summary>
    /// The <see cref="PostgreSqlContainer"/> instance used by the integration tests.
    /// </summary>
    private readonly PostgreSqlContainer _postgreSql = new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>
    /// The <see cref="RabbitMqContainer"/> instance used by the integration tests.
    /// </summary>
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management-alpine").Build();

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _postgreSql.StartAsync(),
            _rabbitMq.StartAsync());
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _postgreSql.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
}

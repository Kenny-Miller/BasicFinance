using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

[assembly: AssemblyFixture(typeof(BasicFinance.DataProcessor.IntegrationTests.Infrastructure.DataProcessorAssemblyFixture))]
namespace BasicFinance.DataProcessor.IntegrationTests.Infrastructure;

public sealed class DataProcessorAssemblyFixture : IAsyncLifetime, IAsyncDisposable
{
    /// <summary>
    /// Gets the connection string for the assembly wide PostgreSQL container used in the integration tests.
    /// </summary>
    public string PostgreSqlConnectionString => _postgreSQL.GetConnectionString();

    /// <summary>
    /// Gets the connection string for the assembly wide RabbitMQ container used in the integration tests.
    /// </summary>
    public string RabbitMqConnectionString => _rabbitMq.GetConnectionString();

    /// <summary>
    /// Gets a shared instance of the assembly wide PostgreSQL container for the running integration tests.
    /// </summary>
    private readonly PostgreSqlContainer _postgreSQL = new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>
    /// Gets a shared instance of the assembly wide RabbitMQ container for the running integration tests.
    /// </summary>
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management-alpine").Build();

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgreSQL.StartAsync(), _rabbitMq.StartAsync());
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _postgreSQL.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
}
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

[assembly: AssemblyFixture(typeof(ApiAssemblyFixture))]
namespace BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;

public sealed class ApiAssemblyFixture : IAsyncLifetime, IAsyncDisposable
{
    public string PostgreSqlConnectionString => _postgreSql.GetConnectionString();

    public string RabbitMqConnectionString => _rabbitMq.GetConnectionString();

    private readonly PostgreSqlContainer _postgreSql = new PostgreSqlBuilder("postgres:17-alpine").Build();

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

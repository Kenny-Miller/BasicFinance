using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Respawn;
using Xunit;

namespace BasicFinance.DataProcessor.IntegrationTests.Infrastructure;

public abstract class DataProcessorTestBase : IClassFixture<DataProcessorAppFixture>
{
    private readonly DataProcessorAppFixture _fixture;
    private readonly IServiceScope _serviceScope;
    protected IHost Host { get; }
    protected AppDbContext DbContext { get; }
    protected IGoogleServiceAccountClient MockGoogleServiceAccountClient { get; }
    protected Guid TestFixtureGuid => _fixture.TestFixtureGuid;
    protected Respawner Respawner => _fixture.Respawner;

    protected DataProcessorTestBase(DataProcessorAppFixture fixture)
    {
        _fixture = fixture;
        _serviceScope = fixture.CreateScope();
        Host = _serviceScope.ServiceProvider.GetRequiredService<IHost>();
        DbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
        MockGoogleServiceAccountClient = _serviceScope.ServiceProvider.GetRequiredService<IGoogleServiceAccountClient>();
    }
}

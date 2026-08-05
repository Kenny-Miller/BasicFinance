using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace BasicFinance.DataProcessor.IntegrationTests.Infrastructure;

public abstract class DataProcessorTestBase : IClassFixture<DataProcessorAppFixture>
{
    private readonly IServiceScope _serviceScope;
    protected IHost Host { get; }
    protected AppDbContext DbContext { get; }
    protected IGoogleServiceAccountClient MockGoogleServiceAccountClient { get; }
    protected Guid TestFixtureGuid { get; private set; }

    protected DataProcessorTestBase(DataProcessorAppFixture fixture)
    {
        _serviceScope = fixture.CreateScope();
        Host = _serviceScope.ServiceProvider.GetRequiredService<IHost>();
        DbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
        MockGoogleServiceAccountClient = _serviceScope.ServiceProvider.GetRequiredService<IGoogleServiceAccountClient>();
        TestFixtureGuid = fixture.TestFixtureGuid;
    }
}

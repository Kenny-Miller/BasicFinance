using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace BasicFinance.DataProcessor.IntegrationTests.InfrastructureV2;

public abstract class DataProcessorTestFixture : IClassFixture<DataProcessorClassFixture>, IAsyncLifetime, IAsyncDisposable
{
    protected IHost Host { get; private set; } = default!;
    protected AppDbContext DbContext { get; private set; } = default!;
    protected IGoogleServiceAccountClient MockGoogleServiceAccountClient { get; private set; } = default!;

    private readonly DataProcessorClassFixture _fixture;
    private IServiceScope _serviceScope { get; set; } = default!;

    protected DataProcessorTestFixture(DataProcessorClassFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        _serviceScope = _fixture.CreateServiceScope();

        Host = _serviceScope.ServiceProvider.GetRequiredService<IHost>();
        await Host.StartAsync();

        DbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
        MockGoogleServiceAccountClient = _serviceScope.ServiceProvider.GetRequiredService<IGoogleServiceAccountClient>();
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await Host.StopAsync();
        _serviceScope?.Dispose();

        GC.SuppressFinalize(this);
    }
}

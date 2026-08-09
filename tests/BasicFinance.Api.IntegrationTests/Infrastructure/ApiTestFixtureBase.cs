using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BasicFinance.Api.IntegrationTests.Infrastructure;

public abstract class ApiTestFixtureBase : IClassFixture<ApiClassFixture>, IAsyncLifetime, IAsyncDisposable
{
    protected HttpClient HttpClient { get; private set; } = default!;

    protected AppDbContext DbContext { get; private set; } = default!;

    protected string TestUserId => _fixture.TestUserId;

    private readonly ApiClassFixture _fixture;

    private IServiceScope _serviceScope = default!;

    protected ApiTestFixtureBase(ApiClassFixture fixture)
    {
        _fixture = fixture;
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        _serviceScope = _fixture.CreateServiceScope();

        DbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DbSeedHelper.SeedGlobalDataAsync(DbContext, _fixture.TestUserId);

        HttpClient = _fixture.CreateClient();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        HttpClient.Dispose();
        await DbContext.DisposeAsync();
        _serviceScope?.Dispose();
        GC.SuppressFinalize(this);
    }
}

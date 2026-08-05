using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BasicFinance.DataProcessor.IntegrationTests.Infrastructure;

/// <summary>
/// Represents the base class for integration test fixtures, providing common functionality for setting up and tearing down test environments,
/// including container management and application factory configuration.
/// </summary>
/// <remarks>
/// Lifecyle of the fixture setup is as follows:
/// RunPreFactoryInitializationAsync() -> RunFactoryInitialization() -> RunPostFactoryInitializationAsync()
/// </remarks>
public abstract class AppFixtureBase : IAsyncLifetime
{
    /// <summary>
    /// Gets or sets the <see cref="Guid"/> representing the fixture instance that can be used to
    /// differentiate between different instances of the fixture in a test run.
    /// </summary>
    public Guid TestFixtureGuid { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the <see cref="TimeProvider"/> instance to be used for time-related operations in the test fixture.
    /// </summary>
    public TimeProvider TestFixtureTimeProvider { get; init; } = TimeProvider.System;

    /// <summary>
    /// Gets the <see cref="WebApplicationFactory{Program}"/> instance used to create application for integration testing.
    /// </summary>
    public WebApplicationFactory<Program> Factory => _factory ?? throw new InvalidOperationException("Factory is not initialized");

    /// <summary>
    /// Gets or sets the <see cref="WebApplicationFactory{Program}"/> instance used to create application for integration testing.
    /// </summary>
    private WebApplicationFactory<Program>? _factory;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await RunPreFactoryInitializationAsync();
        _factory = RunFactoryInitialization();
        var serviceProvider = MaterializeHost();
        await RunPostFactoryInitializationAsync(serviceProvider);
    }

    /// <inheritdoc/>
    public virtual async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }

    /// <summary>
    /// Gets a required service of type <typeparamref name="T"/> from the application's service provider.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetRequiredService<T>() where T : notnull
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Creates a new <see cref="IServiceScope"/> for resolving services from the application's service provider.
    /// </summary>
    /// <returns></returns>
    public IServiceScope CreateScope() => Factory.Services.CreateScope();

    /// <summary>
    /// Runs any pre-factory initialization logic, such as starting containers or setting up dependencies, before the application factory is built.
    /// </summary>
    /// <returns></returns>
    protected abstract Task RunPreFactoryInitializationAsync();

    /// <summary>
    /// Initializes the <see cref="WebApplicationFactory{Program}"/> instance for the test fixture, configuring it with necessary services, settings, and dependencies.
    /// </summary>
    protected abstract WebApplicationFactory<Program> RunFactoryInitialization();

    /// <summary>
    /// Runs any post-factory/pre-test setup logic, such as seeding data or configuring services, after the containers have started and the application factory has been built.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    protected abstract Task RunPostFactoryInitializationAsync(IServiceProvider serviceProvider);

    /// <summary>
    /// Forces the <see cref="WebApplicationFactory{TEntryPoint}"/> to build its host now and
    /// returns the root service provider. The WithWebHostBuilder callback (including
    /// GetConnectionString and the assignment to <see cref="Configuration"/>) runs lazily on
    /// first access to <c>Services</c>. Triggering it here — while the containers are known to be
    /// running — pins host construction to a deterministic point in the lifecycle instead of
    /// leaving it to whichever test first calls CreateClient().
    /// </summary>
    /// <returns>The root service provider.</returns>
    private IServiceProvider MaterializeHost() => Factory.Services;
}

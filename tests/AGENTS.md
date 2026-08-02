# BasicFinance .NET Tests — Agent Instructions

## Framework & Tools

| Tool | Purpose |
|---|---|
| **xUnit** | Test framework. All test projects use `xunit` + `xunit.runner.visualstudio`. |
| **Moq** | Mocking library. Used for stubs and verifying interactions. |
| **Microsoft.NET.Test.Sdk** | Test runner / discovery. |
| **Testcontainers** | Real infrastructure (PostgreSQL, RabbitMQ) for integration tests. |

Run tests from the solution root:
```powershell
dotnet test tests/BasicFinance.Domain.UnitTests
dotnet test tests/BasicFinance.Infrastructure.UnitTests
dotnet test  # runs all test projects
```

## Test Organization

- Each test project mirrors the source project it tests (e.g. `BasicFinance.Domain.UnitTests` tests `BasicFinance.Domain`).
- Test files mirror the folder structure of the source project. A class in `src/BasicFinance.Domain/Internal/DateTimeRange.cs` gets tests in `tests/BasicFinance.Domain.UnitTests/Internal/DateTimeRangeTests.cs`.
- One test class per source class, named `<SourceClassName>Tests`.
- Integration test projects mirror the `Features/` structure of the source project.

## Naming Conventions

### Test project names
- Unit test projects: `<SourceProject>.UnitTests`
- Integration test projects: `<SourceProject>.IntegrationTests`

### Test class names
`<ClassName>Tests` — e.g. `DateTimeRangeTests`, `QueryableExtensionsTests`.

### Test method names
Three-part convention: `<MethodName>_<Scenario>_<ExpectedBehavior>`

```csharp
[Fact]
public void Constructor_ValidRange_CreatesSuccessfully() { }

[Fact]
public void Constructor_StartAfterEnd_ThrowsArgumentException() { }
```

### Test helper names
- Shared test doubles / fakes: `Fake<InterfaceName>.cs` (e.g. `FakeTimeProvider.cs`)
- Factory helpers: `<EntityName>Factory.cs` (e.g. `TransactionFactory.cs`)
- General helpers: `<Name>Helper.cs`

## Test Structure — Arrange-Act-Assert

Every test follows the three-phase pattern with blank line separators:

```csharp
[Fact]
public void Add_EmptyString_ReturnsZero()
{
    // Arrange
    var calculator = new StringCalculator();

    // Act
    var actual = calculator.Add("");

    // Assert
    Assert.Equal(0, actual);
}
```

- **One Act per test** — if a test needs multiple actions, split it or use `[Theory]`.
- **No logic in tests** — avoid `if`, `for`, `while`, `switch` inside test methods. Use `[Theory]` + `[InlineData]` instead.

## xUnit Patterns

| Attribute | Use when |
|---|---|
| `[Fact]` | Tests an invariant — always true, single execution path. |
| `[Theory]` + `[InlineData]` | Same test logic, different input data. |

```csharp
[Theory]
[InlineData(-1)]
[InlineData(0)]
[InlineData(1)]
public void IsPrime_ValuesLessThan2_ReturnsFalse(int value)
{
    var service = new PrimeService();
    Assert.False(service.IsPrime(value));
}
```

## Mocking with Moq

### Stubs — provide data, not asserted against
```csharp
var mockRepo = new Mock<IRepository>();
mockRepo.Setup(r => r.GetById(1)).Returns(new Entity { Id = 1 });
```

### Mocks — asserted against to verify interactions
```csharp
var mockLogger = new Mock<ILogger>();
sut.Process(data);
mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
```

### Helper methods over constructor setup
Prefer private helper methods to initialize shared state rather than constructor fields:

```csharp
[Fact]
public void SomeTest()
{
    var sut = CreateSut();
    // ...
}

static MyService CreateSut() => new();
```

This avoids shared mutable state between tests and makes each test self-contained.

## Test Helpers — `Helpers/` Directory

Each test project has a `Helpers/` directory at its root for shared test utilities:

```
BasicFinance.Domain.UnitTests/
  Helpers/
    FakeTimeProvider.cs
    TransactionFactory.cs
    TestEntity.cs
  Internal/
    DateTimeRangeTests.cs
```

### What goes in `Helpers/`
- **Test doubles** — fake implementations of interfaces (`Fake*`)
- **Test data factories** — methods that create populated domain objects (`*Factory`)
- **Test entity models** — simple POCOs used as in-memory test data (`Test*`)
- **Mock builders** — reusable Moq setup helpers

### What stays in test files
- Test methods (`[Fact]`, `[Theory]`)
- Test-specific inline assertions
- Per-test local variables

### Helper conventions
- Helpers are `internal` (no need for `InternalsVisibleTo` since they're in the same assembly).
- Use `static` helper methods for factories when stateless.
- Use `static readonly` for constant test data.
- Test files reference helpers via `using` — do not embed `private sealed class` test models inline in test files.

## What to Assert

- **Behavior, not implementation** — test the observable outcome of a public method.
- **Public surface area** — test public methods. Private methods are tested indirectly through their public callers.
- **Meaningful assertions** — assert the observable outcome. Closely related assertions on the same result are permitted within a single test. Use `Assert.Equal`, `Assert.True/False`, `Assert.Throws<T>`, `Assert.Contains`.
- **Avoid magic strings** — assign hard-coded test values to `const` or `static readonly` fields with descriptive names.

## What to Avoid

| Anti-pattern | Why |
|---|---|
| Infrastructure dependencies (DB, network, file system) | Makes tests slow and brittle — reserve for integration tests. |
| Multiple Act phases in one test | Obscures which action caused the failure. |
| Logic (`if`, loops) in test body | Introduces bugs in the test suite itself. Use `[Theory]` instead. |
| `Setup` / `Teardown` attributes | xUnit discourages these. Use helper methods. |
| Testing private methods directly | They're implementation details. Test via public API. |
| Overly complex Arrange | Keep input minimal — only what's needed to exercise the behavior. |
| Magic strings / unexplained values | Use named constants for clarity. |

## Integration Tests

Integration tests verify the full pipeline: endpoint → middleware → DbContext → PostgreSQL. They use TestContainers for real infrastructure but mock external services (Keycloak, Google SDK).

### When to write an integration test
- Testing a vertical slice endpoint (e.g., `GET /api/accounts`)
- Testing EF Core queries against a real PostgreSQL database
- Testing Wolverine message handlers with real RabbitMQ
- Testing middleware interactions (auth, validation, error handling)

### When to write a unit test
- Testing domain model behavior
- Testing query extensions on in-memory data
- Testing business logic that doesn't require infrastructure

### TestContainers setup

Use `Testcontainers` NuGet packages for PostgreSQL and RabbitMQ. Share containers across test classes via `ICollectionFixture`:

```csharp
public class TestContainerFixture : ICollectionFixture<TestContainerFixture>
{
    public static readonly IContainer PostgreSQL = new Containers.Builder()
        .WithImage("postgres:17-alpine")
        .WithEnvironment("POSTGRES_USER", "test")
        .WithEnvironment("POSTGRES_PASSWORD", "test")
        .WithEnvironment("POSTGRES_DB", "basicfinance_test")
        .WithPortBinding(5432, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    public static readonly IContainer RabbitMQ = new Containers.Builder()
        .WithImage("rabbitmq:4-management")
        .WithPortBinding(5672, true)
        .WithPortBinding(15672, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672))
        .Build();

    static TestContainerFixture()
    {
        PostgreSQL.Start();
        RabbitMQ.Start();
    }
}
```

### WebApplicationFactory

Create a custom factory inheriting `WebApplicationFactory<Program>` to configure test services:

```csharp
public class TestApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureHostApplicationBuilder(IHostApplicationBuilder builder)
    {
        base.ConfigureHostApplicationBuilder(builder);

        // Replace production DB with TestContainers PostgreSQL
        var postgresPort = TestContainerFixture.PostgreSQL.GetMappedPublicPort(5432);
        builder.Configuration["ConnectionStrings:AppDbContext"] =
            $"Host=localhost;Port={postgresPort};Database=basicfinance_test;Username=test;Password=test";

        // Mock Keycloak authentication
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "TestScheme";
        }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

        // Mock Google services
        builder.Services.Replace(new ServiceDescriptor(IGoogleUserClient, Mock.Of<IGoogleUserClient>()));
    }
}
```

### Mocking Keycloak authentication

Use a custom `AuthenticationHandler<TOptions>` to bypass Keycloak:

```csharp
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "test-user"),
        };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

### Integration test naming

`<FeatureName>_<Scenario>_<ExpectedBehavior>` — e.g. `ListAccounts_AuthenticatedUser_ReturnsAccountList`.

### Integration test structure

Same Arrange-Act-Assert pattern, but Arrange includes DB seeding:

```csharp
public class ListAccountsTests : IClassFixture<TestApplicationFactory>
{
    readonly HttpClient _client;
    readonly TestApplicationFactory _factory;

    public ListAccountsTests(TestApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListAccounts_AuthenticatedUser_ReturnsAccountList()
    {
        // Arrange — seed test data via DbContext
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Accounts.Add(AccountFactory.Create());
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/api/accounts");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListResult<AccountDto>>();
        Assert.NotEmpty(result!.Items);
    }
}
```

### What to assert in integration tests
- HTTP status codes
- Response JSON shape and content
- Database state changes (read back via DbContext)
- Message queue behavior (for Wolverine handler tests)

### What NOT to do in integration tests
- Test framework behavior (assume ASP.NET Core, EF Core, xUnit work correctly)
- Connect to real Keycloak or Google APIs (mock these)
- Use in-memory database or SQLite (use real PostgreSQL via TestContainers)

## Code Style

Follow the same `.editorconfig` rules as the source projects: file-scoped namespaces, `var` preferred, expression-bodied members where concise, braces required on control flow.

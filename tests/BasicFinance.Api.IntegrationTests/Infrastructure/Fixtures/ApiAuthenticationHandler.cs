using System.Security.Claims;
using System.Text.Encodings.Web;
using BasicFinance.SharedServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;

public sealed class ApiAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationHeader = "X-Test-UserId";

    public ApiAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authenticatedUserId = Request.Headers.TryGetValue(AuthenticationHeader, out var id) ? id.ToString() : null;
        if (authenticatedUserId == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Header"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, authenticatedUserId),
            new Claim(ClaimTypes.Name, "testuser1"),
            new Claim(ClaimTypes.GivenName, "Test"),
            new Claim(ClaimTypes.Surname, "User"),
            new Claim(ClaimTypes.Email, "testuser1@test.com")
        };

        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ServiceDiscoveryNames.Keycloak);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

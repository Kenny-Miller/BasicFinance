using System.Security.Claims;
using System.Text.Encodings.Web;
using BasicFinance.SharedServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BasicFinance.Api.IntegrationTests.Infrastructure.Handlers;

/// <summary>
/// The <see cref="ApiAuthenticationHandler"/> represent a <see cref="AuthenticationHandler{TOptions}"/>
/// that is used to bypass conventional authentication during integration testing.
/// </summary>
public sealed class ApiAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// The HTTP header name used by integration tests in order to 
    /// bypass conventional authentication.
    /// </summary>
    public const string AuthenticationHeader = "X-Test-UserId";

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiAuthenticationHandler"/> class.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    /// <param name="encoder"></param>
    public ApiAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc/>
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

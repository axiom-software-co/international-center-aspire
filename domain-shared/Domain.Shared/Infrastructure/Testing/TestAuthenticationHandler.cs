using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Shared.Infrastructure.Testing;

/// <summary>
/// Enhanced test authentication handler for integration testing
/// Supports different authentication scenarios based on request headers
/// Allows testing different roles, authentication states, and user contexts
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if this is an anonymous test (no Authorization header)
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Parse authentication header for test scenarios
        var parts = authHeader.Split(' ');
        if (parts.Length != 2 || !parts[0].Equals("Test", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var testScenario = parts[1];
        var claims = CreateClaimsForScenario(testScenario);

        if (claims.Length == 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid test scenario"));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Create appropriate claims based on test scenario
    /// Supports different user roles and authentication contexts for comprehensive testing
    /// </summary>
    private Claim[] CreateClaimsForScenario(string scenario)
    {
        return scenario.ToLowerInvariant() switch
        {
            "authenticated" => new[]
            {
                new Claim(ClaimTypes.Name, "test-admin-user"),
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim("sub", "test-user-id"),
                new Claim("name", "Test Admin User"),
                new Claim("preferred_username", "test-admin"),
                new Claim("roles", "ServiceAdmin"),
                new Claim("roles", "SystemAdmin"),
                new Claim("aud", "test-client-id")
            },
            
            "serviceadmin" => new[]
            {
                new Claim(ClaimTypes.Name, "service-admin-user"),
                new Claim(ClaimTypes.NameIdentifier, "service-admin-id"),
                new Claim("sub", "service-admin-id"),
                new Claim("name", "Service Admin User"),
                new Claim("preferred_username", "service-admin"),
                new Claim("roles", "ServiceAdmin"),
                new Claim(ClaimTypes.Role, "ServiceAdmin"),
                new Claim("aud", "test-client-id")
            },
            
            "serviceeditor" => new[]
            {
                new Claim(ClaimTypes.Name, "service-editor-user"),
                new Claim(ClaimTypes.NameIdentifier, "service-editor-id"),
                new Claim("sub", "service-editor-id"),
                new Claim("name", "Service Editor User"),
                new Claim("preferred_username", "service-editor"),
                new Claim("roles", "ServiceEditor"),
                new Claim(ClaimTypes.Role, "ServiceEditor"),
                new Claim("aud", "test-client-id")
            },
            
            "serviceviewer" => new[]
            {
                new Claim(ClaimTypes.Name, "service-viewer-user"),
                new Claim(ClaimTypes.NameIdentifier, "service-viewer-id"),
                new Claim("sub", "service-viewer-id"),
                new Claim("name", "Service Viewer User"),
                new Claim("preferred_username", "service-viewer"),
                new Claim("roles", "ServiceViewer"),
                new Claim(ClaimTypes.Role, "ServiceViewer"),
                new Claim("aud", "test-client-id")
            },
            
            "systemadmin" => new[]
            {
                new Claim(ClaimTypes.Name, "system-admin-user"),
                new Claim(ClaimTypes.NameIdentifier, "system-admin-id"),
                new Claim("sub", "system-admin-id"),
                new Claim("name", "System Admin User"),
                new Claim("preferred_username", "system-admin"),
                new Claim("roles", "SystemAdmin"),
                new Claim(ClaimTypes.Role, "SystemAdmin"),
                new Claim("aud", "test-client-id")
            },
            
            "noaccess" => new[]
            {
                new Claim(ClaimTypes.Name, "no-access-user"),
                new Claim(ClaimTypes.NameIdentifier, "no-access-id"),
                new Claim("sub", "no-access-id"),
                new Claim("name", "No Access User"),
                new Claim("preferred_username", "no-access"),
                new Claim("aud", "test-client-id")
                // No role claims - should be denied access
            },
            
            _ => Array.Empty<Claim>()
        };
    }
}
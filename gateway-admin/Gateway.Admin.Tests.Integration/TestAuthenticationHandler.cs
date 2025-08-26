using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace InternationalCenter.Gateway.Admin.Tests.Integration;

/// <summary>
/// Test authentication handler for Admin Gateway integration testing
/// Provides controlled authentication scenarios for contract validation
/// Supports medical-grade role-based access control testing patterns
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ILogger<TestAuthenticationHandler> _logger;

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _logger = logger.CreateLogger<TestAuthenticationHandler>();
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.ToString();
        
        if (string.IsNullOrEmpty(authorizationHeader))
        {
            _logger.LogDebug("No authorization header provided for test authentication");
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            // Extract test authentication data
            var authParts = authorizationHeader.Split(' ');
            if (authParts.Length != 2 || authParts[0] != "Test")
            {
                _logger.LogDebug("Invalid test authorization header format: {AuthHeader}", authorizationHeader);
                return Task.FromResult(AuthenticateResult.Fail("Invalid test authorization format"));
            }

            var testRole = authParts[1];
            var testUserId = Request.Headers["X-User-ID"].FirstOrDefault() ?? $"test-user-{DateTime.UtcNow.Ticks}";
            var correlationId = Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();

            // Create test claims based on role
            var claims = CreateTestClaims(testRole, testUserId, correlationId);
            var identity = new ClaimsIdentity(claims, "Test", ClaimTypes.Name, ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);

            // Log authentication for medical audit compliance
            _logger.LogInformation("TEST_AUTH: User {UserId} authenticated with role {Role} - Correlation: {CorrelationId}", 
                testUserId, testRole, correlationId);

            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test authentication handler failed: {Error}", ex.Message);
            return Task.FromResult(AuthenticateResult.Fail($"Test authentication error: {ex.Message}"));
        }
    }

    private static List<Claim> CreateTestClaims(string testRole, string testUserId, string correlationId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, testUserId),
            new(ClaimTypes.Name, $"Test User {testUserId}"),
            new(ClaimTypes.Email, $"{testUserId}@test.internationalsolutions.medical"),
            new("correlation_id", correlationId),
            new("tenant_id", "test-tenant-12345"),
            new("object_id", testUserId),
            new("preferred_username", $"{testUserId}@test.medical"),
            new("medical_facility", "International Medical Center - Test"),
            new("medical_license", $"ML-{testUserId}-TEST")
        };

        // Add role-specific claims for medical-grade authorization testing
        switch (testRole.ToLower())
        {
            case "systemadmin":
                claims.Add(new(ClaimTypes.Role, "SystemAdmin"));
                claims.Add(new("scope", "system.admin"));
                claims.Add(new("medical_clearance", "MAXIMUM"));
                claims.Add(new("admin_level", "SYSTEM"));
                break;

            case "serviceadmin":
                claims.Add(new(ClaimTypes.Role, "ServiceAdmin"));
                claims.Add(new("scope", "services.admin"));
                claims.Add(new("medical_clearance", "HIGH"));
                claims.Add(new("admin_level", "SERVICE"));
                break;

            case "serviceviewer":
                claims.Add(new(ClaimTypes.Role, "ServiceViewer"));
                claims.Add(new("scope", "services.read"));
                claims.Add(new("medical_clearance", "STANDARD"));
                claims.Add(new("admin_level", "VIEWER"));
                break;

            case "medicalpractitioner":
                claims.Add(new(ClaimTypes.Role, "MedicalPractitioner"));
                claims.Add(new("scope", "medical.read medical.write"));
                claims.Add(new("medical_clearance", "PRACTITIONER"));
                claims.Add(new("practitioner_type", "PHYSICIAN"));
                break;

            case "auditor":
                claims.Add(new(ClaimTypes.Role, "Auditor"));
                claims.Add(new("scope", "audit.read"));
                claims.Add(new("medical_clearance", "AUDIT"));
                claims.Add(new("audit_level", "COMPLIANCE"));
                break;

            default:
                // Default authenticated user with minimal permissions
                claims.Add(new(ClaimTypes.Role, "AuthenticatedUser"));
                claims.Add(new("scope", "basic.read"));
                claims.Add(new("medical_clearance", "BASIC"));
                claims.Add(new("admin_level", "NONE"));
                break;
        }

        return claims;
    }
}

/// <summary>
/// Extension methods for HttpStatusCode validation in tests
/// </summary>
public static class HttpStatusCodeExtensions
{
    /// <summary>
    /// Determines if an HTTP status code represents a successful response (2xx range)
    /// </summary>
    public static bool IsSuccessStatusCode(this System.Net.HttpStatusCode statusCode)
    {
        return ((int)statusCode >= 200) && ((int)statusCode <= 299);
    }
}
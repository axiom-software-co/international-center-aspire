using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Shared.Security;

namespace Shared.Extensions;

public static class ZeroTrustSecurityExtensions
{
    public static IServiceCollection AddZeroTrustSecurity(this IServiceCollection services, IConfiguration configuration, bool isAdminApi = false)
    {
        // Configure JWT authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = !configuration.GetValue<bool>("Security:AllowHttp", false);
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Security:Jwt:Issuer"] ?? "InternationalCenter",
                ValidAudience = configuration["Security:Jwt:Audience"] ?? "InternationalCenter",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Security:Jwt:SecretKey"] ?? 
                        throw new InvalidOperationException("JWT SecretKey not configured"))),
                ClockSkew = TimeSpan.FromMinutes(5),
                
                // Zero-trust: require all standard claims
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                RequireAudience = true
            };
            
            // Enhanced token validation events for zero-trust
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILogger<JwtBearerEvents>>();
                    var userId = context.Principal?.FindFirst(SecurityClaims.UserId)?.Value ?? "unknown";
                    var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    
                    logger.LogInformation("JWT token validated for user {UserId} from {IpAddress}", userId, ipAddress);
                    return Task.CompletedTask;
                },
                
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILogger<JwtBearerEvents>>();
                    var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    
                    logger.LogWarning("JWT authentication failed from {IpAddress}: {Error}", ipAddress, context.Exception?.Message);
                    return Task.CompletedTask;
                },
                
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILogger<JwtBearerEvents>>();
                    var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    
                    logger.LogWarning("JWT authentication challenge from {IpAddress}: {Error}", ipAddress, context.Error);
                    return Task.CompletedTask;
                }
            };
        });

        // Register zero-trust authorization handlers
        services.AddScoped<IAuthorizationHandler, ZeroTrustAuthorizationHandler<AdminAccessRequirement>>();
        services.AddScoped<IAuthorizationHandler, ZeroTrustAuthorizationHandler<ApiAccessRequirement>>();
        services.AddScoped<IAuthorizationHandler, ZeroTrustAuthorizationHandler<HighPrivilegeRequirement>>();
        services.AddScoped<IAuthorizationHandler, ZeroTrustAuthorizationHandler<SessionValidationRequirement>>();

        // Configure zero-trust authorization policies
        services.AddAuthorization(options =>
        {
            ConfigureZeroTrustPolicies(options, isAdminApi);
        });

        return services;
    }

    public static IServiceCollection AddZeroTrustSecurityForTesting(this IServiceCollection services)
    {
        // Minimal authentication for testing - allows anonymous access
        services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });

        services.AddAuthorization(options =>
        {
            // Override all policies to allow anonymous access for testing
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAssertion(_ => true)
                .Build();
                
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAssertion(_ => true)
                .Build();
        });

        return services;
    }

    private static void ConfigureZeroTrustPolicies(AuthorizationOptions options, bool isAdminApi)
    {
        // CRITICAL: Define fallback policy first (zero-trust requirement)
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new SessionValidationRequirement())
            .Build();

        // Default policy with enhanced zero-trust validation
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new SessionValidationRequirement())
            .Build();

        // Base authentication policies
        options.AddPolicy(SecurityPolicies.AuthenticatedUser, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new SessionValidationRequirement()));

        options.AddPolicy(SecurityPolicies.RequireMfa, policy =>
            policy.RequireAuthenticatedUser()
                  .RequireClaim(SecurityClaims.MfaVerified, "true"));

        options.AddPolicy(SecurityPolicies.RequireSessionValidation, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new SessionValidationRequirement()));

        // Admin policies (medical-grade security)
        if (isAdminApi)
        {
            options.AddPolicy(SecurityPolicies.AdminAccess, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new AdminAccessRequirement()));

            options.AddPolicy(SecurityPolicies.AdminCreate, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new AdminAccessRequirement(SecurityRoles.Admin, new List<string> { "create" })));

            options.AddPolicy(SecurityPolicies.AdminUpdate, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new AdminAccessRequirement(SecurityRoles.Admin, new List<string> { "update" })));

            options.AddPolicy(SecurityPolicies.AdminDelete, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new HighPrivilegeRequirement("delete", SecurityPolicies.AdminDelete)));

            options.AddPolicy(SecurityPolicies.AdminExport, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new HighPrivilegeRequirement("export", SecurityPolicies.AdminExport)));

            options.AddPolicy(SecurityPolicies.AdminSystemAccess, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new AdminAccessRequirement(SecurityRoles.SystemAdmin)));
        }

        // API-specific policies
        options.AddPolicy(SecurityPolicies.ServicesRead, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new ApiAccessRequirement("Services", "Read", SecurityPolicies.ServicesRead)));

        options.AddPolicy(SecurityPolicies.ServicesWrite, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new ApiAccessRequirement("Services", "Write", SecurityPolicies.ServicesWrite)));

        options.AddPolicy(SecurityPolicies.NewsRead, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new ApiAccessRequirement("News", "Read", SecurityPolicies.NewsRead)));

        options.AddPolicy(SecurityPolicies.NewsWrite, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new ApiAccessRequirement("News", "Write", SecurityPolicies.NewsWrite)));

        options.AddPolicy(SecurityPolicies.EventsRead, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new ApiAccessRequirement("Events", "Read", SecurityPolicies.EventsRead)));

        options.AddPolicy(SecurityPolicies.EventsWrite, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new ApiAccessRequirement("Events", "Write", SecurityPolicies.EventsWrite)));

        options.AddPolicy(SecurityPolicies.ResearchRead, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new ApiAccessRequirement("Research", "Read", SecurityPolicies.ResearchRead)));

        options.AddPolicy(SecurityPolicies.ResearchWrite, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new ApiAccessRequirement("Research", "Write", SecurityPolicies.ResearchWrite)));

        options.AddPolicy(SecurityPolicies.ContactsRead, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new ApiAccessRequirement("Contacts", "Read", SecurityPolicies.ContactsRead)));

        options.AddPolicy(SecurityPolicies.ContactsWrite, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new ApiAccessRequirement("Contacts", "Write", SecurityPolicies.ContactsWrite)));

        options.AddPolicy(SecurityPolicies.SearchAccess, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new ApiAccessRequirement("Search", "Read", SecurityPolicies.SearchAccess)));

        options.AddPolicy(SecurityPolicies.NewsletterAccess, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new ApiAccessRequirement("Newsletter", "Write", SecurityPolicies.NewsletterAccess)));

        // High-security policies
        options.AddPolicy(SecurityPolicies.AuditAccess, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new HighPrivilegeRequirement("audit_access", SecurityPolicies.AuditAccess)));

        options.AddPolicy(SecurityPolicies.SystemConfiguration, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new HighPrivilegeRequirement("system_config", SecurityPolicies.SystemConfiguration)));

        options.AddPolicy(SecurityPolicies.UserManagement, policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new HighPrivilegeRequirement("user_management", SecurityPolicies.UserManagement)));
    }
}

// Test authentication handler for integration tests
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // For testing, create a fake authenticated user
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "TestUser"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user-id"),
            new System.Security.Claims.Claim(SecurityClaims.UserId, "test-user-id"),
            new System.Security.Claims.Claim(SecurityClaims.Role, SecurityRoles.Admin),
            new System.Security.Claims.Claim(SecurityClaims.MfaVerified, "true"),
            new System.Security.Claims.Claim(SecurityClaims.SecurityLevel, SecurityLevels.Restricted),
            new System.Security.Claims.Claim(SecurityClaims.AdminLevel, SecurityRoles.Admin),
            new System.Security.Claims.Claim(SecurityClaims.SessionId, "test-session"),
            new System.Security.Claims.Claim(SecurityClaims.ApiAccess, "*")
        };

        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
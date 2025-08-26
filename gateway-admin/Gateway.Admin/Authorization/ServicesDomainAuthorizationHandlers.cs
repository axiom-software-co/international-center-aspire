using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using InternationalCenter.Shared.Security;
using InternationalCenter.Shared.Services;
using InternationalCenter.Shared.Infrastructure.Observability;
using System.Security.Claims;

namespace InternationalCenter.Gateway.Admin.Authorization;

/// <summary>
/// Services domain-specific authorization requirements for Admin Gateway
/// Medical-grade compliance with role-based access control and audit integration
/// </summary>
public class ServicesCreateRequirement : AdminAccessRequirement
{
    public ServicesCreateRequirement() : base(SecurityRoles.Admin, new List<string> { "services:create" })
    {
    }
}

public class ServicesUpdateRequirement : AdminAccessRequirement
{
    public ServicesUpdateRequirement() : base(SecurityRoles.Admin, new List<string> { "services:update" })
    {
    }
}

public class ServicesDeleteRequirement : HighPrivilegeRequirement
{
    public ServicesDeleteRequirement() : base("services:delete", SecurityPolicies.ServicesWrite)
    {
    }
}

public class ServicesReadRequirement : ApiAccessRequirement
{
    public ServicesReadRequirement() : base("Services", "Read", SecurityPolicies.ServicesRead)
    {
    }
}

/// <summary>
/// Services Create authorization handler with medical-grade audit integration
/// Validates ServiceAdmin or SystemAdmin roles with proper audit logging
/// </summary>
public class ServicesCreateAuthorizationHandler : AuthorizationHandler<ServicesCreateRequirement>
{
    private readonly ILogger<ServicesCreateAuthorizationHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditService? _auditService;
    private readonly IVersionService _versionService;

    public ServicesCreateAuthorizationHandler(
        ILogger<ServicesCreateAuthorizationHandler> logger,
        IHttpContextAccessor httpContextAccessor,
        IAuditService? auditService,
        IVersionService versionService)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _auditService = auditService;
        _versionService = versionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        ServicesCreateRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            await LogServicesDomainAuditAsync("SERVICES_AUTH_NO_CONTEXT", "No HTTP context for services create authorization", "Error");
            context.Fail();
            return;
        }

        using var scope = _logger.BeginServiceScope(
            "ServicesDomainAuthorization", 
            "ServicesCreate", 
            httpContext.TraceIdentifier,
            _httpContextAccessor,
            _versionService);

        var user = context.User;
        var userId = GetUserId(user);
        var userRoles = GetUserRoles(user);
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogInformation("Evaluating Services.Create authorization for user {UserId} with roles [{UserRoles}] from {ClientIp}",
            userId, string.Join(",", userRoles), clientIp);

        try
        {
            // Validate authentication
            if (!user.Identity?.IsAuthenticated == true)
            {
                await LogServicesDomainAuditAsync("SERVICES_AUTH_NOT_AUTHENTICATED", 
                    $"User {userId} not authenticated for services create", "Warning");
                context.Fail();
                return;
            }

            // Validate required roles (ServiceAdmin or SystemAdmin)
            var hasRequiredRole = userRoles.Contains("ServiceAdmin") || 
                                 userRoles.Contains("SystemAdmin") ||
                                 user.IsInRole("ServiceAdmin") ||
                                 user.IsInRole("SystemAdmin");

            if (!hasRequiredRole)
            {
                await LogServicesDomainAuditAsync("SERVICES_AUTH_INSUFFICIENT_ROLE", 
                    $"User {userId} with roles [{string.Join(",", userRoles)}] lacks ServiceAdmin role for services create", "Warning");
                context.Fail();
                return;
            }

            // Validate audience claim (Microsoft Entra External ID)
            var audienceClaim = user.FindFirst("aud")?.Value;
            if (string.IsNullOrEmpty(audienceClaim))
            {
                await LogServicesDomainAuditAsync("SERVICES_AUTH_NO_AUDIENCE", 
                    $"User {userId} missing audience claim for services create", "Warning");
                context.Fail();
                return;
            }

            // Medical-grade audit: log successful authorization
            await LogServicesDomainAuditAsync("SERVICES_AUTH_CREATE_SUCCESS", 
                $"User {userId} with roles [{string.Join(",", userRoles)}] authorized for services create from {clientIp}", "Info");

            _logger.LogInformation("Services.Create authorization granted for user {UserId}", userId);
            context.Succeed(requirement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Services.Create authorization for user {UserId}", userId);
            await LogServicesDomainAuditAsync("SERVICES_AUTH_ERROR", 
                $"Authorization error for user {userId}: {ex.Message}", "Error");
            context.Fail();
        }
    }

    private string GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirst("sub")?.Value ?? 
               user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               user.FindFirst("oid")?.Value ?? 
               "unknown";
    }

    private List<string> GetUserRoles(ClaimsPrincipal user)
    {
        return user.FindAll("roles")
                  .Concat(user.FindAll(ClaimTypes.Role))
                  .Select(c => c.Value)
                  .Distinct()
                  .ToList();
    }

    private async Task LogServicesDomainAuditAsync(string action, string details, string severity)
    {
        if (_auditService != null)
        {
            await _auditService.LogSecurityEventAsync(action, details, severity);
        }
        
        // Always log to structured logging for developer visibility
        _logger.LogInformation("SERVICES_DOMAIN_AUDIT: {Action} - {Details} - Severity: {Severity}", 
            action, details, severity);
    }
}

/// <summary>
/// Services Update authorization handler with medical-grade audit integration
/// Validates ServiceAdmin, ServiceEditor or SystemAdmin roles
/// </summary>
public class ServicesUpdateAuthorizationHandler : AuthorizationHandler<ServicesUpdateRequirement>
{
    private readonly ILogger<ServicesUpdateAuthorizationHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditService? _auditService;
    private readonly IVersionService _versionService;

    public ServicesUpdateAuthorizationHandler(
        ILogger<ServicesUpdateAuthorizationHandler> logger,
        IHttpContextAccessor httpContextAccessor,
        IAuditService? auditService,
        IVersionService versionService)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _auditService = auditService;
        _versionService = versionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        ServicesUpdateRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            await LogServicesDomainAuditAsync("SERVICES_AUTH_NO_CONTEXT", "No HTTP context for services update authorization", "Error");
            context.Fail();
            return;
        }

        using var scope = _logger.BeginServiceScope(
            "ServicesDomainAuthorization", 
            "ServicesUpdate", 
            httpContext.TraceIdentifier,
            _httpContextAccessor,
            _versionService);

        var user = context.User;
        var userId = GetUserId(user);
        var userRoles = GetUserRoles(user);
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogInformation("Evaluating Services.Update authorization for user {UserId} with roles [{UserRoles}] from {ClientIp}",
            userId, string.Join(",", userRoles), clientIp);

        try
        {
            // Validate authentication
            if (!user.Identity?.IsAuthenticated == true)
            {
                await LogServicesDomainAuditAsync("SERVICES_AUTH_NOT_AUTHENTICATED", 
                    $"User {userId} not authenticated for services update", "Warning");
                context.Fail();
                return;
            }

            // Validate required roles (ServiceAdmin, ServiceEditor, or SystemAdmin)
            var hasRequiredRole = userRoles.Contains("ServiceAdmin") || 
                                 userRoles.Contains("ServiceEditor") ||
                                 userRoles.Contains("SystemAdmin") ||
                                 user.IsInRole("ServiceAdmin") ||
                                 user.IsInRole("ServiceEditor") ||
                                 user.IsInRole("SystemAdmin");

            if (!hasRequiredRole)
            {
                await LogServicesDomainAuditAsync("SERVICES_AUTH_INSUFFICIENT_ROLE", 
                    $"User {userId} with roles [{string.Join(",", userRoles)}] lacks ServiceAdmin/ServiceEditor role for services update", "Warning");
                context.Fail();
                return;
            }

            // Medical-grade audit: log successful authorization
            await LogServicesDomainAuditAsync("SERVICES_AUTH_UPDATE_SUCCESS", 
                $"User {userId} with roles [{string.Join(",", userRoles)}] authorized for services update from {clientIp}", "Info");

            _logger.LogInformation("Services.Update authorization granted for user {UserId}", userId);
            context.Succeed(requirement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Services.Update authorization for user {UserId}", userId);
            await LogServicesDomainAuditAsync("SERVICES_AUTH_ERROR", 
                $"Authorization error for user {userId}: {ex.Message}", "Error");
            context.Fail();
        }
    }

    private string GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirst("sub")?.Value ?? 
               user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               user.FindFirst("oid")?.Value ?? 
               "unknown";
    }

    private List<string> GetUserRoles(ClaimsPrincipal user)
    {
        return user.FindAll("roles")
                  .Concat(user.FindAll(ClaimTypes.Role))
                  .Select(c => c.Value)
                  .Distinct()
                  .ToList();
    }

    private async Task LogServicesDomainAuditAsync(string action, string details, string severity)
    {
        if (_auditService != null)
        {
            await _auditService.LogSecurityEventAsync(action, details, severity);
        }
        
        // Always log to structured logging for developer visibility
        _logger.LogInformation("SERVICES_DOMAIN_AUDIT: {Action} - {Details} - Severity: {Severity}", 
            action, details, severity);
    }
}

/// <summary>
/// Services Delete authorization handler with high-privilege validation
/// Requires ServiceAdmin or SystemAdmin roles with enhanced medical-grade audit
/// </summary>
public class ServicesDeleteAuthorizationHandler : AuthorizationHandler<ServicesDeleteRequirement>
{
    private readonly ILogger<ServicesDeleteAuthorizationHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditService? _auditService;
    private readonly IVersionService _versionService;

    public ServicesDeleteAuthorizationHandler(
        ILogger<ServicesDeleteAuthorizationHandler> logger,
        IHttpContextAccessor httpContextAccessor,
        IAuditService? auditService,
        IVersionService versionService)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _auditService = auditService;
        _versionService = versionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        ServicesDeleteRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            await LogServicesDomainAuditAsync("SERVICES_AUTH_NO_CONTEXT", "No HTTP context for services delete authorization", "Error");
            context.Fail();
            return;
        }

        using var scope = _logger.BeginServiceScope(
            "ServicesDomainAuthorization", 
            "ServicesDelete", 
            httpContext.TraceIdentifier,
            _httpContextAccessor,
            _versionService);

        var user = context.User;
        var userId = GetUserId(user);
        var userRoles = GetUserRoles(user);
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown";

        // Enhanced logging for high-privilege operation
        _logger.LogWarning("Evaluating HIGH-PRIVILEGE Services.Delete authorization for user {UserId} with roles [{UserRoles}] from {ClientIp}",
            userId, string.Join(",", userRoles), clientIp);

        try
        {
            // Validate authentication
            if (!user.Identity?.IsAuthenticated == true)
            {
                await LogServicesDomainAuditAsync("SERVICES_AUTH_NOT_AUTHENTICATED", 
                    $"User {userId} not authenticated for services delete", "Error");
                context.Fail();
                return;
            }

            // Validate required roles (ServiceAdmin or SystemAdmin only - high privilege)
            var hasRequiredRole = userRoles.Contains("ServiceAdmin") || 
                                 userRoles.Contains("SystemAdmin") ||
                                 user.IsInRole("ServiceAdmin") ||
                                 user.IsInRole("SystemAdmin");

            if (!hasRequiredRole)
            {
                // Critical audit event for failed high-privilege access attempt
                await LogServicesDomainAuditAsync("SERVICES_AUTH_HIGH_PRIVILEGE_DENIED", 
                    $"HIGH-PRIVILEGE DENIED: User {userId} with roles [{string.Join(",", userRoles)}] attempted services delete from {clientIp} using {userAgent}", "Critical");
                
                _logger.LogWarning("HIGH-PRIVILEGE OPERATION DENIED: Services delete for user {UserId} from {ClientIp}", 
                    userId, clientIp);
                context.Fail();
                return;
            }

            // Medical-grade audit: log successful high-privilege authorization
            await LogServicesDomainAuditAsync("SERVICES_AUTH_DELETE_SUCCESS", 
                $"HIGH-PRIVILEGE GRANTED: User {userId} with roles [{string.Join(",", userRoles)}] authorized for services delete from {clientIp} using {userAgent}", "Critical");

            _logger.LogWarning("HIGH-PRIVILEGE Services.Delete authorization granted for user {UserId} from {ClientIp}", 
                userId, clientIp);
            context.Succeed(requirement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during HIGH-PRIVILEGE Services.Delete authorization for user {UserId}", userId);
            await LogServicesDomainAuditAsync("SERVICES_AUTH_HIGH_PRIVILEGE_ERROR", 
                $"High-privilege authorization error for user {userId}: {ex.Message}", "Critical");
            context.Fail();
        }
    }

    private string GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirst("sub")?.Value ?? 
               user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               user.FindFirst("oid")?.Value ?? 
               "unknown";
    }

    private List<string> GetUserRoles(ClaimsPrincipal user)
    {
        return user.FindAll("roles")
                  .Concat(user.FindAll(ClaimTypes.Role))
                  .Select(c => c.Value)
                  .Distinct()
                  .ToList();
    }

    private async Task LogServicesDomainAuditAsync(string action, string details, string severity)
    {
        if (_auditService != null)
        {
            await _auditService.LogSecurityEventAsync(action, details, severity);
        }
        
        // Always log to structured logging for developer visibility
        var logLevel = severity switch
        {
            "Critical" => LogLevel.Critical,
            "Error" => LogLevel.Error,
            "Warning" => LogLevel.Warning,
            _ => LogLevel.Information
        };
        
        _logger.Log(logLevel, "SERVICES_DOMAIN_AUDIT: {Action} - {Details} - Severity: {Severity}", 
            action, details, severity);
    }
}

/// <summary>
/// Services Read authorization handler with basic authentication validation
/// Allows ServiceViewer, ServiceEditor, ServiceAdmin, or SystemAdmin roles
/// </summary>
public class ServicesReadAuthorizationHandler : AuthorizationHandler<ServicesReadRequirement>
{
    private readonly ILogger<ServicesReadAuthorizationHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditService? _auditService;
    private readonly IVersionService _versionService;

    public ServicesReadAuthorizationHandler(
        ILogger<ServicesReadAuthorizationHandler> logger,
        IHttpContextAccessor httpContextAccessor,
        IAuditService? auditService,
        IVersionService versionService)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _auditService = auditService;
        _versionService = versionService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        ServicesReadRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var user = context.User;
        var userId = GetUserId(user);
        var userRoles = GetUserRoles(user);

        // Validate authentication
        if (!user.Identity?.IsAuthenticated == true)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Validate required roles (any admin-level role)
        var hasRequiredRole = userRoles.Contains("ServiceViewer") || 
                             userRoles.Contains("ServiceEditor") ||
                             userRoles.Contains("ServiceAdmin") || 
                             userRoles.Contains("SystemAdmin") ||
                             user.IsInRole("ServiceViewer") ||
                             user.IsInRole("ServiceEditor") ||
                             user.IsInRole("ServiceAdmin") ||
                             user.IsInRole("SystemAdmin");

        if (!hasRequiredRole)
        {
            _logger.LogInformation("Services.Read access denied for user {UserId} with roles [{UserRoles}]", 
                userId, string.Join(",", userRoles));
            context.Fail();
            return Task.CompletedTask;
        }

        _logger.LogDebug("Services.Read authorization granted for user {UserId}", userId);
        context.Succeed(requirement);
        return Task.CompletedTask;
    }

    private string GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirst("sub")?.Value ?? 
               user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               user.FindFirst("oid")?.Value ?? 
               "unknown";
    }

    private List<string> GetUserRoles(ClaimsPrincipal user)
    {
        return user.FindAll("roles")
                  .Concat(user.FindAll(ClaimTypes.Role))
                  .Select(c => c.Value)
                  .Distinct()
                  .ToList();
    }
}
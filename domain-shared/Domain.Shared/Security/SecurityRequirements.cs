using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Shared.Services;
using Shared.Models;
using Shared.Infrastructure.Observability;

namespace Shared.Security;

// Base requirement for zero-trust validation
public abstract class ZeroTrustRequirement : IAuthorizationRequirement
{
    public string PolicyName { get; }
    public string SecurityLevel { get; }
    public bool RequireMfa { get; }
    public bool RequireSessionValidation { get; }

    protected ZeroTrustRequirement(string policyName, string securityLevel = SecurityLevels.Internal, bool requireMfa = false, bool requireSessionValidation = true)
    {
        PolicyName = policyName;
        SecurityLevel = securityLevel;
        RequireMfa = requireMfa;
        RequireSessionValidation = requireSessionValidation;
    }
}

// Medical-grade admin access requirement
public class AdminAccessRequirement : ZeroTrustRequirement
{
    public string MinimumAdminLevel { get; }
    public List<string> RequiredPermissions { get; }
    
    public AdminAccessRequirement(string minimumAdminLevel = SecurityRoles.Admin, List<string>? requiredPermissions = null) 
        : base(SecurityPolicies.AdminAccess, SecurityLevels.Restricted, requireMfa: true, requireSessionValidation: true)
    {
        MinimumAdminLevel = minimumAdminLevel;
        RequiredPermissions = requiredPermissions ?? new List<string>();
    }
}

// High-privilege operation requirement
public class HighPrivilegeRequirement : ZeroTrustRequirement
{
    public string Operation { get; }
    
    public HighPrivilegeRequirement(string operation, string policyName) 
        : base(policyName, SecurityLevels.Restricted, requireMfa: true, requireSessionValidation: true)
    {
        Operation = operation;
    }
}

// API access requirement
public class ApiAccessRequirement : ZeroTrustRequirement
{
    public string ApiName { get; }
    public string AccessType { get; }
    
    public ApiAccessRequirement(string apiName, string accessType, string policyName) 
        : base(policyName, SecurityLevels.Internal, requireMfa: false, requireSessionValidation: true)
    {
        ApiName = apiName;
        AccessType = accessType;
    }
}

// Session validation requirement
public class SessionValidationRequirement : ZeroTrustRequirement
{
    public TimeSpan MaxSessionAge { get; }
    public TimeSpan MaxInactivity { get; }
    
    public SessionValidationRequirement(TimeSpan? maxSessionAge = null, TimeSpan? maxInactivity = null) 
        : base(SecurityPolicies.RequireSessionValidation, SecurityLevels.Internal)
    {
        MaxSessionAge = maxSessionAge ?? TimeSpan.FromHours(8);
        MaxInactivity = maxInactivity ?? TimeSpan.FromMinutes(30);
    }
}

// Zero-trust authorization handler
public class ZeroTrustAuthorizationHandler<TRequirement> : AuthorizationHandler<TRequirement> 
    where TRequirement : ZeroTrustRequirement
{
    private readonly ILogger<ZeroTrustAuthorizationHandler<TRequirement>> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditService? _auditService;
    private readonly IVersionService _versionService;

    public ZeroTrustAuthorizationHandler(
        ILogger<ZeroTrustAuthorizationHandler<TRequirement>> logger,
        IHttpContextAccessor httpContextAccessor,
        IAuditService? auditService,
        IVersionService versionService)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _auditService = auditService;
        _versionService = versionService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            await LogSecurityEventAsync("AUTHORIZATION_NO_HTTP_CONTEXT", $"No HTTP context for policy {requirement.PolicyName}", AuditSeverity.Error);
            context.Fail();
            return;
        }

        using var scope = _logger.BeginServiceScope(
            "ZeroTrustAuthorization", 
            "PolicyEvaluation", 
            httpContext.TraceIdentifier,
            _httpContextAccessor,
            _versionService);

        var securityContext = CreateSecurityContext(context.User, httpContext);
        
        _logger.LogInformation("Evaluating zero-trust policy {PolicyName} for user {UserId} from {IpAddress}",
            requirement.PolicyName, securityContext.UserId, securityContext.IpAddress);

        try
        {
            // Base authentication check
            if (!await ValidateAuthenticationAsync(context.User, requirement, securityContext))
            {
                await LogSecurityEventAsync("AUTHORIZATION_FAILED_AUTH", $"Authentication failed for policy {requirement.PolicyName}", AuditSeverity.Warning);
                context.Fail();
                return;
            }

            // Session validation
            if (requirement.RequireSessionValidation && !await ValidateSessionAsync(securityContext, requirement))
            {
                await LogSecurityEventAsync("AUTHORIZATION_FAILED_SESSION", $"Session validation failed for policy {requirement.PolicyName}", AuditSeverity.Warning);
                context.Fail();
                return;
            }

            // MFA validation
            if (requirement.RequireMfa && !await ValidateMfaAsync(securityContext, requirement))
            {
                await LogSecurityEventAsync("AUTHORIZATION_FAILED_MFA", $"MFA validation failed for policy {requirement.PolicyName}", AuditSeverity.Warning);
                context.Fail();
                return;
            }

            // Security level validation
            if (!await ValidateSecurityLevelAsync(securityContext, requirement))
            {
                await LogSecurityEventAsync("AUTHORIZATION_FAILED_SECURITY_LEVEL", $"Security level validation failed for policy {requirement.PolicyName}", AuditSeverity.Warning);
                context.Fail();
                return;
            }

            // Specific requirement validation
            if (!await ValidateSpecificRequirementAsync(securityContext, requirement))
            {
                await LogSecurityEventAsync("AUTHORIZATION_FAILED_SPECIFIC", $"Specific requirement validation failed for policy {requirement.PolicyName}", AuditSeverity.Warning);
                context.Fail();
                return;
            }

            // Log successful authorization
            await LogSecurityEventAsync("AUTHORIZATION_SUCCESS", $"Policy {requirement.PolicyName} granted for user {securityContext.UserId}", AuditSeverity.Info);
            
            _logger.LogInformation("Zero-trust policy {PolicyName} granted for user {UserId}",
                requirement.PolicyName, securityContext.UserId);

            context.Succeed(requirement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating zero-trust policy {PolicyName} for user {UserId}",
                requirement.PolicyName, securityContext.UserId);
            
            await LogSecurityEventAsync("AUTHORIZATION_ERROR", $"Error evaluating policy {requirement.PolicyName}: {ex.Message}", AuditSeverity.Error);
            context.Fail();
        }
    }

    private SecurityContext CreateSecurityContext(ClaimsPrincipal user, HttpContext httpContext)
    {
        return new SecurityContext
        {
            UserId = user.FindFirst(SecurityClaims.UserId)?.Value ?? 
                     user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                     "anonymous",
            UserName = user.FindFirst(ClaimTypes.Name)?.Value ?? "unknown",
            Role = user.FindFirst(SecurityClaims.Role)?.Value ?? 
                   user.FindFirst(ClaimTypes.Role)?.Value ?? 
                   SecurityRoles.Anonymous,
            SessionId = user.FindFirst(SecurityClaims.SessionId)?.Value ?? httpContext.Session?.Id ?? httpContext.Connection.Id,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            UserAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown",
            DeviceId = user.FindFirst(SecurityClaims.DeviceId)?.Value ?? "unknown",
            SecurityLevel = user.FindFirst(SecurityClaims.SecurityLevel)?.Value ?? SecurityLevels.Public,
            MfaVerified = bool.Parse(user.FindFirst(SecurityClaims.MfaVerified)?.Value ?? "false"),
            AdminLevel = user.FindFirst(SecurityClaims.AdminLevel)?.Value ?? string.Empty,
            Permissions = user.FindAll(SecurityClaims.Permissions).Select(c => c.Value).ToList(),
            ApiAccess = user.FindAll(SecurityClaims.ApiAccess).Select(c => c.Value).ToList(),
            LastActivity = DateTime.TryParse(user.FindFirst(SecurityClaims.LastActivity)?.Value, out var lastActivity) 
                ? lastActivity 
                : DateTime.UtcNow
        };
    }

    private Task<bool> ValidateAuthenticationAsync(ClaimsPrincipal user, TRequirement requirement, SecurityContext securityContext)
    {
        if (!user.Identity?.IsAuthenticated == true)
        {
            _logger.LogWarning("User not authenticated for policy {PolicyName}", requirement.PolicyName);
            return Task.FromResult(false);
        }

        if (string.IsNullOrEmpty(securityContext.UserId) || securityContext.UserId == "anonymous")
        {
            _logger.LogWarning("No valid user ID for policy {PolicyName}", requirement.PolicyName);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private Task<bool> ValidateSessionAsync(SecurityContext securityContext, TRequirement requirement)
    {
        if (requirement is SessionValidationRequirement sessionReq)
        {
            var sessionAge = DateTime.UtcNow - securityContext.LastActivity;
            if (sessionAge > sessionReq.MaxInactivity)
            {
                _logger.LogWarning("Session expired for user {UserId}: inactive for {InactiveTime}", 
                    securityContext.UserId, sessionAge);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    private Task<bool> ValidateMfaAsync(SecurityContext securityContext, TRequirement requirement)
    {
        if (!securityContext.MfaVerified)
        {
            _logger.LogWarning("MFA not verified for user {UserId} for policy {PolicyName}", 
                securityContext.UserId, requirement.PolicyName);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private Task<bool> ValidateSecurityLevelAsync(SecurityContext securityContext, TRequirement requirement)
    {
        var userSecurityLevel = GetSecurityLevelValue(securityContext.SecurityLevel);
        var requiredSecurityLevel = GetSecurityLevelValue(requirement.SecurityLevel);

        if (userSecurityLevel < requiredSecurityLevel)
        {
            _logger.LogWarning("Insufficient security level for user {UserId}: has {UserLevel}, requires {RequiredLevel}", 
                securityContext.UserId, securityContext.SecurityLevel, requirement.SecurityLevel);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private async Task<bool> ValidateSpecificRequirementAsync(SecurityContext securityContext, TRequirement requirement)
    {
        return requirement switch
        {
            AdminAccessRequirement adminReq => await ValidateAdminRequirementAsync(securityContext, adminReq),
            ApiAccessRequirement apiReq => await ValidateApiRequirementAsync(securityContext, apiReq),
            HighPrivilegeRequirement privilegeReq => await ValidatePrivilegeRequirementAsync(securityContext, privilegeReq),
            _ => true
        };
    }

    private Task<bool> ValidateAdminRequirementAsync(SecurityContext securityContext, AdminAccessRequirement requirement)
    {
        var hasAdminRole = IsInRoleHierarchy(securityContext.Role, requirement.MinimumAdminLevel);
        if (!hasAdminRole)
        {
            _logger.LogWarning("User {UserId} does not have required admin role {RequiredRole}", 
                securityContext.UserId, requirement.MinimumAdminLevel);
            return Task.FromResult(false);
        }

        if (requirement.RequiredPermissions.Any() && !requirement.RequiredPermissions.All(p => securityContext.Permissions.Contains(p)))
        {
            _logger.LogWarning("User {UserId} missing required permissions for admin access", securityContext.UserId);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private Task<bool> ValidateApiRequirementAsync(SecurityContext securityContext, ApiAccessRequirement requirement)
    {
        var hasApiAccess = securityContext.ApiAccess.Contains(requirement.ApiName) ||
                          securityContext.ApiAccess.Contains("*") ||
                          securityContext.Role == SecurityRoles.SystemAdmin;

        if (!hasApiAccess)
        {
            _logger.LogWarning("User {UserId} does not have access to API {ApiName}", 
                securityContext.UserId, requirement.ApiName);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private Task<bool> ValidatePrivilegeRequirementAsync(SecurityContext securityContext, HighPrivilegeRequirement requirement)
    {
        var hasPermission = securityContext.Permissions.Contains(requirement.Operation) ||
                           securityContext.Role == SecurityRoles.SystemAdmin;

        if (!hasPermission)
        {
            _logger.LogWarning("User {UserId} does not have permission for high-privilege operation {Operation}", 
                securityContext.UserId, requirement.Operation);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private bool IsInRoleHierarchy(string userRole, string requiredRole)
    {
        var roleHierarchy = new Dictionary<string, int>
        {
            { SecurityRoles.Anonymous, 0 },
            { SecurityRoles.User, 1 },
            { SecurityRoles.Member, 2 },
            { SecurityRoles.ServiceManager, 3 },
            { SecurityRoles.Admin, 4 },
            { SecurityRoles.SuperAdmin, 5 },
            { SecurityRoles.SystemAdmin, 6 }
        };

        var userLevel = roleHierarchy.GetValueOrDefault(userRole, 0);
        var requiredLevel = roleHierarchy.GetValueOrDefault(requiredRole, int.MaxValue);

        return userLevel >= requiredLevel;
    }

    private int GetSecurityLevelValue(string securityLevel)
    {
        return securityLevel switch
        {
            SecurityLevels.Public => 1,
            SecurityLevels.Internal => 2,
            SecurityLevels.Confidential => 3,
            SecurityLevels.Restricted => 4,
            SecurityLevels.TopSecret => 5,
            _ => 0
        };
    }

    private async Task LogSecurityEventAsync(string action, string details, string severity)
    {
        if (_auditService != null)
        {
            await _auditService.LogSecurityEventAsync(action, details, severity);
        }
        else
        {
            _logger.LogWarning("Security event not audited - audit service not available: {Action} - {Details}", action, details);
        }
    }
}
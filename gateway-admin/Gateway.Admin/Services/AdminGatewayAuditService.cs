using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Shared.Services;
using Shared.Models;
using Shared.Repositories;
using Shared.Infrastructure.Observability;
using System.Security.Claims;
using System.Text.Json;

namespace InternationalCenter.Gateway.Admin.Services;

/// <summary>
/// Admin Gateway-specific audit service with enhanced authentication context
/// Provides medical-grade audit logging with user tracking for compliance
/// Integrates seamlessly with Microsoft Entra External ID authentication
/// </summary>
public class AdminGatewayAuditService : IAuditService
{
    private readonly ILogger<AdminGatewayAuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditLogRepository _auditRepository;
    private readonly IVersionService _versionService;
    private readonly JsonSerializerOptions _jsonOptions;
    private AuditContext _currentContext = new();

    public AdminGatewayAuditService(
        ILogger<AdminGatewayAuditService> logger,
        IHttpContextAccessor httpContextAccessor,
        IAuditLogRepository auditRepository,
        IVersionService versionService)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _versionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task LogSecurityEventAsync(string action, string details, string severity = AuditSeverity.Warning)
    {
        var adminContext = CreateAuthenticatedAuditContext();
        
        try
        {
            var auditLog = new AuditLog
            {
                EntityType = "Security",
                EntityId = adminContext.SessionId,
                Action = action,
                UserId = adminContext.UserId,
                UserName = adminContext.UserName,
                CorrelationId = adminContext.CorrelationId,
                TraceId = adminContext.CorrelationId,
                RequestUrl = adminContext.RequestUrl,
                RequestMethod = adminContext.HttpMethod,
                RequestIp = adminContext.ClientIpAddress,
                UserAgent = adminContext.UserAgent,
                AppVersion = _versionService.GetVersion(),
                BuildDate = _versionService.BuildDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                SessionId = adminContext.SessionId,
                ClientApplication = "AdminGateway",
                AdditionalData = details,
                Severity = severity,
                AuditTimestamp = DateTime.UtcNow,
                ProcessingDuration = TimeSpan.Zero,
                IsCriticalAction = true, // All security events are critical
                OldValues = "{}",
                NewValues = JsonSerializer.Serialize(new { UserRoles = adminContext.UserRoles, Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" }, _jsonOptions),
                ChangedProperties = "[]"
            };

            var success = await _auditRepository.CreateAuditLogAsync(auditLog);
            if (success)
            {
                _logger.LogWarning("MEDICAL_GRADE_AUDIT: Security event persisted - Action: {Action} | Severity: {Severity} | UserId: {UserId} | UserRoles: [{UserRoles}] | ClientIp: {ClientIp} | Details: {Details}",
                    action, severity, adminContext.UserId, string.Join(",", adminContext.UserRoles), adminContext.ClientIpAddress, details);
            }
            else
            {
                _logger.LogError("MEDICAL_GRADE_AUDIT: Failed to persist security event - Action: {Action} | UserId: {UserId} | Details: {Details}",
                    action, adminContext.UserId, details);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MEDICAL_GRADE_AUDIT: Exception during security event logging - Action: {Action} | Details: {Details}",
                action, details);
        }
    }

    public async Task LogSystemEventAsync(string action, string details, string severity = AuditSeverity.Info)
    {
        var adminContext = CreateSystemAuditContext();

        try
        {
            var auditLog = new AuditLog
            {
                EntityType = "System",
                EntityId = Environment.MachineName,
                Action = action,
                UserId = "system",
                UserName = "system",
                CorrelationId = adminContext.CorrelationId,
                TraceId = adminContext.CorrelationId,
                RequestUrl = adminContext.RequestUrl,
                RequestMethod = adminContext.HttpMethod,
                RequestIp = adminContext.ClientIpAddress,
                UserAgent = "AdminGateway/System",
                AppVersion = _versionService.GetVersion(),
                BuildDate = _versionService.BuildDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                SessionId = adminContext.SessionId,
                ClientApplication = "AdminGateway",
                AdditionalData = details,
                Severity = severity,
                AuditTimestamp = DateTime.UtcNow,
                ProcessingDuration = TimeSpan.Zero,
                IsCriticalAction = IsCriticalAction(action),
                OldValues = "{}",
                NewValues = JsonSerializer.Serialize(new { SystemComponent = "AdminGateway", Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" }, _jsonOptions),
                ChangedProperties = "[]"
            };

            var success = await _auditRepository.CreateAuditLogAsync(auditLog);
            if (success)
            {
                _logger.LogInformation("MEDICAL_GRADE_AUDIT: System event persisted - Action: {Action} | Severity: {Severity} | CorrelationId: {CorrelationId} | Details: {Details}",
                    action, severity, adminContext.CorrelationId, details);
            }
            else
            {
                _logger.LogError("MEDICAL_GRADE_AUDIT: Failed to persist system event - Action: {Action} | Details: {Details}",
                    action, details);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MEDICAL_GRADE_AUDIT: Exception during system event logging - Action: {Action} | Details: {Details}",
                action, details);
        }
    }

    public async Task LogUserActionAsync(string action, string details, string severity)
    {
        var auditContext = CreateAuthenticatedAuditContext();

        var auditEvent = new
        {
            EventType = "USER_ACTION",
            Action = action,
            Details = details,
            Severity = severity,
            UserId = auditContext.UserId,
            UserName = auditContext.UserName,
            UserRoles = auditContext.UserRoles,
            RequestUrl = auditContext.RequestUrl,
            HttpMethod = auditContext.HttpMethod,
            ClientIp = auditContext.ClientIpAddress,
            UserAgent = auditContext.UserAgent,
            CorrelationId = auditContext.CorrelationId,
            SessionId = auditContext.SessionId,
            Timestamp = DateTime.UtcNow,
            Gateway = "AdminGateway",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        };

        // Enhanced logging for user actions in medical-grade compliance
        _logger.LogInformation("MEDICAL_GRADE_AUDIT: {EventType} | Action: {Action} | Severity: {Severity} | UserId: {UserId} | UserName: {UserName} | UserRoles: [{UserRoles}] | RequestUrl: {RequestUrl} | HttpMethod: {HttpMethod} | ClientIp: {ClientIp} | UserAgent: {UserAgent} | CorrelationId: {CorrelationId} | SessionId: {SessionId} | Details: {Details}",
            auditEvent.EventType,
            auditEvent.Action,
            auditEvent.Severity,
            auditEvent.UserId,
            auditEvent.UserName,
            string.Join(",", auditEvent.UserRoles),
            auditEvent.RequestUrl,
            auditEvent.HttpMethod,
            auditEvent.ClientIp,
            auditEvent.UserAgent,
            auditEvent.CorrelationId,
            auditEvent.SessionId,
            auditEvent.Details);

        await Task.CompletedTask;
    }

    public async Task LogBusinessEventAsync(string action, string entityType, string entityId, object? additionalData = null, string severity = AuditSeverity.Info)
    {
        var adminContext = CreateAuthenticatedAuditContext();
        
        try
        {
            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                UserId = adminContext.UserId,
                UserName = adminContext.UserName,
                CorrelationId = adminContext.CorrelationId,
                TraceId = adminContext.CorrelationId, // Use correlation ID as trace ID in gateway context
                RequestUrl = adminContext.RequestUrl,
                RequestMethod = adminContext.HttpMethod,
                RequestIp = adminContext.ClientIpAddress,
                UserAgent = adminContext.UserAgent,
                AppVersion = _versionService.GetVersion(),
                BuildDate = _versionService.BuildDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                SessionId = adminContext.SessionId,
                ClientApplication = "AdminGateway",
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData, _jsonOptions) : null,
                Severity = severity,
                AuditTimestamp = DateTime.UtcNow,
                ProcessingDuration = TimeSpan.Zero,
                IsCriticalAction = IsCriticalAction(action),
                OldValues = "{}",
                NewValues = JsonSerializer.Serialize(new { UserRoles = adminContext.UserRoles, IsAuthenticated = adminContext.IsAuthenticated }, _jsonOptions),
                ChangedProperties = "[]"
            };

            var success = await _auditRepository.CreateAuditLogAsync(auditLog);
            if (success)
            {
                _logger.LogInformation("MEDICAL_GRADE_AUDIT: Business event persisted - Action: {Action} | EntityType: {EntityType} | EntityId: {EntityId} | UserId: {UserId} | UserRoles: [{UserRoles}]",
                    action, entityType, entityId, adminContext.UserId, string.Join(",", adminContext.UserRoles));
            }
            else
            {
                _logger.LogError("MEDICAL_GRADE_AUDIT: Failed to persist business event - Action: {Action} | EntityType: {EntityType} | EntityId: {EntityId}",
                    action, entityType, entityId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MEDICAL_GRADE_AUDIT: Exception during business event logging - Action: {Action} | EntityType: {EntityType} | EntityId: {EntityId}",
                action, entityType, entityId);
        }
    }

    public async Task<List<AuditLog>> CaptureChangesAsync(ChangeTracker changeTracker, AuditContext auditContext)
    {
        // In Admin Gateway context, this method is not typically used as we don't have direct EF change tracking
        // Gateway focuses on API operation auditing rather than database change tracking
        // Return empty list to satisfy interface contract
        await Task.CompletedTask;
        return new List<AuditLog>();
    }

    public void SetAuditContext(AuditContext context)
    {
        _currentContext = context ?? new AuditContext();
    }

    public AuditContext GetCurrentAuditContext()
    {
        return _currentContext;
    }

    /// <summary>
    /// Creates audit context with authenticated user information from Microsoft Entra External ID
    /// Extracts user context from JWT claims provided by Admin Gateway authentication
    /// </summary>
    private AdminAuditContext CreateAuthenticatedAuditContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return CreateAnonymousAuditContext();
        }

        var user = httpContext.User;
        var isAuthenticated = user?.Identity?.IsAuthenticated == true;

        return new AdminAuditContext
        {
            UserId = isAuthenticated && user != null
                ? GetClaimValue(user, "sub") ?? GetClaimValue(user, ClaimTypes.NameIdentifier) ?? GetClaimValue(user, "oid") ?? "system"
                : "anonymous",
            
            UserName = isAuthenticated && user != null
                ? GetClaimValue(user, "name") ?? GetClaimValue(user, ClaimTypes.Name) ?? GetClaimValue(user, "preferred_username") ?? "unknown"
                : "anonymous",
            
            UserRoles = isAuthenticated && user != null
                ? GetUserRoles(user)
                : new[] { "anonymous" },
            
            ClientIpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            UserAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown",
            RequestUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}{httpContext.Request.QueryString}",
            HttpMethod = httpContext.Request.Method,
            CorrelationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? httpContext.TraceIdentifier,
            SessionId = httpContext.Session?.Id ?? httpContext.Connection.Id ?? "unknown",
            IsAuthenticated = isAuthenticated,
            Timestamp = DateTime.UtcNow
        };
    }

    private AdminAuditContext CreateSystemAuditContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        return new AdminAuditContext
        {
            UserId = "system",
            UserName = "system",
            UserRoles = new[] { "system" },
            ClientIpAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "localhost",
            UserAgent = "AdminGateway/System",
            RequestUrl = httpContext?.Request?.Path.ToString() ?? "/system",
            HttpMethod = httpContext?.Request?.Method ?? "SYSTEM",
            CorrelationId = httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString(),
            SessionId = "system",
            IsAuthenticated = true,
            Timestamp = DateTime.UtcNow
        };
    }

    private AdminAuditContext CreateAnonymousAuditContext()
    {
        return new AdminAuditContext
        {
            UserId = "anonymous",
            UserName = "anonymous",
            UserRoles = new[] { "anonymous" },
            ClientIpAddress = "unknown",
            UserAgent = "unknown",
            RequestUrl = "/unknown",
            HttpMethod = "UNKNOWN",
            CorrelationId = Guid.NewGuid().ToString(),
            SessionId = "unknown",
            IsAuthenticated = false,
            Timestamp = DateTime.UtcNow
        };
    }

    private string? GetClaimValue(ClaimsPrincipal user, string claimType)
    {
        return user?.FindFirst(claimType)?.Value;
    }

    private string[] GetUserRoles(ClaimsPrincipal user)
    {
        if (user == null) return Array.Empty<string>();

        return user.FindAll("roles")
                   .Concat(user.FindAll(ClaimTypes.Role))
                   .Select(c => c.Value)
                   .Distinct()
                   .ToArray();
    }

    private bool IsCriticalAction(string action)
    {
        return action switch
        {
            AuditActions.Delete => true,
            AuditActions.Export => true,
            AuditActions.Import => true,
            AuditActions.Archive => true,
            AuditActions.Login => true,
            AuditActions.Logout => true,
            _ when action.Contains("Security", StringComparison.OrdinalIgnoreCase) => true,
            _ when action.Contains("Admin", StringComparison.OrdinalIgnoreCase) => true,
            _ when action.Contains("GATEWAY", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }
}

/// <summary>
/// Admin Gateway audit context with comprehensive user information for medical-grade compliance
/// Contains all required fields for healthcare audit trail requirements
/// </summary>
public class AdminAuditContext
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string[] UserRoles { get; set; } = Array.Empty<string>();
    public string ClientIpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string RequestUrl { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public bool IsAuthenticated { get; set; }
    public DateTime Timestamp { get; set; }
}
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Shared.Models;
using Shared.Infrastructure.Observability;
using Shared.Repositories;

namespace Shared.Services;

public interface IAuditService
{
    Task<List<AuditLog>> CaptureChangesAsync(ChangeTracker changeTracker, AuditContext auditContext);
    Task LogBusinessEventAsync(string action, string entityType, string entityId, object? additionalData = null, string severity = AuditSeverity.Info);
    Task LogSecurityEventAsync(string action, string details, string severity = AuditSeverity.Warning);
    Task LogSystemEventAsync(string action, string details, string severity = AuditSeverity.Info);
    void SetAuditContext(AuditContext context);
    AuditContext GetCurrentAuditContext();
}

public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;
    private readonly AuditConfiguration _config;
    private readonly IVersionService _versionService;
    private readonly IAuditLogRepository _auditRepository;
    private AuditContext _currentContext = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public AuditService(
        ILogger<AuditService> logger,
        AuditConfiguration config,
        IVersionService versionService,
        IAuditLogRepository auditRepository)
    {
        _logger = logger;
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _versionService = versionService;
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public void SetAuditContext(AuditContext context)
    {
        _currentContext = context ?? new AuditContext();
    }

    public AuditContext GetCurrentAuditContext()
    {
        return _currentContext;
    }

    public async Task<List<AuditLog>> CaptureChangesAsync(ChangeTracker changeTracker, AuditContext auditContext)
    {
        if (!_config.EnableAuditing)
        {
            return new List<AuditLog>();
        }

        var auditLogs = new List<AuditLog>();
        var entries = changeTracker.Entries()
            .Where(e => e.Entity is IAuditable && ShouldAudit(e))
            .ToList();

        foreach (var entry in entries)
        {
            try
            {
                var auditLog = await CreateAuditLogAsync(entry, auditContext);
                if (auditLog != null)
                {
                    auditLogs.Add(auditLog);
                    
                    // Persist audit log immediately for medical-grade compliance
                    var success = await _auditRepository.CreateAuditLogAsync(auditLog);
                    if (!success)
                    {
                        _logger.LogError("Failed to persist audit log for entity {EntityType}:{EntityId}",
                            entry.Entity.GetType().Name, GetEntityId(entry.Entity));
                    }
                }
            }
            catch (Exception ex)
            {
                // Medical-grade compliance: Never fail the main operation due to audit issues
                // But log the audit failure for investigation
                _logger.LogError(ex, "Failed to create audit log for entity {EntityType}:{EntityId}",
                    entry.Entity.GetType().Name, GetEntityId(entry.Entity));
                
                // Create a fallback audit entry to ensure we don't lose audit trail
                var fallbackLog = CreateFallbackAuditLog(entry, auditContext, ex.Message);
                auditLogs.Add(fallbackLog);
                
                // Try to persist the fallback log
                try
                {
                    await _auditRepository.CreateAuditLogAsync(fallbackLog);
                }
                catch (Exception persistEx)
                {
                    _logger.LogCritical(persistEx, "CRITICAL: Failed to persist fallback audit log for entity {EntityType}:{EntityId}",
                        entry.Entity.GetType().Name, GetEntityId(entry.Entity));
                }
            }
        }

        return auditLogs;
    }

    public async Task LogBusinessEventAsync(string action, string entityType, string entityId, object? additionalData = null, string severity = AuditSeverity.Info)
    {
        using var scope = _logger.BeginServiceScope("AuditService", "LogBusinessEvent", _currentContext.CorrelationId);
        
        try
        {
            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                UserId = _currentContext.UserId,
                UserName = _currentContext.UserName,
                CorrelationId = _currentContext.CorrelationId,
                TraceId = _currentContext.TraceId,
                RequestUrl = _currentContext.RequestUrl,
                RequestMethod = _currentContext.RequestMethod,
                RequestIp = _currentContext.RequestIp,
                UserAgent = _currentContext.UserAgent,
                AppVersion = _versionService.GetVersion(),
                BuildDate = _versionService.BuildDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                SessionId = _currentContext.SessionId,
                ClientApplication = _currentContext.ClientApplication,
                Reason = _currentContext.Reason,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData, _jsonOptions) : null,
                Severity = severity,
                AuditTimestamp = DateTime.UtcNow,
                ProcessingDuration = DateTime.UtcNow - _currentContext.RequestStartTime,
                IsCriticalAction = IsCriticalAction(action)
            };

            var success = await _auditRepository.CreateAuditLogAsync(auditLog);
            if (success)
            {
                _logger.LogInformation("Business event logged: {Action} on {EntityType}:{EntityId} by {UserId}",
                    action, entityType, entityId, _currentContext.UserId);
            }
            else
            {
                _logger.LogError("Failed to persist business event audit log: {Action} on {EntityType}:{EntityId}",
                    action, entityType, entityId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log business event: {Action} on {EntityType}:{EntityId}",
                action, entityType, entityId);
        }
    }

    public async Task LogSecurityEventAsync(string action, string details, string severity = AuditSeverity.Warning)
    {
        using var scope = _logger.BeginServiceScope("AuditService", "LogSecurityEvent", _currentContext.CorrelationId);
        
        try
        {
            var auditLog = new AuditLog
            {
                EntityType = "Security",
                EntityId = _currentContext.SessionId,
                Action = action,
                UserId = _currentContext.UserId,
                UserName = _currentContext.UserName,
                CorrelationId = _currentContext.CorrelationId,
                TraceId = _currentContext.TraceId,
                RequestUrl = _currentContext.RequestUrl,
                RequestMethod = _currentContext.RequestMethod,
                RequestIp = _currentContext.RequestIp,
                UserAgent = _currentContext.UserAgent,
                AppVersion = _versionService.GetVersion(),
                BuildDate = _versionService.BuildDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                SessionId = _currentContext.SessionId,
                ClientApplication = _currentContext.ClientApplication,
                AdditionalData = details,
                Severity = severity,
                AuditTimestamp = DateTime.UtcNow,
                ProcessingDuration = DateTime.UtcNow - _currentContext.RequestStartTime,
                IsCriticalAction = true // All security events are critical
            };

            var success = await _auditRepository.CreateAuditLogAsync(auditLog);
            if (success)
            {
                _logger.LogWarning("Security event logged: {Action} - {Details} by {UserId} from {RequestIp}",
                    action, details, _currentContext.UserId, _currentContext.RequestIp);
            }
            else
            {
                _logger.LogError("Failed to persist security event audit log: {Action} - {Details}",
                    action, details);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event: {Action} - {Details}",
                action, details);
        }
    }

    public async Task LogSystemEventAsync(string action, string details, string severity = AuditSeverity.Info)
    {
        using var scope = _logger.BeginServiceScope("AuditService", "LogSystemEvent", _currentContext.CorrelationId);
        
        try
        {
            var auditLog = new AuditLog
            {
                EntityType = "System",
                EntityId = Environment.MachineName,
                Action = action,
                UserId = "system",
                UserName = "system",
                CorrelationId = _currentContext.CorrelationId,
                TraceId = _currentContext.TraceId,
                RequestUrl = "N/A",
                RequestMethod = "N/A",
                RequestIp = "N/A",
                UserAgent = "System Process",
                AppVersion = _versionService.GetVersion(),
                BuildDate = _versionService.BuildDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                SessionId = _currentContext.SessionId,
                AdditionalData = details,
                Severity = severity,
                AuditTimestamp = DateTime.UtcNow,
                ProcessingDuration = TimeSpan.Zero
            };

            var success = await _auditRepository.CreateAuditLogAsync(auditLog);
            if (success)
            {
                _logger.LogInformation("System event logged: {Action} - {Details}",
                    action, details);
            }
            else
            {
                _logger.LogError("Failed to persist system event audit log: {Action} - {Details}",
                    action, details);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log system event: {Action} - {Details}",
                action, details);
        }
    }

    private Task<AuditLog?> CreateAuditLogAsync(EntityEntry entry, AuditContext auditContext)
    {
        var action = GetAuditAction(entry.State);
        if (string.IsNullOrEmpty(action) || !ShouldAuditAction(action))
        {
            return Task.FromResult<AuditLog?>(null);
        }

        var entityType = entry.Entity.GetType().Name;
        var entityId = GetEntityId(entry.Entity);
        
        var auditLog = new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = auditContext.UserId,
            UserName = auditContext.UserName,
            CorrelationId = auditContext.CorrelationId,
            TraceId = auditContext.TraceId,
            RequestUrl = auditContext.RequestUrl,
            RequestMethod = auditContext.RequestMethod,
            RequestIp = auditContext.RequestIp,
            UserAgent = auditContext.UserAgent,
            AppVersion = _versionService.GetVersion(),
            BuildDate = _versionService.BuildDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            SessionId = auditContext.SessionId,
            ClientApplication = auditContext.ClientApplication,
            Reason = auditContext.Reason,
            AuditTimestamp = DateTime.UtcNow,
            ProcessingDuration = DateTime.UtcNow - auditContext.RequestStartTime,
            IsCriticalAction = IsCriticalAction(action),
            Severity = GetAuditSeverity(action, entityType)
        };

        // Capture old and new values for updates
        if (entry.State == EntityState.Modified)
        {
            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();
            var changedProperties = new List<string>();

            foreach (var property in entry.Properties.Where(p => p.IsModified))
            {
                var propertyName = property.Metadata.Name;
                
                if (_config.ExcludedProperties.Contains(propertyName))
                    continue;

                changedProperties.Add(propertyName);
                
                // Handle sensitive properties
                if (_config.SensitiveProperties.Any(s => propertyName.Contains(s, StringComparison.OrdinalIgnoreCase)))
                {
                    oldValues[propertyName] = "[REDACTED]";
                    newValues[propertyName] = "[REDACTED]";
                }
                else
                {
                    oldValues[propertyName] = property.OriginalValue;
                    newValues[propertyName] = property.CurrentValue;
                }
            }

            auditLog.OldValues = JsonSerializer.Serialize(oldValues, _jsonOptions);
            auditLog.NewValues = JsonSerializer.Serialize(newValues, _jsonOptions);
            auditLog.ChangedProperties = JsonSerializer.Serialize(changedProperties, _jsonOptions);
        }
        else if (entry.State == EntityState.Added)
        {
            var newValues = new Dictionary<string, object?>();
            foreach (var property in entry.Properties)
            {
                var propertyName = property.Metadata.Name;
                
                if (_config.ExcludedProperties.Contains(propertyName))
                    continue;

                if (_config.SensitiveProperties.Any(s => propertyName.Contains(s, StringComparison.OrdinalIgnoreCase)))
                {
                    newValues[propertyName] = "[REDACTED]";
                }
                else
                {
                    newValues[propertyName] = property.CurrentValue;
                }
            }

            auditLog.NewValues = JsonSerializer.Serialize(newValues, _jsonOptions);
        }

        return Task.FromResult<AuditLog?>(auditLog);
    }

    private AuditLog CreateFallbackAuditLog(EntityEntry entry, AuditContext auditContext, string errorMessage)
    {
        return new AuditLog
        {
            EntityType = entry.Entity.GetType().Name,
            EntityId = GetEntityId(entry.Entity),
            Action = "AUDIT_ERROR",
            UserId = auditContext.UserId,
            UserName = auditContext.UserName,
            CorrelationId = auditContext.CorrelationId,
            TraceId = auditContext.TraceId,
            RequestUrl = auditContext.RequestUrl,
            RequestMethod = auditContext.RequestMethod,
            RequestIp = auditContext.RequestIp,
            UserAgent = auditContext.UserAgent,
            AppVersion = _versionService.GetVersion(),
            BuildDate = _versionService.BuildDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            SessionId = auditContext.SessionId,
            AdditionalData = $"Audit capture failed: {errorMessage}",
            Severity = AuditSeverity.Error,
            AuditTimestamp = DateTime.UtcNow,
            ProcessingDuration = DateTime.UtcNow - auditContext.RequestStartTime,
            IsCriticalAction = true
        };
    }

    private bool ShouldAudit(EntityEntry entry)
    {
        return entry.State switch
        {
            EntityState.Added => _config.AuditCreates,
            EntityState.Modified => _config.AuditUpdates,
            EntityState.Deleted => _config.AuditDeletes,
            _ => false
        };
    }

    private bool ShouldAuditAction(string action)
    {
        return action switch
        {
            AuditActions.Create => _config.AuditCreates,
            AuditActions.Update => _config.AuditUpdates,
            AuditActions.Delete => _config.AuditDeletes,
            AuditActions.Read => _config.AuditReads,
            _ => true
        };
    }

    private string GetAuditAction(EntityState state)
    {
        return state switch
        {
            EntityState.Added => AuditActions.Create,
            EntityState.Modified => AuditActions.Update,
            EntityState.Deleted => AuditActions.Delete,
            _ => string.Empty
        };
    }

    private string GetEntityId(object entity)
    {
        return entity switch
        {
            IAuditable auditable => auditable.Id,
            _ => "unknown"
        };
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
            _ => false
        };
    }

    private string GetAuditSeverity(string action, string entityType)
    {
        if (IsCriticalAction(action))
            return AuditSeverity.Critical;
        
        if (action == AuditActions.Delete)
            return AuditSeverity.Warning;
        
        return AuditSeverity.Info;
    }
}
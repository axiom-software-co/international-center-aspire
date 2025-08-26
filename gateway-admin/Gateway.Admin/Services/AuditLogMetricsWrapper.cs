using System.Diagnostics;
using Service.Audit.Abstractions;
using Service.Audit.Models;

namespace Gateway.Admin.Services;

public class AuditLogMetricsWrapper : IAuditService
{
    private readonly IAuditService _auditService;
    private readonly AdminGatewayMetricsService _metricsService;
    private readonly ILogger<AuditLogMetricsWrapper> _logger;

    public AuditLogMetricsWrapper(
        IAuditService auditService,
        AdminGatewayMetricsService metricsService,
        ILogger<AuditLogMetricsWrapper> logger)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuditEvent> LogAsync(
        AuditEventType eventType,
        string userId,
        string resourceId,
        object? beforeState = null,
        object? afterState = null,
        string? correlationId = null,
        Dictionary<string, object>? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        AuditEvent? result = null;

        try
        {
            _logger.LogDebug("Starting audit log creation: eventType={EventType}, userId={UserId}, resourceId={ResourceId}, correlationId={CorrelationId}",
                eventType, userId, resourceId, correlationId);

            result = await _auditService.LogAsync(eventType, userId, resourceId, beforeState, afterState, correlationId, additionalData, cancellationToken);
            success = true;

            _logger.LogDebug("Audit log created successfully: auditId={AuditId}, eventType={EventType}, userId={UserId}, latency={Latency}ms",
                result.Id, eventType, userId, stopwatch.Elapsed.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Failed to create audit log: eventType={EventType}, userId={UserId}, resourceId={ResourceId}, correlationId={CorrelationId}",
                eventType, userId, resourceId, correlationId);

            // This is critical for medical compliance - audit log failures must be tracked
            _metricsService.RecordComplianceViolation("audit_log_failure", userId, 
                $"Failed to create audit log for {eventType}: {ex.Message}", "critical");

            throw;
        }
        finally
        {
            stopwatch.Stop();
            var latencySeconds = stopwatch.Elapsed.TotalSeconds;

            // Record audit metrics regardless of success/failure
            _metricsService.RecordAuditLogEntry(eventType.ToString(), userId, latencySeconds, success);

            // Update audit backlog metrics if this was a failure
            if (!success)
            {
                // In a real implementation, you might query the audit service for actual backlog size
                // For now, we'll use a placeholder approach
                UpdateAuditBacklogMetrics();
            }

            _logger.LogDebug("Audit log metrics recorded: eventType={EventType}, userId={UserId}, success={Success}, latency={Latency}ms",
                eventType, userId, success, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    public async Task<AuditEvent?> GetByIdAsync(Guid auditId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var result = await _auditService.GetByIdAsync(auditId, cancellationToken);
            success = result != null;
            
            _logger.LogDebug("Audit log retrieval: auditId={AuditId}, found={Found}, latency={Latency}ms",
                auditId, success, stopwatch.Elapsed.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Failed to retrieve audit log: auditId={AuditId}", auditId);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            // Record as a query operation for tracking purposes
            _metricsService.RecordAuditLogEntry("QUERY", "system", stopwatch.Elapsed.TotalSeconds, success);
        }
    }

    public async Task<IEnumerable<AuditEvent>> GetByUserIdAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int take = 100,
        int skip = 0,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        var resultCount = 0;

        try
        {
            var results = await _auditService.GetByUserIdAsync(userId, fromDate, toDate, take, skip, cancellationToken);
            var resultList = results.ToList();
            resultCount = resultList.Count;
            success = true;

            _logger.LogDebug("Audit log user query: userId={UserId}, resultCount={Count}, take={Take}, skip={Skip}, latency={Latency}ms",
                userId, resultCount, take, skip, stopwatch.Elapsed.TotalMilliseconds);

            return resultList;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Failed to query audit logs by user: userId={UserId}", userId);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordAuditLogEntry("USER_QUERY", userId, stopwatch.Elapsed.TotalSeconds, success);
            
            // Record user activity for compliance tracking
            if (success)
            {
                _metricsService.RecordUserActivity(userId, "AUDIT_LOG_QUERY", $"Retrieved {resultCount} audit entries");
            }
        }
    }

    public async Task<IEnumerable<AuditEvent>> GetByResourceIdAsync(
        string resourceId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int take = 100,
        int skip = 0,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        var resultCount = 0;

        try
        {
            var results = await _auditService.GetByResourceIdAsync(resourceId, fromDate, toDate, take, skip, cancellationToken);
            var resultList = results.ToList();
            resultCount = resultList.Count;
            success = true;

            _logger.LogDebug("Audit log resource query: resourceId={ResourceId}, resultCount={Count}, latency={Latency}ms",
                resourceId, resultCount, stopwatch.Elapsed.TotalMilliseconds);

            return resultList;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Failed to query audit logs by resource: resourceId={ResourceId}", resourceId);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordAuditLogEntry("RESOURCE_QUERY", "system", stopwatch.Elapsed.TotalSeconds, success);
        }
    }

    public async Task<IEnumerable<AuditEvent>> GetByEventTypeAsync(
        AuditEventType eventType,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int take = 100,
        int skip = 0,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        var resultCount = 0;

        try
        {
            var results = await _auditService.GetByEventTypeAsync(eventType, fromDate, toDate, take, skip, cancellationToken);
            var resultList = results.ToList();
            resultCount = resultList.Count;
            success = true;

            _logger.LogDebug("Audit log event type query: eventType={EventType}, resultCount={Count}, latency={Latency}ms",
                eventType, resultCount, stopwatch.Elapsed.TotalMilliseconds);

            return resultList;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Failed to query audit logs by event type: eventType={EventType}", eventType);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordAuditLogEntry("EVENT_TYPE_QUERY", "system", stopwatch.Elapsed.TotalSeconds, success);
        }
    }

    public async Task<IEnumerable<AuditEvent>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        var resultCount = 0;

        try
        {
            var results = await _auditService.GetByCorrelationIdAsync(correlationId, cancellationToken);
            var resultList = results.ToList();
            resultCount = resultList.Count;
            success = true;

            _logger.LogDebug("Audit log correlation query: correlationId={CorrelationId}, resultCount={Count}, latency={Latency}ms",
                correlationId, resultCount, stopwatch.Elapsed.TotalMilliseconds);

            return resultList;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Failed to query audit logs by correlation ID: correlationId={CorrelationId}", correlationId);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordAuditLogEntry("CORRELATION_QUERY", "system", stopwatch.Elapsed.TotalSeconds, success);
        }
    }

    public async Task<bool> ValidateIntegrityAsync(
        Guid auditId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        var isValid = false;

        try
        {
            isValid = await _auditService.ValidateIntegrityAsync(auditId, cancellationToken);
            success = true;

            if (!isValid)
            {
                // Integrity validation failure is a critical compliance violation
                _metricsService.RecordComplianceViolation("audit_integrity_violation", "system", 
                    $"Audit log integrity validation failed for audit ID {auditId}", "critical");
                
                _logger.LogWarning("Audit log integrity validation failed: auditId={AuditId}", auditId);
            }

            _logger.LogDebug("Audit log integrity validation: auditId={AuditId}, isValid={IsValid}, latency={Latency}ms",
                auditId, isValid, stopwatch.Elapsed.TotalMilliseconds);

            return isValid;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Failed to validate audit log integrity: auditId={AuditId}", auditId);
            
            // Failed integrity validation is also a compliance violation
            _metricsService.RecordComplianceViolation("audit_integrity_check_failure", "system",
                $"Failed to validate audit log integrity for audit ID {auditId}: {ex.Message}", "critical");
            
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordAuditLogEntry("INTEGRITY_VALIDATION", "system", stopwatch.Elapsed.TotalSeconds, success);
        }
    }

    public async Task<long> GetCountAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? userId = null,
        AuditEventType? eventType = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        long count = 0;

        try
        {
            count = await _auditService.GetCountAsync(fromDate, toDate, userId, eventType, cancellationToken);
            success = true;

            _logger.LogDebug("Audit log count query: count={Count}, userId={UserId}, eventType={EventType}, latency={Latency}ms",
                count, userId ?? "null", eventType?.ToString() ?? "null", stopwatch.Elapsed.TotalMilliseconds);

            return count;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Failed to get audit log count: userId={UserId}, eventType={EventType}", userId, eventType);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordAuditLogEntry("COUNT_QUERY", userId ?? "system", stopwatch.Elapsed.TotalSeconds, success);
        }
    }

    private void UpdateAuditBacklogMetrics()
    {
        // In a real implementation, you would query the audit service or database for actual backlog size
        // For now, we'll use a placeholder approach to track failures
        try
        {
            // This would typically be implemented by querying failed audit operations from a queue or database
            // _metricsService.UpdateAuditBacklogSize(actualBacklogSize);
            
            _logger.LogDebug("Updated audit backlog metrics");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update audit backlog metrics");
        }
    }
}
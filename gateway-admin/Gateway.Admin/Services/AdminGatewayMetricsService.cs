using Infrastructure.Metrics.Abstractions;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Claims;

namespace Gateway.Admin.Services;

public sealed class AdminGatewayMetricsService : IDisposable
{
    private readonly ICustomMetricsRegistry _metricsRegistry;
    private readonly IPrometheusMetricsExporter _prometheusExporter;
    private readonly ILogger<AdminGatewayMetricsService> _logger;
    
    private readonly Meter _meter;
    
    // Authentication metrics
    private readonly Counter<long> _authenticationAttemptsCounter;
    private readonly Counter<long> _authenticationSuccessCounter;
    private readonly Counter<long> _authenticationFailuresCounter;
    private readonly Histogram<double> _authenticationDuration;
    private readonly Counter<long> _tokenValidationCounter;
    private readonly Histogram<double> _tokenValidationDuration;
    
    // Authorization (RBAC) metrics
    private readonly Counter<long> _authorizationChecksCounter;
    private readonly Counter<long> _authorizationSuccessCounter;
    private readonly Counter<long> _authorizationFailuresCounter;
    private readonly Histogram<double> _authorizationDecisionDuration;
    private readonly Counter<long> _rbacPolicyEvaluationsCounter;
    private readonly Counter<long> _rbacViolationsCounter;
    
    // User session metrics
    private readonly Gauge<int> _activeSessionsCount;
    private readonly Counter<long> _sessionCreationCounter;
    private readonly Counter<long> _sessionDestructionCounter;
    private readonly Histogram<double> _sessionDuration;
    private readonly Counter<long> _userActivityCounter;
    
    // Medical-grade audit metrics
    private readonly Counter<long> _auditLogEntriesCounter;
    private readonly Histogram<double> _auditLogLatency;
    private readonly Counter<long> _auditLogErrorsCounter;
    private readonly Gauge<long> _auditLogBacklogSize;
    private readonly Counter<long> _complianceViolationsCounter;
    
    // Gateway-specific admin metrics
    private readonly Counter<long> _adminRequestsCounter;
    private readonly Histogram<double> _adminRequestDuration;
    private readonly Counter<long> _adminResponsesCounter;
    private readonly Counter<long> _securityViolationsCounter;
    private readonly Counter<long> _medicalComplianceEventsCounter;
    
    private int _currentActiveSessions = 0;
    private long _currentAuditBacklog = 0;
    
    public AdminGatewayMetricsService(
        ICustomMetricsRegistry metricsRegistry,
        IPrometheusMetricsExporter prometheusExporter,
        ILogger<AdminGatewayMetricsService> logger)
    {
        _metricsRegistry = metricsRegistry ?? throw new ArgumentNullException(nameof(metricsRegistry));
        _prometheusExporter = prometheusExporter ?? throw new ArgumentNullException(nameof(prometheusExporter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _meter = _metricsRegistry.CreateMeter("Gateway.Admin", "1.0.0");
        
        // Initialize authentication instruments
        _authenticationAttemptsCounter = _meter.CreateCounter<long>(
            "gateway_admin_authentication_attempts_total",
            "count",
            "Total number of authentication attempts on the Admin Gateway");
            
        _authenticationSuccessCounter = _meter.CreateCounter<long>(
            "gateway_admin_authentication_success_total",
            "count",
            "Total number of successful authentications on the Admin Gateway");
            
        _authenticationFailuresCounter = _meter.CreateCounter<long>(
            "gateway_admin_authentication_failures_total",
            "count",
            "Total number of failed authentications on the Admin Gateway");
            
        _authenticationDuration = _meter.CreateHistogram<double>(
            "gateway_admin_authentication_duration_seconds",
            "seconds",
            "Duration of authentication processes on the Admin Gateway");
            
        _tokenValidationCounter = _meter.CreateCounter<long>(
            "gateway_admin_token_validations_total",
            "count",
            "Total number of token validations performed");
            
        _tokenValidationDuration = _meter.CreateHistogram<double>(
            "gateway_admin_token_validation_duration_seconds",
            "seconds",
            "Duration of token validation processes");
            
        // Initialize authorization (RBAC) instruments
        _authorizationChecksCounter = _meter.CreateCounter<long>(
            "gateway_admin_authorization_checks_total",
            "count",
            "Total number of authorization checks performed");
            
        _authorizationSuccessCounter = _meter.CreateCounter<long>(
            "gateway_admin_authorization_success_total",
            "count",
            "Total number of successful authorization decisions");
            
        _authorizationFailuresCounter = _meter.CreateCounter<long>(
            "gateway_admin_authorization_failures_total",
            "count",
            "Total number of failed authorization decisions");
            
        _authorizationDecisionDuration = _meter.CreateHistogram<double>(
            "gateway_admin_authorization_decision_duration_seconds",
            "seconds",
            "Duration of authorization decision processes");
            
        _rbacPolicyEvaluationsCounter = _meter.CreateCounter<long>(
            "gateway_admin_rbac_policy_evaluations_total",
            "count",
            "Total number of RBAC policy evaluations");
            
        _rbacViolationsCounter = _meter.CreateCounter<long>(
            "gateway_admin_rbac_violations_total",
            "count",
            "Total number of RBAC policy violations");
            
        // Initialize user session instruments
        _activeSessionsCount = _meter.CreateGauge<int>(
            "gateway_admin_active_sessions",
            "count",
            "Current number of active user sessions");
            
        _sessionCreationCounter = _meter.CreateCounter<long>(
            "gateway_admin_sessions_created_total",
            "count",
            "Total number of user sessions created");
            
        _sessionDestructionCounter = _meter.CreateCounter<long>(
            "gateway_admin_sessions_destroyed_total",
            "count",
            "Total number of user sessions destroyed");
            
        _sessionDuration = _meter.CreateHistogram<double>(
            "gateway_admin_session_duration_seconds",
            "seconds",
            "Duration of user sessions");
            
        _userActivityCounter = _meter.CreateCounter<long>(
            "gateway_admin_user_activity_total",
            "count",
            "Total number of user activities tracked");
            
        // Initialize audit instruments
        _auditLogEntriesCounter = _meter.CreateCounter<long>(
            "gateway_admin_audit_log_entries_total",
            "count",
            "Total number of audit log entries created");
            
        _auditLogLatency = _meter.CreateHistogram<double>(
            "gateway_admin_audit_log_latency_seconds",
            "seconds",
            "Latency of audit log entry creation and persistence");
            
        _auditLogErrorsCounter = _meter.CreateCounter<long>(
            "gateway_admin_audit_log_errors_total",
            "count",
            "Total number of audit log errors");
            
        _auditLogBacklogSize = _meter.CreateGauge<long>(
            "gateway_admin_audit_log_backlog_size",
            "count",
            "Current size of audit log processing backlog");
            
        _complianceViolationsCounter = _meter.CreateCounter<long>(
            "gateway_admin_compliance_violations_total",
            "count",
            "Total number of medical compliance violations detected");
            
        // Initialize gateway-specific instruments
        _adminRequestsCounter = _meter.CreateCounter<long>(
            "gateway_admin_requests_total",
            "count",
            "Total number of admin requests received");
            
        _adminRequestDuration = _meter.CreateHistogram<double>(
            "gateway_admin_request_duration_seconds",
            "seconds",
            "Duration of admin requests");
            
        _adminResponsesCounter = _meter.CreateCounter<long>(
            "gateway_admin_responses_total",
            "count",
            "Total number of admin responses sent");
            
        _securityViolationsCounter = _meter.CreateCounter<long>(
            "gateway_admin_security_violations_total",
            "count",
            "Total number of security violations detected");
            
        _medicalComplianceEventsCounter = _meter.CreateCounter<long>(
            "gateway_admin_medical_compliance_events_total",
            "count",
            "Total number of medical compliance events tracked");
            
        _logger.LogInformation("AdminGatewayMetricsService initialized with meter: {MeterName}", _meter.Name);
    }
    
    public void RecordAuthenticationAttempt(string scheme, string userId, string clientIp)
    {
        var tags = new TagList
        {
            ["auth_scheme"] = scheme,
            ["gateway"] = "admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _authenticationAttemptsCounter.Add(1, tags);
        _logger.LogDebug("Authentication attempt recorded: scheme={Scheme}, userId={UserId}, clientIp={ClientIp}", 
            scheme, userId, clientIp);
    }
    
    public void RecordAuthenticationResult(string scheme, string userId, bool success, double durationSeconds, string failureReason = "")
    {
        var tags = new TagList
        {
            ["auth_scheme"] = scheme,
            ["result"] = success ? "success" : "failure",
            ["gateway"] = "admin",
            ["compliance_level"] = "medical_grade"
        };
        
        if (success)
        {
            _authenticationSuccessCounter.Add(1, tags);
        }
        else
        {
            tags["failure_reason"] = failureReason.ToLowerInvariant().Replace(" ", "_");
            _authenticationFailuresCounter.Add(1, tags);
        }
        
        _authenticationDuration.Record(durationSeconds, tags);
        
        _logger.LogDebug("Authentication result recorded: success={Success}, userId={UserId}, duration={Duration}ms",
            success, userId, durationSeconds * 1000);
    }
    
    public void RecordTokenValidation(string tokenType, bool success, double durationSeconds)
    {
        var tags = new TagList
        {
            ["token_type"] = tokenType,
            ["result"] = success ? "success" : "failure",
            ["gateway"] = "admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _tokenValidationCounter.Add(1, tags);
        _tokenValidationDuration.Record(durationSeconds, tags);
        
        _logger.LogDebug("Token validation recorded: type={TokenType}, success={Success}, duration={Duration}ms",
            tokenType, success, durationSeconds * 1000);
    }
    
    public void RecordAuthorizationCheck(string policy, string userId, string[] roles, bool allowed, double durationSeconds, string resource = "")
    {
        var tags = new TagList
        {
            ["policy"] = policy,
            ["result"] = allowed ? "allowed" : "denied",
            ["gateway"] = "admin",
            ["compliance_level"] = "medical_grade"
        };
        
        if (!string.IsNullOrEmpty(resource))
        {
            tags["resource"] = SanitizeResourcePath(resource);
        }
        
        _authorizationChecksCounter.Add(1, tags);
        _authorizationDecisionDuration.Record(durationSeconds, tags);
        
        if (allowed)
        {
            _authorizationSuccessCounter.Add(1, tags);
        }
        else
        {
            _authorizationFailuresCounter.Add(1, tags);
            _rbacViolationsCounter.Add(1, tags);
        }
        
        _logger.LogDebug("Authorization check recorded: policy={Policy}, userId={UserId}, allowed={Allowed}, duration={Duration}ms",
            policy, userId, allowed, durationSeconds * 1000);
    }
    
    public void RecordRbacPolicyEvaluation(string policyName, string userId, string[] userRoles, bool success, double durationSeconds)
    {
        var tags = new TagList
        {
            ["policy_name"] = policyName,
            ["result"] = success ? "success" : "failure",
            ["user_role_count"] = userRoles.Length.ToString(),
            ["gateway"] = "admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _rbacPolicyEvaluationsCounter.Add(1, tags);
        
        if (!success)
        {
            _rbacViolationsCounter.Add(1, tags);
        }
        
        _logger.LogDebug("RBAC policy evaluation recorded: policy={Policy}, userId={UserId}, success={Success}",
            policyName, userId, success);
    }
    
    public void RecordUserSessionCreated(string userId, string sessionId, string clientIp)
    {
        var tags = new TagList
        {
            ["gateway"] = "admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _sessionCreationCounter.Add(1, tags);
        
        Interlocked.Increment(ref _currentActiveSessions);
        _activeSessionsCount.Record(_currentActiveSessions, tags);
        
        _logger.LogDebug("User session created: userId={UserId}, sessionId={SessionId}, clientIp={ClientIp}",
            userId, sessionId, clientIp);
    }
    
    public void RecordUserSessionDestroyed(string userId, string sessionId, double sessionDurationSeconds, string reason = "logout")
    {
        var tags = new TagList
        {
            ["destruction_reason"] = reason.ToLowerInvariant(),
            ["gateway"] = "admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _sessionDestructionCounter.Add(1, tags);
        _sessionDuration.Record(sessionDurationSeconds, tags);
        
        Interlocked.Decrement(ref _currentActiveSessions);
        _activeSessionsCount.Record(_currentActiveSessions, tags);
        
        _logger.LogDebug("User session destroyed: userId={UserId}, sessionId={SessionId}, duration={Duration}s, reason={Reason}",
            userId, sessionId, sessionDurationSeconds, reason);
    }
    
    public void RecordUserActivity(string userId, string activityType, string resource = "")
    {
        var tags = new TagList
        {
            ["activity_type"] = activityType.ToLowerInvariant().Replace(" ", "_"),
            ["gateway"] = "admin",
            ["compliance_level"] = "medical_grade"
        };
        
        if (!string.IsNullOrEmpty(resource))
        {
            tags["resource"] = SanitizeResourcePath(resource);
        }
        
        _userActivityCounter.Add(1, tags);
        
        _logger.LogDebug("User activity recorded: userId={UserId}, activity={Activity}, resource={Resource}",
            userId, activityType, resource);
    }
    
    public void RecordAuditLogEntry(string eventType, string userId, double latencySeconds, bool success)
    {
        var tags = new TagList
        {
            ["event_type"] = eventType.ToLowerInvariant().Replace(" ", "_"),
            ["result"] = success ? "success" : "failure",
            ["gateway"] = "admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _auditLogEntriesCounter.Add(1, tags);
        _auditLogLatency.Record(latencySeconds, tags);
        
        if (!success)
        {
            _auditLogErrorsCounter.Add(1, tags);
            Interlocked.Increment(ref _currentAuditBacklog);
        }
        
        _auditLogBacklogSize.Record(_currentAuditBacklog, tags);
        
        _logger.LogDebug("Audit log entry recorded: eventType={EventType}, userId={UserId}, success={Success}, latency={Latency}ms",
            eventType, userId, success, latencySeconds * 1000);
    }
    
    public void RecordComplianceViolation(string violationType, string userId, string details, string severity = "medium")
    {
        var tags = new TagList
        {
            ["violation_type"] = violationType.ToLowerInvariant().Replace(" ", "_"),
            ["severity"] = severity.ToLowerInvariant(),
            ["gateway"] = "admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _complianceViolationsCounter.Add(1, tags);
        _medicalComplianceEventsCounter.Add(1, tags);
        
        _logger.LogWarning("Medical compliance violation recorded: type={ViolationType}, userId={UserId}, severity={Severity}, details={Details}",
            violationType, userId, severity, details);
    }
    
    public void RecordAdminRequest(string method, string path, string userId, string[] userRoles, string clientIp)
    {
        var tags = new TagList
        {
            ["method"] = method,
            ["path"] = SanitizeResourcePath(path),
            ["user_role_count"] = userRoles.Length.ToString(),
            ["gateway"] = "admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _adminRequestsCounter.Add(1, tags);
        
        _logger.LogDebug("Admin request recorded: {Method} {Path} by userId={UserId} with roles=[{Roles}] from {ClientIp}",
            method, path, userId, string.Join(",", userRoles), clientIp);
    }
    
    public void RecordAdminResponse(string method, string path, int statusCode, double durationSeconds, string userId)
    {
        var tags = new TagList
        {
            ["method"] = method,
            ["path"] = SanitizeResourcePath(path),
            ["status_code"] = statusCode.ToString(),
            ["status_class"] = GetStatusClass(statusCode),
            ["gateway"] = "admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _adminResponsesCounter.Add(1, tags);
        _adminRequestDuration.Record(durationSeconds, tags);
        
        _logger.LogDebug("Admin response recorded: {StatusCode} for {Method} {Path} by userId={UserId} in {Duration}ms",
            statusCode, method, path, userId, durationSeconds * 1000);
    }
    
    public void RecordSecurityViolation(string violationType, string userId, string clientIp, string details = "")
    {
        var tags = new TagList
        {
            ["violation_type"] = violationType.ToLowerInvariant().Replace(" ", "_"),
            ["gateway"] = "admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _securityViolationsCounter.Add(1, tags);
        _medicalComplianceEventsCounter.Add(1, tags);
        
        _logger.LogWarning("Security violation recorded: type={ViolationType}, userId={UserId}, clientIp={ClientIp}, details={Details}",
            violationType, userId, clientIp, details);
    }
    
    public void UpdateAuditBacklogSize(long backlogSize)
    {
        Interlocked.Exchange(ref _currentAuditBacklog, backlogSize);
        var tags = new TagList
        {
            ["gateway"] = "admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _auditLogBacklogSize.Record(backlogSize, tags);
    }
    
    public async Task<string> ExportMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _prometheusExporter.GetMetricsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Admin Gateway metrics");
            throw;
        }
    }
    
    private static string SanitizeResourcePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "/";
        
        // Replace dynamic segments with placeholders for better cardinality control
        var sanitized = path.ToLowerInvariant();
        
        // Common patterns to normalize
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"/api/v\d+", "/api/v*");
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"/\d+(/|$)", "/{id}$1");
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"/[a-f0-9-]{36}(/|$)", "/{guid}$1");
        
        // Limit path length for cardinality
        if (sanitized.Length > 100)
        {
            sanitized = sanitized[..97] + "...";
        }
        
        return sanitized;
    }
    
    private static string GetStatusClass(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "2xx",
            >= 300 and < 400 => "3xx",
            >= 400 and < 500 => "4xx",
            >= 500 => "5xx",
            _ => "1xx"
        };
    }
    
    public void Dispose()
    {
        _meter?.Dispose();
        _logger.LogInformation("AdminGatewayMetricsService disposed");
    }
}
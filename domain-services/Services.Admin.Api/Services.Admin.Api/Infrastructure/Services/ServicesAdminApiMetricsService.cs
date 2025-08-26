using Infrastructure.Metrics.Abstractions;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Services.Admin.Api.Infrastructure.Services;

public sealed class ServicesAdminApiMetricsService : IDisposable
{
    private readonly ICustomMetricsRegistry _metricsRegistry;
    private readonly IPrometheusMetricsExporter _prometheusExporter;
    private readonly ILogger<ServicesAdminApiMetricsService> _logger;
    
    private readonly Meter _meter;
    
    // Admin operation success metrics
    private readonly Counter<long> _adminOperationsCounter;
    private readonly Counter<long> _adminOperationSuccessCounter;
    private readonly Counter<long> _adminOperationFailuresCounter;
    private readonly Histogram<double> _adminOperationDuration;
    private readonly Counter<long> _dataValidationErrorsCounter;
    
    // EF Core performance tracking
    private readonly Counter<long> _efCoreQueriesCounter;
    private readonly Histogram<double> _efCoreQueryDuration;
    private readonly Counter<long> _efCoreSaveChangesCounter;
    private readonly Histogram<double> _efCoreSaveChangesDuration;
    private readonly Counter<long> _efCoreChangeTrackingCounter;
    private readonly Histogram<double> _efCoreChangeTrackingDuration;
    private readonly Gauge<int> _efCoreTrackedEntitiesCount;
    private readonly Counter<long> _efCoreQueryErrorsCounter;
    
    // Data mutation patterns
    private readonly Counter<long> _serviceCreateCounter;
    private readonly Counter<long> _serviceUpdateCounter;
    private readonly Counter<long> _serviceDeleteCounter;
    private readonly Counter<long> _categoryCreateCounter;
    private readonly Counter<long> _categoryUpdateCounter;
    private readonly Counter<long> _categoryDeleteCounter;
    private readonly Histogram<double> _dataMutationDuration;
    
    // Medical-grade audit compliance
    private readonly Counter<long> _auditLogGenerationCounter;
    private readonly Histogram<double> _auditLogGenerationLatency;
    private readonly Counter<long> _auditLogErrorsCounter;
    private readonly Counter<long> _medicalComplianceViolationsCounter;
    private readonly Gauge<long> _auditLogBacklogSize;
    
    // Admin user context metrics
    private readonly Counter<long> _adminUserOperationsCounter;
    private readonly Counter<long> _rbacAuthorizationChecksCounter;
    private readonly Counter<long> _rbacViolationsCounter;
    private readonly Histogram<double> _rbacDecisionDuration;
    
    // Database transaction metrics
    private readonly Counter<long> _databaseTransactionsCounter;
    private readonly Histogram<double> _transactionDuration;
    private readonly Counter<long> _transactionRollbacksCounter;
    private readonly Counter<long> _deadlockErrorsCounter;
    private readonly Counter<long> _concurrencyConflictsCounter;
    
    private int _currentTrackedEntities = 0;
    private long _auditBacklogSize = 0;
    
    public ServicesAdminApiMetricsService(
        ICustomMetricsRegistry metricsRegistry,
        IPrometheusMetricsExporter prometheusExporter,
        ILogger<ServicesAdminApiMetricsService> logger)
    {
        _metricsRegistry = metricsRegistry ?? throw new ArgumentNullException(nameof(metricsRegistry));
        _prometheusExporter = prometheusExporter ?? throw new ArgumentNullException(nameof(prometheusExporter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _meter = _metricsRegistry.CreateMeter("Services.Admin.Api", "1.0.0");
        
        // Initialize admin operation instruments
        _adminOperationsCounter = _meter.CreateCounter<long>(
            "services_admin_api_operations_total",
            "count",
            "Total number of admin operations performed");
            
        _adminOperationSuccessCounter = _meter.CreateCounter<long>(
            "services_admin_api_operations_success_total",
            "count",
            "Total number of successful admin operations");
            
        _adminOperationFailuresCounter = _meter.CreateCounter<long>(
            "services_admin_api_operations_failures_total",
            "count",
            "Total number of failed admin operations");
            
        _adminOperationDuration = _meter.CreateHistogram<double>(
            "services_admin_api_operation_duration_seconds",
            "seconds",
            "Duration of admin operations");
            
        _dataValidationErrorsCounter = _meter.CreateCounter<long>(
            "services_admin_api_validation_errors_total",
            "count",
            "Total number of data validation errors");
            
        // Initialize EF Core performance instruments
        _efCoreQueriesCounter = _meter.CreateCounter<long>(
            "services_admin_api_efcore_queries_total",
            "count",
            "Total number of EF Core queries executed");
            
        _efCoreQueryDuration = _meter.CreateHistogram<double>(
            "services_admin_api_efcore_query_duration_seconds",
            "seconds",
            "Duration of EF Core queries");
            
        _efCoreSaveChangesCounter = _meter.CreateCounter<long>(
            "services_admin_api_efcore_save_changes_total",
            "count",
            "Total number of EF Core SaveChanges operations");
            
        _efCoreSaveChangesDuration = _meter.CreateHistogram<double>(
            "services_admin_api_efcore_save_changes_duration_seconds",
            "seconds",
            "Duration of EF Core SaveChanges operations");
            
        _efCoreChangeTrackingCounter = _meter.CreateCounter<long>(
            "services_admin_api_efcore_change_tracking_operations_total",
            "count",
            "Total number of EF Core change tracking operations");
            
        _efCoreChangeTrackingDuration = _meter.CreateHistogram<double>(
            "services_admin_api_efcore_change_tracking_duration_seconds",
            "seconds",
            "Duration of EF Core change tracking operations");
            
        _efCoreTrackedEntitiesCount = _meter.CreateGauge<int>(
            "services_admin_api_efcore_tracked_entities",
            "count",
            "Current number of entities being tracked by EF Core");
            
        _efCoreQueryErrorsCounter = _meter.CreateCounter<long>(
            "services_admin_api_efcore_query_errors_total",
            "count",
            "Total number of EF Core query errors");
            
        // Initialize data mutation instruments
        _serviceCreateCounter = _meter.CreateCounter<long>(
            "services_admin_api_service_creates_total",
            "count",
            "Total number of service creation operations");
            
        _serviceUpdateCounter = _meter.CreateCounter<long>(
            "services_admin_api_service_updates_total",
            "count",
            "Total number of service update operations");
            
        _serviceDeleteCounter = _meter.CreateCounter<long>(
            "services_admin_api_service_deletes_total",
            "count",
            "Total number of service delete operations");
            
        _categoryCreateCounter = _meter.CreateCounter<long>(
            "services_admin_api_category_creates_total",
            "count",
            "Total number of category creation operations");
            
        _categoryUpdateCounter = _meter.CreateCounter<long>(
            "services_admin_api_category_updates_total",
            "count",
            "Total number of category update operations");
            
        _categoryDeleteCounter = _meter.CreateCounter<long>(
            "services_admin_api_category_deletes_total",
            "count",
            "Total number of category delete operations");
            
        _dataMutationDuration = _meter.CreateHistogram<double>(
            "services_admin_api_data_mutation_duration_seconds",
            "seconds",
            "Duration of data mutation operations");
            
        // Initialize audit compliance instruments
        _auditLogGenerationCounter = _meter.CreateCounter<long>(
            "services_admin_api_audit_log_generation_total",
            "count",
            "Total number of audit log generation operations");
            
        _auditLogGenerationLatency = _meter.CreateHistogram<double>(
            "services_admin_api_audit_log_generation_latency_seconds",
            "seconds",
            "Latency of audit log generation for medical compliance");
            
        _auditLogErrorsCounter = _meter.CreateCounter<long>(
            "services_admin_api_audit_log_errors_total",
            "count",
            "Total number of audit log generation errors");
            
        _medicalComplianceViolationsCounter = _meter.CreateCounter<long>(
            "services_admin_api_medical_compliance_violations_total",
            "count",
            "Total number of medical compliance violations detected");
            
        _auditLogBacklogSize = _meter.CreateGauge<long>(
            "services_admin_api_audit_log_backlog_size",
            "count",
            "Current size of audit log processing backlog");
            
        // Initialize admin user context instruments
        _adminUserOperationsCounter = _meter.CreateCounter<long>(
            "services_admin_api_admin_user_operations_total",
            "count",
            "Total number of operations performed by admin users");
            
        _rbacAuthorizationChecksCounter = _meter.CreateCounter<long>(
            "services_admin_api_rbac_authorization_checks_total",
            "count",
            "Total number of RBAC authorization checks performed");
            
        _rbacViolationsCounter = _meter.CreateCounter<long>(
            "services_admin_api_rbac_violations_total",
            "count",
            "Total number of RBAC authorization violations");
            
        _rbacDecisionDuration = _meter.CreateHistogram<double>(
            "services_admin_api_rbac_decision_duration_seconds",
            "seconds",
            "Duration of RBAC authorization decisions");
            
        // Initialize database transaction instruments
        _databaseTransactionsCounter = _meter.CreateCounter<long>(
            "services_admin_api_database_transactions_total",
            "count",
            "Total number of database transactions");
            
        _transactionDuration = _meter.CreateHistogram<double>(
            "services_admin_api_transaction_duration_seconds",
            "seconds",
            "Duration of database transactions");
            
        _transactionRollbacksCounter = _meter.CreateCounter<long>(
            "services_admin_api_transaction_rollbacks_total",
            "count",
            "Total number of transaction rollbacks");
            
        _deadlockErrorsCounter = _meter.CreateCounter<long>(
            "services_admin_api_deadlock_errors_total",
            "count",
            "Total number of database deadlock errors");
            
        _concurrencyConflictsCounter = _meter.CreateCounter<long>(
            "services_admin_api_concurrency_conflicts_total",
            "count",
            "Total number of concurrency conflict errors");
            
        _logger.LogInformation("ServicesAdminApiMetricsService initialized with meter: {MeterName}", _meter.Name);
    }
    
    public void RecordAdminOperation(string operation, string userId, string[] userRoles, double durationSeconds, bool success, string entityType = "", string entityId = "")
    {
        var tags = new TagList
        {
            ["operation"] = operation.ToLowerInvariant(),
            ["result"] = success ? "success" : "failure",
            ["user_role_count"] = userRoles.Length.ToString(),
            ["api"] = "services_admin",
            ["compliance_level"] = "medical_grade"
        };
        
        if (!string.IsNullOrEmpty(entityType))
        {
            tags["entity_type"] = entityType.ToLowerInvariant();
        }
        
        _adminOperationsCounter.Add(1, tags);
        _adminOperationDuration.Record(durationSeconds, tags);
        _adminUserOperationsCounter.Add(1, tags);
        
        if (success)
        {
            _adminOperationSuccessCounter.Add(1, tags);
        }
        else
        {
            _adminOperationFailuresCounter.Add(1, tags);
        }
        
        _logger.LogDebug("Admin operation recorded: operation={Operation}, userId={UserId}, success={Success}, duration={Duration}ms",
            operation, userId, success, durationSeconds * 1000);
    }
    
    public void RecordEfCoreQuery(string queryType, double durationSeconds, bool success, int resultCount = 0)
    {
        var tags = new TagList
        {
            ["query_type"] = queryType.ToLowerInvariant(),
            ["result"] = success ? "success" : "error",
            ["api"] = "services_admin",
            ["compliance_level"] = "medical_grade"
        };
        
        if (resultCount > 0)
        {
            tags["result_count"] = GetResultCountBucket(resultCount);
        }
        
        _efCoreQueriesCounter.Add(1, tags);
        _efCoreQueryDuration.Record(durationSeconds, tags);
        
        if (!success)
        {
            _efCoreQueryErrorsCounter.Add(1, tags);
        }
        
        _logger.LogDebug("EF Core query recorded: queryType={QueryType}, success={Success}, duration={Duration}ms, resultCount={ResultCount}",
            queryType, success, durationSeconds * 1000, resultCount);
    }
    
    public void RecordEfCoreSaveChanges(double durationSeconds, bool success, int affectedEntities, int changeTrackingTime = 0)
    {
        var tags = new TagList
        {
            ["result"] = success ? "success" : "error",
            ["affected_entities"] = GetResultCountBucket(affectedEntities),
            ["api"] = "services_admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _efCoreSaveChangesCounter.Add(1, tags);
        _efCoreSaveChangesDuration.Record(durationSeconds, tags);
        
        if (changeTrackingTime > 0)
        {
            _efCoreChangeTrackingCounter.Add(1, tags);
            _efCoreChangeTrackingDuration.Record(changeTrackingTime / 1000.0, tags);
        }
        
        _logger.LogDebug("EF Core SaveChanges recorded: success={Success}, duration={Duration}ms, affectedEntities={AffectedEntities}",
            success, durationSeconds * 1000, affectedEntities);
    }
    
    public void RecordDataMutation(string operationType, string entityType, string entityId, string userId, double durationSeconds, bool success)
    {
        var tags = new TagList
        {
            ["operation_type"] = operationType.ToLowerInvariant(),
            ["entity_type"] = entityType.ToLowerInvariant(),
            ["result"] = success ? "success" : "failure",
            ["api"] = "services_admin",
            ["compliance_level"] = "medical_grade"
        };
        
        // Record specific entity operation counters
        switch (entityType.ToLowerInvariant())
        {
            case "service":
                switch (operationType.ToLowerInvariant())
                {
                    case "create": _serviceCreateCounter.Add(1, tags); break;
                    case "update": _serviceUpdateCounter.Add(1, tags); break;
                    case "delete": _serviceDeleteCounter.Add(1, tags); break;
                }
                break;
            case "category":
                switch (operationType.ToLowerInvariant())
                {
                    case "create": _categoryCreateCounter.Add(1, tags); break;
                    case "update": _categoryUpdateCounter.Add(1, tags); break;
                    case "delete": _categoryDeleteCounter.Add(1, tags); break;
                }
                break;
        }
        
        _dataMutationDuration.Record(durationSeconds, tags);
        
        _logger.LogDebug("Data mutation recorded: operation={OperationType}, entity={EntityType}:{EntityId}, userId={UserId}, success={Success}, duration={Duration}ms",
            operationType, entityType, entityId, userId, success, durationSeconds * 1000);
    }
    
    public void RecordAuditLogGeneration(string eventType, string entityType, string entityId, string userId, double latencySeconds, bool success)
    {
        var tags = new TagList
        {
            ["event_type"] = eventType.ToLowerInvariant(),
            ["entity_type"] = entityType.ToLowerInvariant(),
            ["result"] = success ? "success" : "failure",
            ["api"] = "services_admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _auditLogGenerationCounter.Add(1, tags);
        _auditLogGenerationLatency.Record(latencySeconds, tags);
        
        if (!success)
        {
            _auditLogErrorsCounter.Add(1, tags);
            // Audit log failures are critical for medical compliance
            Interlocked.Increment(ref _auditBacklogSize);
            _auditLogBacklogSize.Record(_auditBacklogSize, tags);
        }
        
        _logger.LogDebug("Audit log generation recorded: eventType={EventType}, entity={EntityType}:{EntityId}, userId={UserId}, success={Success}, latency={Latency}ms",
            eventType, entityType, entityId, userId, success, latencySeconds * 1000);
    }
    
    public void RecordMedicalComplianceViolation(string violationType, string userId, string entityType, string entityId, string details)
    {
        var tags = new TagList
        {
            ["violation_type"] = violationType.ToLowerInvariant().Replace(" ", "_"),
            ["entity_type"] = entityType.ToLowerInvariant(),
            ["api"] = "services_admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _medicalComplianceViolationsCounter.Add(1, tags);
        
        _logger.LogWarning("Medical compliance violation recorded: type={ViolationType}, userId={UserId}, entity={EntityType}:{EntityId}, details={Details}",
            violationType, userId, entityType, entityId, details);
    }
    
    public void RecordRbacCheck(string policy, string userId, string[] userRoles, bool allowed, double decisionTimeSeconds, string resource = "")
    {
        var tags = new TagList
        {
            ["policy"] = policy,
            ["result"] = allowed ? "allowed" : "denied",
            ["user_role_count"] = userRoles.Length.ToString(),
            ["api"] = "services_admin",
            ["compliance_level"] = "medical_grade"
        };
        
        if (!string.IsNullOrEmpty(resource))
        {
            tags["resource"] = resource.ToLowerInvariant();
        }
        
        _rbacAuthorizationChecksCounter.Add(1, tags);
        _rbacDecisionDuration.Record(decisionTimeSeconds, tags);
        
        if (!allowed)
        {
            _rbacViolationsCounter.Add(1, tags);
        }
        
        _logger.LogDebug("RBAC check recorded: policy={Policy}, userId={UserId}, allowed={Allowed}, duration={Duration}ms",
            policy, userId, allowed, decisionTimeSeconds * 1000);
    }
    
    public void RecordDatabaseTransaction(string operationType, double durationSeconds, bool committed, bool deadlockDetected = false, bool concurrencyConflict = false)
    {
        var tags = new TagList
        {
            ["operation_type"] = operationType.ToLowerInvariant(),
            ["result"] = committed ? "committed" : "rolled_back",
            ["api"] = "services_admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _databaseTransactionsCounter.Add(1, tags);
        _transactionDuration.Record(durationSeconds, tags);
        
        if (!committed)
        {
            _transactionRollbacksCounter.Add(1, tags);
        }
        
        if (deadlockDetected)
        {
            _deadlockErrorsCounter.Add(1, tags);
        }
        
        if (concurrencyConflict)
        {
            _concurrencyConflictsCounter.Add(1, tags);
        }
        
        _logger.LogDebug("Database transaction recorded: operation={OperationType}, committed={Committed}, duration={Duration}ms, deadlock={Deadlock}, concurrency={ConcurrencyConflict}",
            operationType, committed, durationSeconds * 1000, deadlockDetected, concurrencyConflict);
    }
    
    public void RecordDataValidationError(string entityType, string field, string errorType, string userId)
    {
        var tags = new TagList
        {
            ["entity_type"] = entityType.ToLowerInvariant(),
            ["field"] = field.ToLowerInvariant(),
            ["error_type"] = errorType.ToLowerInvariant(),
            ["api"] = "services_admin",
            ["compliance_level"] = "medical_grade"
        };
        
        _dataValidationErrorsCounter.Add(1, tags);
        
        _logger.LogDebug("Data validation error recorded: entity={EntityType}, field={Field}, error={ErrorType}, userId={UserId}",
            entityType, field, errorType, userId);
    }
    
    public void UpdateTrackedEntitiesCount(int count)
    {
        Interlocked.Exchange(ref _currentTrackedEntities, count);
        var tags = new TagList 
        {
            ["api"] = "services_admin",
            ["compliance_level"] = "medical_grade"
        };
        _efCoreTrackedEntitiesCount.Record(count, tags);
    }
    
    public void UpdateAuditBacklogSize(long backlogSize)
    {
        Interlocked.Exchange(ref _auditBacklogSize, backlogSize);
        var tags = new TagList 
        {
            ["api"] = "services_admin",
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
            _logger.LogError(ex, "Failed to export Services Admin API metrics");
            throw;
        }
    }
    
    private static string GetResultCountBucket(int count)
    {
        return count switch
        {
            0 => "0",
            1 => "1",
            >= 2 and <= 10 => "2-10",
            >= 11 and <= 50 => "11-50",
            >= 51 and <= 100 => "51-100",
            _ => "100+"
        };
    }
    
    public void Dispose()
    {
        _meter?.Dispose();
        _logger.LogInformation("ServicesAdminApiMetricsService disposed");
    }
}
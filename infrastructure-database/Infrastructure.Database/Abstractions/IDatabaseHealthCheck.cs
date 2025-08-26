namespace Infrastructure.Database.Abstractions;

/// <summary>
/// CONTRACT: Generic database health check interface for monitoring and observability.
/// 
/// TDD PRINCIPLE: Interface drives the design of database health monitoring
/// DEPENDENCY INVERSION: Abstractions for variable health check concerns
/// INFRASTRUCTURE: Generic health check patterns reusable by any domain
/// DOMAIN AGNOSTIC: No knowledge of Services, News, Events, or any specific domain
/// </summary>
public interface IDatabaseHealthCheck
{
    /// <summary>
    /// CONTRACT: Check database connection health
    /// 
    /// POSTCONDITION: Returns health check result with connection status
    /// MONITORING: Generic connection health verification
    /// INFRASTRUCTURE: Database connectivity monitoring for any domain
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Health check result with connection details</returns>
    Task<DatabaseHealthResult> CheckConnectionHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Check database query performance health
    /// 
    /// POSTCONDITION: Returns performance health result with timing metrics
    /// MONITORING: Generic query performance verification
    /// PERFORMANCE: Database response time monitoring
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Health check result with performance metrics</returns>
    Task<DatabaseHealthResult> CheckQueryPerformanceHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Check database disk space health
    /// 
    /// POSTCONDITION: Returns disk space health result with storage metrics
    /// MONITORING: Generic storage capacity verification
    /// RESOURCE MONITORING: Database storage monitoring
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Health check result with storage details</returns>
    Task<DatabaseHealthResult> CheckDiskSpaceHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Check overall database health
    /// 
    /// POSTCONDITION: Returns comprehensive health result combining all checks
    /// MONITORING: Complete database health assessment
    /// INFRASTRUCTURE: Overall database health for any domain
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Comprehensive health check result</returns>
    Task<DatabaseHealthResult> CheckOverallHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Get database health metrics for monitoring
    /// 
    /// POSTCONDITION: Returns detailed health metrics for observability
    /// OBSERVABILITY: Database health metrics collection
    /// MONITORING: Health trend analysis data
    /// </summary>
    /// <returns>Detailed health metrics</returns>
    DatabaseHealthMetrics GetHealthMetrics();
}

/// <summary>
/// Database health check result.
/// INFRASTRUCTURE: Generic health result for any domain
/// </summary>
public sealed class DatabaseHealthResult
{
    /// <summary>Overall health status</summary>
    public DatabaseHealthStatus Status { get; init; }
    
    /// <summary>Health check description</summary>
    public required string Description { get; init; }
    
    /// <summary>Health check duration in milliseconds</summary>
    public double DurationMs { get; init; }
    
    /// <summary>Exception details if health check failed</summary>
    public Exception? Exception { get; init; }
    
    /// <summary>Additional health data</summary>
    public Dictionary<string, object> Data { get; init; } = new();
    
    /// <summary>Health check timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>Create healthy result</summary>
    public static DatabaseHealthResult Healthy(string description, double durationMs, Dictionary<string, object>? data = null) =>
        new()
        {
            Status = DatabaseHealthStatus.Healthy,
            Description = description,
            DurationMs = durationMs,
            Data = data ?? new Dictionary<string, object>()
        };
    
    /// <summary>Create degraded result</summary>
    public static DatabaseHealthResult Degraded(string description, double durationMs, Exception? exception = null, Dictionary<string, object>? data = null) =>
        new()
        {
            Status = DatabaseHealthStatus.Degraded,
            Description = description,
            DurationMs = durationMs,
            Exception = exception,
            Data = data ?? new Dictionary<string, object>()
        };
    
    /// <summary>Create unhealthy result</summary>
    public static DatabaseHealthResult Unhealthy(string description, double durationMs, Exception? exception = null, Dictionary<string, object>? data = null) =>
        new()
        {
            Status = DatabaseHealthStatus.Unhealthy,
            Description = description,
            DurationMs = durationMs,
            Exception = exception,
            Data = data ?? new Dictionary<string, object>()
        };
}

/// <summary>
/// Database health status enumeration.
/// INFRASTRUCTURE: Generic health status levels
/// </summary>
public enum DatabaseHealthStatus
{
    /// <summary>Database is healthy and operating normally</summary>
    Healthy = 0,
    
    /// <summary>Database is operational but with degraded performance</summary>
    Degraded = 1,
    
    /// <summary>Database is not operational</summary>
    Unhealthy = 2
}

/// <summary>
/// Database health metrics for monitoring and observability.
/// INFRASTRUCTURE: Generic health metrics for any domain
/// </summary>
public sealed class DatabaseHealthMetrics
{
    /// <summary>Connection health statistics</summary>
    public ConnectionHealthMetrics Connection { get; init; } = new();
    
    /// <summary>Query performance health statistics</summary>
    public QueryPerformanceHealthMetrics QueryPerformance { get; init; } = new();
    
    /// <summary>Storage health statistics</summary>
    public StorageHealthMetrics Storage { get; init; } = new();
    
    /// <summary>Overall health statistics</summary>
    public OverallHealthMetrics Overall { get; init; } = new();
    
    /// <summary>Metrics timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>Connection health metrics</summary>
public sealed class ConnectionHealthMetrics
{
    /// <summary>Connection success rate (percentage)</summary>
    public double SuccessRate { get; init; }
    
    /// <summary>Average connection time in milliseconds</summary>
    public double AverageConnectionTimeMs { get; init; }
    
    /// <summary>Total connection attempts</summary>
    public int TotalAttempts { get; init; }
    
    /// <summary>Failed connection attempts</summary>
    public int FailedAttempts { get; init; }
    
    /// <summary>Last successful connection timestamp</summary>
    public DateTime? LastSuccessfulConnection { get; init; }
}

/// <summary>Query performance health metrics</summary>
public sealed class QueryPerformanceHealthMetrics
{
    /// <summary>Average query response time in milliseconds</summary>
    public double AverageResponseTimeMs { get; init; }
    
    /// <summary>95th percentile query response time in milliseconds</summary>
    public double P95ResponseTimeMs { get; init; }
    
    /// <summary>Number of slow queries</summary>
    public int SlowQueryCount { get; init; }
    
    /// <summary>Total queries executed</summary>
    public int TotalQueries { get; init; }
    
    /// <summary>Query timeout count</summary>
    public int TimeoutCount { get; init; }
}

/// <summary>Storage health metrics</summary>
public sealed class StorageHealthMetrics
{
    /// <summary>Database size in bytes</summary>
    public long DatabaseSizeBytes { get; init; }
    
    /// <summary>Available disk space in bytes</summary>
    public long AvailableDiskSpaceBytes { get; init; }
    
    /// <summary>Total disk space in bytes</summary>
    public long TotalDiskSpaceBytes { get; init; }
    
    /// <summary>Disk usage percentage</summary>
    public double DiskUsagePercentage { get; init; }
    
    /// <summary>Whether disk space is critical</summary>
    public bool IsDiskSpaceCritical { get; init; }
}

/// <summary>Overall health metrics</summary>
public sealed class OverallHealthMetrics
{
    /// <summary>Current overall health status</summary>
    public DatabaseHealthStatus Status { get; init; }
    
    /// <summary>Health score (0-100)</summary>
    public double HealthScore { get; init; }
    
    /// <summary>Uptime percentage</summary>
    public double UptimePercentage { get; init; }
    
    /// <summary>Last health check timestamp</summary>
    public DateTime LastHealthCheck { get; init; }
    
    /// <summary>Health check frequency in seconds</summary>
    public int HealthCheckIntervalSeconds { get; init; }
}
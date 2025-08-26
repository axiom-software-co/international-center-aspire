namespace Service.Monitoring.Abstractions;

public interface IMetricsCollector
{
    Task<IDictionary<string, object>> CollectAllMetricsAsync(CancellationToken cancellationToken = default);
    
    Task<IDictionary<string, object>> CollectSystemMetricsAsync(CancellationToken cancellationToken = default);
    
    Task<IDictionary<string, object>> CollectDatabaseMetricsAsync(CancellationToken cancellationToken = default);
    
    Task<IDictionary<string, object>> CollectRedisMetricsAsync(CancellationToken cancellationToken = default);
    
    void RecordHealthCheckDuration(string checkName, TimeSpan duration);
    
    void RecordHealthCheckResult(string checkName, HealthStatus status);
    
    void IncrementHealthCheckCount(string checkName);
}
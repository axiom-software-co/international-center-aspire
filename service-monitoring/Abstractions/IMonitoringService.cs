namespace Service.Monitoring.Abstractions;

public interface IMonitoringService
{
    Task<HealthCheckReport> CheckHealthAsync(HealthCheckType checkType = HealthCheckType.Full, 
        CancellationToken cancellationToken = default);
        
    Task<HealthStatus> CheckLivenessAsync(CancellationToken cancellationToken = default);
    
    Task<HealthStatus> CheckReadinessAsync(CancellationToken cancellationToken = default);
    
    Task<IDictionary<string, object>> GetMetricsAsync(CancellationToken cancellationToken = default);
    
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
namespace Service.Monitoring.Abstractions;

public interface IRedisHealthCheck
{
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    
    Task<bool> CanReadWriteAsync(CancellationToken cancellationToken = default);
    
    Task<TimeSpan> MeasureLatencyAsync(CancellationToken cancellationToken = default);
    
    Task<long> GetMemoryUsageAsync(CancellationToken cancellationToken = default);
}
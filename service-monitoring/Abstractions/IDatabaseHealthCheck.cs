namespace Service.Monitoring.Abstractions;

public interface IDatabaseHealthCheck
{
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    
    Task<bool> AreMigrationsCurrentAsync(CancellationToken cancellationToken = default);
    
    Task<TimeSpan> MeasureLatencyAsync(CancellationToken cancellationToken = default);
}
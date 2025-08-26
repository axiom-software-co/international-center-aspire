namespace Service.Monitoring.Models;

public sealed class HealthCheckResult
{
    public string Name { get; init; } = string.Empty;
    public HealthStatus Status { get; init; }
    public string Description { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public string? Exception { get; init; }
    public IDictionary<string, object> Data { get; init; } = new Dictionary<string, object>();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class HealthCheckReport
{
    public HealthStatus Status { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public IDictionary<string, HealthCheckResult> Results { get; init; } = new Dictionary<string, HealthCheckResult>();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public enum HealthCheckType
{
    Liveness = 1,
    Readiness = 2,
    Full = 3
}
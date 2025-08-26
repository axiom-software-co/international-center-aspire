namespace Services.Shared.Models;

/// <summary>
/// Services domain-specific audit log entry for admin operations
/// Provides comprehensive audit trail for service management operations
/// </summary>
public class ServiceAuditLogEntry
{
    public string Operation { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public string Changes { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Services domain-specific performance metrics for admin operations
/// Tracks performance characteristics of service management operations
/// </summary>
public class ServicePerformanceMetrics
{
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan ValidationDuration { get; set; }
    public TimeSpan DatabaseDuration { get; set; }
    public int RecordsAffected { get; set; } = 1;
}
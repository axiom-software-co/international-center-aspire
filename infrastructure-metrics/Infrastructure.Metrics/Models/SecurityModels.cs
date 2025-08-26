namespace Infrastructure.Metrics.Models;

public sealed class SecurityValidationResult
{
    public bool IsValid { get; init; }
    public string? Reason { get; init; }
    public SecurityValidationType ValidationType { get; init; }
    public string ClientIp { get; init; } = string.Empty;
    public string? UserAgent { get; init; }
    public DateTimeOffset ValidatedAt { get; init; } = DateTimeOffset.UtcNow;
    public IDictionary<string, string> Context { get; init; } = new Dictionary<string, string>();
}

public sealed class MetricsAccessAttempt
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string ClientIp { get; init; } = string.Empty;
    public string? UserAgent { get; init; }
    public string RequestPath { get; init; } = string.Empty;
    public string HttpMethod { get; init; } = string.Empty;
    public bool Authorized { get; init; }
    public string? DenialReason { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public TimeSpan ProcessingTime { get; init; }
    public long ResponseSize { get; init; }
    public int StatusCode { get; init; }
    public IDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
    public string? CorrelationId { get; init; }
}

public enum SecurityEventType
{
    UnauthorizedAccess = 1,
    IpBlocked = 2,
    RateLimitExceeded = 3,
    InvalidAuthentication = 4,
    SuspiciousActivity = 5,
    AccessGranted = 6,
    ConfigurationChanged = 7,
    SecurityHeaderMissing = 8
}

public enum SecurityValidationType
{
    IpAddress = 1,
    Authentication = 2,
    Authorization = 3,
    RateLimit = 4,
    Headers = 5,
    UserAgent = 6,
    RequestSize = 7
}
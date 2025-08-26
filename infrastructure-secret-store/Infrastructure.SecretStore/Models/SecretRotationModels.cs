namespace Infrastructure.SecretStore.Models;

public sealed class SecretRotationResult
{
    public string SecretName { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? OldVersion { get; init; }
    public string? NewVersion { get; init; }
    public DateTimeOffset RotatedAt { get; init; } = DateTimeOffset.UtcNow;
    public TimeSpan Duration { get; init; }
    public SecretRotationType RotationType { get; init; }
}

public sealed class SecretRotationSchedule
{
    public string SecretName { get; init; } = string.Empty;
    public TimeSpan RotationInterval { get; init; }
    public DateTimeOffset NextRotationDate { get; init; }
    public DateTimeOffset? LastRotationDate { get; init; }
    public bool AutoRotationEnabled { get; init; } = true;
    public int MaxRetryAttempts { get; init; } = 3;
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMinutes(5);
    public SecretRotationType RotationType { get; init; } = SecretRotationType.Manual;
    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class SecretRotationHistory
{
    public string SecretName { get; init; } = string.Empty;
    public DateTimeOffset RotatedAt { get; init; }
    public string? OldVersion { get; init; }
    public string? NewVersion { get; init; }
    public SecretRotationType RotationType { get; init; }
    public bool Success { get; init; }
    public string? Error { get; init; }
    public TimeSpan Duration { get; init; }
    public string? RotatedBy { get; init; }
    public string? Reason { get; init; }
}

public enum SecretRotationType
{
    Manual = 0,
    Scheduled = 1,
    OnDemand = 2,
    Emergency = 3,
    Compliance = 4
}

public enum SecretRotationStatus
{
    NotScheduled = 0,
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}
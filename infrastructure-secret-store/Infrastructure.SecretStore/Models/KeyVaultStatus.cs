namespace Infrastructure.SecretStore.Models;

public sealed class KeyVaultStatus
{
    public string VaultName { get; init; } = string.Empty;
    public Uri? VaultUri { get; init; }
    public bool IsHealthy { get; init; }
    public KeyVaultConnectionStatus ConnectionStatus { get; init; }
    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.UtcNow;
    public TimeSpan ResponseTime { get; init; }
    public string? Error { get; init; }
    public KeyVaultPermissions Permissions { get; init; } = new();
    public KeyVaultMetrics Metrics { get; init; } = new();
}

public sealed class KeyVaultPermissions
{
    public bool CanReadSecrets { get; init; }
    public bool CanWriteSecrets { get; init; }
    public bool CanDeleteSecrets { get; init; }
    public bool CanListSecrets { get; init; }
    public bool CanReadKeys { get; init; }
    public bool CanWriteKeys { get; init; }
    public bool CanDeleteKeys { get; init; }
    public bool CanReadCertificates { get; init; }
    public bool CanWriteCertificates { get; init; }
    public bool CanDeleteCertificates { get; init; }
}

public sealed class KeyVaultMetrics
{
    public int SecretCount { get; init; }
    public int KeyCount { get; init; }
    public int CertificateCount { get; init; }
    public int DeletedSecretCount { get; init; }
    public int DeletedKeyCount { get; init; }
    public int DeletedCertificateCount { get; init; }
    public TimeSpan AverageResponseTime { get; init; }
    public DateTimeOffset LastRequestTime { get; init; }
    public long TotalRequests { get; init; }
    public long SuccessfulRequests { get; init; }
    public long FailedRequests { get; init; }
}

public enum KeyVaultConnectionStatus
{
    Unknown = 0,
    Connected = 1,
    Disconnected = 2,
    AuthenticationError = 3,
    AuthorizationError = 4,
    NotFound = 5,
    Throttled = 6,
    ServiceUnavailable = 7
}
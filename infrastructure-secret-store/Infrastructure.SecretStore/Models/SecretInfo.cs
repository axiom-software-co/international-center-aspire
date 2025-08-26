namespace Infrastructure.SecretStore.Models;

public sealed class SecretInfo
{
    public string Name { get; init; } = string.Empty;
    public string? Value { get; init; }
    public string? Version { get; init; }
    public DateTimeOffset CreatedOn { get; init; }
    public DateTimeOffset UpdatedOn { get; init; }
    public DateTimeOffset? ExpiresOn { get; init; }
    public DateTimeOffset? NotBefore { get; init; }
    public bool Enabled { get; init; } = true;
    public IDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
    public string ContentType { get; init; } = string.Empty;
    public Uri? Id { get; init; }
    public Uri? VaultUri { get; init; }
    public bool Managed { get; init; }
    public string RecoveryLevel { get; init; } = string.Empty;
}
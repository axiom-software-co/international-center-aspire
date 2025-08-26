namespace Infrastructure.SecretStore.Configuration;

public sealed class SecretStoreOptions
{
    public const string SectionName = "SecretStore";
    
    public bool Enabled { get; set; } = true;
    public SecretStoreProvider Provider { get; set; } = SecretStoreProvider.AzureKeyVault;
    public string? VaultUri { get; set; }
    public string? VaultName { get; set; }
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
    public bool EnableCaching { get; set; } = true;
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(15);
    public bool EnableMetrics { get; set; } = true;
    public bool EnableHealthChecks { get; set; } = true;
    
    // Authentication settings
    public AuthenticationOptions Authentication { get; set; } = new();
    
    // Rotation settings
    public RotationOptions Rotation { get; set; } = new();
    
    // Performance settings
    public PerformanceOptions Performance { get; set; } = new();
}

public sealed class AuthenticationOptions
{
    public AuthenticationProvider Provider { get; set; } = AuthenticationProvider.ManagedIdentity;
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TenantId { get; set; }
    public string? CertificateThumbprint { get; set; }
    public string? CertificatePath { get; set; }
    public bool UseDefaultAzureCredential { get; set; } = true;
    public string? Authority { get; set; }
    public string[]? AdditionallyAllowedTenants { get; set; }
    public bool DisableInstanceDiscovery { get; set; } = false;
    public bool EnableCae { get; set; } = false;
}

public sealed class RotationOptions
{
    public bool EnableAutoRotation { get; set; } = false;
    public TimeSpan DefaultRotationInterval { get; set; } = TimeSpan.FromDays(90);
    public TimeSpan RotationCheckInterval { get; set; } = TimeSpan.FromHours(6);
    public int MaxConcurrentRotations { get; set; } = 5;
    public TimeSpan RotationTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public bool NotifyOnRotation { get; set; } = true;
    public string[]? NotificationEndpoints { get; set; }
    public bool KeepOldVersions { get; set; } = true;
    public int MaxVersionsToKeep { get; set; } = 5;
}

public sealed class PerformanceOptions
{
    public int MaxConcurrentRequests { get; set; } = 50;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableConnectionPooling { get; set; } = true;
    public int MaxConnectionsPerEndpoint { get; set; } = 10;
    public TimeSpan PooledConnectionIdleTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan PooledConnectionLifetime { get; set; } = TimeSpan.FromMinutes(15);
    public bool EnableResponseCompression { get; set; } = true;
}

public enum SecretStoreProvider
{
    AzureKeyVault = 1,
    HashiCorpVault = 2,
    AWS_SecretsManager = 3,
    Local = 4
}

public enum AuthenticationProvider
{
    ManagedIdentity = 1,
    ServicePrincipal = 2,
    Certificate = 3,
    DefaultAzureCredential = 4,
    Interactive = 5
}
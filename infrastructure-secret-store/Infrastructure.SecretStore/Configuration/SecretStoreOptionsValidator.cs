namespace Infrastructure.SecretStore.Configuration;

public sealed class SecretStoreOptionsValidator : AbstractValidator<SecretStoreOptions>
{
    public SecretStoreOptionsValidator()
    {
        RuleFor(x => x.VaultUri)
            .NotEmpty()
            .When(x => x.Enabled && x.Provider == SecretStoreProvider.AzureKeyVault)
            .WithMessage("Vault URI is required when Azure Key Vault is enabled")
            .Must(BeValidUri)
            .When(x => !string.IsNullOrEmpty(x.VaultUri))
            .WithMessage("Vault URI must be a valid URI");
            
        RuleFor(x => x.VaultName)
            .NotEmpty()
            .When(x => x.Enabled && x.Provider == SecretStoreProvider.AzureKeyVault)
            .WithMessage("Vault name is required when Azure Key Vault is enabled");
            
        RuleFor(x => x.DefaultTimeout)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(1))
            .WithMessage("Default timeout must be at least 1 second")
            .LessThanOrEqualTo(TimeSpan.FromMinutes(10))
            .WithMessage("Default timeout cannot exceed 10 minutes");
            
        RuleFor(x => x.MaxRetryAttempts)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Max retry attempts cannot be negative")
            .LessThanOrEqualTo(10)
            .WithMessage("Max retry attempts cannot exceed 10");
            
        RuleFor(x => x.RetryDelay)
            .GreaterThanOrEqualTo(TimeSpan.Zero)
            .WithMessage("Retry delay cannot be negative")
            .LessThanOrEqualTo(TimeSpan.FromMinutes(1))
            .WithMessage("Retry delay cannot exceed 1 minute");
            
        RuleFor(x => x.CacheDuration)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(30))
            .WithMessage("Cache duration must be at least 30 seconds")
            .LessThanOrEqualTo(TimeSpan.FromHours(24))
            .WithMessage("Cache duration cannot exceed 24 hours")
            .When(x => x.EnableCaching);
        
        RuleFor(x => x.Authentication)
            .SetValidator(new AuthenticationOptionsValidator());
            
        RuleFor(x => x.Rotation)
            .SetValidator(new RotationOptionsValidator());
            
        RuleFor(x => x.Performance)
            .SetValidator(new PerformanceOptionsValidator());
    }
    
    private static bool BeValidUri(string? uri)
    {
        return Uri.TryCreate(uri, UriKind.Absolute, out var result) && 
               (result.Scheme == Uri.UriSchemeHttps || result.Scheme == Uri.UriSchemeHttp);
    }
}

public sealed class AuthenticationOptionsValidator : AbstractValidator<AuthenticationOptions>
{
    public AuthenticationOptionsValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty()
            .When(x => x.Provider == AuthenticationProvider.ServicePrincipal)
            .WithMessage("Client ID is required for Service Principal authentication");
            
        RuleFor(x => x.ClientSecret)
            .NotEmpty()
            .When(x => x.Provider == AuthenticationProvider.ServicePrincipal)
            .WithMessage("Client secret is required for Service Principal authentication");
            
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .When(x => x.Provider == AuthenticationProvider.ServicePrincipal)
            .WithMessage("Tenant ID is required for Service Principal authentication");
            
        RuleFor(x => x.CertificateThumbprint)
            .NotEmpty()
            .When(x => x.Provider == AuthenticationProvider.Certificate)
            .WithMessage("Certificate thumbprint is required for Certificate authentication");
            
        RuleFor(x => x.CertificatePath)
            .NotEmpty()
            .When(x => x.Provider == AuthenticationProvider.Certificate && string.IsNullOrEmpty(x.CertificateThumbprint))
            .WithMessage("Certificate path is required when thumbprint is not provided");
            
        RuleFor(x => x.Authority)
            .Must(BeValidUri)
            .When(x => !string.IsNullOrEmpty(x.Authority))
            .WithMessage("Authority must be a valid URI");
    }
    
    private static bool BeValidUri(string? uri)
    {
        return Uri.TryCreate(uri, UriKind.Absolute, out var result) && 
               result.Scheme == Uri.UriSchemeHttps;
    }
}

public sealed class RotationOptionsValidator : AbstractValidator<RotationOptions>
{
    public RotationOptionsValidator()
    {
        RuleFor(x => x.DefaultRotationInterval)
            .GreaterThanOrEqualTo(TimeSpan.FromDays(1))
            .WithMessage("Default rotation interval must be at least 1 day")
            .LessThanOrEqualTo(TimeSpan.FromDays(365))
            .WithMessage("Default rotation interval cannot exceed 365 days");
            
        RuleFor(x => x.RotationCheckInterval)
            .GreaterThanOrEqualTo(TimeSpan.FromMinutes(30))
            .WithMessage("Rotation check interval must be at least 30 minutes")
            .LessThanOrEqualTo(TimeSpan.FromDays(7))
            .WithMessage("Rotation check interval cannot exceed 7 days");
            
        RuleFor(x => x.MaxConcurrentRotations)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Max concurrent rotations must be at least 1")
            .LessThanOrEqualTo(20)
            .WithMessage("Max concurrent rotations cannot exceed 20");
            
        RuleFor(x => x.RotationTimeout)
            .GreaterThanOrEqualTo(TimeSpan.FromMinutes(1))
            .WithMessage("Rotation timeout must be at least 1 minute")
            .LessThanOrEqualTo(TimeSpan.FromHours(1))
            .WithMessage("Rotation timeout cannot exceed 1 hour");
            
        RuleFor(x => x.MaxVersionsToKeep)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Max versions to keep must be at least 1")
            .LessThanOrEqualTo(100)
            .WithMessage("Max versions to keep cannot exceed 100")
            .When(x => x.KeepOldVersions);
    }
}

public sealed class PerformanceOptionsValidator : AbstractValidator<PerformanceOptions>
{
    public PerformanceOptionsValidator()
    {
        RuleFor(x => x.MaxConcurrentRequests)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Max concurrent requests must be at least 1")
            .LessThanOrEqualTo(1000)
            .WithMessage("Max concurrent requests cannot exceed 1000");
            
        RuleFor(x => x.RequestTimeout)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(1))
            .WithMessage("Request timeout must be at least 1 second")
            .LessThanOrEqualTo(TimeSpan.FromMinutes(10))
            .WithMessage("Request timeout cannot exceed 10 minutes");
            
        RuleFor(x => x.MaxConnectionsPerEndpoint)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Max connections per endpoint must be at least 1")
            .LessThanOrEqualTo(100)
            .WithMessage("Max connections per endpoint cannot exceed 100")
            .When(x => x.EnableConnectionPooling);
            
        RuleFor(x => x.PooledConnectionIdleTimeout)
            .GreaterThanOrEqualTo(TimeSpan.FromSeconds(30))
            .WithMessage("Pooled connection idle timeout must be at least 30 seconds")
            .LessThanOrEqualTo(TimeSpan.FromMinutes(30))
            .WithMessage("Pooled connection idle timeout cannot exceed 30 minutes")
            .When(x => x.EnableConnectionPooling);
            
        RuleFor(x => x.PooledConnectionLifetime)
            .GreaterThanOrEqualTo(TimeSpan.FromMinutes(1))
            .WithMessage("Pooled connection lifetime must be at least 1 minute")
            .LessThanOrEqualTo(TimeSpan.FromHours(24))
            .WithMessage("Pooled connection lifetime cannot exceed 24 hours")
            .When(x => x.EnableConnectionPooling);
    }
}
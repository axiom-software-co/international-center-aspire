namespace Infrastructure.SecretStore.Abstractions;

public interface IKeyVaultManager
{
    // Secret management
    Task<string?> GetSecretValueAsync(string secretName, string? version = null, 
        CancellationToken cancellationToken = default);
    
    Task<KeyVaultSecret?> GetSecretAsync(string secretName, string? version = null, 
        CancellationToken cancellationToken = default);
    
    Task<KeyVaultSecret> SetSecretAsync(string secretName, string secretValue, 
        IDictionary<string, string>? tags = null, CancellationToken cancellationToken = default);
    
    Task<DeletedSecret> DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);
    
    // Key management
    Task<KeyVaultKey?> GetKeyAsync(string keyName, string? version = null, 
        CancellationToken cancellationToken = default);
    
    Task<KeyVaultKey> CreateKeyAsync(string keyName, KeyType keyType, 
        CreateKeyOptions? options = null, CancellationToken cancellationToken = default);
    
    Task<DeletedKey> DeleteKeyAsync(string keyName, CancellationToken cancellationToken = default);
    
    // Certificate management
    Task<KeyVaultCertificate?> GetCertificateAsync(string certificateName, string? version = null, 
        CancellationToken cancellationToken = default);
    
    Task<KeyVaultCertificateWithPolicy> CreateCertificateAsync(string certificateName, 
        CertificatePolicy policy, CancellationToken cancellationToken = default);
    
    Task<DeletedCertificate> DeleteCertificateAsync(string certificateName, 
        CancellationToken cancellationToken = default);
    
    // Health and status
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    
    Task<KeyVaultStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}
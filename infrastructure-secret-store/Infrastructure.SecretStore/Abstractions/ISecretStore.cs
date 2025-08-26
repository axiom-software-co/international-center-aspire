namespace Infrastructure.SecretStore.Abstractions;

public interface ISecretStore
{
    Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
    
    Task<TOptions?> GetSecretAsync<TOptions>(string secretName, CancellationToken cancellationToken = default) 
        where TOptions : class;
    
    Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);
    
    Task SetSecretAsync<TOptions>(string secretName, TOptions options, CancellationToken cancellationToken = default)
        where TOptions : class;
    
    Task<bool> DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);
    
    Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<SecretProperties>> ListSecretsAsync(CancellationToken cancellationToken = default);
    
    Task<SecretInfo?> GetSecretInfoAsync(string secretName, CancellationToken cancellationToken = default);
}
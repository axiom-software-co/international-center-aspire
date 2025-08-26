using System.Diagnostics;
using Azure;

namespace Infrastructure.SecretStore.Services;

public sealed class AzureKeyVaultManager : IKeyVaultManager, IDisposable
{
    private readonly SecretClient _secretClient;
    private readonly KeyClient _keyClient;
    private readonly CertificateClient _certificateClient;
    private readonly ILogger<AzureKeyVaultManager> _logger;
    private readonly SecretStoreOptions _options;
    private readonly SemaphoreSlim _semaphore;

    public AzureKeyVaultManager(
        SecretClient secretClient,
        KeyClient keyClient,
        CertificateClient certificateClient,
        ILogger<AzureKeyVaultManager> logger,
        IOptions<SecretStoreOptions> options)
    {
        _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
        _keyClient = keyClient ?? throw new ArgumentNullException(nameof(keyClient));
        _certificateClient = certificateClient ?? throw new ArgumentNullException(nameof(certificateClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        _semaphore = new SemaphoreSlim(_options.Performance.MaxConcurrentRequests, _options.Performance.MaxConcurrentRequests);
    }

    public async Task<string?> GetSecretValueAsync(string secretName, string? version = null, 
        CancellationToken cancellationToken = default)
    {
        var secret = await GetSecretAsync(secretName, version, cancellationToken);
        return secret?.Value;
    }

    public async Task<KeyVaultSecret?> GetSecretAsync(string secretName, string? version = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var response = await _secretClient.GetSecretAsync(secretName, version, cancellationToken);
            
            _logger.LogDebug("Retrieved secret {SecretName} in {Duration}ms", 
                secretName, stopwatch.Elapsed.TotalMilliseconds);
                
            return response?.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve secret {SecretName}: {StatusCode} - {Message}", 
                secretName, ex.Status, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving secret {SecretName}", secretName);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<KeyVaultSecret> SetSecretAsync(string secretName, string secretValue, 
        IDictionary<string, string>? tags = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        if (string.IsNullOrEmpty(secretValue))
        {
            throw new ArgumentException("Secret value cannot be null or empty", nameof(secretValue));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var secret = new KeyVaultSecret(secretName, secretValue);
            
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    secret.Properties.Tags[tag.Key] = tag.Value;
                }
            }
            
            var response = await _secretClient.SetSecretAsync(secret, cancellationToken);
            
            _logger.LogInformation("Set secret {SecretName} in {Duration}ms", 
                secretName, stopwatch.Elapsed.TotalMilliseconds);
                
            return response.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to set secret {SecretName}: {StatusCode} - {Message}", 
                secretName, ex.Status, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error setting secret {SecretName}", secretName);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<DeletedSecret> DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var deleteOperation = await _secretClient.StartDeleteSecretAsync(secretName, cancellationToken);
            var deletedSecret = await deleteOperation.WaitForCompletionAsync(cancellationToken);
            
            _logger.LogInformation("Deleted secret {SecretName} in {Duration}ms", 
                secretName, stopwatch.Elapsed.TotalMilliseconds);
                
            return deletedSecret.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete secret {SecretName}: {StatusCode} - {Message}", 
                secretName, ex.Status, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting secret {SecretName}", secretName);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<KeyVaultKey?> GetKeyAsync(string keyName, string? version = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(keyName))
        {
            throw new ArgumentException("Key name cannot be null or empty", nameof(keyName));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var response = await _keyClient.GetKeyAsync(keyName, version, cancellationToken);
            
            _logger.LogDebug("Retrieved key {KeyName} in {Duration}ms", 
                keyName, stopwatch.Elapsed.TotalMilliseconds);
                
            return response?.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve key {KeyName}: {StatusCode} - {Message}", 
                keyName, ex.Status, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving key {KeyName}", keyName);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<KeyVaultKey> CreateKeyAsync(string keyName, KeyType keyType, 
        CreateKeyOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(keyName))
        {
            throw new ArgumentException("Key name cannot be null or empty", nameof(keyName));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var response = await _keyClient.CreateKeyAsync(keyName, keyType, options, cancellationToken);
            
            _logger.LogInformation("Created key {KeyName} of type {KeyType} in {Duration}ms", 
                keyName, keyType, stopwatch.Elapsed.TotalMilliseconds);
                
            return response.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to create key {KeyName}: {StatusCode} - {Message}", 
                keyName, ex.Status, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating key {KeyName}", keyName);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<DeletedKey> DeleteKeyAsync(string keyName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(keyName))
        {
            throw new ArgumentException("Key name cannot be null or empty", nameof(keyName));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var deleteOperation = await _keyClient.StartDeleteKeyAsync(keyName, cancellationToken);
            var deletedKey = await deleteOperation.WaitForCompletionAsync(cancellationToken);
            
            _logger.LogInformation("Deleted key {KeyName} in {Duration}ms", 
                keyName, stopwatch.Elapsed.TotalMilliseconds);
                
            return deletedKey.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete key {KeyName}: {StatusCode} - {Message}", 
                keyName, ex.Status, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting key {KeyName}", keyName);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<KeyVaultCertificate?> GetCertificateAsync(string certificateName, string? version = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(certificateName))
        {
            throw new ArgumentException("Certificate name cannot be null or empty", nameof(certificateName));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var response = await _certificateClient.GetCertificateAsync(certificateName, version, cancellationToken);
            
            _logger.LogDebug("Retrieved certificate {CertificateName} in {Duration}ms", 
                certificateName, stopwatch.Elapsed.TotalMilliseconds);
                
            return response?.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve certificate {CertificateName}: {StatusCode} - {Message}", 
                certificateName, ex.Status, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving certificate {CertificateName}", certificateName);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<KeyVaultCertificateWithPolicy> CreateCertificateAsync(string certificateName, 
        CertificatePolicy policy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(certificateName))
        {
            throw new ArgumentException("Certificate name cannot be null or empty", nameof(certificateName));
        }

        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var createOperation = await _certificateClient.StartCreateCertificateAsync(certificateName, policy, cancellationToken);
            var certificate = await createOperation.WaitForCompletionAsync(cancellationToken);
            
            _logger.LogInformation("Created certificate {CertificateName} in {Duration}ms", 
                certificateName, stopwatch.Elapsed.TotalMilliseconds);
                
            return certificate.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to create certificate {CertificateName}: {StatusCode} - {Message}", 
                certificateName, ex.Status, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating certificate {CertificateName}", certificateName);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<DeletedCertificate> DeleteCertificateAsync(string certificateName, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(certificateName))
        {
            throw new ArgumentException("Certificate name cannot be null or empty", nameof(certificateName));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var deleteOperation = await _certificateClient.StartDeleteCertificateAsync(certificateName, cancellationToken);
            var deletedCertificate = await deleteOperation.WaitForCompletionAsync(cancellationToken);
            
            _logger.LogInformation("Deleted certificate {CertificateName} in {Duration}ms", 
                certificateName, stopwatch.Elapsed.TotalMilliseconds);
                
            return deletedCertificate.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete certificate {CertificateName}: {StatusCode} - {Message}", 
                certificateName, ex.Status, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting certificate {CertificateName}", certificateName);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await GetStatusAsync(cancellationToken);
            return status.IsHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed");
            return false;
        }
    }

    public async Task<KeyVaultStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var status = new KeyVaultStatus
        {
            VaultName = _options.VaultName ?? "Unknown",
            VaultUri = string.IsNullOrEmpty(_options.VaultUri) ? null : new Uri(_options.VaultUri),
            CheckedAt = DateTimeOffset.UtcNow
        };

        try
        {
            // Test basic connectivity by listing secrets (with minimal results)
            var secretPages = _secretClient.GetPropertiesOfSecretsAsync(cancellationToken);
            var secretCount = 0;
            
            await foreach (var secretPage in secretPages)
            {
                secretCount++;
                if (secretCount >= 10) break; // Limit for performance
            }
            
            stopwatch.Stop();
            
            return status with 
            {
                IsHealthy = true,
                ConnectionStatus = KeyVaultConnectionStatus.Connected,
                ResponseTime = stopwatch.Elapsed,
                Metrics = status.Metrics with { SecretCount = secretCount }
            };
        }
        catch (RequestFailedException ex)
        {
            stopwatch.Stop();
            
            var connectionStatus = ex.Status switch
            {
                401 => KeyVaultConnectionStatus.AuthenticationError,
                403 => KeyVaultConnectionStatus.AuthorizationError,
                404 => KeyVaultConnectionStatus.NotFound,
                429 => KeyVaultConnectionStatus.Throttled,
                503 => KeyVaultConnectionStatus.ServiceUnavailable,
                _ => KeyVaultConnectionStatus.Disconnected
            };
            
            return status with 
            {
                IsHealthy = false,
                ConnectionStatus = connectionStatus,
                ResponseTime = stopwatch.Elapsed,
                Error = $"{ex.Status}: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            return status with 
            {
                IsHealthy = false,
                ConnectionStatus = KeyVaultConnectionStatus.Disconnected,
                ResponseTime = stopwatch.Elapsed,
                Error = ex.Message
            };
        }
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}
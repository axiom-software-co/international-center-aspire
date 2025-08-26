using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Infrastructure.SecretStore.Services;

public sealed class SecretStore : ISecretStore, IDisposable
{
    private readonly IKeyVaultManager _keyVaultManager;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecretStore> _logger;
    private readonly SecretStoreOptions _options;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _secretAccessTimes = new();

    public SecretStore(
        IKeyVaultManager keyVaultManager,
        IMemoryCache cache,
        ILogger<SecretStore> logger,
        IOptions<SecretStoreOptions> options)
    {
        _keyVaultManager = keyVaultManager ?? throw new ArgumentNullException(nameof(keyVaultManager));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        if (!_options.Enabled)
        {
            _logger.LogDebug("Secret store is disabled, returning null for secret {SecretName}", secretName);
            return null;
        }

        var cacheKey = $"secret:{secretName}";
        
        // Try cache first if caching is enabled
        if (_options.EnableCaching && _cache.TryGetValue<string>(cacheKey, out var cachedValue))
        {
            _logger.LogDebug("Retrieved secret {SecretName} from cache", secretName);
            UpdateAccessTime(secretName);
            return cachedValue;
        }

        try
        {
            var secretValue = await _keyVaultManager.GetSecretValueAsync(secretName, cancellationToken: cancellationToken);
            
            if (secretValue != null)
            {
                // Cache the secret if caching is enabled
                if (_options.EnableCaching)
                {
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _options.CacheDuration,
                        SlidingExpiration = _options.CacheDuration / 2,
                        Priority = CacheItemPriority.High
                    };
                    
                    _cache.Set(cacheKey, secretValue, cacheOptions);
                    _logger.LogDebug("Cached secret {SecretName} for {CacheDuration}", 
                        secretName, _options.CacheDuration);
                }
                
                UpdateAccessTime(secretName);
                _logger.LogDebug("Retrieved secret {SecretName} from Key Vault", secretName);
            }
            else
            {
                _logger.LogWarning("Secret {SecretName} not found in Key Vault", secretName);
            }
            
            return secretValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret {SecretName}", secretName);
            throw;
        }
    }

    public async Task<TOptions?> GetSecretAsync<TOptions>(string secretName, 
        CancellationToken cancellationToken = default) where TOptions : class
    {
        var secretJson = await GetSecretAsync(secretName, cancellationToken);
        
        if (string.IsNullOrEmpty(secretJson))
        {
            return null;
        }

        try
        {
            var options = JsonSerializer.Deserialize<TOptions>(secretJson);
            _logger.LogDebug("Deserialized secret {SecretName} to type {Type}", secretName, typeof(TOptions).Name);
            return options;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize secret {SecretName} to type {Type}", 
                secretName, typeof(TOptions).Name);
            throw;
        }
    }

    public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        if (string.IsNullOrEmpty(secretValue))
        {
            throw new ArgumentException("Secret value cannot be null or empty", nameof(secretValue));
        }

        if (!_options.Enabled)
        {
            _logger.LogDebug("Secret store is disabled, skipping set for secret {SecretName}", secretName);
            return;
        }

        try
        {
            var tags = new Dictionary<string, string>
            {
                ["CreatedBy"] = "SecretStore",
                ["CreatedAt"] = DateTimeOffset.UtcNow.ToString("O"),
                ["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            };

            await _keyVaultManager.SetSecretAsync(secretName, secretValue, tags, cancellationToken);
            
            // Invalidate cache if caching is enabled
            if (_options.EnableCaching)
            {
                var cacheKey = $"secret:{secretName}";
                _cache.Remove(cacheKey);
                _logger.LogDebug("Invalidated cache for secret {SecretName}", secretName);
            }
            
            UpdateAccessTime(secretName);
            _logger.LogInformation("Successfully set secret {SecretName}", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret {SecretName}", secretName);
            throw;
        }
    }

    public async Task SetSecretAsync<TOptions>(string secretName, TOptions options, 
        CancellationToken cancellationToken = default) where TOptions : class
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        try
        {
            var secretJson = JsonSerializer.Serialize(options, new JsonSerializerOptions 
            { 
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await SetSecretAsync(secretName, secretJson, cancellationToken);
            _logger.LogDebug("Serialized and set secret {SecretName} from type {Type}", 
                secretName, typeof(TOptions).Name);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize options of type {Type} for secret {SecretName}", 
                typeof(TOptions).Name, secretName);
            throw;
        }
    }

    public async Task<bool> DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        if (!_options.Enabled)
        {
            _logger.LogDebug("Secret store is disabled, skipping delete for secret {SecretName}", secretName);
            return false;
        }

        try
        {
            await _keyVaultManager.DeleteSecretAsync(secretName, cancellationToken);
            
            // Remove from cache if caching is enabled
            if (_options.EnableCaching)
            {
                var cacheKey = $"secret:{secretName}";
                _cache.Remove(cacheKey);
                _logger.LogDebug("Removed secret {SecretName} from cache", secretName);
            }
            
            // Remove access time tracking
            _secretAccessTimes.TryRemove(secretName, out _);
            
            _logger.LogInformation("Successfully deleted secret {SecretName}", secretName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret {SecretName}", secretName);
            return false;
        }
    }

    public async Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        if (!_options.Enabled)
        {
            return false;
        }

        try
        {
            var secret = await _keyVaultManager.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            var exists = secret != null;
            
            _logger.LogDebug("Secret {SecretName} exists: {Exists}", secretName, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if secret {SecretName} exists", secretName);
            return false;
        }
    }

    public async Task<IReadOnlyList<SecretProperties>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return Array.Empty<SecretProperties>();
        }

        try
        {
            var secrets = new List<SecretProperties>();
            
            // Get the secret client from the Key Vault manager
            // This is a simplified approach - in a real implementation, you'd expose this through the interface
            await foreach (var secretProperty in GetSecretPropertiesAsync(cancellationToken))
            {
                secrets.Add(secretProperty);
            }
            
            _logger.LogDebug("Listed {Count} secrets from Key Vault", secrets.Count);
            return secrets.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list secrets from Key Vault");
            throw;
        }
    }

    public async Task<SecretInfo?> GetSecretInfoAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        if (!_options.Enabled)
        {
            return null;
        }

        try
        {
            var secret = await _keyVaultManager.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            
            if (secret == null)
            {
                _logger.LogDebug("Secret {SecretName} not found", secretName);
                return null;
            }

            var secretInfo = new SecretInfo
            {
                Name = secret.Name,
                Value = secret.Value, // Consider if you want to expose the value here
                Version = secret.Properties.Version,
                CreatedOn = secret.Properties.CreatedOn ?? DateTimeOffset.MinValue,
                UpdatedOn = secret.Properties.UpdatedOn ?? DateTimeOffset.MinValue,
                ExpiresOn = secret.Properties.ExpiresOn,
                NotBefore = secret.Properties.NotBefore,
                Enabled = secret.Properties.Enabled ?? true,
                Tags = secret.Properties.Tags?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
                ContentType = secret.Properties.ContentType ?? string.Empty,
                Id = secret.Properties.Id,
                VaultUri = secret.Properties.VaultUri,
                Managed = secret.Properties.Managed,
                RecoveryLevel = secret.Properties.RecoveryLevel
            };

            _logger.LogDebug("Retrieved info for secret {SecretName}", secretName);
            return secretInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret info for {SecretName}", secretName);
            throw;
        }
    }

    private void UpdateAccessTime(string secretName)
    {
        _secretAccessTimes.AddOrUpdate(secretName, DateTimeOffset.UtcNow, (key, oldValue) => DateTimeOffset.UtcNow);
    }

    // This would need to be exposed through the IKeyVaultManager interface in a real implementation
    private async IAsyncEnumerable<SecretProperties> GetSecretPropertiesAsync(CancellationToken cancellationToken)
    {
        // This is a placeholder - you'd need to modify IKeyVaultManager to expose this functionality
        await Task.CompletedTask;
        yield break;
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}
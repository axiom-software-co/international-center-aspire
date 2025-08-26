namespace Infrastructure.SecretStore.HealthChecks;

public sealed class KeyVaultHealthCheck : IHealthCheck
{
    private readonly IKeyVaultManager _keyVaultManager;
    private readonly ILogger<KeyVaultHealthCheck> _logger;
    private readonly SecretStoreOptions _options;

    public KeyVaultHealthCheck(
        IKeyVaultManager keyVaultManager,
        ILogger<KeyVaultHealthCheck> logger,
        IOptions<SecretStoreOptions> options)
    {
        _keyVaultManager = keyVaultManager ?? throw new ArgumentNullException(nameof(keyVaultManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return HealthCheckResult.Healthy("Secret store is disabled");
        }

        try
        {
            var status = await _keyVaultManager.GetStatusAsync(cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                ["vault_name"] = status.VaultName,
                ["vault_uri"] = status.VaultUri?.ToString() ?? "Unknown",
                ["connection_status"] = status.ConnectionStatus.ToString(),
                ["response_time_ms"] = status.ResponseTime.TotalMilliseconds,
                ["checked_at"] = status.CheckedAt.ToString("O"),
                ["secret_count"] = status.Metrics.SecretCount,
                ["key_count"] = status.Metrics.KeyCount,
                ["certificate_count"] = status.Metrics.CertificateCount
            };

            if (status.IsHealthy)
            {
                var description = $"Key Vault is healthy (response time: {status.ResponseTime.TotalMilliseconds:F0}ms)";
                
                _logger.LogDebug("Key Vault health check passed: {Description}", description);
                
                return HealthCheckResult.Healthy(description, data);
            }
            else
            {
                var description = $"Key Vault is unhealthy: {status.ConnectionStatus}";
                if (!string.IsNullOrEmpty(status.Error))
                {
                    description += $" - {status.Error}";
                    data["error"] = status.Error;
                }
                
                _logger.LogWarning("Key Vault health check failed: {Description}", description);
                
                // Determine if this is degraded or unhealthy
                var healthStatus = status.ConnectionStatus switch
                {
                    KeyVaultConnectionStatus.Throttled => HealthStatus.Degraded,
                    KeyVaultConnectionStatus.AuthorizationError => HealthStatus.Degraded,
                    _ => HealthStatus.Unhealthy
                };
                
                return new HealthCheckResult(healthStatus, description, data: data);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            const string description = "Key Vault health check was cancelled";
            _logger.LogWarning(description);
            return HealthCheckResult.Unhealthy(description);
        }
        catch (Exception ex)
        {
            var description = $"Key Vault health check failed with exception: {ex.Message}";
            var data = new Dictionary<string, object>
            {
                ["exception"] = ex.Message,
                ["exception_type"] = ex.GetType().Name
            };
            
            _logger.LogError(ex, "Key Vault health check failed with exception");
            
            return HealthCheckResult.Unhealthy(description, ex, data);
        }
    }
}
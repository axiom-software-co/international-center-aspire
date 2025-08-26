using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.SecretStore.Services;

public sealed class SecretRotationService : ISecretRotationService, IDisposable
{
    private readonly IKeyVaultManager _keyVaultManager;
    private readonly ILogger<SecretRotationService> _logger;
    private readonly SecretStoreOptions _options;
    private readonly SemaphoreSlim _rotationSemaphore;
    private readonly ConcurrentDictionary<string, SecretRotationSchedule> _rotationSchedules = new();
    private readonly ConcurrentDictionary<string, List<SecretRotationHistory>> _rotationHistory = new();
    private readonly Timer? _rotationTimer;

    public SecretRotationService(
        IKeyVaultManager keyVaultManager,
        ILogger<SecretRotationService> logger,
        IOptions<SecretStoreOptions> options)
    {
        _keyVaultManager = keyVaultManager ?? throw new ArgumentNullException(nameof(keyVaultManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        _rotationSemaphore = new SemaphoreSlim(_options.Rotation.MaxConcurrentRotations, 
            _options.Rotation.MaxConcurrentRotations);

        if (_options.Rotation.EnableAutoRotation)
        {
            _rotationTimer = new Timer(CheckForDueRotations, null, 
                _options.Rotation.RotationCheckInterval, 
                _options.Rotation.RotationCheckInterval);
            
            _logger.LogInformation("Auto-rotation enabled with check interval: {Interval}", 
                _options.Rotation.RotationCheckInterval);
        }
    }

    public async Task<SecretRotationResult> RotateSecretAsync(string secretName, 
        CancellationToken cancellationToken = default)
    {
        return await RotateSecretAsync(secretName, GenerateRandomSecret, cancellationToken);
    }

    public async Task<SecretRotationResult> RotateSecretAsync(string secretName, 
        Func<string, Task<string>> secretGenerator, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        if (secretGenerator == null)
        {
            throw new ArgumentNullException(nameof(secretGenerator));
        }

        await _rotationSemaphore.WaitAsync(cancellationToken);
        var startTime = DateTimeOffset.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting rotation for secret {SecretName}", secretName);

            // Get current secret info
            var currentSecret = await _keyVaultManager.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            var oldVersion = currentSecret?.Properties?.Version;

            // Generate new secret value
            var newSecretValue = await secretGenerator(secretName);

            if (string.IsNullOrEmpty(newSecretValue))
            {
                throw new InvalidOperationException("Secret generator returned null or empty value");
            }

            // Set the new secret
            var newSecret = await _keyVaultManager.SetSecretAsync(secretName, newSecretValue, 
                new Dictionary<string, string>
                {
                    ["RotatedAt"] = startTime.ToString("O"),
                    ["RotationType"] = SecretRotationType.OnDemand.ToString(),
                    ["PreviousVersion"] = oldVersion ?? "Unknown"
                }, 
                cancellationToken);

            stopwatch.Stop();

            var result = new SecretRotationResult
            {
                SecretName = secretName,
                Success = true,
                OldVersion = oldVersion,
                NewVersion = newSecret.Properties.Version,
                RotatedAt = startTime,
                Duration = stopwatch.Elapsed,
                RotationType = SecretRotationType.OnDemand
            };

            // Record the rotation in history
            RecordRotationHistory(result);

            // Update rotation schedule if it exists
            if (_rotationSchedules.TryGetValue(secretName, out var schedule))
            {
                var updatedSchedule = schedule with 
                { 
                    LastRotationDate = startTime,
                    NextRotationDate = startTime.Add(schedule.RotationInterval)
                };
                _rotationSchedules.TryUpdate(secretName, updatedSchedule, schedule);
            }

            _logger.LogInformation("Successfully rotated secret {SecretName} from version {OldVersion} to {NewVersion} in {Duration}ms",
                secretName, oldVersion, result.NewVersion, stopwatch.Elapsed.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            var result = new SecretRotationResult
            {
                SecretName = secretName,
                Success = false,
                Error = ex.Message,
                RotatedAt = startTime,
                Duration = stopwatch.Elapsed,
                RotationType = SecretRotationType.OnDemand
            };

            RecordRotationHistory(result);

            _logger.LogError(ex, "Failed to rotate secret {SecretName} after {Duration}ms", 
                secretName, stopwatch.Elapsed.TotalMilliseconds);

            return result;
        }
        finally
        {
            _rotationSemaphore.Release();
        }
    }

    public async Task<bool> IsSecretDueForRotationAsync(string secretName, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        if (!_rotationSchedules.TryGetValue(secretName, out var schedule))
        {
            // No schedule means not due for rotation
            return false;
        }

        if (!schedule.AutoRotationEnabled)
        {
            return false;
        }

        var isDue = DateTimeOffset.UtcNow >= schedule.NextRotationDate;
        
        _logger.LogDebug("Secret {SecretName} due for rotation: {IsDue} (next rotation: {NextRotation})",
            secretName, isDue, schedule.NextRotationDate);

        await Task.CompletedTask; // Make method truly async for consistency
        return isDue;
    }

    public async Task<IReadOnlyList<string>> GetSecretsDueForRotationAsync(
        CancellationToken cancellationToken = default)
    {
        var dueSecrets = new List<string>();
        var now = DateTimeOffset.UtcNow;

        foreach (var kvp in _rotationSchedules)
        {
            if (kvp.Value.AutoRotationEnabled && now >= kvp.Value.NextRotationDate)
            {
                dueSecrets.Add(kvp.Key);
            }
        }

        _logger.LogDebug("Found {Count} secrets due for rotation", dueSecrets.Count);
        
        await Task.CompletedTask;
        return dueSecrets.AsReadOnly();
    }

    public async Task<SecretRotationSchedule?> GetRotationScheduleAsync(string secretName, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        _rotationSchedules.TryGetValue(secretName, out var schedule);
        
        await Task.CompletedTask;
        return schedule;
    }

    public async Task SetRotationScheduleAsync(string secretName, SecretRotationSchedule schedule, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        if (schedule == null)
        {
            throw new ArgumentNullException(nameof(schedule));
        }

        var updatedSchedule = schedule with { SecretName = secretName };
        _rotationSchedules.AddOrUpdate(secretName, updatedSchedule, (key, oldValue) => updatedSchedule);

        _logger.LogInformation("Set rotation schedule for secret {SecretName}: interval {Interval}, next rotation {NextRotation}",
            secretName, schedule.RotationInterval, schedule.NextRotationDate);

        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<SecretRotationHistory>> GetRotationHistoryAsync(string secretName, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        if (!_rotationHistory.TryGetValue(secretName, out var history))
        {
            return Array.Empty<SecretRotationHistory>();
        }

        // Return a copy sorted by rotation date (most recent first)
        var sortedHistory = history
            .OrderByDescending(h => h.RotatedAt)
            .ToList()
            .AsReadOnly();

        await Task.CompletedTask;
        return sortedHistory;
    }

    private async void CheckForDueRotations(object? state)
    {
        try
        {
            var dueSecrets = await GetSecretsDueForRotationAsync();
            
            if (dueSecrets.Count == 0)
            {
                _logger.LogDebug("No secrets due for rotation");
                return;
            }

            _logger.LogInformation("Found {Count} secrets due for auto-rotation", dueSecrets.Count);

            var rotationTasks = dueSecrets.Select(async secretName =>
            {
                try
                {
                    var result = await RotateSecretAsync(secretName, GenerateRandomSecret, CancellationToken.None);
                    if (result.Success)
                    {
                        _logger.LogInformation("Auto-rotated secret {SecretName} successfully", secretName);
                    }
                    else
                    {
                        _logger.LogWarning("Auto-rotation failed for secret {SecretName}: {Error}", 
                            secretName, result.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception during auto-rotation of secret {SecretName}", secretName);
                }
            });

            await Task.WhenAll(rotationTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during scheduled rotation check");
        }
    }

    private void RecordRotationHistory(SecretRotationResult result)
    {
        var historyEntry = new SecretRotationHistory
        {
            SecretName = result.SecretName,
            RotatedAt = result.RotatedAt,
            OldVersion = result.OldVersion,
            NewVersion = result.NewVersion,
            RotationType = result.RotationType,
            Success = result.Success,
            Error = result.Error,
            Duration = result.Duration,
            RotatedBy = "SecretRotationService",
            Reason = result.RotationType == SecretRotationType.OnDemand ? "Manual rotation" : "Scheduled rotation"
        };

        _rotationHistory.AddOrUpdate(result.SecretName, 
            new List<SecretRotationHistory> { historyEntry },
            (key, existingHistory) =>
            {
                existingHistory.Add(historyEntry);
                
                // Keep only the last 100 entries per secret to prevent memory growth
                if (existingHistory.Count > 100)
                {
                    existingHistory.RemoveRange(0, existingHistory.Count - 100);
                }
                
                return existingHistory;
            });
    }

    private static async Task<string> GenerateRandomSecret(string secretName)
    {
        await Task.CompletedTask; // Make the method async
        
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";
        const int length = 64;
        
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        
        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            result.Append(chars[bytes[i] % chars.Length]);
        }
        
        return result.ToString();
    }

    public void Dispose()
    {
        _rotationTimer?.Dispose();
        _rotationSemaphore?.Dispose();
    }
}
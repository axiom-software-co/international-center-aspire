using Infrastructure.Cache.Abstractions;
using Infrastructure.Cache.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Infrastructure.Cache.Base;

/// <summary>
/// Generic rate limiting service base implementation for protecting APIs and resources.
/// INFRASTRUCTURE: Generic rate limiting patterns reusable by any domain
/// </summary>
public abstract class BaseRateLimitingService : IRateLimitingService
{
    private readonly IRedisConnectionFactory _connectionFactory;
    private readonly RateLimitingCacheOptions _options;
    private readonly ILogger<BaseRateLimitingService> _logger;
    
    private long _totalRequests;
    private long _allowedRequests;
    private long _deniedRequests;
    private int _uniqueClients;
    private int _cleanupOperations;
    private long _entriesCleanedUp;
    private readonly DateTime _startTime = DateTime.UtcNow;
    private readonly HashSet<string> _seenClients = new();
    private readonly object _lockObject = new();

    protected BaseRateLimitingService(
        IRedisConnectionFactory connectionFactory,
        IOptions<RedisConnectionOptions> options,
        ILogger<BaseRateLimitingService> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _options = options.Value?.RateLimiting ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RateLimitResult> IsRequestAllowedAsync(
        string clientId, 
        string resource, 
        int limit, 
        TimeSpan window, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero((int)window.TotalSeconds);

        Interlocked.Increment(ref _totalRequests);
        TrackUniqueClient(clientId);

        try
        {
            var database = await _connectionFactory.GetRateLimitingDatabaseAsync(cancellationToken);
            var key = BuildRateLimitKey(clientId, resource);
            var windowSeconds = (int)window.TotalSeconds;
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var windowStart = currentTime - windowSeconds;

            // Use sliding window algorithm
            var result = await ExecuteSlidingWindowRateLimitAsync(
                database, key, limit, currentTime, windowStart, windowSeconds);

            if (result.IsAllowed)
            {
                Interlocked.Increment(ref _allowedRequests);
                _logger.LogDebug(
                    "Rate limit check ALLOWED for client {ClientId}, resource {Resource}: {CurrentCount}/{Limit}",
                    clientId, resource, result.CurrentCount, limit);
            }
            else
            {
                Interlocked.Increment(ref _deniedRequests);
                _logger.LogWarning(
                    "Rate limit check DENIED for client {ClientId}, resource {Resource}: {CurrentCount}/{Limit}",
                    clientId, resource, result.CurrentCount, limit);
            }

            return result;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _deniedRequests);
            _logger.LogError(ex, 
                "Failed to check rate limit for client {ClientId}, resource {Resource}. Denying request for safety.",
                clientId, resource);
            
            // Fail-safe: deny request on error
            return RateLimitResult.Denied(0, limit, window, window, null);
        }
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(
        string clientId, 
        RateLimitPolicy policy, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentNullException.ThrowIfNull(policy);

        // Check if resource matches policy pattern
        if (!string.IsNullOrEmpty(policy.ResourcePattern))
        {
            // Simple wildcard pattern matching for now
            // In production, you might want more sophisticated pattern matching
            var matches = policy.ResourcePattern == "*" || 
                         policy.ResourcePattern.Equals(policy.ResourcePattern, StringComparison.OrdinalIgnoreCase);
            
            if (!matches)
            {
                _logger.LogDebug("Resource pattern {Pattern} does not match for client {ClientId}", 
                    policy.ResourcePattern, clientId);
                return RateLimitResult.Allowed(0, policy.Limit, policy.Window, policy.Name);
            }
        }

        var result = await IsRequestAllowedAsync(clientId, policy.ResourcePattern, policy.Limit, policy.Window, cancellationToken);
        
        // Update result with policy name
        return new RateLimitResult
        {
            IsAllowed = result.IsAllowed,
            CurrentCount = result.CurrentCount,
            Limit = result.Limit,
            ResetTime = result.ResetTime,
            RetryAfter = result.RetryAfter,
            PolicyName = policy.Name,
            Metadata = result.Metadata,
            Timestamp = result.Timestamp
        };
    }

    public async Task<bool> RecordRequestAsync(
        string clientId, 
        string resource, 
        int cost = 1, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cost);

        try
        {
            var database = await _connectionFactory.GetRateLimitingDatabaseAsync(cancellationToken);
            var key = BuildRateLimitKey(clientId, resource);
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Record request with timestamp and cost
            var requestData = $"{currentTime}:{cost}";
            await database.SetAddAsync(key, requestData);
            
            // Set expiration for cleanup
            var defaultWindow = TimeSpan.FromMinutes(_options.DefaultWindowMinutes);
            await database.KeyExpireAsync(key, defaultWindow);

            _logger.LogDebug("Recorded request for client {ClientId}, resource {Resource}, cost {Cost}",
                clientId, resource, cost);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record request for client {ClientId}, resource {Resource}",
                clientId, resource);
            return false;
        }
    }

    public async Task<RateLimitStatus> GetRateLimitStatusAsync(
        string clientId, 
        string resource, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);

        try
        {
            var database = await _connectionFactory.GetRateLimitingDatabaseAsync(cancellationToken);
            var key = BuildRateLimitKey(clientId, resource);
            var defaultWindow = TimeSpan.FromMinutes(_options.DefaultWindowMinutes);
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var windowStart = currentTime - (int)defaultWindow.TotalSeconds;

            var requests = await database.SetMembersAsync(key);
            var validRequests = requests
                .Select(r => r.ToString())
                .Where(r => !string.IsNullOrEmpty(r))
                .Select(ParseRequestData)
                .Where(data => data.timestamp >= windowStart)
                .ToList();

            var currentCount = validRequests.Sum(data => data.cost);
            var firstRequest = validRequests.MinBy(data => data.timestamp);
            var lastRequest = validRequests.MaxBy(data => data.timestamp);

            return new RateLimitStatus
            {
                ClientId = clientId,
                Resource = resource,
                CurrentCount = currentCount,
                Limit = 0, // Would need to be passed or stored separately
                Window = defaultWindow,
                ResetTime = TimeSpan.FromSeconds(defaultWindow.TotalSeconds - (currentTime - windowStart)),
                IsLimited = false, // Would need limit comparison
                FirstRequestTime = firstRequest.timestamp > 0 ? DateTimeOffset.FromUnixTimeSeconds(firstRequest.timestamp).DateTime : null,
                LastRequestTime = lastRequest.timestamp > 0 ? DateTimeOffset.FromUnixTimeSeconds(lastRequest.timestamp).DateTime : null,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rate limit status for client {ClientId}, resource {Resource}",
                clientId, resource);
            
            return new RateLimitStatus
            {
                ClientId = clientId,
                Resource = resource,
                CurrentCount = 0,
                Limit = 0,
                Window = TimeSpan.Zero,
                ResetTime = TimeSpan.Zero,
                IsLimited = false,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public async Task<bool> ResetRateLimitAsync(
        string clientId, 
        string resource, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);

        try
        {
            var database = await _connectionFactory.GetRateLimitingDatabaseAsync(cancellationToken);
            var key = BuildRateLimitKey(clientId, resource);
            
            var result = await database.KeyDeleteAsync(key);
            
            if (result)
            {
                _logger.LogInformation("Reset rate limit for client {ClientId}, resource {Resource}",
                    clientId, resource);
            }
            else
            {
                _logger.LogDebug("No rate limit data found to reset for client {ClientId}, resource {Resource}",
                    clientId, resource);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset rate limit for client {ClientId}, resource {Resource}",
                clientId, resource);
            return false;
        }
    }

    public RateLimitingStatistics GetRateLimitingStatistics()
    {
        var totalRequests = _totalRequests;
        var allowRatio = totalRequests > 0 ? (double)_allowedRequests / totalRequests * 100 : 100;

        return new RateLimitingStatistics
        {
            TotalRequests = totalRequests,
            AllowedRequests = _allowedRequests,
            DeniedRequests = _deniedRequests,
            AllowRatio = allowRatio,
            UniqueClients = _uniqueClients,
            ActiveEntries = 0, // Would need Redis scan to count
            AverageEvaluationTimeMs = 0, // Would need timing implementation
            CleanupOperations = _cleanupOperations,
            EntriesCleanedUp = _entriesCleanedUp,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<int> CleanupExpiredEntriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var server = await _connectionFactory.GetServerAsync(cancellationToken);
            var database = await _connectionFactory.GetRateLimitingDatabaseAsync(cancellationToken);
            
            var keyPattern = BuildRateLimitKey("*", "*");
            var keys = server.Keys(pattern: keyPattern).ToArray();
            
            var expiredCount = 0;
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var maxAge = TimeSpan.FromMinutes(_options.DefaultWindowMinutes * 2); // Clean entries older than 2x window
            var cutoffTime = currentTime - (int)maxAge.TotalSeconds;

            foreach (var key in keys)
            {
                try
                {
                    var requests = await database.SetMembersAsync(key);
                    var expiredRequests = new List<RedisValue>();
                    
                    foreach (var request in requests)
                    {
                        var data = ParseRequestData(request!);
                        if (data.timestamp < cutoffTime)
                        {
                            expiredRequests.Add(request);
                        }
                    }

                    if (expiredRequests.Any())
                    {
                        await database.SetRemoveAsync(key, expiredRequests.ToArray());
                        expiredCount += expiredRequests.Count;
                    }

                    // Remove empty keys
                    var remainingCount = await database.SetLengthAsync(key);
                    if (remainingCount == 0)
                    {
                        await database.KeyDeleteAsync(key);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup rate limit key: {Key}", key);
                }
            }

            Interlocked.Increment(ref _cleanupOperations);
            Interlocked.Add(ref _entriesCleanedUp, expiredCount);
            
            if (expiredCount > 0)
            {
                _logger.LogDebug("Cleaned up {ExpiredCount} expired rate limit entries", expiredCount);
            }

            return expiredCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired rate limit entries");
            return 0;
        }
    }

    private async Task<RateLimitResult> ExecuteSlidingWindowRateLimitAsync(
        IDatabase database,
        string key,
        int limit,
        long currentTime,
        long windowStart,
        int windowSeconds)
    {
        // Remove expired entries
        var expiredCutoff = currentTime - windowSeconds;
        await CleanupExpiredRequestsAsync(database, key, expiredCutoff);

        // Count current requests in window
        var requests = await database.SetMembersAsync(key);
        var validRequests = requests
            .Select(r => r.ToString())
            .Where(r => !string.IsNullOrEmpty(r))
            .Select(ParseRequestData)
            .Where(data => data.timestamp >= windowStart)
            .ToList();

        var currentCount = validRequests.Sum(data => data.cost);
        var resetTime = TimeSpan.FromSeconds(windowSeconds);
        
        if (currentCount >= limit)
        {
            var oldestRequest = validRequests.MinBy(data => data.timestamp);
            var timeUntilReset = oldestRequest.timestamp > 0 
                ? TimeSpan.FromSeconds(oldestRequest.timestamp + windowSeconds - currentTime)
                : resetTime;
            
            return RateLimitResult.Denied(currentCount, limit, resetTime, timeUntilReset);
        }

        // Add current request
        var requestData = $"{currentTime}:1";
        await database.SetAddAsync(key, requestData);
        await database.KeyExpireAsync(key, resetTime.Add(TimeSpan.FromMinutes(1))); // Extra buffer for cleanup

        return RateLimitResult.Allowed(currentCount + 1, limit, resetTime);
    }

    private async Task CleanupExpiredRequestsAsync(IDatabase database, string key, long expiredCutoff)
    {
        var requests = await database.SetMembersAsync(key);
        var expiredRequests = new List<RedisValue>();

        foreach (var request in requests)
        {
            var data = ParseRequestData(request!);
            if (data.timestamp < expiredCutoff)
            {
                expiredRequests.Add(request);
            }
        }

        if (expiredRequests.Any())
        {
            await database.SetRemoveAsync(key, expiredRequests.ToArray());
        }
    }

    private (long timestamp, int cost) ParseRequestData(string requestData)
    {
        if (string.IsNullOrEmpty(requestData))
            return (0, 1);

        var parts = requestData.Split(':');
        if (parts.Length >= 2 && 
            long.TryParse(parts[0], out var timestamp) && 
            int.TryParse(parts[1], out var cost))
        {
            return (timestamp, cost);
        }

        // Fallback for simple timestamp format
        if (long.TryParse(requestData, out timestamp))
        {
            return (timestamp, 1);
        }

        return (0, 1);
    }

    private string BuildRateLimitKey(string clientId, string resource)
    {
        return $"{_options.KeyPrefix}{clientId}:{resource}";
    }

    private void TrackUniqueClient(string clientId)
    {
        lock (_lockObject)
        {
            if (_seenClients.Add(clientId))
            {
                Interlocked.Increment(ref _uniqueClients);
            }
        }
    }
}
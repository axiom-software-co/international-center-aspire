using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Threading.RateLimiting;

namespace Shared.Infrastructure.RateLimiting;

/// <summary>
/// Redis-backed fixed window rate limiter for distributed rate limiting across multiple gateway instances
/// Implements medical-grade compliance and structured logging for gateway rate limiting
/// </summary>
public sealed class RedisFixedWindowRateLimiter : RateLimiter
{
    private readonly IDatabase _database;
    private readonly string _keyPrefix;
    private readonly int _permitLimit;
    private readonly TimeSpan _window;
    private readonly ILogger<RedisFixedWindowRateLimiter> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public RedisFixedWindowRateLimiter(
        IConnectionMultiplexer connectionMultiplexer,
        string keyPrefix,
        int permitLimit,
        TimeSpan window,
        ILogger<RedisFixedWindowRateLimiter> logger)
    {
        _database = connectionMultiplexer.GetDatabase();
        _keyPrefix = keyPrefix;
        _permitLimit = permitLimit;
        _window = window;
        _logger = logger;
    }

    /// <summary>
    /// Gets the idle duration (not applicable for Redis-backed rate limiter)
    /// </summary>
    public override TimeSpan? IdleDuration => null;

    /// <summary>
    /// Attempts to acquire rate limit permits synchronously
    /// </summary>
    protected override RateLimitLease AttemptAcquireCore(int permitCount = 1)
    {
        var lease = AcquireAsyncCore(permitCount, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        return lease;
    }

    /// <summary>
    /// Core implementation for acquiring permits asynchronously using Redis atomic operations
    /// Uses Redis EVAL with Lua script for atomic increment and expiry
    /// </summary>
    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        var currentWindow = GetCurrentWindowKey();
        
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            
            // Lua script for atomic increment and expiry check
            var luaScript = @"
                local key = KEYS[1]
                local limit = tonumber(ARGV[1])
                local window = tonumber(ARGV[2])
                local permits = tonumber(ARGV[3])
                
                local current = redis.call('GET', key)
                if current == false then
                    current = 0
                else
                    current = tonumber(current)
                end
                
                if current + permits > limit then
                    return {current, redis.call('TTL', key)}
                end
                
                local newValue = redis.call('INCRBY', key, permits)
                if current == 0 then
                    redis.call('EXPIRE', key, window)
                end
                
                return {newValue, redis.call('TTL', key)}";

            var result = await _database.ScriptEvaluateAsync(
                luaScript, 
                new RedisKey[] { currentWindow }, 
                new RedisValue[] { _permitLimit, (int)_window.TotalSeconds, permitCount });

            var resultArray = (RedisValue[])result!;
            var currentCount = (int)resultArray![0];
            var ttl = (int)resultArray[1];

            if (currentCount > _permitLimit)
            {
                // Rate limit exceeded
                _logger.LogDebug("Rate limit exceeded for key {Key}: {Current}/{Limit}, TTL: {TTL}s", 
                    currentWindow, currentCount - permitCount, _permitLimit, ttl);
                
                return new RedisRateLimitLease(false, TimeSpan.FromSeconds(Math.Max(ttl, 0)));
            }

            // Rate limit not exceeded
            _logger.LogDebug("Rate limit check passed for key {Key}: {Current}/{Limit}, TTL: {TTL}s", 
                currentWindow, currentCount, _permitLimit, ttl);
            
            return new RedisRateLimitLease(true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis rate limiting error for key {Key}", currentWindow);
            
            // Fail open - allow request if Redis is unavailable
            return new RedisRateLimitLease(true, null);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private string GetCurrentWindowKey()
    {
        var windowStart = DateTimeOffset.UtcNow.Ticks / _window.Ticks;
        return $"rate_limit:{_keyPrefix}:{windowStart}";
    }

    /// <summary>
    /// Get statistics about the current rate limiting state
    /// </summary>
    public override RateLimiterStatistics? GetStatistics()
    {
        return null; // Redis-backed rate limiter doesn't maintain local statistics
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _semaphore.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override ValueTask DisposeAsyncCore()
    {
        _semaphore.Dispose();
        return base.DisposeAsyncCore();
    }
}

/// <summary>
/// Rate limit lease implementation for Redis-backed rate limiter
/// </summary>
public sealed class RedisRateLimitLease : RateLimitLease
{
    private readonly bool _isAcquired;
    private readonly TimeSpan? _retryAfter;

    public RedisRateLimitLease(bool isAcquired, TimeSpan? retryAfter)
    {
        _isAcquired = isAcquired;
        _retryAfter = retryAfter;
    }

    public override bool IsAcquired => _isAcquired;

    public override IEnumerable<string> MetadataNames => 
        !_isAcquired && _retryAfter.HasValue ? new[] { MetadataName.RetryAfter.Name } : Array.Empty<string>();

    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        if (metadataName == MetadataName.RetryAfter.Name && !_isAcquired && _retryAfter.HasValue)
        {
            metadata = _retryAfter.Value;
            return true;
        }
        
        metadata = null;
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        // Redis lease doesn't need disposal
    }
}
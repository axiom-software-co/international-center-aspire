using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO.Compression;
using StackExchange.Redis;

namespace Shared.Infrastructure.Caching;

/// <summary>
/// Redis cache service implementation using Redis API
/// Provides high-performance distributed caching with Redis server
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var cachedBytes = await _distributedCache.GetAsync(key, cancellationToken);
            if (cachedBytes == null || cachedBytes.Length == 0)
            {
                _logger.LogDebug("Cache miss for key: {CacheKey}", key);
                return null;
            }

            // Use JSON deserialization for all objects
            var jsonString = await DecompressDataAsync(cachedBytes);
            var result = JsonSerializer.Deserialize<T>(jsonString, _jsonOptions);
            
            _logger.LogDebug("Cache hit for key: {CacheKey}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get cache value for key: {CacheKey}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = new CacheOptions
        {
            AbsoluteExpiration = expiration ?? TimeSpan.FromMinutes(30)
        };
        
        await SetAsync(key, value, options, cancellationToken);
    }

    public async Task SetAsync<T>(string key, T value, CacheOptions options, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            // Use JSON serialization for all objects
            var jsonString = JsonSerializer.Serialize(value, _jsonOptions);
            var dataBytes = options.CompressData 
                ? await CompressDataAsync(jsonString)
                : System.Text.Encoding.UTF8.GetBytes(jsonString);

            var distributedCacheOptions = new DistributedCacheEntryOptions();
            
            if (options.AbsoluteExpiration.HasValue)
            {
                distributedCacheOptions.SetAbsoluteExpiration(options.AbsoluteExpiration.Value);
            }
            
            if (options.SlidingExpiration.HasValue)
            {
                distributedCacheOptions.SetSlidingExpiration(options.SlidingExpiration.Value);
            }

            await _distributedCache.SetAsync(key, dataBytes, distributedCacheOptions, cancellationToken);
            
            // Set tags for cache invalidation if provided
            if (options.Tags?.Any() == true)
            {
                await SetCacheTagsAsync(key, options.Tags, cancellationToken);
            }
            
            _logger.LogDebug("Cached value for key: {CacheKey} with expiration: {Expiration}", 
                key, options.AbsoluteExpiration ?? options.SlidingExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set cache value for key: {CacheKey}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Removed cache entry for key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cache entry for key: {CacheKey}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        try
        {
            var database = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            
            var keys = server.Keys(pattern: pattern).ToArray();
            if (keys.Length > 0)
            {
                await database.KeyDeleteAsync(keys);
                _logger.LogDebug("Removed {Count} cache entries matching pattern: {Pattern}", keys.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cache entries by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            // Check existence by attempting to get the value from IDistributedCache
            // This ensures consistency with how keys are stored (with instance name prefixes)
            var cachedBytes = await _distributedCache.GetAsync(key, cancellationToken);
            return cachedBytes != null && cachedBytes.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check cache key existence: {CacheKey}", key);
            return false;
        }
    }

    public async Task<TimeSpan?> GetTtlAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var database = _redis.GetDatabase();
            // Try to find the actual Redis key by searching with instance name prefix
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: $"*{key}*").ToArray();
            
            if (keys.Length > 0)
            {
                return await database.KeyTimeToLiveAsync(keys.First());
            }
            
            // Fallback: try the key as-is
            return await database.KeyTimeToLiveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get TTL for cache key: {CacheKey}", key);
            return null;
        }
    }

    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await _distributedCache.RefreshAsync(key, cancellationToken);
            _logger.LogDebug("Refreshed cache entry for key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh cache entry for key: {CacheKey}", key);
        }
    }

    private static async Task<byte[]> CompressDataAsync(string data)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            await gzip.WriteAsync(bytes);
        }
        return output.ToArray();
    }

    private static async Task<string> DecompressDataAsync(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        
        await gzip.CopyToAsync(output);
        return System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    private async Task SetCacheTagsAsync(string key, IEnumerable<string> tags, CancellationToken cancellationToken)
    {
        try
        {
            var database = _redis.GetDatabase();
            foreach (var tag in tags)
            {
                var tagKey = $"tag:{tag}";
                await database.SetAddAsync(tagKey, key);
                await database.KeyExpireAsync(tagKey, TimeSpan.FromHours(24)); // Tags expire after 24 hours
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cache tags for key: {CacheKey}", key);
        }
    }
}
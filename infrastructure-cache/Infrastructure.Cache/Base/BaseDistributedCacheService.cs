using Infrastructure.Cache.Abstractions;
using Infrastructure.Cache.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;
using System.IO.Compression;

namespace Infrastructure.Cache.Base;

/// <summary>
/// Generic distributed cache service base implementation for high-performance caching operations.
/// INFRASTRUCTURE: Generic caching patterns reusable by any domain
/// </summary>
public abstract class BaseDistributedCacheService : IDistributedCacheService
{
    private readonly IRedisConnectionFactory _connectionFactory;
    private readonly DistributedCacheOptions _options;
    private readonly ILogger<BaseDistributedCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private long _totalGets;
    private long _cacheHits;
    private long _cacheMisses;
    private long _totalSets;
    private long _totalRemoves;
    private long _expiredKeys = 0;
    private readonly DateTime _startTime = DateTime.UtcNow;

    protected BaseDistributedCacheService(
        IRedisConnectionFactory connectionFactory,
        IOptions<RedisConnectionOptions> options,
        ILogger<BaseDistributedCacheService> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _options = options.Value?.DistributedCache ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ValidateKey(key);
        
        Interlocked.Increment(ref _totalGets);

        try
        {
            var fullKey = BuildKey(key);
            var database = await _connectionFactory.GetDatabaseAsync(cancellationToken: cancellationToken);
            
            var cachedValue = await database.StringGetAsync(fullKey);
            
            if (!cachedValue.HasValue)
            {
                Interlocked.Increment(ref _cacheMisses);
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            Interlocked.Increment(ref _cacheHits);
            
            var deserializedValue = await DeserializeAsync<T>(cachedValue!, cancellationToken);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            
            return deserializedValue;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _cacheMisses);
            _logger.LogError(ex, "Failed to get cached item with key: {Key}", key);
            return null;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T item, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(item);
        ValidateKey(key);
        
        Interlocked.Increment(ref _totalSets);

        try
        {
            var fullKey = BuildKey(key);
            var database = await _connectionFactory.GetDatabaseAsync(cancellationToken: cancellationToken);
            
            var serializedValue = await SerializeAsync(item, cancellationToken);
            var expirationTime = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);
            
            var result = await database.StringSetAsync(fullKey, serializedValue, expirationTime);
            
            if (result)
            {
                _logger.LogDebug("Cached item with key: {Key}, expiration: {Expiration}", key, expirationTime);
            }
            else
            {
                _logger.LogWarning("Failed to cache item with key: {Key}", key);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set cached item with key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ValidateKey(key);
        
        Interlocked.Increment(ref _totalRemoves);

        try
        {
            var fullKey = BuildKey(key);
            var database = await _connectionFactory.GetDatabaseAsync(cancellationToken: cancellationToken);
            
            var result = await database.KeyDeleteAsync(fullKey);
            
            if (result)
            {
                _logger.LogDebug("Removed cached item with key: {Key}", key);
            }
            else
            {
                _logger.LogDebug("No cached item found to remove with key: {Key}", key);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cached item with key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ValidateKey(key);

        try
        {
            var fullKey = BuildKey(key);
            var database = await _connectionFactory.GetDatabaseAsync(cancellationToken: cancellationToken);
            
            var exists = await database.KeyExistsAsync(fullKey);
            
            _logger.LogDebug("Key existence check for {Key}: {Exists}", key, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of key: {Key}", key);
            return false;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);
        ValidateKey(key);

        var cachedItem = await GetAsync<T>(key, cancellationToken);
        if (cachedItem != null)
        {
            return cachedItem;
        }

        try
        {
            var item = await factory();
            if (item != null)
            {
                await SetAsync(key, item, expiration, cancellationToken);
            }
            
            return item!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute factory function for key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> SetBatchAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(items);
        
        if (!items.Any())
        {
            return true;
        }

        try
        {
            var database = await _connectionFactory.GetDatabaseAsync(cancellationToken: cancellationToken);
            var batch = database.CreateBatch();
            var tasks = new List<Task<bool>>();
            var expirationTime = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

            foreach (var kvp in items)
            {
                ValidateKey(kvp.Key);
                var fullKey = BuildKey(kvp.Key);
                var serializedValue = await SerializeAsync(kvp.Value, cancellationToken);
                
                var task = batch.StringSetAsync(fullKey, serializedValue, expirationTime);
                tasks.Add(task);
            }

            batch.Execute();
            var results = await Task.WhenAll(tasks);
            
            var successCount = results.Count(r => r);
            Interlocked.Add(ref _totalSets, successCount);
            
            _logger.LogDebug("Batch set completed: {SuccessCount}/{TotalCount} items cached", 
                successCount, items.Count);
            
            return successCount == items.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute batch set operation for {ItemCount} items", items.Count);
            return false;
        }
    }

    public async Task<int> RemoveBatchAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);
        
        var keyList = keys.ToList();
        if (!keyList.Any())
        {
            return 0;
        }

        try
        {
            var database = await _connectionFactory.GetDatabaseAsync(cancellationToken: cancellationToken);
            var fullKeys = keyList.Select(k => (RedisKey)BuildKey(k)).ToArray();
            
            foreach (var key in keyList)
            {
                ValidateKey(key);
            }
            
            var deletedCount = await database.KeyDeleteAsync(fullKeys);
            
            Interlocked.Add(ref _totalRemoves, deletedCount);
            
            _logger.LogDebug("Batch remove completed: {DeletedCount}/{TotalCount} items removed", 
                deletedCount, keyList.Count);
            
            return (int)deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute batch remove operation for {KeyCount} keys", keyList.Count);
            return 0;
        }
    }

    public async Task<int> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        try
        {
            var server = await _connectionFactory.GetServerAsync(cancellationToken);
            var fullPattern = BuildKey(pattern);
            
            var keys = server.Keys(pattern: fullPattern).ToArray();
            
            if (!keys.Any())
            {
                _logger.LogDebug("No keys found matching pattern: {Pattern}", pattern);
                return 0;
            }

            var database = await _connectionFactory.GetDatabaseAsync(cancellationToken: cancellationToken);
            var deletedCount = await database.KeyDeleteAsync(keys);
            
            Interlocked.Add(ref _totalRemoves, deletedCount);
            
            _logger.LogDebug("Pattern remove completed: {DeletedCount} keys removed for pattern: {Pattern}", 
                deletedCount, pattern);
            
            return (int)deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove keys by pattern: {Pattern}", pattern);
            return 0;
        }
    }

    public async Task<bool> RefreshAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ValidateKey(key);

        try
        {
            var fullKey = BuildKey(key);
            var database = await _connectionFactory.GetDatabaseAsync(cancellationToken: cancellationToken);
            
            var result = await database.KeyExpireAsync(fullKey, expiration);
            
            if (result)
            {
                _logger.LogDebug("Refreshed expiration for key: {Key}, new expiration: {Expiration}", key, expiration);
            }
            else
            {
                _logger.LogWarning("Failed to refresh expiration for key: {Key} (key may not exist)", key);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh cached item expiration for key: {Key}", key);
            return false;
        }
    }

    public CacheStatistics GetCacheStatistics()
    {
        var totalRequests = _totalGets;
        var hitRatio = totalRequests > 0 ? (double)_cacheHits / totalRequests * 100 : 0;

        return new CacheStatistics
        {
            TotalGets = _totalGets,
            CacheHits = _cacheHits,
            CacheMisses = _cacheMisses,
            HitRatio = hitRatio,
            TotalSets = _totalSets,
            TotalRemoves = _totalRemoves,
            AverageGetTimeMs = 0, // Would need timing implementation
            AverageSetTimeMs = 0, // Would need timing implementation
            ExpiredKeys = _expiredKeys,
            EvictedKeys = 0, // Would need Redis eviction tracking
            CurrentItems = 0, // Would need Redis info commands
            MemoryUsageBytes = 0, // Would need Redis memory tracking
            Timestamp = DateTime.UtcNow
        };
    }

    private string BuildKey(string key)
    {
        return string.IsNullOrEmpty(_options.KeyPrefix) 
            ? key 
            : $"{_options.KeyPrefix}{key}";
    }

    private void ValidateKey(string key)
    {
        if (key.Length > _options.MaxKeyLength)
        {
            throw new ArgumentException($"Cache key length exceeds maximum allowed length of {_options.MaxKeyLength} characters", nameof(key));
        }
    }

    private async Task<byte[]> SerializeAsync<T>(T item, CancellationToken cancellationToken) where T : class
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, item, _jsonOptions, cancellationToken);
        var data = stream.ToArray();

        if (_options.EnableCompression && data.Length > _options.CompressionThresholdBytes)
        {
            using var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal))
            {
                await gzipStream.WriteAsync(data, cancellationToken);
            }
            
            var compressedData = compressedStream.ToArray();
            _logger.LogDebug("Compressed cache data from {OriginalSize} to {CompressedSize} bytes", 
                data.Length, compressedData.Length);
            
            return compressedData;
        }

        return data;
    }

    private async Task<T?> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken) where T : class
    {
        try
        {
            using var stream = new MemoryStream(data);
            
            // Check if data is compressed (GZip magic number)
            if (data.Length > 2 && data[0] == 0x1f && data[1] == 0x8b)
            {
                using var decompressedStream = new MemoryStream();
                using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
                {
                    await gzipStream.CopyToAsync(decompressedStream, cancellationToken);
                }
                
                decompressedStream.Position = 0;
                return await JsonSerializer.DeserializeAsync<T>(decompressedStream, _jsonOptions, cancellationToken);
            }
            
            return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize cached data");
            return null;
        }
    }
}
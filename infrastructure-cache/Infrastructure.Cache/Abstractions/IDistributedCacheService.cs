namespace Infrastructure.Cache.Abstractions;

/// <summary>
/// CONTRACT: Generic distributed cache service interface for high-performance caching operations.
/// 
/// TDD PRINCIPLE: Interface drives the design of distributed caching architecture
/// DEPENDENCY INVERSION: Abstractions for variable caching concerns
/// INFRASTRUCTURE: Generic caching patterns reusable by any domain
/// DOMAIN AGNOSTIC: No knowledge of Services, News, Events, or any specific domain
/// </summary>
public interface IDistributedCacheService
{
    /// <summary>
    /// CONTRACT: Get cached item by key with automatic deserialization
    /// 
    /// PRECONDITION: Valid cache key
    /// POSTCONDITION: Returns cached item or null if not found/expired
    /// INFRASTRUCTURE: Generic cache retrieval for any domain
    /// PERFORMANCE: High-speed data retrieval from Redis
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Cached item or null if not found</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// CONTRACT: Set cached item by key with automatic serialization
    /// 
    /// PRECONDITION: Valid cache key and item
    /// POSTCONDITION: Item cached with specified or default expiration
    /// INFRASTRUCTURE: Generic cache storage for any domain
    /// TTL: Time-to-live management for cache entries
    /// </summary>
    /// <typeparam name="T">Type of item to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="item">Item to cache</param>
    /// <param name="expiration">Optional expiration time (uses default if not specified)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if cached successfully</returns>
    Task<bool> SetAsync<T>(string key, T item, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// CONTRACT: Remove cached item by key
    /// 
    /// PRECONDITION: Valid cache key
    /// POSTCONDITION: Item removed from cache if it existed
    /// INFRASTRUCTURE: Generic cache removal for any domain
    /// CLEANUP: Cache entry cleanup operations
    /// </summary>
    /// <param name="key">Cache key to remove</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if item was removed</returns>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Check if cached item exists by key
    /// 
    /// PRECONDITION: Valid cache key
    /// POSTCONDITION: Returns true if item exists and has not expired
    /// INFRASTRUCTURE: Generic cache existence check for any domain
    /// VALIDATION: Cache presence verification
    /// </summary>
    /// <param name="key">Cache key to check</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if cached item exists</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Get or set cached item with factory function
    /// 
    /// PRECONDITION: Valid cache key and factory function
    /// POSTCONDITION: Returns cached item if exists, otherwise creates and caches new item
    /// INFRASTRUCTURE: Generic cache-aside pattern for any domain
    /// PERFORMANCE: Combines get/set operations for efficiency
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Factory function to create item if not cached</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Cached or newly created item</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// CONTRACT: Set multiple cached items in batch operation
    /// 
    /// PRECONDITION: Valid dictionary of key-value pairs
    /// POSTCONDITION: All items cached with specified or default expiration
    /// INFRASTRUCTURE: Generic batch caching for any domain
    /// PERFORMANCE: Optimized multi-item caching
    /// </summary>
    /// <typeparam name="T">Type of items to cache</typeparam>
    /// <param name="items">Dictionary of key-value pairs to cache</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if all items cached successfully</returns>
    Task<bool> SetBatchAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// CONTRACT: Remove multiple cached items by keys
    /// 
    /// PRECONDITION: Valid collection of cache keys
    /// POSTCONDITION: All specified items removed from cache
    /// INFRASTRUCTURE: Generic batch removal for any domain
    /// CLEANUP: Bulk cache cleanup operations
    /// </summary>
    /// <param name="keys">Collection of cache keys to remove</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Number of items successfully removed</returns>
    Task<int> RemoveBatchAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Remove cached items by pattern
    /// 
    /// PRECONDITION: Valid pattern string (supports wildcards)
    /// POSTCONDITION: All items matching pattern removed from cache
    /// INFRASTRUCTURE: Generic pattern-based removal for any domain
    /// BULK OPERATIONS: Pattern-based cache cleanup
    /// </summary>
    /// <param name="pattern">Pattern to match cache keys (supports * wildcard)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Number of items successfully removed</returns>
    Task<int> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Refresh cached item expiration
    /// 
    /// PRECONDITION: Valid cache key and item exists
    /// POSTCONDITION: Item expiration updated to new value
    /// INFRASTRUCTURE: Generic expiration management for any domain
    /// TTL MANAGEMENT: Cache entry lifetime extension
    /// </summary>
    /// <param name="key">Cache key to refresh</param>
    /// <param name="expiration">New expiration time</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if expiration was refreshed</returns>
    Task<bool> RefreshAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Get cache statistics and metrics
    /// 
    /// POSTCONDITION: Returns cache performance metrics for monitoring
    /// OBSERVABILITY: Cache performance monitoring
    /// INFRASTRUCTURE: Generic cache metrics for any domain
    /// </summary>
    /// <returns>Cache statistics and metrics</returns>
    CacheStatistics GetCacheStatistics();
}

/// <summary>
/// Cache statistics for monitoring and observability.
/// INFRASTRUCTURE: Generic cache metrics for any domain
/// </summary>
public sealed class CacheStatistics
{
    /// <summary>Total cache get operations</summary>
    public long TotalGets { get; init; }
    
    /// <summary>Number of cache hits</summary>
    public long CacheHits { get; init; }
    
    /// <summary>Number of cache misses</summary>
    public long CacheMisses { get; init; }
    
    /// <summary>Cache hit ratio (percentage)</summary>
    public double HitRatio { get; init; }
    
    /// <summary>Total cache set operations</summary>
    public long TotalSets { get; init; }
    
    /// <summary>Total cache remove operations</summary>
    public long TotalRemoves { get; init; }
    
    /// <summary>Average get operation time in milliseconds</summary>
    public double AverageGetTimeMs { get; init; }
    
    /// <summary>Average set operation time in milliseconds</summary>
    public double AverageSetTimeMs { get; init; }
    
    /// <summary>Number of expired keys</summary>
    public long ExpiredKeys { get; init; }
    
    /// <summary>Number of evicted keys</summary>
    public long EvictedKeys { get; init; }
    
    /// <summary>Current number of cached items</summary>
    public long CurrentItems { get; init; }
    
    /// <summary>Total memory usage in bytes</summary>
    public long MemoryUsageBytes { get; init; }
    
    /// <summary>Statistics timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
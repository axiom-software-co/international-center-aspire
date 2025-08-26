using InternationalCenter.Tests.Shared.Containers;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace InternationalCenter.Tests.Shared.Fixtures;

/// <summary>
/// Manages Garnet TestContainer lifecycle for caching integration tests
/// Uses real Garnet instances (Redis-compatible) to test cache decorators and performance
/// </summary>
public sealed class CacheFixture : IAsyncLifetime
{
    private RedisContainer? _container;
    private IConnectionMultiplexer? _connection;

    public string ConnectionString { get; private set; } = string.Empty;
    public IConnectionMultiplexer Connection => _connection ?? throw new InvalidOperationException("Cache fixture not initialized");
    public IDatabase Database => Connection.GetDatabase();

    public async Task InitializeAsync()
    {
        // Create and start Garnet container with Podman
        _container = PodmanContainerConfiguration
            .CreateGarnetContainer()
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // Create Garnet connection multiplexer (Redis-compatible)
        var configuration = ConfigurationOptions.Parse(ConnectionString);
        configuration.AbortOnConnectFail = false;
        configuration.ConnectRetry = 3;
        configuration.ConnectTimeout = 5000;
        configuration.SyncTimeout = 5000;
        
        _connection = await ConnectionMultiplexer.ConnectAsync(configuration);
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Clears all cache data between test methods
    /// Ensures test isolation for cache-dependent tests
    /// </summary>
    public async Task ClearCacheAsync()
    {
        if (_connection == null) return;
        
        var server = _connection.GetServer(_connection.GetEndPoints().First());
        await server.FlushAllDatabasesAsync();
    }

    /// <summary>
    /// Validates cache performance metrics
    /// Tests compression effectiveness and response times
    /// </summary>
    public async Task<CacheMetrics> GetCacheMetricsAsync()
    {
        if (_connection == null) 
            return new CacheMetrics();

        var server = _connection.GetServer(_connection.GetEndPoints().First());
        var info = await server.InfoAsync();
        
        return new CacheMetrics
        {
            TotalKeys = (long)await Database.ExecuteAsync("DBSIZE"),
            MemoryUsed = ParseInfoValue(info, "used_memory"),
            HitRate = CalculateHitRate(info),
            IsConnected = _connection.IsConnected
        };
    }

    /// <summary>
    /// Tests cache key expiration and TTL functionality
    /// </summary>
    public async Task<TimeSpan?> GetKeyTtlAsync(string key)
    {
        var ttl = await Database.KeyTimeToLiveAsync(key);
        return ttl;
    }

    /// <summary>
    /// Tests tag-based cache invalidation patterns
    /// </summary>
    public async Task InvalidateByTagAsync(string tag)
    {
        var server = _connection!.GetServer(_connection.GetEndPoints().First());
        var keys = server.Keys(pattern: $"*:{tag}:*");
        
        var keyArray = keys.ToArray();
        if (keyArray.Length > 0)
        {
            await Database.KeyDeleteAsync(keyArray);
        }
    }

    private static long ParseInfoValue(IGrouping<string, KeyValuePair<string, string>>[] info, string key)
    {
        var memoryGroup = info.FirstOrDefault(g => g.Key == "memory");
        if (memoryGroup != null)
        {
            var memoryInfo = memoryGroup.FirstOrDefault(kvp => kvp.Key == key);
            if (long.TryParse(memoryInfo.Value, out var value))
                return value;
        }
        return 0;
    }

    private static double CalculateHitRate(IGrouping<string, KeyValuePair<string, string>>[] info)
    {
        var statsGroup = info.FirstOrDefault(g => g.Key == "stats");
        if (statsGroup == null) return 0.0;

        var hits = statsGroup.FirstOrDefault(kvp => kvp.Key == "keyspace_hits");
        var misses = statsGroup.FirstOrDefault(kvp => kvp.Key == "keyspace_misses");

        if (long.TryParse(hits.Value, out var hitCount) && 
            long.TryParse(misses.Value, out var missCount))
        {
            var total = hitCount + missCount;
            return total > 0 ? (double)hitCount / total * 100 : 0.0;
        }

        return 0.0;
    }
}

public class CacheMetrics
{
    public long TotalKeys { get; set; }
    public long MemoryUsed { get; set; }
    public double HitRate { get; set; }
    public bool IsConnected { get; set; }
}
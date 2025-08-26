using StackExchange.Redis;
using System.Diagnostics;

namespace Gateway.Public.Services;

public class RedisMetricsWrapper : IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly PublicGatewayMetricsService _metricsService;
    private readonly ILogger<RedisMetricsWrapper> _logger;
    private readonly Timer _connectionPoolMonitor;

    public RedisMetricsWrapper(
        IConnectionMultiplexer redis,
        PublicGatewayMetricsService metricsService,
        ILogger<RedisMetricsWrapper> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Monitor connection pool utilization every 30 seconds
        _connectionPoolMonitor = new Timer(MonitorConnectionPool, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    public IDatabase GetDatabase(int db = -1, object? asyncState = null)
    {
        var database = _redis.GetDatabase(db, asyncState);
        return new DatabaseMetricsWrapper(database, _metricsService, _logger);
    }

    public async Task<bool> StringSetAsync(int database, RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        
        try
        {
            var db = GetDatabase(database);
            var result = await ((DatabaseMetricsWrapper)db).StringSetAsync(key, value, expiry, when);
            success = result;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis StringSetAsync operation failed for key: {Key}", key);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordRedisOperation("string_set", success, stopwatch.Elapsed.TotalSeconds);
        }
    }

    public async Task<RedisValue> StringGetAsync(int database, RedisKey key)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        
        try
        {
            var db = GetDatabase(database);
            var result = await ((DatabaseMetricsWrapper)db).StringGetAsync(key);
            success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis StringGetAsync operation failed for key: {Key}", key);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordRedisOperation("string_get", success, stopwatch.Elapsed.TotalSeconds);
        }
    }

    public async Task<bool> KeyDeleteAsync(int database, RedisKey key)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        
        try
        {
            var db = GetDatabase(database);
            var result = await ((DatabaseMetricsWrapper)db).KeyDeleteAsync(key);
            success = result;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis KeyDeleteAsync operation failed for key: {Key}", key);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordRedisOperation("key_delete", success, stopwatch.Elapsed.TotalSeconds);
        }
    }

    public async Task<bool> KeyExistsAsync(int database, RedisKey key)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        
        try
        {
            var db = GetDatabase(database);
            var result = await ((DatabaseMetricsWrapper)db).KeyExistsAsync(key);
            success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis KeyExistsAsync operation failed for key: {Key}", key);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordRedisOperation("key_exists", success, stopwatch.Elapsed.TotalSeconds);
        }
    }

    public async Task<TimeSpan?> KeyTimeToLiveAsync(int database, RedisKey key)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        
        try
        {
            var db = GetDatabase(database);
            var result = await ((DatabaseMetricsWrapper)db).KeyTimeToLiveAsync(key);
            success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis KeyTimeToLiveAsync operation failed for key: {Key}", key);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordRedisOperation("key_ttl", success, stopwatch.Elapsed.TotalSeconds);
        }
    }

    private void MonitorConnectionPool(object? state)
    {
        try
        {
            if (_redis.IsConnected)
            {
                // Calculate connection pool utilization
                // This is a simplified calculation - in a real scenario you'd want more detailed metrics
                var endpoints = _redis.GetEndPoints();
                var totalConnections = 0;
                var activeConnections = 0;

                foreach (var endpoint in endpoints)
                {
                    var server = _redis.GetServer(endpoint);
                    if (server.IsConnected)
                    {
                        activeConnections++;
                    }
                    totalConnections++;
                }

                var utilization = totalConnections > 0 ? (double)activeConnections / totalConnections : 0;
                _metricsService.RecordRedisConnectionPoolUtilization(utilization);

                _logger.LogDebug("Redis connection pool utilization: {Utilization:P2} ({Active}/{Total})",
                    utilization, activeConnections, totalConnections);
            }
            else
            {
                _metricsService.RecordRedisConnectionPoolUtilization(0);
                _logger.LogWarning("Redis connection multiplexer is not connected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to monitor Redis connection pool");
            _metricsService.RecordRedisConnectionPoolUtilization(0);
        }
    }

    public void Dispose()
    {
        _connectionPoolMonitor?.Dispose();
        _logger.LogInformation("RedisMetricsWrapper disposed");
    }
}

internal class DatabaseMetricsWrapper : IDatabase
{
    private readonly IDatabase _database;
    private readonly PublicGatewayMetricsService _metricsService;
    private readonly ILogger _logger;

    public DatabaseMetricsWrapper(IDatabase database, PublicGatewayMetricsService metricsService, ILogger logger)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        
        try
        {
            var result = await _database.StringSetAsync(key, value, expiry, when, flags);
            success = result;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis StringSetAsync failed for key: {Key}", key);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordRedisOperation("string_set", success, stopwatch.Elapsed.TotalSeconds);
        }
    }

    public async Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        
        try
        {
            var result = await _database.StringGetAsync(key, flags);
            success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis StringGetAsync failed for key: {Key}", key);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordRedisOperation("string_get", success, stopwatch.Elapsed.TotalSeconds);
        }
    }

    public async Task<bool> KeyDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        
        try
        {
            var result = await _database.KeyDeleteAsync(key, flags);
            success = result;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis KeyDeleteAsync failed for key: {Key}", key);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordRedisOperation("key_delete", success, stopwatch.Elapsed.TotalSeconds);
        }
    }

    public async Task<bool> KeyExistsAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        
        try
        {
            var result = await _database.KeyExistsAsync(key, flags);
            success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis KeyExistsAsync failed for key: {Key}", key);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordRedisOperation("key_exists", success, stopwatch.Elapsed.TotalSeconds);
        }
    }

    public async Task<TimeSpan?> KeyTimeToLiveAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        
        try
        {
            var result = await _database.KeyTimeToLiveAsync(key, flags);
            success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis KeyTimeToLiveAsync failed for key: {Key}", key);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.RecordRedisOperation("key_ttl", success, stopwatch.Elapsed.TotalSeconds);
        }
    }

    // Implement the remaining IDatabase interface methods by delegating to _database
    // For brevity, I'm including just the core methods above and a few key ones below
    
    public IConnectionMultiplexer Multiplexer => _database.Multiplexer;
    public int Database => _database.Database;

    // Add remaining IDatabase method implementations as needed...
    // This is a simplified wrapper focusing on the most common Redis operations
    // In a production environment, you'd want to wrap all IDatabase methods

    #region IDatabase Delegation (remaining methods)
    // For brevity, implementing pass-through for remaining methods
    
    public IBatch CreateBatch(object? asyncState = null) => _database.CreateBatch(asyncState);
    public ITransaction CreateTransaction(object? asyncState = null) => _database.CreateTransaction(asyncState);
    
    // Add all other IDatabase method implementations by delegating to _database
    // This is a simplified example focusing on the most important operations
    
    #pragma warning disable CS1998 // This async method lacks 'await' operators - by design for pass-through
    public async Task<long> HashDeleteAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
        => await _database.HashDeleteAsync(key, hashField, flags);
    
    public async Task<long> HashDeleteAsync(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
        => await _database.HashDeleteAsync(key, hashFields, flags);
    
    // ... implement remaining methods similarly
    #pragma warning restore CS1998
    #endregion
}
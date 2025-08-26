using Infrastructure.Cache.Abstractions;
using StackExchange.Redis;

namespace Service.Monitoring.Services;

public sealed class RedisHealthCheck : IRedisHealthCheck
{
    private readonly IRedisConnectionFactory _connectionFactory;
    private readonly ILogger<RedisHealthCheck> _logger;
    private readonly RedisHealthOptions _options;

    public RedisHealthCheck(
        IRedisConnectionFactory connectionFactory,
        ILogger<RedisHealthCheck> logger,
        IOptions<MonitoringOptions> options)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value?.Redis ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();

        try
        {
            var canConnect = await CanConnectAsync(cancellationToken);
            data["can_connect"] = canConnect;

            if (!canConnect)
            {
                return new HealthCheckResult
                {
                    Name = "Redis Cache",
                    Status = HealthStatus.Unhealthy,
                    Description = "Cannot establish Redis connection",
                    Duration = stopwatch.Elapsed,
                    Data = data
                };
            }

            var canReadWrite = await CanReadWriteAsync(cancellationToken);
            data["can_read_write"] = canReadWrite;

            if (!canReadWrite)
            {
                return new HealthCheckResult
                {
                    Name = "Redis Cache",
                    Status = HealthStatus.Degraded,
                    Description = "Redis connection established but read/write operations failed",
                    Duration = stopwatch.Elapsed,
                    Data = data
                };
            }

            var latency = await MeasureLatencyAsync(cancellationToken);
            data["latency_ms"] = latency.TotalMilliseconds;

            var memoryUsage = await GetMemoryUsageAsync(cancellationToken);
            if (memoryUsage > 0)
            {
                data["memory_usage_bytes"] = memoryUsage;
            }

            var status = latency > TimeSpan.FromSeconds(1) ? HealthStatus.Degraded : HealthStatus.Healthy;
            var description = status == HealthStatus.Healthy 
                ? "Redis is healthy and responsive" 
                : $"Redis is responding slowly ({latency.TotalMilliseconds:F0}ms)";

            _logger.LogDebug("Redis health check completed: {Status} in {Duration}ms", 
                status, stopwatch.Elapsed.TotalMilliseconds);

            return new HealthCheckResult
            {
                Name = "Redis Cache",
                Status = status,
                Description = description,
                Duration = stopwatch.Elapsed,
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed after {Duration}ms", 
                stopwatch.Elapsed.TotalMilliseconds);

            return new HealthCheckResult
            {
                Name = "Redis Cache",
                Status = HealthStatus.Unhealthy,
                Description = "Redis health check failed",
                Duration = stopwatch.Elapsed,
                Exception = ex.Message,
                Data = data
            };
        }
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        while (attempt <= _options.MaxRetryAttempts)
        {
            try
            {
                using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);
                var database = connection.GetDatabase();
                
                await database.PingAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis connection attempt {Attempt} failed", attempt + 1);
                
                if (attempt < _options.MaxRetryAttempts)
                {
                    await Task.Delay(_options.RetryDelay, cancellationToken);
                }
                
                attempt++;
            }
        }

        return false;
    }

    public async Task<bool> CanReadWriteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);
            var database = connection.GetDatabase();
            
            var testKey = $"{_options.TestKey}:{Guid.NewGuid():N}";
            var testValue = _options.TestValue;
            
            // Test write
            var writeResult = await database.StringSetAsync(testKey, testValue, _options.Timeout);
            if (!writeResult)
            {
                _logger.LogWarning("Redis write operation failed for key {TestKey}", testKey);
                return false;
            }
            
            // Test read
            var readResult = await database.StringGetAsync(testKey);
            if (!readResult.HasValue || readResult != testValue)
            {
                _logger.LogWarning("Redis read operation failed for key {TestKey}", testKey);
                return false;
            }
            
            // Cleanup
            await database.KeyDeleteAsync(testKey);
            
            _logger.LogDebug("Redis read/write test completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis read/write test failed");
            return false;
        }
    }

    public async Task<TimeSpan> MeasureLatencyAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);
            var database = connection.GetDatabase();
            
            await database.PingAsync();
            
            return stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to measure Redis latency");
            return TimeSpan.MaxValue;
        }
    }

    public async Task<long> GetMemoryUsageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);
            var server = connection.GetServer(connection.GetEndPoints().First());
            
            var info = await server.InfoAsync("memory");
            var memoryInfo = info.FirstOrDefault(x => x.Key == "memory");
            
            if (memoryInfo.Key != null)
            {
                var usedMemoryLine = memoryInfo.Value
                    .Split('\n')
                    .FirstOrDefault(line => line.StartsWith("used_memory:"));
                    
                if (usedMemoryLine != null && usedMemoryLine.Split(':').Length > 1)
                {
                    if (long.TryParse(usedMemoryLine.Split(':')[1].Trim(), out var memoryUsage))
                    {
                        _logger.LogDebug("Redis memory usage: {MemoryUsage} bytes", memoryUsage);
                        return memoryUsage;
                    }
                }
            }
            
            _logger.LogDebug("Could not retrieve Redis memory usage");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Redis memory usage");
            return 0;
        }
    }
}
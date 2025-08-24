using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace InternationalCenter.Shared.Infrastructure.Observability;

/// <summary>
/// Health check for Microsoft Redis cache server (Redis-compatible)
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisHealthCheck> _logger;
    
    public RedisHealthCheck(IConnectionMultiplexer redis, ILogger<RedisHealthCheck> logger)
    {
        _redis = redis;
        _logger = logger;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var activity = Activity.Current?.Source.StartActivity("health_check.redis");
            activity?.AddTag("health_check.type", "redis");
            
            var database = _redis.GetDatabase();
            var stopwatch = Stopwatch.StartNew();
            var ping = await database.PingAsync();
            stopwatch.Stop();
            
            var data = new Dictionary<string, object>
            {
                ["redis.status"] = "healthy",
                ["redis.ping_duration_ms"] = ping.TotalMilliseconds,
                ["redis.connection_time_ms"] = stopwatch.ElapsedMilliseconds,
                ["redis.is_connected"] = _redis.IsConnected
            };
            
            // Get server info if available
            try
            {
                if (_redis.GetEndPoints().Any())
                {
                    var server = _redis.GetServer(_redis.GetEndPoints().First());
                    var info = await server.InfoAsync("memory");
                    
                    var memoryInfo = new Dictionary<string, string>();
                    foreach (var group in info)
                    {
                        foreach (var kvp in group)
                        {
                            memoryInfo[kvp.Key] = kvp.Value;
                        }
                    }
                    
                    if (memoryInfo.TryGetValue("used_memory", out var usedMemoryStr) && 
                        long.TryParse(usedMemoryStr, out var usedMemory))
                    {
                        data["redis.used_memory_bytes"] = usedMemory;
                        
                        if (memoryInfo.TryGetValue("maxmemory", out var maxMemoryStr) && 
                            long.TryParse(maxMemoryStr, out var maxMemory))
                        {
                            data["redis.max_memory_bytes"] = maxMemory;
                            
                            // Check memory usage - warn if over 90%
                            if (maxMemory > 0 && usedMemory > maxMemory * 0.9)
                            {
                                _logger.LogWarning("Redis memory usage is high: {UsedMemory}/{MaxMemory} bytes", usedMemory, maxMemory);
                                return HealthCheckResult.Degraded("High Redis memory usage", data: data);
                            }
                        }
                    }
                }
            }
            catch (Exception infoEx)
            {
                _logger.LogWarning(infoEx, "Could not retrieve Redis server info");
                data["redis.info_error"] = infoEx.Message;
            }
            
            // Check ping performance
            if (ping.TotalMilliseconds > 100)
            {
                _logger.LogWarning("Redis ping is slow: {PingMs}ms", ping.TotalMilliseconds);
                return HealthCheckResult.Degraded($"Slow Redis response ({ping.TotalMilliseconds:F2}ms)", data: data);
            }
            
            activity?.AddTag("redis.ping_duration_ms", ping.TotalMilliseconds);
            activity?.AddTag("redis.is_connected", _redis.IsConnected);
            
            return HealthCheckResult.Healthy($"Redis responding in {ping.TotalMilliseconds:F2}ms", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            
            Activity.Current?.AddTag("error", true);
            Activity.Current?.AddTag("error.type", ex.GetType().Name);
            Activity.Current?.AddTag("error.message", ex.Message);
            
            return HealthCheckResult.Unhealthy("Redis connection failed", ex, new Dictionary<string, object>
            {
                ["redis.status"] = "unhealthy",
                ["error.message"] = ex.Message,
                ["error.type"] = ex.GetType().Name,
                ["redis.is_connected"] = _redis.IsConnected
            });
        }
    }
}
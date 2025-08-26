using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure.Observability;

public class DatabaseHealthCheck<TContext> : IHealthCheck where TContext : DbContext
{
    private readonly TContext _context;
    private readonly ILogger<DatabaseHealthCheck<TContext>> _logger;
    private readonly string _serviceName;
    
    public DatabaseHealthCheck(TContext context, ILogger<DatabaseHealthCheck<TContext>> logger)
    {
        _context = context;
        _logger = logger;
        _serviceName = typeof(TContext).Name;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var activity = Activity.Current?.Source.StartActivity("health_check.database");
            activity?.AddTag("health_check.type", "database");
            activity?.AddTag("database.context", _serviceName);
            
            var stopwatch = Stopwatch.StartNew();
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            stopwatch.Stop();
            
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }
            
            var data = new Dictionary<string, object>
            {
                ["database.status"] = "healthy",
                ["database.connection_time_ms"] = stopwatch.ElapsedMilliseconds,
                ["database.provider"] = _context.Database.ProviderName ?? "unknown",
                ["database.context"] = _serviceName
            };
            
            // Check connection time performance
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning("Database connection is slow: {ElapsedMs}ms for {ServiceName}", 
                    stopwatch.ElapsedMilliseconds, _serviceName);
                return HealthCheckResult.Degraded($"Slow database connection ({stopwatch.ElapsedMilliseconds}ms)", data: data);
            }
            
            activity?.AddTag("database.connection_time_ms", stopwatch.ElapsedMilliseconds);
            
            return HealthCheckResult.Healthy($"Database responding in {stopwatch.ElapsedMilliseconds}ms", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed for {ServiceName}", _serviceName);
            
            Activity.Current?.AddTag("error", true);
            Activity.Current?.AddTag("error.type", ex.GetType().Name);
            Activity.Current?.AddTag("error.message", ex.Message);
            
            return HealthCheckResult.Unhealthy("Database connection failed", ex, new Dictionary<string, object>
            {
                ["database.status"] = "unhealthy",
                ["database.context"] = _serviceName,
                ["error.message"] = ex.Message,
                ["error.type"] = ex.GetType().Name
            });
        }
    }
}
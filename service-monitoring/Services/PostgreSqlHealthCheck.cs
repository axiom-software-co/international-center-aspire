using Infrastructure.Database.Abstractions;
using Npgsql;
using System.Data;

namespace Service.Monitoring.Services;

public sealed class PostgreSqlHealthCheck : IDatabaseHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PostgreSqlHealthCheck> _logger;
    private readonly DatabaseHealthOptions _options;

    public PostgreSqlHealthCheck(
        IDbConnectionFactory connectionFactory,
        ILogger<PostgreSqlHealthCheck> logger,
        IOptions<MonitoringOptions> options)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value?.Database ?? throw new ArgumentNullException(nameof(options));
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
                    Name = "PostgreSQL Database",
                    Status = HealthStatus.Unhealthy,
                    Description = "Cannot establish database connection",
                    Duration = stopwatch.Elapsed,
                    Data = data
                };
            }

            var latency = await MeasureLatencyAsync(cancellationToken);
            data["latency_ms"] = latency.TotalMilliseconds;

            if (_options.CheckMigrations)
            {
                var migrationsUpToDate = await AreMigrationsCurrentAsync(cancellationToken);
                data["migrations_current"] = migrationsUpToDate;

                if (!migrationsUpToDate)
                {
                    return new HealthCheckResult
                    {
                        Name = "PostgreSQL Database",
                        Status = HealthStatus.Degraded,
                        Description = "Database migrations are not current",
                        Duration = stopwatch.Elapsed,
                        Data = data
                    };
                }
            }

            var status = latency > TimeSpan.FromSeconds(2) ? HealthStatus.Degraded : HealthStatus.Healthy;
            var description = status == HealthStatus.Healthy 
                ? "Database is healthy and responsive" 
                : $"Database is responding slowly ({latency.TotalMilliseconds:F0}ms)";

            _logger.LogDebug("PostgreSQL health check completed: {Status} in {Duration}ms", 
                status, stopwatch.Elapsed.TotalMilliseconds);

            return new HealthCheckResult
            {
                Name = "PostgreSQL Database",
                Status = status,
                Description = description,
                Duration = stopwatch.Elapsed,
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PostgreSQL health check failed after {Duration}ms", 
                stopwatch.Elapsed.TotalMilliseconds);

            return new HealthCheckResult
            {
                Name = "PostgreSQL Database",
                Status = HealthStatus.Unhealthy,
                Description = "Database health check failed",
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
                using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                return connection.State == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database connection attempt {Attempt} failed", attempt + 1);
                
                if (attempt < _options.MaxRetryAttempts)
                {
                    await Task.Delay(_options.RetryDelay, cancellationToken);
                }
                
                attempt++;
            }
        }

        return false;
    }

    public async Task<bool> AreMigrationsCurrentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            using var command = connection.CreateCommand();
            
            command.CommandText = @"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = '__EFMigrationsHistory'
                )";
            command.CommandTimeout = (int)_options.Timeout.TotalSeconds;

            var migrationTableExists = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
            
            _logger.LogDebug("Migration table exists: {Exists}", migrationTableExists);
            
            return migrationTableExists;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check migration status");
            return false;
        }
    }

    public async Task<TimeSpan> MeasureLatencyAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            using var command = connection.CreateCommand();
            
            command.CommandText = _options.TestQuery;
            command.CommandTimeout = (int)_options.Timeout.TotalSeconds;
            
            await command.ExecuteScalarAsync(cancellationToken);
            
            return stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to measure database latency");
            return TimeSpan.MaxValue;
        }
    }
}
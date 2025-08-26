using Infrastructure.Database.Abstractions;
using Infrastructure.Database.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Polly;
using System.Data;

namespace Infrastructure.Database.Base;

/// <summary>
/// Generic base Dapper connection factory for high-performance PostgreSQL data access.
/// INFRASTRUCTURE: Generic connection patterns reusable by any domain
/// DOMAIN AGNOSTIC: No knowledge of specific domains (Services, News, Events, etc.)
/// HIGH PERFORMANCE: Optimized connection pooling and retry policies
/// </summary>
public abstract class BaseDapperConnectionFactory : IDbConnectionFactory
{
    private readonly DatabaseConnectionOptions _options;
    private readonly ILogger<BaseDapperConnectionFactory> _logger;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly DatabaseConnectionStatistics _statistics;
    private readonly SemaphoreSlim _connectionSemaphore;
    private bool _disposed;

    protected BaseDapperConnectionFactory(
        IOptions<DatabaseConnectionOptions> options,
        ILogger<BaseDapperConnectionFactory> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _statistics = new DatabaseConnectionStatistics();
        _connectionSemaphore = new SemaphoreSlim(_options.ConnectionPool.MaxPoolSize, _options.ConnectionPool.MaxPoolSize);
        _retryPolicy = CreateRetryPolicy();
    }

    /// <summary>
    /// Create database connection for read/write operations.
    /// INFRASTRUCTURE: Generic connection creation for any domain
    /// </summary>
    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await CreateConnectionInternalAsync(_options.ConnectionString, "ReadWrite", cancellationToken);
    }

    /// <summary>
    /// Create read-only database connection for read operations.
    /// SCALABILITY: Read replica routing for high-read domains
    /// </summary>
    public async Task<IDbConnection> CreateReadOnlyConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = !string.IsNullOrEmpty(_options.ReadOnlyConnectionString) 
            ? _options.ReadOnlyConnectionString 
            : _options.ConnectionString;
            
        return await CreateConnectionInternalAsync(connectionString, "ReadOnly", cancellationToken);
    }

    /// <summary>
    /// Test database connection health.
    /// MONITORING: Generic health check pattern for any domain
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await CreateConnectionAsync(cancellationToken);
            
            // Simple health check query
            const string healthCheckQuery = "SELECT 1";
            using var command = connection.CreateCommand();
            command.CommandText = healthCheckQuery;
            command.CommandTimeout = Math.Min(_options.HealthCheck.TimeoutSeconds, _options.CommandTimeoutSeconds);
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            var isHealthy = result != null && result.Equals(1);
            
            _logger.LogDebug("Database health check completed: {IsHealthy}", isHealthy);
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health check failed");
            return false;
        }
    }

    /// <summary>
    /// Execute database operation with automatic retry policy.
    /// RESILIENCE: Generic retry patterns for database operations
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(Func<IDbConnection, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var connection = await CreateConnectionAsync(cancellationToken);
            return await operation(connection);
        });
    }

    /// <summary>
    /// Get current database connection statistics.
    /// MONITORING: Connection pool and performance statistics
    /// </summary>
    public DatabaseConnectionStatistics GetConnectionStatistics()
    {
        return _statistics with
        {
            Timestamp = DateTime.UtcNow,
            MaxPoolSize = _options.ConnectionPool.MaxPoolSize,
            MinPoolSize = _options.ConnectionPool.MinPoolSize
        };
    }

    /// <summary>
    /// Create database connection with retry policy and connection management.
    /// INFRASTRUCTURE: Internal connection creation with all policies applied
    /// </summary>
    private async Task<IDbConnection> CreateConnectionInternalAsync(string connectionString, string connectionType, CancellationToken cancellationToken)
    {
        await _connectionSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            _logger.LogDebug("Creating {ConnectionType} database connection", connectionType);
            
            var connection = new NpgsqlConnection(connectionString);
            
            // Configure connection properties
            ConfigureConnection(connection);
            
            // Open connection with timeout
            var openTask = connection.OpenAsync(cancellationToken);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_options.CommandTimeoutSeconds), cancellationToken);
            
            var completedTask = await Task.WhenAny(openTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                connection.Dispose();
                throw new TimeoutException($"Database connection timeout after {_options.CommandTimeoutSeconds} seconds");
            }
            
            var duration = DateTime.UtcNow - startTime;
            
            // Update statistics
            UpdateConnectionStatistics(duration, true);
            
            _logger.LogDebug("Successfully created {ConnectionType} database connection in {DurationMs}ms", 
                connectionType, duration.TotalMilliseconds);
            
            return connection;
        }
        catch (Exception ex)
        {
            _connectionSemaphore.Release();
            UpdateConnectionStatistics(TimeSpan.Zero, false);
            
            _logger.LogError(ex, "Failed to create {ConnectionType} database connection", connectionType);
            throw;
        }
    }

    /// <summary>
    /// Configure database connection properties.
    /// INFRASTRUCTURE: Generic connection configuration
    /// </summary>
    protected virtual void ConfigureConnection(NpgsqlConnection connection)
    {
        // Configure connection-specific properties
        connection.CommandTimeout = _options.CommandTimeoutSeconds;
        
        // Set application name for monitoring
        if (!string.IsNullOrEmpty(_options.ApplicationName))
        {
            var builder = new NpgsqlConnectionStringBuilder(connection.ConnectionString)
            {
                ApplicationName = _options.ApplicationName
            };
            connection.ConnectionString = builder.ConnectionString;
        }
    }

    /// <summary>
    /// Create retry policy for transient failures.
    /// RESILIENCE: Exponential backoff with jitter
    /// </summary>
    private IAsyncPolicy CreateRetryPolicy()
    {
        var retryPolicy = Policy
            .Handle<NpgsqlException>(ex => IsTransientError(ex))
            .Or<TimeoutException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: _options.Retry.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt =>
                {
                    var delay = TimeSpan.FromSeconds(_options.Retry.RetryDelaySeconds);
                    
                    if (_options.Retry.EnableExponentialBackoff)
                    {
                        delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * _options.Retry.RetryDelaySeconds);
                    }
                    
                    if (_options.Retry.EnableJitter)
                    {
                        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                        delay = delay.Add(jitter);
                    }
                    
                    return delay;
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Database operation retry {RetryCount} after {DelayMs}ms: {Exception}", 
                        retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message);
                    
                    UpdateRetryStatistics();
                });

        return retryPolicy;
    }

    /// <summary>
    /// Determine if exception is transient and should be retried.
    /// RESILIENCE: Transient error classification
    /// </summary>
    private static bool IsTransientError(NpgsqlException ex)
    {
        // PostgreSQL-specific transient error codes
        return ex.SqlState switch
        {
            "08000" => true, // connection_exception
            "08003" => true, // connection_does_not_exist  
            "08006" => true, // connection_failure
            "08001" => true, // sqlclient_unable_to_establish_sqlconnection
            "08004" => true, // sqlserver_rejected_establishment_of_sqlconnection
            "40001" => true, // serialization_failure
            "40P01" => true, // deadlock_detected
            "53000" => true, // insufficient_resources
            "53100" => true, // disk_full
            "53200" => true, // out_of_memory
            "53300" => true, // too_many_connections
            "54000" => true, // program_limit_exceeded
            "57P03" => true, // cannot_connect_now
            "58000" => true, // system_error
            "58030" => true, // io_error
            _ => false
        };
    }

    /// <summary>
    /// Update connection statistics.
    /// OBSERVABILITY: Connection performance tracking
    /// </summary>
    private void UpdateConnectionStatistics(TimeSpan duration, bool successful)
    {
        // Statistics would be updated here in a thread-safe manner
        // Implementation depends on metrics collection strategy
        if (successful)
        {
            // Update successful connection metrics
        }
        else
        {
            // Update failed connection metrics
        }
    }

    /// <summary>
    /// Update retry statistics.
    /// OBSERVABILITY: Retry attempt tracking
    /// </summary>
    private void UpdateRetryStatistics()
    {
        // Retry statistics would be updated here
        // Implementation depends on metrics collection strategy
    }

    /// <summary>
    /// Dispose of all managed database connections.
    /// RESOURCE MANAGEMENT: Proper cleanup of database resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            _connectionSemaphore?.Dispose();
            await DisposeAsyncCore();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during connection factory disposal");
        }
        finally
        {
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Core disposal logic for derived classes to override.
    /// </summary>
    protected virtual ValueTask DisposeAsyncCore()
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Dispose pattern implementation.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
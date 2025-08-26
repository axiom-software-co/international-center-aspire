using Infrastructure.Cache.Abstractions;
using Infrastructure.Cache.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Infrastructure.Cache.Base;

/// <summary>
/// Generic Redis connection factory base implementation for high-performance caching and rate limiting.
/// INFRASTRUCTURE: Generic Redis connection management reusable by any domain
/// </summary>
public abstract class BaseRedisConnectionFactory : IRedisConnectionFactory, IAsyncDisposable
{
    private readonly RedisConnectionOptions _options;
    private readonly ILogger<BaseRedisConnectionFactory> _logger;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly Dictionary<int, IDatabase> _databases;
    private readonly object _lockObject = new();
    
    private IConnectionMultiplexer? _connection;
    private volatile bool _disposed;
    private long _commandCount;
    private long _failedConnections;
    private long _successfulRetries;
    private readonly DateTime _startTime = DateTime.UtcNow;

    protected BaseRedisConnectionFactory(
        IOptions<RedisConnectionOptions> options,
        ILogger<BaseRedisConnectionFactory> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionSemaphore = new SemaphoreSlim(1, 1);
        _databases = new Dictionary<int, IDatabase>();
    }

    public async Task<IDatabase> GetDatabaseAsync(int? database = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        var dbNumber = database ?? _options.Database;
        
        if (_databases.TryGetValue(dbNumber, out var existingDb))
        {
            return existingDb;
        }

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_databases.TryGetValue(dbNumber, out existingDb))
            {
                return existingDb;
            }

            var connection = await GetConnectionAsync(cancellationToken);
            var db = connection.GetDatabase(dbNumber);
            
            lock (_lockObject)
            {
                _databases[dbNumber] = db;
            }

            _logger.LogDebug("Created Redis database connection for database {DatabaseNumber}", dbNumber);
            return db;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<IDatabase> GetRateLimitingDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var rateLimitingDb = _options.RateLimiting.Database;
        _logger.LogDebug("Using rate limiting database {DatabaseNumber}", rateLimitingDb);
        return await GetDatabaseAsync(rateLimitingDb, cancellationToken);
    }

    public async Task<IServer> GetServerAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        var connection = await GetConnectionAsync(cancellationToken);
        var endpoints = connection.GetEndPoints();
        
        if (endpoints.Length == 0)
        {
            throw new InvalidOperationException("No Redis endpoints available for server operations");
        }

        return connection.GetServer(endpoints[0]);
    }

    public async Task<ISubscriber> GetSubscriberAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        var connection = await GetConnectionAsync(cancellationToken);
        return connection.GetSubscriber();
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var database = await GetDatabaseAsync(cancellationToken: cancellationToken);
            await database.PingAsync();
            
            _logger.LogDebug("Redis connection health check passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis connection health check failed");
            return false;
        }
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<IDatabase, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(operation);

        var retryCount = 0;
        var maxRetries = _options.Retry.MaxRetryAttempts;
        var baseDelay = TimeSpan.FromSeconds(_options.Retry.RetryDelaySeconds);

        while (true)
        {
            try
            {
                var database = await GetDatabaseAsync(cancellationToken: cancellationToken);
                var result = await operation(database);
                
                Interlocked.Increment(ref _commandCount);
                
                if (retryCount > 0)
                {
                    Interlocked.Increment(ref _successfulRetries);
                    _logger.LogDebug("Redis operation succeeded after {RetryCount} retries", retryCount);
                }

                return result;
            }
            catch (Exception ex) when (retryCount < maxRetries && IsTransientException(ex))
            {
                retryCount++;
                var delay = CalculateDelay(baseDelay, retryCount);
                
                _logger.LogWarning(ex, 
                    "Redis operation failed (attempt {AttemptNumber}/{MaxAttempts}). Retrying in {DelayMs}ms", 
                    retryCount, maxRetries + 1, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    public RedisConnectionStatistics GetConnectionStatistics()
    {
        var uptime = DateTime.UtcNow - _startTime;
        var activeConnections = _connection?.IsConnected == true ? 1 : 0;
        
        return new RedisConnectionStatistics
        {
            TotalConnectionsCreated = 1,
            ActiveConnections = activeConnections,
            IdleConnections = _connection?.IsConnected == true ? 0 : 1,
            MaxPoolSize = _options.ConnectionPool.MaxPoolSize,
            AverageConnectionTimeMs = 0, // Would need timing implementation
            FailedConnections = (int)_failedConnections,
            SuccessfulRetries = (int)_successfulRetries,
            TotalCommandsExecuted = _commandCount,
            AverageCommandTimeMs = 0, // Would need timing implementation
            TimeoutCount = 0, // Would need timeout tracking
            Timestamp = DateTime.UtcNow
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        _connectionSemaphore.Dispose();
        
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }

        lock (_lockObject)
        {
            _databases.Clear();
        }

        _logger.LogInformation("Redis connection factory disposed");
    }

    private async Task<IConnectionMultiplexer> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection?.IsConnected == true)
        {
            return _connection;
        }

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_connection?.IsConnected == true)
            {
                return _connection;
            }

            _logger.LogDebug("Creating new Redis connection");
            
            var configurationOptions = new ConfigurationOptions
            {
                ConnectTimeout = _options.ConnectTimeoutSeconds * 1000,
                SyncTimeout = _options.CommandTimeoutSeconds * 1000,
                AbortOnConnectFail = _options.ConnectionPool.AbortOnConnectFail,
                AllowAdmin = _options.ConnectionPool.AllowAdmin,
                ClientName = _options.ApplicationName
            };
            
            configurationOptions.EndPoints.Add(_options.ConnectionString);

            try
            {
                _connection = await ConnectionMultiplexer.ConnectAsync(configurationOptions);
                
                _connection.ConnectionFailed += (sender, args) =>
                {
                    _logger.LogError("Redis connection failed: {Exception}", args.Exception?.Message);
                    Interlocked.Increment(ref _failedConnections);
                };

                _connection.ConnectionRestored += (sender, args) =>
                {
                    _logger.LogInformation("Redis connection restored");
                };

                _logger.LogInformation("Redis connection established successfully");
                return _connection;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedConnections);
                _logger.LogError(ex, "Failed to create Redis connection");
                throw;
            }
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    private TimeSpan CalculateDelay(TimeSpan baseDelay, int attempt)
    {
        if (!_options.Retry.EnableExponentialBackoff)
        {
            return baseDelay;
        }

        var exponentialDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
        
        if (_options.Retry.EnableJitter)
        {
            var jitter = Random.Shared.Next(0, (int)(exponentialDelay.TotalMilliseconds * 0.1));
            exponentialDelay = exponentialDelay.Add(TimeSpan.FromMilliseconds(jitter));
        }

        return exponentialDelay;
    }

    private static bool IsTransientException(Exception ex)
    {
        return ex is RedisConnectionException or RedisTimeoutException or TaskCanceledException;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(BaseRedisConnectionFactory));
        }
    }
}
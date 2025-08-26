using StackExchange.Redis;

namespace Infrastructure.Cache.Abstractions;

/// <summary>
/// CONTRACT: Generic Redis connection factory for high-performance caching and rate limiting.
/// 
/// TDD PRINCIPLE: Interface drives the design of Redis connection management
/// DEPENDENCY INVERSION: Abstractions for variable Redis concerns
/// INFRASTRUCTURE: Generic Redis connection patterns reusable by any domain
/// DOMAIN AGNOSTIC: No knowledge of Services, News, Events, or any specific domain
/// </summary>
public interface IRedisConnectionFactory
{
    /// <summary>
    /// CONTRACT: Create Redis database connection for caching operations
    /// 
    /// POSTCONDITION: Returns configured Redis database ready for use
    /// INFRASTRUCTURE: Generic Redis connection for any domain
    /// CACHING: Database connection for distributed caching operations
    /// </summary>
    /// <param name="database">Optional database number (uses default if not specified)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Redis database connection ready for operations</returns>
    Task<IDatabase> GetDatabaseAsync(int? database = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Create Redis database connection for rate limiting operations
    /// 
    /// PRECONDITION: Rate limiting database configured
    /// POSTCONDITION: Returns rate limiting database connection
    /// RATE LIMITING: Dedicated database for rate limiting operations
    /// ISOLATION: Separate database to isolate rate limiting from general caching
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Rate limiting database connection</returns>
    Task<IDatabase> GetRateLimitingDatabaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Get Redis server for administrative operations
    /// 
    /// PRECONDITION: Administrative operations enabled in configuration
    /// POSTCONDITION: Returns Redis server for admin operations
    /// ADMINISTRATION: Server-level operations and monitoring
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Redis server for administrative operations</returns>
    Task<IServer> GetServerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Get Redis subscriber for pub/sub operations
    /// 
    /// POSTCONDITION: Returns Redis subscriber for messaging
    /// MESSAGING: Pub/sub messaging patterns
    /// REAL-TIME: Real-time communication capabilities
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Redis subscriber for pub/sub operations</returns>
    Task<ISubscriber> GetSubscriberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Test Redis connection health
    /// 
    /// POSTCONDITION: Returns true if Redis is accessible and healthy
    /// MONITORING: Generic health check pattern for any domain
    /// RESILIENCE: Connection health verification
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if Redis connection is healthy</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Execute Redis operation with automatic retry policy
    /// 
    /// PRECONDITION: Valid Redis operation function
    /// POSTCONDITION: Operation executed with retry logic for transient failures
    /// RESILIENCE: Generic retry patterns for Redis operations
    /// </summary>
    /// <typeparam name="T">Return type of Redis operation</typeparam>
    /// <param name="operation">Redis operation to execute</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Result of Redis operation</returns>
    Task<T> ExecuteWithRetryAsync<T>(Func<IDatabase, Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Get current Redis connection statistics
    /// 
    /// POSTCONDITION: Returns connection and performance statistics
    /// MONITORING: Connection metrics for any domain
    /// OBSERVABILITY: Redis connection monitoring
    /// </summary>
    /// <returns>Redis connection statistics</returns>
    RedisConnectionStatistics GetConnectionStatistics();

    /// <summary>
    /// CONTRACT: Dispose of all managed Redis connections
    /// 
    /// POSTCONDITION: All connections properly disposed and resources cleaned up
    /// RESOURCE MANAGEMENT: Proper cleanup of Redis resources
    /// </summary>
    ValueTask DisposeAsync();
}

/// <summary>
/// Redis connection statistics for monitoring and observability.
/// INFRASTRUCTURE: Generic connection monitoring for any domain
/// </summary>
public sealed class RedisConnectionStatistics
{
    /// <summary>Total number of connections created</summary>
    public int TotalConnectionsCreated { get; init; }
    
    /// <summary>Currently active connections</summary>
    public int ActiveConnections { get; init; }
    
    /// <summary>Currently idle connections in pool</summary>
    public int IdleConnections { get; init; }
    
    /// <summary>Maximum pool size configured</summary>
    public int MaxPoolSize { get; init; }
    
    /// <summary>Average connection creation time in milliseconds</summary>
    public double AverageConnectionTimeMs { get; init; }
    
    /// <summary>Number of failed connection attempts</summary>
    public int FailedConnections { get; init; }
    
    /// <summary>Number of successful retry attempts</summary>
    public int SuccessfulRetries { get; init; }
    
    /// <summary>Total Redis commands executed</summary>
    public long TotalCommandsExecuted { get; init; }
    
    /// <summary>Average command execution time in milliseconds</summary>
    public double AverageCommandTimeMs { get; init; }
    
    /// <summary>Number of timeouts</summary>
    public int TimeoutCount { get; init; }
    
    /// <summary>Statistics timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
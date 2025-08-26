using System.Data;

namespace Infrastructure.Database.Abstractions;

/// <summary>
/// CONTRACT: Generic database connection factory for high-performance data access.
/// 
/// TDD PRINCIPLE: Interface drives the design of database connection management
/// DEPENDENCY INVERSION: Abstractions for variable connection concerns
/// INFRASTRUCTURE: Generic database connection patterns reusable by any domain
/// DOMAIN AGNOSTIC: No knowledge of Services, News, Events, or any specific domain
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// CONTRACT: Create database connection for read/write operations
    /// 
    /// POSTCONDITION: Returns configured PostgreSQL connection ready for use
    /// INFRASTRUCTURE: Generic connection creation for any domain
    /// CONNECTION MANAGEMENT: Proper connection lifecycle and pooling
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Database connection ready for operations</returns>
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Create read-only database connection for read operations
    /// 
    /// PRECONDITION: Read-only connection string configured
    /// POSTCONDITION: Returns read-only connection for query operations
    /// SCALABILITY: Read replica routing for high-read domains
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Read-only database connection for queries</returns>
    Task<IDbConnection> CreateReadOnlyConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Test database connection health
    /// 
    /// POSTCONDITION: Returns true if database is accessible and healthy
    /// MONITORING: Generic health check pattern for any domain
    /// RESILIENCE: Connection health verification
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if database connection is healthy</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Execute database operation with automatic retry policy
    /// 
    /// PRECONDITION: Valid database operation function
    /// POSTCONDITION: Operation executed with retry logic for transient failures
    /// RESILIENCE: Generic retry patterns for database operations
    /// </summary>
    /// <typeparam name="T">Return type of database operation</typeparam>
    /// <param name="operation">Database operation to execute</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Result of database operation</returns>
    Task<T> ExecuteWithRetryAsync<T>(Func<IDbConnection, Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Get current database connection statistics
    /// 
    /// POSTCONDITION: Returns connection pool and performance statistics
    /// MONITORING: Connection pool metrics for any domain
    /// OBSERVABILITY: Database connection monitoring
    /// </summary>
    /// <returns>Database connection statistics</returns>
    DatabaseConnectionStatistics GetConnectionStatistics();

    /// <summary>
    /// CONTRACT: Dispose of all managed database connections
    /// 
    /// POSTCONDITION: All connections properly disposed and resources cleaned up
    /// RESOURCE MANAGEMENT: Proper cleanup of database resources
    /// </summary>
    ValueTask DisposeAsync();
}

/// <summary>
/// Database connection statistics for monitoring and observability.
/// INFRASTRUCTURE: Generic connection monitoring for any domain
/// </summary>
public sealed class DatabaseConnectionStatistics
{
    /// <summary>Total number of connections created</summary>
    public int TotalConnectionsCreated { get; init; }
    
    /// <summary>Currently active connections</summary>
    public int ActiveConnections { get; init; }
    
    /// <summary>Currently idle connections in pool</summary>
    public int IdleConnections { get; init; }
    
    /// <summary>Maximum pool size configured</summary>
    public int MaxPoolSize { get; init; }
    
    /// <summary>Minimum pool size configured</summary>
    public int MinPoolSize { get; init; }
    
    /// <summary>Average connection creation time in milliseconds</summary>
    public double AverageConnectionTimeMs { get; init; }
    
    /// <summary>Number of failed connection attempts</summary>
    public int FailedConnections { get; init; }
    
    /// <summary>Number of successful retry attempts</summary>
    public int SuccessfulRetries { get; init; }
    
    /// <summary>Statistics timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
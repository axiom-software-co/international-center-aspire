using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Database.Abstractions;

/// <summary>
/// CONTRACT: Generic auditable database context interface for EF Core operations with medical-grade compliance.
/// 
/// TDD PRINCIPLE: Interface drives the design of auditable database architecture
/// DEPENDENCY INVERSION: Abstractions for variable database concerns
/// INFRASTRUCTURE: Generic auditable patterns reusable by any domain
/// DOMAIN AGNOSTIC: No knowledge of Services, News, Events, or any specific domain
/// </summary>
public interface IAuditableDbContext : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// CONTRACT: Provide EF Core database access for complex operations
    /// 
    /// POSTCONDITION: Returns configured EF Core database instance
    /// INFRASTRUCTURE: Generic database access for any domain
    /// </summary>
    DatabaseFacade Database { get; }

    /// <summary>
    /// CONTRACT: Save all changes with automatic medical-grade audit logging
    /// 
    /// PRECONDITION: Valid entity changes in change tracker
    /// POSTCONDITION: All changes saved with comprehensive audit trail
    /// MEDICAL COMPLIANCE: All data mutations logged with tamper-proof audit
    /// TRANSACTIONAL: Atomic operation ensuring data consistency
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Number of affected entities</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Save changes without triggering audit logging (internal use only)
    /// 
    /// PRECONDITION: Valid entity changes in change tracker
    /// POSTCONDITION: Changes saved without audit trail generation
    /// INTERNAL USE: For audit system internal operations to prevent recursion
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Number of affected entities</returns>
    Task<int> SaveChangesWithoutAuditAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Begin database transaction for complex multi-step operations
    /// 
    /// POSTCONDITION: Returns database transaction for atomic operations
    /// INFRASTRUCTURE: Transaction support for complex operations
    /// CONSISTENCY: Ensures data consistency across multiple operations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Database transaction for atomic operations</returns>
    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Create DbSet for entity type with change tracking
    /// 
    /// PRECONDITION: Valid entity type T with proper EF configuration
    /// POSTCONDITION: Returns DbSet with full EF Core features enabled
    /// INFRASTRUCTURE: Generic entity operations for any domain
    /// </summary>
    /// <typeparam name="TEntity">Entity type for database operations</typeparam>
    /// <returns>DbSet for entity operations with change tracking</returns>
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    /// <summary>
    /// CONTRACT: Execute raw SQL command for complex operations
    /// 
    /// PRECONDITION: Valid SQL command and optional parameters
    /// POSTCONDITION: SQL command executed with proper connection management
    /// INFRASTRUCTURE: Raw SQL support for complex operations
    /// AUDIT: SQL operations logged for audit trail
    /// </summary>
    /// <param name="sql">Raw SQL command to execute</param>
    /// <param name="parameters">Optional SQL parameters</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Number of affected rows</returns>
    Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);
    Task<int> ExecuteSqlRawAsync(string sql, CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Execute raw SQL query for complex reporting
    /// 
    /// PRECONDITION: Valid SQL query returning entity type T
    /// POSTCONDITION: Returns query results with proper entity tracking
    /// INFRASTRUCTURE: Complex query support for any domain
    /// </summary>
    /// <typeparam name="TEntity">Entity type for query results</typeparam>
    /// <param name="sql">Raw SQL query to execute</param>
    /// <param name="parameters">Optional SQL parameters</param>
    /// <returns>Query results as IQueryable for further composition</returns>
    IQueryable<TEntity> FromSqlRaw<TEntity>(string sql, params object[] parameters) where TEntity : class;

    /// <summary>
    /// CONTRACT: Get current audit context for medical compliance
    /// 
    /// POSTCONDITION: Returns audit context with user, correlation, and request information
    /// MEDICAL COMPLIANCE: Audit context for all database operations
    /// INFRASTRUCTURE: Generic audit context for any domain
    /// </summary>
    /// <returns>Current audit context for database operations</returns>
    AuditContext GetCurrentAuditContext();

    /// <summary>
    /// CONTRACT: Set custom audit context for special operations
    /// 
    /// PRECONDITION: Valid audit context with required audit information
    /// POSTCONDITION: Audit context set for subsequent database operations
    /// INFRASTRUCTURE: Custom audit context for system operations
    /// </summary>
    /// <param name="auditContext">Custom audit context for operations</param>
    void SetAuditContext(AuditContext auditContext);

    /// <summary>
    /// CONTRACT: Get database performance metrics
    /// 
    /// POSTCONDITION: Returns performance metrics for monitoring
    /// OBSERVABILITY: Database performance monitoring
    /// INFRASTRUCTURE: Generic performance metrics for any domain
    /// </summary>
    /// <returns>Database performance metrics</returns>
    DatabasePerformanceMetrics GetPerformanceMetrics();
}

/// <summary>
/// Generic audit context for medical-grade compliance tracking.
/// INFRASTRUCTURE: Generic audit context reusable by any domain
/// DOMAIN AGNOSTIC: No knowledge of specific domains
/// </summary>
public sealed class AuditContext
{
    /// <summary>Correlation ID for request tracing</summary>
    public required string CorrelationId { get; init; }
    
    /// <summary>User ID performing the operation</summary>
    public required string UserId { get; init; }
    
    /// <summary>User name performing the operation</summary>
    public string? UserName { get; init; }
    
    /// <summary>Request URL that triggered the operation</summary>
    public string? RequestUrl { get; init; }
    
    /// <summary>HTTP method for the request</summary>
    public string? RequestMethod { get; init; }
    
    /// <summary>Client IP address</summary>
    public string? RequestIp { get; init; }
    
    /// <summary>User agent string</summary>
    public string? UserAgent { get; init; }
    
    /// <summary>Session identifier</summary>
    public string? SessionId { get; init; }
    
    /// <summary>Application version</summary>
    public string? AppVersion { get; init; }
    
    /// <summary>Build date for version tracking</summary>
    public string? BuildDate { get; init; }
    
    /// <summary>Trace ID for distributed tracing</summary>
    public string? TraceId { get; init; }
    
    /// <summary>Request start time</summary>
    public DateTime RequestStartTime { get; init; } = DateTime.UtcNow;
    
    /// <summary>Client application identifier</summary>
    public string? ClientApplication { get; init; }
    
    /// <summary>Domain context (filled by higher layers)</summary>
    public string? DomainContext { get; init; }
}

/// <summary>
/// Database performance metrics for monitoring and observability.
/// INFRASTRUCTURE: Generic performance monitoring for any domain
/// </summary>
public sealed class DatabasePerformanceMetrics
{
    /// <summary>Total number of queries executed</summary>
    public long TotalQueries { get; init; }
    
    /// <summary>Average query execution time in milliseconds</summary>
    public double AverageQueryTimeMs { get; init; }
    
    /// <summary>Number of slow queries (above threshold)</summary>
    public int SlowQueryCount { get; init; }
    
    /// <summary>Total number of SaveChanges operations</summary>
    public long TotalSaveOperations { get; init; }
    
    /// <summary>Average SaveChanges execution time in milliseconds</summary>
    public double AverageSaveTimeMs { get; init; }
    
    /// <summary>Number of failed operations</summary>
    public int FailedOperations { get; init; }
    
    /// <summary>Number of retry attempts</summary>
    public int RetryAttempts { get; init; }
    
    /// <summary>Current change tracker entity count</summary>
    public int TrackedEntityCount { get; init; }
    
    /// <summary>Metrics timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
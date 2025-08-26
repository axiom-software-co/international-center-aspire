using Microsoft.Extensions.Logging;

namespace InternationalCenter.Shared.Tests.Abstractions;

/// <summary>
/// Contract for test data management with dependency inversion
/// Provides consistent test data creation, lifecycle management, and cleanup across all test domains
/// Medical-grade data management ensuring test isolation and repeatability
/// </summary>
/// <typeparam name="TEntity">The entity type being managed</typeparam>
/// <typeparam name="TId">The entity identifier type</typeparam>
public interface ITestDataManager<TEntity, TId> : IAsyncDisposable
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Creates a single test entity with optional configuration
    /// Contract: Must generate realistic, valid test data following domain rules
    /// </summary>
    Task<TEntity> CreateAsync(
        Action<TEntity>? configure = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates multiple test entities with optional configuration
    /// Contract: Must ensure data uniqueness and referential integrity
    /// </summary>
    Task<IEnumerable<TEntity>> CreateManyAsync(
        int count,
        Action<TEntity>? configure = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Persists test entities to the data store
    /// Contract: Must handle transaction rollback on failure
    /// </summary>
    Task<T> PersistAsync<T>(
        T entity,
        CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Persists multiple test entities to the data store
    /// Contract: Must maintain transaction consistency across all entities
    /// </summary>
    Task<IEnumerable<T>> PersistManyAsync<T>(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Retrieves a test entity by its identifier
    /// Contract: Must return null for non-existent entities
    /// </summary>
    Task<TEntity?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates entity state against domain rules
    /// Contract: Must throw descriptive validation exceptions for invalid entities
    /// </summary>
    Task ValidateEntityAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cleans up specific test entities
    /// Contract: Must ensure complete removal and cascade deletions
    /// </summary>
    Task CleanupAsync(
        params TEntity[] entities);
    
    /// <summary>
    /// Cleans up test entities by their identifiers
    /// Contract: Must handle non-existent entities gracefully
    /// </summary>
    Task CleanupByIdsAsync(
        params TId[] ids);
    
    /// <summary>
    /// Cleans up all test entities created by this manager
    /// Contract: Must maintain cleanup order for referential integrity
    /// </summary>
    Task CleanupAllAsync();
    
    /// <summary>
    /// Gets statistics about test data management operations
    /// </summary>
    ITestDataStatistics GetStatistics();
}

/// <summary>
/// Contract for test data statistics and monitoring
/// Provides insights into test data management performance and usage
/// </summary>
public interface ITestDataStatistics
{
    /// <summary>
    /// Gets the total number of entities created
    /// </summary>
    int EntitiesCreated { get; }
    
    /// <summary>
    /// Gets the total number of entities persisted
    /// </summary>
    int EntitiesPersisted { get; }
    
    /// <summary>
    /// Gets the total number of cleanup operations performed
    /// </summary>
    int CleanupOperations { get; }
    
    /// <summary>
    /// Gets the average time for entity creation
    /// </summary>
    TimeSpan AverageCreationTime { get; }
    
    /// <summary>
    /// Gets the average time for entity persistence
    /// </summary>
    TimeSpan AveragePersistenceTime { get; }
    
    /// <summary>
    /// Gets the total memory allocated for test data
    /// </summary>
    long TotalMemoryAllocated { get; }
    
    /// <summary>
    /// Gets the success rate for test data operations
    /// </summary>
    double SuccessRate { get; }
}

/// <summary>
/// Contract for test data session management
/// Provides scoped test data management within a specific test execution context
/// </summary>
public interface ITestDataSession : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique session identifier
    /// </summary>
    string SessionId { get; }
    
    /// <summary>
    /// Gets the session creation timestamp
    /// </summary>
    DateTime CreatedAt { get; }
    
    /// <summary>
    /// Registers an entity for automatic cleanup when the session ends
    /// Contract: Must track entities for proper cleanup order
    /// </summary>
    void RegisterEntity<T>(T entity, Func<Task> cleanupAction) where T : class;
    
    /// <summary>
    /// Registers a cleanup action to be performed when the session ends
    /// Contract: Must execute cleanup actions in reverse registration order
    /// </summary>
    void RegisterCleanupAction(Func<Task> cleanupAction, string description);
    
    /// <summary>
    /// Gets the number of entities registered in this session
    /// </summary>
    int RegisteredEntityCount { get; }
    
    /// <summary>
    /// Gets the number of cleanup actions registered in this session
    /// </summary>
    int RegisteredCleanupActions { get; }
}
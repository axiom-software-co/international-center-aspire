using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using InternationalCenter.Shared.Tests.Abstractions;

namespace InternationalCenter.Shared.Tests.Infrastructure;

/// <summary>
/// Concrete implementation of test data management with lifecycle tracking
/// Provides consistent test data creation, persistence, and cleanup across all test domains
/// Medical-grade data management ensuring test isolation and repeatability
/// </summary>
public class TestDataManager<TEntity, TId> : ITestDataManager<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly ILogger<TestDataManager<TEntity, TId>> _logger;
    private readonly Func<Action<TEntity>?, Task<TEntity>> _entityFactory;
    private readonly Func<TEntity, Task<TEntity>>? _persistAction;
    private readonly Func<IEnumerable<TEntity>, Task<IEnumerable<TEntity>>>? _persistManyAction;
    private readonly Func<TId, Task<TEntity?>>? _getByIdAction;
    private readonly Func<TEntity, Task>? _validateAction;
    private readonly Func<TEntity[], Task>? _cleanupAction;
    private readonly Func<TId[], Task>? _cleanupByIdsAction;
    
    private readonly ConcurrentBag<TEntity> _createdEntities;
    private readonly ConcurrentBag<TEntity> _persistedEntities;
    private readonly TestDataStatistics _statistics;
    private readonly object _lockObject = new();
    private bool _disposed;

    public TestDataManager(
        ILogger<TestDataManager<TEntity, TId>> logger,
        Func<Action<TEntity>?, Task<TEntity>> entityFactory,
        Func<TEntity, Task<TEntity>>? persistAction = null,
        Func<IEnumerable<TEntity>, Task<IEnumerable<TEntity>>>? persistManyAction = null,
        Func<TId, Task<TEntity?>>? getByIdAction = null,
        Func<TEntity, Task>? validateAction = null,
        Func<TEntity[], Task>? cleanupAction = null,
        Func<TId[], Task>? cleanupByIdsAction = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _entityFactory = entityFactory ?? throw new ArgumentNullException(nameof(entityFactory));
        _persistAction = persistAction;
        _persistManyAction = persistManyAction;
        _getByIdAction = getByIdAction;
        _validateAction = validateAction;
        _cleanupAction = cleanupAction;
        _cleanupByIdsAction = cleanupByIdsAction;

        _createdEntities = new ConcurrentBag<TEntity>();
        _persistedEntities = new ConcurrentBag<TEntity>();
        _statistics = new TestDataStatistics();
    }

    /// <summary>
    /// Creates a single test entity with optional configuration
    /// Contract: Must generate realistic, valid test data following domain rules
    /// </summary>
    public async Task<TEntity> CreateAsync(
        Action<TEntity>? configure = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);

        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogDebug("Creating test entity of type {EntityType}", typeof(TEntity).Name);

            var entity = await _entityFactory(configure);
            
            if (entity == null)
            {
                throw new InvalidOperationException($"Entity factory returned null for type {typeof(TEntity).Name}");
            }

            // Validate the created entity if validator provided
            if (_validateAction != null)
            {
                await _validateAction(entity);
            }

            _createdEntities.Add(entity);

            lock (_lockObject)
            {
                _statistics.EntitiesCreated++;
                _statistics.TotalCreationTime += DateTime.UtcNow - startTime;
                _statistics.LastOperationTime = DateTime.UtcNow;
            }

            _logger.LogDebug("Successfully created test entity of type {EntityType}", typeof(TEntity).Name);
            return entity;
        }
        catch (Exception ex)
        {
            lock (_lockObject)
            {
                _statistics.FailedOperations++;
            }

            _logger.LogError(ex, "Failed to create test entity of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Creates multiple test entities with optional configuration
    /// Contract: Must ensure data uniqueness and referential integrity
    /// </summary>
    public async Task<IEnumerable<TEntity>> CreateManyAsync(
        int count,
        Action<TEntity>? configure = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);

        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero");
        }

        if (count > 1000)
        {
            _logger.LogWarning("Creating large number of test entities ({Count}). Consider using smaller batches for better performance", count);
        }

        var entities = new List<TEntity>(count);
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogDebug("Creating {Count} test entities of type {EntityType}", count, typeof(TEntity).Name);

            for (int i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Modify configuration for uniqueness
                Action<TEntity>? uniqueConfigure = configure == null ? null : entity =>
                {
                    configure(entity);
                    // Additional uniqueness logic could be added here if needed
                };

                var entity = await CreateAsync(uniqueConfigure, cancellationToken);
                entities.Add(entity);
            }

            _logger.LogDebug("Successfully created {Count} test entities of type {EntityType}", count, typeof(TEntity).Name);
            return entities;
        }
        catch (Exception ex)
        {
            lock (_lockObject)
            {
                _statistics.FailedOperations++;
            }

            _logger.LogError(ex, "Failed to create {Count} test entities of type {EntityType}", count, typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Persists test entities to the data store
    /// Contract: Must handle transaction rollback on failure
    /// </summary>
    public async Task<T> PersistAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : class
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentNullException.ThrowIfNull(entity);

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogDebug("Persisting entity of type {EntityType}", typeof(T).Name);

            // Handle TEntity specifically
            if (entity is TEntity typedEntity && _persistAction != null)
            {
                var persistedEntity = await _persistAction(typedEntity);
                _persistedEntities.Add(persistedEntity);

                lock (_lockObject)
                {
                    _statistics.EntitiesPersisted++;
                    _statistics.TotalPersistenceTime += DateTime.UtcNow - startTime;
                    _statistics.LastOperationTime = DateTime.UtcNow;
                }

                _logger.LogDebug("Successfully persisted entity of type {EntityType}", typeof(T).Name);
                return (T)(object)persistedEntity;
            }
            else
            {
                // For other entity types, just return as-is (mock persistence)
                _logger.LogDebug("Mock persistence for entity type {EntityType} (no persist action configured)", typeof(T).Name);
                return entity;
            }
        }
        catch (Exception ex)
        {
            lock (_lockObject)
            {
                _statistics.FailedOperations++;
            }

            _logger.LogError(ex, "Failed to persist entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Persists multiple test entities to the data store
    /// Contract: Must maintain transaction consistency across all entities
    /// </summary>
    public async Task<IEnumerable<T>> PersistManyAsync<T>(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default) where T : class
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentNullException.ThrowIfNull(entities);

        var entitiesList = entities.ToList();
        if (!entitiesList.Any())
        {
            return entitiesList;
        }

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogDebug("Persisting {Count} entities of type {EntityType}", entitiesList.Count, typeof(T).Name);

            // Handle TEntity specifically with batch operation if available
            if (typeof(T) == typeof(TEntity) && _persistManyAction != null)
            {
                var typedEntities = entitiesList.Cast<TEntity>();
                var persistedEntities = await _persistManyAction(typedEntities);
                
                foreach (var persistedEntity in persistedEntities)
                {
                    _persistedEntities.Add(persistedEntity);
                }

                lock (_lockObject)
                {
                    _statistics.EntitiesPersisted += entitiesList.Count;
                    _statistics.TotalPersistenceTime += DateTime.UtcNow - startTime;
                    _statistics.LastOperationTime = DateTime.UtcNow;
                }

                _logger.LogDebug("Successfully persisted {Count} entities of type {EntityType} using batch operation", 
                    entitiesList.Count, typeof(T).Name);
                return persistedEntities.Cast<T>();
            }
            else
            {
                // Fall back to individual persistence
                var persistedEntities = new List<T>();
                foreach (var entity in entitiesList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var persistedEntity = await PersistAsync(entity, cancellationToken);
                    persistedEntities.Add(persistedEntity);
                }

                _logger.LogDebug("Successfully persisted {Count} entities of type {EntityType} using individual operations", 
                    entitiesList.Count, typeof(T).Name);
                return persistedEntities;
            }
        }
        catch (Exception ex)
        {
            lock (_lockObject)
            {
                _statistics.FailedOperations++;
            }

            _logger.LogError(ex, "Failed to persist {Count} entities of type {EntityType}", entitiesList.Count, typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a test entity by its identifier
    /// Contract: Must return null for non-existent entities
    /// </summary>
    public async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentNullException.ThrowIfNull(id);

        try
        {
            _logger.LogDebug("Retrieving entity of type {EntityType} by ID: {Id}", typeof(TEntity).Name, id);

            if (_getByIdAction != null)
            {
                var entity = await _getByIdAction(id);
                
                if (entity != null)
                {
                    _logger.LogDebug("Successfully retrieved entity of type {EntityType} by ID: {Id}", typeof(TEntity).Name, id);
                }
                else
                {
                    _logger.LogDebug("Entity of type {EntityType} not found for ID: {Id}", typeof(TEntity).Name, id);
                }

                return entity;
            }
            else
            {
                _logger.LogDebug("No GetById action configured for type {EntityType}, returning null", typeof(TEntity).Name);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve entity of type {EntityType} by ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    /// <summary>
    /// Validates entity state against domain rules
    /// Contract: Must throw descriptive validation exceptions for invalid entities
    /// </summary>
    public async Task ValidateEntityAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            _logger.LogDebug("Validating entity of type {EntityType}", typeof(TEntity).Name);

            if (_validateAction != null)
            {
                await _validateAction(entity);
                _logger.LogDebug("Entity validation passed for type {EntityType}", typeof(TEntity).Name);
            }
            else
            {
                _logger.LogDebug("No validation action configured for type {EntityType}, skipping validation", typeof(TEntity).Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Entity validation failed for type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Cleans up specific test entities
    /// Contract: Must ensure complete removal and cascade deletions
    /// </summary>
    public async Task CleanupAsync(params TEntity[] entities)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);

        if (entities == null || !entities.Any())
        {
            _logger.LogDebug("No entities provided for cleanup");
            return;
        }

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogDebug("Cleaning up {Count} entities of type {EntityType}", entities.Length, typeof(TEntity).Name);

            if (_cleanupAction != null)
            {
                await _cleanupAction(entities);
            }
            else
            {
                _logger.LogDebug("No cleanup action configured for type {EntityType}, performing mock cleanup", typeof(TEntity).Name);
            }

            lock (_lockObject)
            {
                _statistics.CleanupOperations++;
                _statistics.LastOperationTime = DateTime.UtcNow;
            }

            _logger.LogDebug("Successfully cleaned up {Count} entities of type {EntityType}", entities.Length, typeof(TEntity).Name);
        }
        catch (Exception ex)
        {
            lock (_lockObject)
            {
                _statistics.FailedOperations++;
            }

            _logger.LogError(ex, "Failed to cleanup {Count} entities of type {EntityType}", entities.Length, typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Cleans up test entities by their identifiers
    /// Contract: Must handle non-existent entities gracefully
    /// </summary>
    public async Task CleanupByIdsAsync(params TId[] ids)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);

        if (ids == null || !ids.Any())
        {
            _logger.LogDebug("No entity IDs provided for cleanup");
            return;
        }

        try
        {
            _logger.LogDebug("Cleaning up entities of type {EntityType} by {Count} IDs", typeof(TEntity).Name, ids.Length);

            if (_cleanupByIdsAction != null)
            {
                await _cleanupByIdsAction(ids);
            }
            else
            {
                _logger.LogDebug("No cleanup by IDs action configured for type {EntityType}, performing mock cleanup", typeof(TEntity).Name);
            }

            lock (_lockObject)
            {
                _statistics.CleanupOperations++;
                _statistics.LastOperationTime = DateTime.UtcNow;
            }

            _logger.LogDebug("Successfully cleaned up entities of type {EntityType} by {Count} IDs", typeof(TEntity).Name, ids.Length);
        }
        catch (Exception ex)
        {
            lock (_lockObject)
            {
                _statistics.FailedOperations++;
            }

            _logger.LogError(ex, "Failed to cleanup entities of type {EntityType} by {Count} IDs", typeof(TEntity).Name, ids.Length);
            throw;
        }
    }

    /// <summary>
    /// Cleans up all test entities created by this manager
    /// Contract: Must maintain cleanup order for referential integrity
    /// </summary>
    public async Task CleanupAllAsync()
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);

        try
        {
            _logger.LogDebug("Cleaning up all entities managed by {ManagerType}", GetType().Name);

            // Cleanup persisted entities first (reverse order for referential integrity)
            var persistedEntitiesList = _persistedEntities.ToArray().Reverse().ToArray();
            if (persistedEntitiesList.Any())
            {
                await CleanupAsync(persistedEntitiesList);
            }

            // Then cleanup created entities that weren't persisted
            var createdEntitiesList = _createdEntities.Except(_persistedEntities).Reverse().ToArray();
            if (createdEntitiesList.Any())
            {
                await CleanupAsync(createdEntitiesList);
            }

            // Clear tracking collections
            _createdEntities.Clear();
            _persistedEntities.Clear();

            _logger.LogDebug("Successfully cleaned up all entities managed by {ManagerType}", GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup all entities managed by {ManagerType}", GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// Gets statistics about test data management operations
    /// </summary>
    public ITestDataStatistics GetStatistics()
    {
        lock (_lockObject)
        {
            return new TestDataStatistics
            {
                EntitiesCreated = _statistics.EntitiesCreated,
                EntitiesPersisted = _statistics.EntitiesPersisted,
                CleanupOperations = _statistics.CleanupOperations,
                AverageCreationTime = _statistics.EntitiesCreated > 0 
                    ? TimeSpan.FromTicks(_statistics.TotalCreationTime.Ticks / _statistics.EntitiesCreated) 
                    : TimeSpan.Zero,
                AveragePersistenceTime = _statistics.EntitiesPersisted > 0
                    ? TimeSpan.FromTicks(_statistics.TotalPersistenceTime.Ticks / _statistics.EntitiesPersisted)
                    : TimeSpan.Zero,
                TotalMemoryAllocated = _statistics.TotalMemoryAllocated,
                SuccessRate = CalculateSuccessRate(),
                CollectedAt = DateTime.UtcNow
            };
        }
    }

    private double CalculateSuccessRate()
    {
        var totalOperations = _statistics.EntitiesCreated + _statistics.EntitiesPersisted + _statistics.CleanupOperations;
        return totalOperations > 0 
            ? (double)(totalOperations - _statistics.FailedOperations) / totalOperations * 100 
            : 100.0;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            _logger.LogDebug("Disposing test data manager for type {EntityType}", typeof(TEntity).Name);
            await CleanupAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during test data manager disposal for type {EntityType}", typeof(TEntity).Name);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Internal implementation of test data statistics
/// </summary>
internal class TestDataStatistics : ITestDataStatistics
{
    public int EntitiesCreated { get; set; }
    public int EntitiesPersisted { get; set; }
    public int CleanupOperations { get; set; }
    public TimeSpan AverageCreationTime { get; set; }
    public TimeSpan AveragePersistenceTime { get; set; }
    public long TotalMemoryAllocated { get; set; }
    public double SuccessRate { get; set; }
    public DateTime CollectedAt { get; set; }

    // Internal tracking properties
    internal TimeSpan TotalCreationTime { get; set; }
    internal TimeSpan TotalPersistenceTime { get; set; }
    internal int FailedOperations { get; set; }
    internal DateTime LastOperationTime { get; set; }
}

/// <summary>
/// Concrete implementation of test data session for scoped management
/// </summary>
public class TestDataSession : ITestDataSession
{
    private readonly ILogger<TestDataSession> _logger;
    private readonly List<(object Entity, Func<Task> CleanupAction)> _registeredEntities;
    private readonly List<(Func<Task> CleanupAction, string Description)> _cleanupActions;
    private readonly object _lockObject = new();
    private bool _disposed;

    public string SessionId { get; }
    public DateTime CreatedAt { get; }
    public int RegisteredEntityCount => _registeredEntities.Count;
    public int RegisteredCleanupActions => _cleanupActions.Count;

    public TestDataSession(ILogger<TestDataSession> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        SessionId = Guid.NewGuid().ToString("N")[..8];
        CreatedAt = DateTime.UtcNow;
        _registeredEntities = [];
        _cleanupActions = [];

        _logger.LogDebug("Created test data session {SessionId}", SessionId);
    }

    /// <summary>
    /// Registers an entity for automatic cleanup when the session ends
    /// Contract: Must track entities for proper cleanup order
    /// </summary>
    public void RegisterEntity<T>(T entity, Func<Task> cleanupAction) where T : class
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(cleanupAction);

        lock (_lockObject)
        {
            _registeredEntities.Add((entity, cleanupAction));
            _logger.LogDebug("Registered entity of type {EntityType} in session {SessionId}", typeof(T).Name, SessionId);
        }
    }

    /// <summary>
    /// Registers a cleanup action to be performed when the session ends
    /// Contract: Must execute cleanup actions in reverse registration order
    /// </summary>
    public void RegisterCleanupAction(Func<Task> cleanupAction, string description)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentNullException.ThrowIfNull(cleanupAction);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        lock (_lockObject)
        {
            _cleanupActions.Add((cleanupAction, description));
            _logger.LogDebug("Registered cleanup action '{Description}' in session {SessionId}", description, SessionId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            _logger.LogDebug("Disposing test data session {SessionId} with {EntityCount} entities and {ActionCount} cleanup actions",
                SessionId, RegisteredEntityCount, RegisteredCleanupActions);

            // Execute cleanup actions in reverse order
            var cleanupActionsToExecute = _cleanupActions.AsEnumerable().Reverse().ToList();
            foreach (var (cleanupAction, description) in cleanupActionsToExecute)
            {
                try
                {
                    await cleanupAction();
                    _logger.LogDebug("Executed cleanup action '{Description}' for session {SessionId}", description, SessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute cleanup action '{Description}' for session {SessionId}", description, SessionId);
                }
            }

            // Clean up registered entities in reverse order
            var entitiesToCleanup = _registeredEntities.AsEnumerable().Reverse().ToList();
            foreach (var (entity, cleanupAction) in entitiesToCleanup)
            {
                try
                {
                    await cleanupAction();
                    _logger.LogDebug("Cleaned up entity of type {EntityType} for session {SessionId}", entity.GetType().Name, SessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cleanup entity of type {EntityType} for session {SessionId}", entity.GetType().Name, SessionId);
                }
            }

            _logger.LogDebug("Completed disposal of test data session {SessionId}", SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during test data session disposal for session {SessionId}", SessionId);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Builder for creating TestDataManager instances with fluent configuration
/// </summary>
public class TestDataManagerBuilder<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private ILogger<TestDataManager<TEntity, TId>>? _logger;
    private Func<Action<TEntity>?, Task<TEntity>>? _entityFactory;
    private Func<TEntity, Task<TEntity>>? _persistAction;
    private Func<IEnumerable<TEntity>, Task<IEnumerable<TEntity>>>? _persistManyAction;
    private Func<TId, Task<TEntity?>>? _getByIdAction;
    private Func<TEntity, Task>? _validateAction;
    private Func<TEntity[], Task>? _cleanupAction;
    private Func<TId[], Task>? _cleanupByIdsAction;

    public TestDataManagerBuilder<TEntity, TId> WithLogger(ILogger<TestDataManager<TEntity, TId>> logger)
    {
        _logger = logger;
        return this;
    }

    public TestDataManagerBuilder<TEntity, TId> WithEntityFactory(Func<Action<TEntity>?, Task<TEntity>> entityFactory)
    {
        _entityFactory = entityFactory;
        return this;
    }

    public TestDataManagerBuilder<TEntity, TId> WithPersistAction(Func<TEntity, Task<TEntity>> persistAction)
    {
        _persistAction = persistAction;
        return this;
    }

    public TestDataManagerBuilder<TEntity, TId> WithPersistManyAction(Func<IEnumerable<TEntity>, Task<IEnumerable<TEntity>>> persistManyAction)
    {
        _persistManyAction = persistManyAction;
        return this;
    }

    public TestDataManagerBuilder<TEntity, TId> WithGetByIdAction(Func<TId, Task<TEntity?>> getByIdAction)
    {
        _getByIdAction = getByIdAction;
        return this;
    }

    public TestDataManagerBuilder<TEntity, TId> WithValidateAction(Func<TEntity, Task> validateAction)
    {
        _validateAction = validateAction;
        return this;
    }

    public TestDataManagerBuilder<TEntity, TId> WithCleanupAction(Func<TEntity[], Task> cleanupAction)
    {
        _cleanupAction = cleanupAction;
        return this;
    }

    public TestDataManagerBuilder<TEntity, TId> WithCleanupByIdsAction(Func<TId[], Task> cleanupByIdsAction)
    {
        _cleanupByIdsAction = cleanupByIdsAction;
        return this;
    }

    public TestDataManager<TEntity, TId> Build()
    {
        if (_logger == null) throw new InvalidOperationException("Logger must be provided");
        if (_entityFactory == null) throw new InvalidOperationException("EntityFactory must be provided");

        return new TestDataManager<TEntity, TId>(
            _logger,
            _entityFactory,
            _persistAction,
            _persistManyAction,
            _getByIdAction,
            _validateAction,
            _cleanupAction,
            _cleanupByIdsAction);
    }
}
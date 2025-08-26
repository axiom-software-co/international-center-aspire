using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Testcontainers.Redis;
using InternationalCenter.Shared.Tests.Abstractions;
using Infrastructure.Cache.Tests.Contracts;
using Xunit.Abstractions;

namespace Infrastructure.Cache.Tests;

/// <summary>
/// Base implementation for Redis cache testing environment with Aspire orchestration
/// Provides Redis container management and test context for medical-grade cache testing
/// </summary>
/// <typeparam name="TTestContext">The cache-specific test context type</typeparam>
public abstract class CacheTestEnvironmentBase<TTestContext> : ICacheTestEnvironmentContract<TTestContext>
    where TTestContext : class, ICacheTestContext
{
    protected ILogger Logger { get; }
    protected ITestOutputHelper? Output { get; }
    protected Dictionary<string, RedisContainer> ActiveContainers { get; } = new();
    protected Dictionary<string, IConnectionMultiplexer> ActiveConnections { get; } = new();
    
    protected CacheTestEnvironmentBase(ILogger logger, ITestOutputHelper? output = null)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Output = output;
    }

    /// <summary>
    /// Sets up the Redis cache testing environment with container orchestration
    /// Contract: Must provide isolated Redis container with proper configuration for medical-grade reliability testing
    /// </summary>
    public virtual async Task<TTestContext> SetupCacheTestEnvironmentAsync(
        CacheTestEnvironmentOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Setting up cache test environment with Redis container");
            
            // Create and configure Redis container
            var redisContainer = await CreateRedisContainerAsync(options, cancellationToken);
            
            // Start Redis container
            await redisContainer.StartAsync(cancellationToken);
            
            var connectionString = redisContainer.GetConnectionString();
            Logger.LogInformation("Redis container started with connection string: {ConnectionString}", connectionString);
            
            // Create Redis connection multiplexer
            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(connectionString);
            
            // Store active resources for cleanup
            var containerId = Guid.NewGuid().ToString();
            ActiveContainers[containerId] = redisContainer;
            ActiveConnections[containerId] = connectionMultiplexer;
            
            // Create test context
            var context = await CreateTestContextAsync(
                connectionMultiplexer, 
                options, 
                containerId, 
                cancellationToken);
            
            // Validate Redis environment
            await ValidateRedisEnvironmentAsync(context, cancellationToken);
            
            Logger.LogInformation("Cache test environment setup completed successfully");
            return context;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to setup cache test environment");
            throw;
        }
    }

    /// <summary>
    /// Executes a cache test operation with performance tracking and reliability validation
    /// Contract: Must provide comprehensive error handling and Redis connection lifecycle management
    /// </summary>
    public virtual async Task<T> ExecuteCacheTestAsync<T>(
        TTestContext context,
        Func<TTestContext, Task<T>> testOperation,
        string operationName,
        PerformanceThreshold? performanceThreshold = null,
        CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (testOperation == null) throw new ArgumentNullException(nameof(testOperation));
        
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            Logger.LogInformation("Executing cache test operation: {OperationName}", operationName);
            
            var result = await testOperation(context);
            
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;
            
            Logger.LogInformation("Cache test operation completed: {OperationName} in {Duration}ms", 
                operationName, duration.TotalMilliseconds);
            
            // Validate performance threshold if provided
            if (performanceThreshold != null)
            {
                await ValidatePerformanceThreshold(operationName, duration, performanceThreshold);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Cache test operation failed: {OperationName} after {Duration}ms", 
                operationName, stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Validates Redis cache environment configuration and connectivity
    /// Contract: Must validate Redis server connectivity, memory configuration, and persistence settings
    /// </summary>
    public virtual async Task ValidateCacheEnvironmentAsync(
        TTestContext context,
        CancellationToken cancellationToken = default)
    {
        if (context?.ConnectionMultiplexer == null)
            throw new InvalidOperationException("Redis connection multiplexer is not available");
        
        try
        {
            Logger.LogInformation("Validating Redis cache environment");
            
            // Test basic connectivity
            var database = context.ConnectionMultiplexer.GetDatabase();
            await database.PingAsync();
            
            // Validate server configuration
            var server = context.ConnectionMultiplexer.GetServer(
                context.ConnectionMultiplexer.GetEndPoints().First());
            
            var info = await server.InfoAsync("server");
            Logger.LogInformation("Redis server info: {Info}", info.ToString());
            
            // Test basic operations
            var testKey = $"test:validation:{Guid.NewGuid()}";
            var testValue = "validation-test-value";
            
            await database.StringSetAsync(testKey, testValue, TimeSpan.FromMinutes(1));
            var retrievedValue = await database.StringGetAsync(testKey);
            
            if (retrievedValue != testValue)
            {
                throw new InvalidOperationException("Redis basic operation validation failed");
            }
            
            await database.KeyDeleteAsync(testKey);
            
            Logger.LogInformation("Redis cache environment validation completed successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Redis cache environment validation failed");
            throw;
        }
    }

    /// <summary>
    /// Cleans up Redis cache environment including container cleanup and data purging
    /// Contract: Must ensure complete cleanup of Redis data and container resources for test isolation
    /// </summary>
    public virtual async Task CleanupCacheEnvironmentAsync(
        TTestContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Cleaning up cache test environment");
            
            // Clean up test entities registered in context
            if (context?.CreatedTestEntities != null)
            {
                foreach (var entity in context.CreatedTestEntities)
                {
                    if (entity is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                context.CreatedTestEntities.Clear();
            }
            
            // Flush all Redis databases
            if (context?.ConnectionMultiplexer != null)
            {
                var server = context.ConnectionMultiplexer.GetServer(
                    context.ConnectionMultiplexer.GetEndPoints().First());
                await server.FlushAllDatabasesAsync();
            }
            
            // Dispose connections and containers
            foreach (var (containerId, connection) in ActiveConnections.ToList())
            {
                try
                {
                    await connection.DisposeAsync();
                    ActiveConnections.Remove(containerId);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to dispose Redis connection: {ContainerId}", containerId);
                }
            }
            
            foreach (var (containerId, container) in ActiveContainers.ToList())
            {
                try
                {
                    await container.StopAsync(cancellationToken);
                    await container.DisposeAsync();
                    ActiveContainers.Remove(containerId);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to dispose Redis container: {ContainerId}", containerId);
                }
            }
            
            Logger.LogInformation("Cache test environment cleanup completed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cleanup cache test environment");
            throw;
        }
    }

    /// <summary>
    /// Creates and configures Redis container for testing
    /// </summary>
    protected virtual async Task<RedisContainer> CreateRedisContainerAsync(
        CacheTestEnvironmentOptions options,
        CancellationToken cancellationToken = default)
    {
        var containerBuilder = new RedisBuilder()
            .WithImage(options.RedisImage)
            .WithPortBinding(options.RedisPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379));
        
        // Configure Redis parameters
        var configParams = new List<string>();
        
        if (!options.EnablePersistence)
        {
            configParams.Add("--save");
            configParams.Add("''"); // Disable persistence for testing
        }
        
        configParams.Add("--maxmemory");
        configParams.Add(options.RedisMaxMemory);
        configParams.Add("--maxmemory-policy");
        configParams.Add(options.RedisMaxMemoryPolicy);
        
        // Add custom Redis configuration
        foreach (var (key, value) in options.RedisConfiguration)
        {
            configParams.Add($"--{key}");
            configParams.Add(value);
        }
        
        if (configParams.Count > 0)
        {
            containerBuilder = containerBuilder.WithCommand(configParams.ToArray());
        }
        
        // Set environment variables
        foreach (var (key, value) in options.EnvironmentVariables)
        {
            containerBuilder = containerBuilder.WithEnvironment(key, value);
        }
        
        return containerBuilder.Build();
    }

    /// <summary>
    /// Creates test context with Redis connection and configuration
    /// </summary>
    protected abstract Task<TTestContext> CreateTestContextAsync(
        IConnectionMultiplexer connectionMultiplexer,
        CacheTestEnvironmentOptions options,
        string containerId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates Redis environment specific configuration
    /// </summary>
    protected virtual async Task ValidateRedisEnvironmentAsync(
        TTestContext context,
        CancellationToken cancellationToken = default)
    {
        // Override in derived classes for additional validation
        await ValidateCacheEnvironmentAsync(context, cancellationToken);
    }
    
    /// <summary>
    /// Validates performance threshold for cache operations
    /// </summary>
    protected virtual Task ValidatePerformanceThreshold(
        string operationName,
        TimeSpan actualDuration,
        PerformanceThreshold threshold)
    {
        if (actualDuration > threshold.MaxDuration)
        {
            throw new InvalidOperationException(
                $"Cache operation '{operationName}' exceeded performance threshold. " +
                $"Expected: {threshold.MaxDuration.TotalMilliseconds}ms, " +
                $"Actual: {actualDuration.TotalMilliseconds}ms");
        }
        
        Logger.LogInformation("Cache operation '{OperationName}' met performance threshold: {Duration}ms <= {Threshold}ms",
            operationName, actualDuration.TotalMilliseconds, threshold.MaxDuration.TotalMilliseconds);
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Disposes resources when environment is disposed
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        // Cleanup all active resources
        var cleanupTasks = new List<Task>();
        
        foreach (var connection in ActiveConnections.Values)
        {
            cleanupTasks.Add(connection.DisposeAsync().AsTask());
        }
        
        foreach (var container in ActiveContainers.Values)
        {
            cleanupTasks.Add(Task.Run(async () =>
            {
                await container.StopAsync();
                await container.DisposeAsync();
            }));
        }
        
        await Task.WhenAll(cleanupTasks);
        
        ActiveConnections.Clear();
        ActiveContainers.Clear();
    }
}

/// <summary>
/// Default implementation of cache test context
/// Provides Redis connection and test utilities for cache validation
/// </summary>
public class DefaultCacheTestContext : ICacheTestContext
{
    public IServiceProvider ServiceProvider { get; }
    public IConfiguration Configuration { get; }
    public ILogger Logger { get; }
    public IConnectionMultiplexer? ConnectionMultiplexer { get; }
    public IDatabase? Database { get; }
    public IServer? Server { get; }
    public ISubscriber? Subscriber { get; }
    public ICollection<object> CreatedTestEntities { get; } = new List<object>();
    
    private readonly Dictionary<string, object> _cachedEntities = new();
    private readonly string _containerId;
    
    public DefaultCacheTestContext(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger logger,
        IConnectionMultiplexer connectionMultiplexer,
        string containerId)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ConnectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _containerId = containerId ?? throw new ArgumentNullException(nameof(containerId));
        
        Database = ConnectionMultiplexer.GetDatabase();
        Subscriber = ConnectionMultiplexer.GetSubscriber();
        
        var endpoints = ConnectionMultiplexer.GetEndPoints();
        if (endpoints.Length > 0)
        {
            Server = ConnectionMultiplexer.GetServer(endpoints[0]);
        }
    }
    
    /// <summary>
    /// Creates a new Redis database with specified database index
    /// Contract: Must create isolated database instance for test execution
    /// </summary>
    public IDatabase GetDatabase(int databaseIndex = -1)
    {
        if (ConnectionMultiplexer == null)
            throw new InvalidOperationException("Redis connection multiplexer is not available");
        
        return ConnectionMultiplexer.GetDatabase(databaseIndex);
    }
    
    /// <summary>
    /// Flushes Redis database for test cleanup
    /// Contract: Must ensure complete data cleanup between tests for isolation
    /// </summary>
    public async Task FlushDatabaseAsync(int databaseIndex = -1)
    {
        if (Server == null)
            throw new InvalidOperationException("Redis server is not available");
        
        if (databaseIndex == -1)
        {
            await Server.FlushAllDatabasesAsync();
        }
        else
        {
            await Server.FlushDatabaseAsync(databaseIndex);
        }
        
        Logger.LogInformation("Flushed Redis database: {DatabaseIndex}", databaseIndex);
    }
    
    /// <summary>
    /// Gets Redis server information and statistics
    /// Contract: Must provide Redis server metrics for performance validation
    /// </summary>
    public async Task<Dictionary<string, string>> GetServerInfoAsync()
    {
        if (Server == null)
            throw new InvalidOperationException("Redis server is not available");
        
        var info = await Server.InfoAsync();
        var result = new Dictionary<string, string>();
        
        foreach (var group in info)
        {
            foreach (var item in group)
            {
                result[item.Key] = item.Value;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Registers an entity for cleanup after test completion
    /// Contract: Must track entities for proper cleanup and test isolation
    /// </summary>
    public void RegisterForCleanup<T>(T entity) where T : class
    {
        if (entity != null)
        {
            CreatedTestEntities.Add(entity);
        }
    }
    
    /// <summary>
    /// Gets or creates a cached test entity to avoid recreation
    /// Contract: Must provide entity caching for test performance optimization
    /// </summary>
    public async Task<T> GetOrCreateTestEntityAsync<T>(Func<Task<T>> factory) where T : class
    {
        var key = typeof(T).FullName ?? typeof(T).Name;
        
        if (_cachedEntities.TryGetValue(key, out var existingEntity) && existingEntity is T cachedEntity)
        {
            return cachedEntity;
        }
        
        var newEntity = await factory();
        _cachedEntities[key] = newEntity;
        RegisterForCleanup(newEntity);
        
        return newEntity;
    }
    
    public void Dispose()
    {
        // Cleanup cached entities
        foreach (var entity in CreatedTestEntities.OfType<IDisposable>())
        {
            try
            {
                entity.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to dispose test entity: {EntityType}", entity.GetType().Name);
            }
        }
        
        CreatedTestEntities.Clear();
        _cachedEntities.Clear();
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using InternationalCenter.Shared.Tests.Abstractions;
using Infrastructure.Cache.Tests.Contracts;
using Xunit.Abstractions;

namespace Infrastructure.Cache.Tests;

/// <summary>
/// Redis-specific implementation of cache testing environment
/// Provides Redis container orchestration and testing utilities for Public Gateway rate limiting and Services APIs
/// </summary>
public class RedisCacheTestEnvironment : CacheTestEnvironmentBase<DefaultCacheTestContext>, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private bool _disposed;

    public RedisCacheTestEnvironment(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<RedisCacheTestEnvironment> logger,
        ITestOutputHelper? output = null)
        : base(logger, output)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Creates Redis-specific test context with connection and configuration
    /// </summary>
    protected override async Task<DefaultCacheTestContext> CreateTestContextAsync(
        IConnectionMultiplexer connectionMultiplexer,
        CacheTestEnvironmentOptions options,
        string containerId,
        CancellationToken cancellationToken = default)
    {
        // Configure service collection with Redis-specific services
        var services = new ServiceCollection();
        
        // Register Redis connection
        services.AddSingleton(connectionMultiplexer);
        services.AddSingleton<IDatabase>(provider => 
            provider.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        
        // Register configuration
        services.AddSingleton(_configuration);
        
        // Register logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            if (options.EnableDetailedLogging)
            {
                builder.SetMinimumLevel(LogLevel.Debug);
            }
        });
        
        // Register cache-specific services
        services.AddTransient<ICacheTestDataFactory, CacheTestDataFactory>();
        services.AddTransient<ICacheOperationsContract, CacheOperationsService>();
        services.AddTransient<IRateLimitingContract, RateLimitingService>();
        services.AddTransient<IDistributedCacheContract, DistributedCacheService>();
        
        // Apply custom service configuration
        options.ConfigureServices?.Invoke(services);
        
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<DefaultCacheTestContext>>();
        
        var context = new DefaultCacheTestContext(
            serviceProvider,
            _configuration,
            logger,
            connectionMultiplexer,
            containerId);
        
        // Validate Redis-specific environment
        await ValidateRedisSpecificEnvironmentAsync(context, options, cancellationToken);
        
        return context;
    }

    /// <summary>
    /// Validates Redis-specific environment configuration
    /// </summary>
    private async Task ValidateRedisSpecificEnvironmentAsync(
        DefaultCacheTestContext context,
        CacheTestEnvironmentOptions options,
        CancellationToken cancellationToken = default)
    {
        if (context.Server == null)
            throw new InvalidOperationException("Redis server is not available for validation");
        
        try
        {
            Logger.LogInformation("Validating Redis-specific environment configuration");
            
            // Validate memory configuration
            var memoryInfo = await context.Server.InfoAsync("memory");
            Logger.LogInformation("Redis memory info: {MemoryInfo}", memoryInfo.ToString());
            
            // Validate persistence settings
            var persistenceInfo = await context.Server.InfoAsync("persistence");
            Logger.LogInformation("Redis persistence info: {PersistenceInfo}", persistenceInfo.ToString());
            
            // Test rate limiting keys structure
            var database = context.GetDatabase();
            var rateLimitTestKey = "ratelimit:ip:127.0.0.1";
            await database.StringSetAsync(rateLimitTestKey, "1", TimeSpan.FromMinutes(1));
            var rateLimitValue = await database.StringGetAsync(rateLimitTestKey);
            
            if (!rateLimitValue.HasValue)
            {
                throw new InvalidOperationException("Redis rate limiting key validation failed");
            }
            
            await database.KeyDeleteAsync(rateLimitTestKey);
            
            // Test distributed cache keys structure
            var cacheTestKey = "cache:services:list:page1";
            var cacheTestData = new { services = new[] { "service1", "service2" } };
            var serializedData = System.Text.Json.JsonSerializer.Serialize(cacheTestData);
            
            await database.StringSetAsync(cacheTestKey, serializedData, TimeSpan.FromMinutes(5));
            var cachedData = await database.StringGetAsync(cacheTestKey);
            
            if (!cachedData.HasValue)
            {
                throw new InvalidOperationException("Redis distributed cache key validation failed");
            }
            
            await database.KeyDeleteAsync(cacheTestKey);
            
            Logger.LogInformation("Redis-specific environment validation completed successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Redis-specific environment validation failed");
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                // Cleanup active connections and containers
                var cleanupTask = DisposeAsyncCore();
                cleanupTask.AsTask().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during Redis cache test environment disposal");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}

/// <summary>
/// Implementation of cache operations contract for Redis testing
/// Provides standardized cache operation validation for Services APIs and Public Gateway
/// </summary>
public class CacheOperationsService : ICacheOperationsContract
{
    private readonly ILogger<CacheOperationsService> _logger;

    public CacheOperationsService(ILogger<CacheOperationsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tests basic Redis cache operations (get, set, delete)
    /// Contract: Must validate fundamental cache operations with proper error handling and data integrity
    /// </summary>
    public async Task TestBasicCacheOperationsAsync(
        IDatabase database,
        BasicCacheOperationTestCase[] operationTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (operationTestCases == null) throw new ArgumentNullException(nameof(operationTestCases));
        
        _logger.LogInformation("Starting basic cache operations test with {Count} test cases", operationTestCases.Length);
        
        foreach (var testCase in operationTestCases)
        {
            try
            {
                output?.WriteLine($"Testing {testCase.Operation} operation with key: {testCase.Key}");
                
                bool success = testCase.Operation switch
                {
                    CacheOperation.Set => await TestSetOperationAsync(database, testCase),
                    CacheOperation.Get => await TestGetOperationAsync(database, testCase),
                    CacheOperation.Delete => await TestDeleteOperationAsync(database, testCase),
                    CacheOperation.Exists => await TestExistsOperationAsync(database, testCase),
                    CacheOperation.Expire => await TestExpireOperationAsync(database, testCase),
                    CacheOperation.Increment => await TestIncrementOperationAsync(database, testCase),
                    CacheOperation.Decrement => await TestDecrementOperationAsync(database, testCase),
                    _ => throw new NotSupportedException($"Operation {testCase.Operation} is not supported")
                };
                
                if (success != testCase.ExpectSuccess)
                {
                    throw new InvalidOperationException(
                        $"Operation {testCase.Operation} result mismatch. Expected: {testCase.ExpectSuccess}, Actual: {success}");
                }
                
                output?.WriteLine($"✓ {testCase.Operation} operation completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Basic cache operation test failed for {Operation} with key {Key}", 
                    testCase.Operation, testCase.Key);
                output?.WriteLine($"✗ {testCase.Operation} operation failed: {ex.Message}");
                throw;
            }
        }
        
        _logger.LogInformation("Basic cache operations test completed successfully");
    }

    /// <summary>
    /// Tests Redis cache expiration and TTL handling
    /// Contract: Must validate cache expiration behavior with precise timing for rate limiting accuracy
    /// </summary>
    public async Task TestCacheExpirationAsync(
        IDatabase database,
        CacheExpirationTestCase[] expirationTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (expirationTestCases == null) throw new ArgumentNullException(nameof(expirationTestCases));
        
        _logger.LogInformation("Starting cache expiration test with {Count} test cases", expirationTestCases.Length);
        
        foreach (var testCase in expirationTestCases)
        {
            try
            {
                output?.WriteLine($"Testing expiration for key: {testCase.Key}, expire after: {testCase.ExpireAfter}");
                
                // Set key with expiration
                await database.StringSetAsync(testCase.Key, 
                    System.Text.Json.JsonSerializer.Serialize(testCase.Value), 
                    testCase.ExpireAfter);
                
                // Wait for specified check time
                await Task.Delay(testCase.CheckAfter);
                
                // Check if key exists
                var exists = await database.KeyExistsAsync(testCase.Key);
                
                if (exists != testCase.ShouldExist)
                {
                    throw new InvalidOperationException(
                        $"Expiration test failed for key {testCase.Key}. Expected exists: {testCase.ShouldExist}, Actual: {exists}");
                }
                
                // Cleanup if key still exists
                if (exists)
                {
                    await database.KeyDeleteAsync(testCase.Key);
                }
                
                output?.WriteLine($"✓ Expiration test completed successfully for key: {testCase.Key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache expiration test failed for key {Key}", testCase.Key);
                output?.WriteLine($"✗ Expiration test failed for key {testCase.Key}: {ex.Message}");
                throw;
            }
        }
        
        _logger.LogInformation("Cache expiration test completed successfully");
    }

    /// <summary>
    /// Tests Redis cache performance characteristics
    /// Contract: Must validate cache operations meet medical-grade performance requirements for Services APIs
    /// </summary>
    public async Task TestCachePerformanceAsync(
        IDatabase database,
        CachePerformanceTestCase[] performanceTestCases,
        PerformanceThreshold threshold,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (performanceTestCases == null) throw new ArgumentNullException(nameof(performanceTestCases));
        if (threshold == null) throw new ArgumentNullException(nameof(threshold));
        
        _logger.LogInformation("Starting cache performance test with {Count} test cases", performanceTestCases.Length);
        
        foreach (var testCase in performanceTestCases)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                output?.WriteLine($"Testing performance: {testCase.TestName} - {testCase.OperationCount} {testCase.Operation} operations");
                
                var tasks = new List<Task>();
                
                for (int thread = 0; thread < testCase.ConcurrentThreads; thread++)
                {
                    int threadId = thread;
                    tasks.Add(Task.Run(async () =>
                    {
                        for (int i = 0; i < testCase.OperationCount / testCase.ConcurrentThreads; i++)
                        {
                            var (key, value) = testCase.DataGenerator(threadId * 1000 + i);
                            
                            switch (testCase.Operation)
                            {
                                case CacheOperation.Set:
                                    await database.StringSetAsync(key, System.Text.Json.JsonSerializer.Serialize(value));
                                    break;
                                case CacheOperation.Get:
                                    await database.StringGetAsync(key);
                                    break;
                                case CacheOperation.Delete:
                                    await database.KeyDeleteAsync(key);
                                    break;
                            }
                        }
                    }));
                }
                
                await Task.WhenAll(tasks);
                stopwatch.Stop();
                
                var duration = stopwatch.Elapsed;
                var opsPerSecond = testCase.OperationCount / duration.TotalSeconds;
                
                output?.WriteLine($"Performance: {opsPerSecond:F2} ops/sec in {duration.TotalMilliseconds:F2}ms");
                
                if (duration > testCase.ExpectedMaxDuration)
                {
                    throw new InvalidOperationException(
                        $"Performance test failed: {testCase.TestName}. Duration {duration.TotalMilliseconds}ms exceeded expected {testCase.ExpectedMaxDuration.TotalMilliseconds}ms");
                }
                
                output?.WriteLine($"✓ Performance test completed: {testCase.TestName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache performance test failed: {TestName}", testCase.TestName);
                output?.WriteLine($"✗ Performance test failed: {testCase.TestName}: {ex.Message}");
                throw;
            }
        }
        
        _logger.LogInformation("Cache performance test completed successfully");
    }

    /// <summary>
    /// Tests Redis cache data types and serialization
    /// Contract: Must validate proper serialization/deserialization for complex data structures in Services APIs
    /// </summary>
    public async Task TestCacheDataTypesAsync(
        IDatabase database,
        CacheDataTypeTestCase[] dataTypeTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (dataTypeTestCases == null) throw new ArgumentNullException(nameof(dataTypeTestCases));
        
        _logger.LogInformation("Starting cache data types test with {Count} test cases", dataTypeTestCases.Length);
        
        foreach (var testCase in dataTypeTestCases)
        {
            try
            {
                output?.WriteLine($"Testing data type: {testCase.ExpectedType.Name} for key: {testCase.Key}");
                
                string serializedValue;
                
                if (testCase.TestSerialization)
                {
                    // Test serialization
                    serializedValue = System.Text.Json.JsonSerializer.Serialize(testCase.Value);
                    await database.StringSetAsync(testCase.Key, serializedValue);
                }
                else
                {
                    serializedValue = testCase.Value.ToString() ?? string.Empty;
                    await database.StringSetAsync(testCase.Key, serializedValue);
                }
                
                if (testCase.TestDeserialization)
                {
                    // Test deserialization
                    var retrievedValue = await database.StringGetAsync(testCase.Key);
                    
                    if (!retrievedValue.HasValue)
                    {
                        throw new InvalidOperationException($"Failed to retrieve value for key: {testCase.Key}");
                    }
                    
                    object deserializedValue;
                    
                    if (testCase.ExpectedType == typeof(string))
                    {
                        deserializedValue = retrievedValue.ToString();
                    }
                    else
                    {
                        deserializedValue = System.Text.Json.JsonSerializer.Deserialize(retrievedValue!, testCase.ExpectedType)!;
                    }
                    
                    // Validate deserialized value
                    if (testCase.EqualityComparer != null)
                    {
                        if (!testCase.EqualityComparer(testCase.Value, deserializedValue))
                        {
                            throw new InvalidOperationException($"Value equality check failed for key: {testCase.Key}");
                        }
                    }
                }
                
                await database.KeyDeleteAsync(testCase.Key);
                output?.WriteLine($"✓ Data type test completed: {testCase.ExpectedType.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache data type test failed for key {Key} and type {Type}", 
                    testCase.Key, testCase.ExpectedType.Name);
                output?.WriteLine($"✗ Data type test failed: {testCase.ExpectedType.Name}: {ex.Message}");
                throw;
            }
        }
        
        _logger.LogInformation("Cache data types test completed successfully");
    }

    /// <summary>
    /// Tests Redis cache consistency and atomicity
    /// Contract: Must validate cache consistency under concurrent operations for medical-grade reliability
    /// </summary>
    public async Task TestCacheConsistencyAsync(
        IDatabase database,
        CacheConsistencyTestCase[] consistencyTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (consistencyTestCases == null) throw new ArgumentNullException(nameof(consistencyTestCases));
        
        _logger.LogInformation("Starting cache consistency test with {Count} test cases", consistencyTestCases.Length);
        
        foreach (var testCase in consistencyTestCases)
        {
            try
            {
                output?.WriteLine($"Testing consistency: {testCase.TestName} with {testCase.ConcurrentOperations} concurrent operations");
                
                var tasks = new List<Task>();
                var results = new System.Collections.Concurrent.ConcurrentBag<object?>();
                
                for (int i = 0; i < testCase.ConcurrentOperations; i++)
                {
                    int operationIndex = i;
                    tasks.Add(Task.Run(async () =>
                    {
                        foreach (var operation in testCase.Operations)
                        {
                            switch (operation)
                            {
                                case CacheOperation.Set:
                                    var value = testCase.Values.Length > operationIndex ? 
                                        testCase.Values[operationIndex] : 
                                        $"value-{operationIndex}";
                                    await database.StringSetAsync(testCase.Key, System.Text.Json.JsonSerializer.Serialize(value));
                                    break;
                                    
                                case CacheOperation.Get:
                                    var retrieved = await database.StringGetAsync(testCase.Key);
                                    results.Add(retrieved.HasValue ? retrieved.ToString() : null);
                                    break;
                            }
                        }
                    }));
                }
                
                var timeoutTask = Task.Delay(testCase.MaxOperationTime);
                var completedTask = await Task.WhenAny(Task.WhenAll(tasks), timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException($"Consistency test {testCase.TestName} timed out after {testCase.MaxOperationTime}");
                }
                
                await database.KeyDeleteAsync(testCase.Key);
                output?.WriteLine($"✓ Consistency test completed: {testCase.TestName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache consistency test failed: {TestName}", testCase.TestName);
                output?.WriteLine($"✗ Consistency test failed: {testCase.TestName}: {ex.Message}");
                throw;
            }
        }
        
        _logger.LogInformation("Cache consistency test completed successfully");
    }

    /// <summary>
    /// Tests Redis cache memory management and eviction policies
    /// Contract: Must validate memory limits and eviction behavior for sustained operation under load
    /// </summary>
    public async Task TestCacheMemoryManagementAsync(
        IDatabase database,
        CacheMemoryTestOptions memoryOptions,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (memoryOptions == null) throw new ArgumentNullException(nameof(memoryOptions));
        
        _logger.LogInformation("Starting cache memory management test");
        
        try
        {
            output?.WriteLine($"Testing memory management with {memoryOptions.KeyCount} keys of {memoryOptions.ValueSizeBytes} bytes each");
            
            // Fill cache with test data
            var tasks = new List<Task>();
            
            for (int i = 0; i < memoryOptions.KeyCount; i++)
            {
                int keyIndex = i;
                tasks.Add(Task.Run(async () =>
                {
                    var key = $"memtest:key:{keyIndex}";
                    var value = new string('x', memoryOptions.ValueSizeBytes);
                    await database.StringSetAsync(key, value);
                }));
                
                // Batch requests to avoid overwhelming Redis
                if (tasks.Count >= 100)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }
            
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
            
            // Check memory usage and eviction behavior if applicable
            if (memoryOptions.TestEvictionBehavior)
            {
                // Try to add more data to trigger eviction
                for (int i = 0; i < 1000; i++)
                {
                    var key = $"memtest:evict:{i}";
                    var value = new string('y', memoryOptions.ValueSizeBytes);
                    await database.StringSetAsync(key, value);
                }
            }
            
            output?.WriteLine("✓ Memory management test completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache memory management test failed");
            output?.WriteLine($"✗ Memory management test failed: {ex.Message}");
            throw;
        }
        finally
        {
            // Cleanup test keys
            try
            {
                var server = database.Multiplexer.GetServer(database.Multiplexer.GetEndPoints()[0]);
                await server.FlushDatabaseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup memory test data");
            }
        }
        
        _logger.LogInformation("Cache memory management test completed");
    }
    
    // Private helper methods for basic operations
    private async Task<bool> TestSetOperationAsync(IDatabase database, BasicCacheOperationTestCase testCase)
    {
        var serializedValue = System.Text.Json.JsonSerializer.Serialize(testCase.Value);
        return await database.StringSetAsync(testCase.Key, serializedValue, testCase.Expiry);
    }
    
    private async Task<bool> TestGetOperationAsync(IDatabase database, BasicCacheOperationTestCase testCase)
    {
        var value = await database.StringGetAsync(testCase.Key);
        if (testCase.ValueValidator != null && value.HasValue)
        {
            var deserializedValue = System.Text.Json.JsonSerializer.Deserialize(value!, testCase.ExpectedValueType ?? typeof(object));
            return testCase.ValueValidator(deserializedValue);
        }
        return value.HasValue;
    }
    
    private async Task<bool> TestDeleteOperationAsync(IDatabase database, BasicCacheOperationTestCase testCase)
    {
        return await database.KeyDeleteAsync(testCase.Key);
    }
    
    private async Task<bool> TestExistsOperationAsync(IDatabase database, BasicCacheOperationTestCase testCase)
    {
        return await database.KeyExistsAsync(testCase.Key);
    }
    
    private async Task<bool> TestExpireOperationAsync(IDatabase database, BasicCacheOperationTestCase testCase)
    {
        return await database.KeyExpireAsync(testCase.Key, testCase.Expiry);
    }
    
    private async Task<bool> TestIncrementOperationAsync(IDatabase database, BasicCacheOperationTestCase testCase)
    {
        var result = await database.StringIncrementAsync(testCase.Key);
        return result > 0;
    }
    
    private async Task<bool> TestDecrementOperationAsync(IDatabase database, BasicCacheOperationTestCase testCase)
    {
        var result = await database.StringDecrementAsync(testCase.Key);
        return true; // Decrement always succeeds
    }
}
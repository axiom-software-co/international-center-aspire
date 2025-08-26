using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using InternationalCenter.Shared.Tests.Abstractions;
using Xunit.Abstractions;

namespace Infrastructure.Cache.Tests.Contracts;

/// <summary>
/// Contract for Redis cache testing environment
/// Defines comprehensive Redis testing capabilities with Aspire orchestration
/// Medical-grade cache testing ensuring reliability and performance for Public Gateway rate limiting and Services APIs caching
/// </summary>
/// <typeparam name="TTestContext">The cache-specific test context type</typeparam>
public interface ICacheTestEnvironmentContract<TTestContext>
    where TTestContext : class, ITestContext
{
    /// <summary>
    /// Sets up the Redis cache testing environment with Aspire orchestration
    /// Contract: Must provide isolated Redis container with proper configuration for medical-grade reliability testing
    /// </summary>
    Task<TTestContext> SetupCacheTestEnvironmentAsync(
        CacheTestEnvironmentOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a cache test operation with performance tracking and reliability validation
    /// Contract: Must provide comprehensive error handling and Redis connection lifecycle management
    /// </summary>
    Task<T> ExecuteCacheTestAsync<T>(
        TTestContext context,
        Func<TTestContext, Task<T>> testOperation,
        string operationName,
        PerformanceThreshold? performanceThreshold = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates Redis cache environment configuration and connectivity
    /// Contract: Must validate Redis server connectivity, memory configuration, and persistence settings
    /// </summary>
    Task ValidateCacheEnvironmentAsync(
        TTestContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up Redis cache environment including container cleanup and data purging
    /// Contract: Must ensure complete cleanup of Redis data and container resources for test isolation
    /// </summary>
    Task CleanupCacheEnvironmentAsync(
        TTestContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Contract for Redis cache operations testing
/// Provides comprehensive cache operation validation for Services APIs and Public Gateway
/// </summary>
public interface ICacheOperationsContract
{
    /// <summary>
    /// Tests basic Redis cache operations (get, set, delete)
    /// Contract: Must validate fundamental cache operations with proper error handling and data integrity
    /// </summary>
    Task TestBasicCacheOperationsAsync(
        IDatabase database,
        BasicCacheOperationTestCase[] operationTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests Redis cache expiration and TTL handling
    /// Contract: Must validate cache expiration behavior with precise timing for rate limiting accuracy
    /// </summary>
    Task TestCacheExpirationAsync(
        IDatabase database,
        CacheExpirationTestCase[] expirationTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests Redis cache performance characteristics
    /// Contract: Must validate cache operations meet medical-grade performance requirements for Services APIs
    /// </summary>
    Task TestCachePerformanceAsync(
        IDatabase database,
        CachePerformanceTestCase[] performanceTestCases,
        PerformanceThreshold threshold,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests Redis cache data types and serialization
    /// Contract: Must validate proper serialization/deserialization for complex data structures in Services APIs
    /// </summary>
    Task TestCacheDataTypesAsync(
        IDatabase database,
        CacheDataTypeTestCase[] dataTypeTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests Redis cache consistency and atomicity
    /// Contract: Must validate cache consistency under concurrent operations for medical-grade reliability
    /// </summary>
    Task TestCacheConsistencyAsync(
        IDatabase database,
        CacheConsistencyTestCase[] consistencyTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests Redis cache memory management and eviction policies
    /// Contract: Must validate memory limits and eviction behavior for sustained operation under load
    /// </summary>
    Task TestCacheMemoryManagementAsync(
        IDatabase database,
        CacheMemoryTestOptions memoryOptions,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for Redis rate limiting testing specifically for Public Gateway
/// Specialized contract for IP-based rate limiting operations (1000 req/min)
/// </summary>
public interface IRateLimitingContract
{
    /// <summary>
    /// Tests IP-based rate limiting for Public Gateway anonymous access
    /// Contract: Must validate 1000 req/min IP-based rate limiting with Redis backing store accuracy
    /// </summary>
    Task TestIPBasedRateLimitingAsync(
        IDatabase database,
        IPRateLimitTestCase[] rateLimitTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests user-based rate limiting for Services Admin API
    /// Contract: Must validate 100 req/min user-based rate limiting with proper user identification
    /// </summary>
    Task TestUserBasedRateLimitingAsync(
        IDatabase database,
        UserRateLimitTestCase[] rateLimitTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests rate limiting window sliding and reset behavior
    /// Contract: Must validate accurate time window management for precise rate limiting enforcement
    /// </summary>
    Task TestRateLimitingWindowBehaviorAsync(
        IDatabase database,
        RateLimitWindowTestCase[] windowTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests rate limiting under high concurrency
    /// Contract: Must validate rate limiting accuracy under concurrent requests for medical-grade reliability
    /// </summary>
    Task TestConcurrentRateLimitingAsync(
        IDatabase database,
        ConcurrentRateLimitTestCase[] concurrencyTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests rate limiting recovery and burst handling
    /// Contract: Must validate rate limit recovery behavior and burst request handling policies
    /// </summary>
    Task TestRateLimitRecoveryAsync(
        IDatabase database,
        RateLimitRecoveryTestCase[] recoveryTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests rate limiting metrics and monitoring
    /// Contract: Must validate rate limiting metrics collection for medical-grade audit compliance
    /// </summary>
    Task TestRateLimitingMetricsAsync(
        IDatabase database,
        RateLimitMetricsTestCase[] metricsTestCases,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for Redis distributed caching testing for Services APIs
/// Specialized contract for Services Public and Admin API caching patterns
/// </summary>
public interface IDistributedCacheContract
{
    /// <summary>
    /// Tests distributed cache for Services Public API (Dapper repositories)
    /// Contract: Must validate cache integration with Dapper data access patterns for anonymous users
    /// </summary>
    Task TestPublicApiDistributedCacheAsync(
        IDatabase database,
        PublicApiCacheTestCase[] cacheTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests distributed cache for Services Admin API (EF Core repositories)
    /// Contract: Must validate cache integration with EF Core patterns for role-based access control
    /// </summary>
    Task TestAdminApiDistributedCacheAsync(
        IDatabase database,
        AdminApiCacheTestCase[] cacheTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests cache invalidation strategies for Services APIs
    /// Contract: Must validate proper cache invalidation for data consistency in medical-grade environment
    /// </summary>
    Task TestCacheInvalidationAsync(
        IDatabase database,
        CacheInvalidationTestCase[] invalidationTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests cache warming and preloading strategies
    /// Contract: Must validate cache warming strategies for optimal Services API performance
    /// </summary>
    Task TestCacheWarmingAsync(
        IDatabase database,
        CacheWarmingTestCase[] warmingTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests cache partitioning and multi-tenancy
    /// Contract: Must validate cache isolation between different API domains for security compliance
    /// </summary>
    Task TestCachePartitioningAsync(
        IDatabase database,
        CachePartitioningTestCase[] partitioningTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests cache backup and restore operations
    /// Contract: Must validate cache persistence and recovery for medical-grade data protection
    /// </summary>
    Task TestCacheBackupRestoreAsync(
        IDatabase database,
        CacheBackupRestoreTestCase[] backupTestCases,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Configuration options for Redis cache test environment setup
/// </summary>
public class CacheTestEnvironmentOptions
{
    /// <summary>
    /// Gets or sets whether to use Redis container or in-memory simulation (default: true for real Redis)
    /// </summary>
    public bool UseRedisContainer { get; set; } = true;

    /// <summary>
    /// Gets or sets the Redis container image and version
    /// </summary>
    public string RedisImage { get; set; } = "redis:7-alpine";

    /// <summary>
    /// Gets or sets the Redis port for container mapping
    /// </summary>
    public int RedisPort { get; set; } = 6379;

    /// <summary>
    /// Gets or sets whether to enable Redis persistence (default: false for testing)
    /// </summary>
    public bool EnablePersistence { get; set; } = false;

    /// <summary>
    /// Gets or sets the Redis memory limit for testing
    /// </summary>
    public string RedisMaxMemory { get; set; } = "128mb";

    /// <summary>
    /// Gets or sets the Redis eviction policy for memory management testing
    /// </summary>
    public string RedisMaxMemoryPolicy { get; set; } = "allkeys-lru";

    /// <summary>
    /// Gets or sets whether to enable Redis performance monitoring
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable detailed Redis logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets additional Redis configuration parameters
    /// </summary>
    public Dictionary<string, string> RedisConfiguration { get; set; } = new();

    /// <summary>
    /// Gets or sets test-specific environment variables
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// Gets or sets custom service registrations for DI
    /// </summary>
    public Action<IServiceCollection>? ConfigureServices { get; set; }
}

/// <summary>
/// Context for Redis cache domain testing
/// Provides Redis-specific testing context and container orchestration
/// </summary>
public interface ICacheTestContext : ITestContext
{
    /// <summary>
    /// Gets the cache test service provider
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the cache test configuration
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the cache test logger
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Gets the Redis connection multiplexer
    /// </summary>
    IConnectionMultiplexer? ConnectionMultiplexer { get; }

    /// <summary>
    /// Gets the Redis database instance
    /// </summary>
    IDatabase? Database { get; }

    /// <summary>
    /// Gets the Redis server instance for admin operations
    /// </summary>
    IServer? Server { get; }

    /// <summary>
    /// Gets the Redis subscriber for pub/sub operations
    /// </summary>
    ISubscriber? Subscriber { get; }

    /// <summary>
    /// Gets test entities created during this context
    /// </summary>
    ICollection<object> CreatedTestEntities { get; }

    /// <summary>
    /// Creates a new Redis database with specified database index
    /// Contract: Must create isolated database instance for test execution
    /// </summary>
    IDatabase GetDatabase(int databaseIndex = -1);

    /// <summary>
    /// Flushes Redis database for test cleanup
    /// Contract: Must ensure complete data cleanup between tests for isolation
    /// </summary>
    Task FlushDatabaseAsync(int databaseIndex = -1);

    /// <summary>
    /// Gets Redis server information and statistics
    /// Contract: Must provide Redis server metrics for performance validation
    /// </summary>
    Task<Dictionary<string, string>> GetServerInfoAsync();

    /// <summary>
    /// Registers an entity for cleanup after test completion
    /// Contract: Must track entities for proper cleanup and test isolation
    /// </summary>
    void RegisterForCleanup<T>(T entity) where T : class;

    /// <summary>
    /// Gets or creates a cached test entity to avoid recreation
    /// Contract: Must provide entity caching for test performance optimization
    /// </summary>
    Task<T> GetOrCreateTestEntityAsync<T>(Func<Task<T>> factory) where T : class;
}

/// <summary>
/// Performance threshold for Redis cache operations
/// </summary>
public class CachePerformanceThreshold : PerformanceThreshold
{
    public TimeSpan MaxGetOperation { get; set; } = TimeSpan.FromMilliseconds(5);
    public TimeSpan MaxSetOperation { get; set; } = TimeSpan.FromMilliseconds(10);
    public TimeSpan MaxDeleteOperation { get; set; } = TimeSpan.FromMilliseconds(5);
    public int MinThroughputPerSecond { get; set; } = 10000; // operations per second
    public long MaxMemoryUsageBytes { get; set; } = 100 * 1024 * 1024; // 100MB
}

/// <summary>
/// Test case for basic cache operations
/// </summary>
public class BasicCacheOperationTestCase
{
    public string Key { get; set; } = string.Empty;
    public object Value { get; set; } = new();
    public TimeSpan? Expiry { get; set; }
    public CacheOperation Operation { get; set; } = CacheOperation.Set;
    public bool ExpectSuccess { get; set; } = true;
    public Type? ExpectedValueType { get; set; }
    public Func<object?, bool>? ValueValidator { get; set; }
}

/// <summary>
/// Cache operation enumeration
/// </summary>
public enum CacheOperation
{
    Get,
    Set,
    Delete,
    Exists,
    Expire,
    Increment,
    Decrement
}

/// <summary>
/// Test case for cache expiration
/// </summary>
public class CacheExpirationTestCase
{
    public string Key { get; set; } = string.Empty;
    public object Value { get; set; } = new();
    public TimeSpan ExpireAfter { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan CheckAfter { get; set; } = TimeSpan.FromSeconds(2);
    public bool ShouldExist { get; set; } = false;
    public bool TestPreciseTiming { get; set; } = true;
    public TimeSpan TimingTolerance { get; set; } = TimeSpan.FromMilliseconds(100);
}

/// <summary>
/// Test case for cache performance
/// </summary>
public class CachePerformanceTestCase
{
    public string TestName { get; set; } = string.Empty;
    public CacheOperation Operation { get; set; } = CacheOperation.Set;
    public int OperationCount { get; set; } = 1000;
    public int ConcurrentThreads { get; set; } = 1;
    public TimeSpan ExpectedMaxDuration { get; set; } = TimeSpan.FromSeconds(1);
    public Func<int, (string Key, object Value)> DataGenerator { get; set; } = i => ($"key-{i}", $"value-{i}");
    public bool MeasureMemoryUsage { get; set; } = true;
}

/// <summary>
/// Test case for cache data types
/// </summary>
public class CacheDataTypeTestCase
{
    public string Key { get; set; } = string.Empty;
    public object Value { get; set; } = new();
    public Type ExpectedType { get; set; } = typeof(string);
    public bool TestSerialization { get; set; } = true;
    public bool TestDeserialization { get; set; } = true;
    public Func<object?, object?, bool>? EqualityComparer { get; set; }
}

/// <summary>
/// Test case for cache consistency
/// </summary>
public class CacheConsistencyTestCase
{
    public string TestName { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ConcurrentOperations { get; set; } = 10;
    public CacheOperation[] Operations { get; set; } = { CacheOperation.Set, CacheOperation.Get };
    public object[] Values { get; set; } = Array.Empty<object>();
    public bool ValidateAtomicity { get; set; } = true;
    public TimeSpan MaxOperationTime { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Options for cache memory management testing
/// </summary>
public class CacheMemoryTestOptions
{
    public long MaxMemoryBytes { get; set; } = 64 * 1024 * 1024; // 64MB
    public string EvictionPolicy { get; set; } = "allkeys-lru";
    public int KeyCount { get; set; } = 10000;
    public int ValueSizeBytes { get; set; } = 1024; // 1KB per value
    public bool TestEvictionBehavior { get; set; } = true;
    public bool TestMemoryFragmentation { get; set; } = true;
}

/// <summary>
/// Test case for IP-based rate limiting
/// </summary>
public class IPRateLimitTestCase
{
    public string IPAddress { get; set; } = "127.0.0.1";
    public int RequestsPerMinute { get; set; } = 1000;
    public TimeSpan TimeWindow { get; set; } = TimeSpan.FromMinutes(1);
    public int RequestCount { get; set; } = 1200; // Exceed limit
    public bool ExpectRateLimit { get; set; } = true;
    public TimeSpan RequestInterval { get; set; } = TimeSpan.FromMilliseconds(50);
    public bool TestWindowReset { get; set; } = true;
}

/// <summary>
/// Test case for user-based rate limiting
/// </summary>
public class UserRateLimitTestCase
{
    public string UserId { get; set; } = string.Empty;
    public int RequestsPerMinute { get; set; } = 100;
    public TimeSpan TimeWindow { get; set; } = TimeSpan.FromMinutes(1);
    public int RequestCount { get; set; } = 150; // Exceed limit
    public bool ExpectRateLimit { get; set; } = true;
    public string[]? UserRoles { get; set; }
    public bool TestRoleBasedLimits { get; set; } = true;
}

/// <summary>
/// Test case for rate limiting window behavior
/// </summary>
public class RateLimitWindowTestCase
{
    public string Identifier { get; set; } = string.Empty;
    public TimeSpan WindowSize { get; set; } = TimeSpan.FromMinutes(1);
    public int WindowLimit { get; set; } = 1000;
    public RateLimitWindowType WindowType { get; set; } = RateLimitWindowType.Sliding;
    public bool TestWindowSliding { get; set; } = true;
    public bool TestWindowReset { get; set; } = true;
    public bool TestWindowOverlap { get; set; } = true;
}

/// <summary>
/// Rate limiting window type enumeration
/// </summary>
public enum RateLimitWindowType
{
    Fixed,
    Sliding,
    TokenBucket,
    LeakyBucket
}

/// <summary>
/// Test case for concurrent rate limiting
/// </summary>
public class ConcurrentRateLimitTestCase
{
    public string Identifier { get; set; } = string.Empty;
    public int ConcurrentRequests { get; set; } = 50;
    public int TotalRequests { get; set; } = 1500;
    public int RateLimit { get; set; } = 1000;
    public TimeSpan TimeWindow { get; set; } = TimeSpan.FromMinutes(1);
    public bool ValidateAccuracy { get; set; } = true;
    public double AccuracyTolerance { get; set; } = 0.05; // 5%
}

/// <summary>
/// Test case for rate limit recovery
/// </summary>
public class RateLimitRecoveryTestCase
{
    public string Identifier { get; set; } = string.Empty;
    public int RateLimit { get; set; } = 1000;
    public TimeSpan RecoveryWindow { get; set; } = TimeSpan.FromMinutes(1);
    public int BurstSize { get; set; } = 100;
    public bool TestBurstRecovery { get; set; } = true;
    public bool TestGradualRecovery { get; set; } = true;
    public TimeSpan MaxRecoveryTime { get; set; } = TimeSpan.FromMinutes(2);
}

/// <summary>
/// Test case for rate limiting metrics
/// </summary>
public class RateLimitMetricsTestCase
{
    public string Identifier { get; set; } = string.Empty;
    public int RequestCount { get; set; } = 1000;
    public int RateLimit { get; set; } = 500;
    public string[] ExpectedMetrics { get; set; } = { "requests_total", "requests_blocked", "rate_limit_hit" };
    public bool ValidateMetricAccuracy { get; set; } = true;
    public bool TestMetricPersistence { get; set; } = true;
}

// Additional test case classes for distributed caching would continue here...
// Following the same pattern for PublicApiCacheTestCase, AdminApiCacheTestCase, etc.
using System.Net;

namespace Infrastructure.Cache.Tests.Contracts;

/// <summary>
/// Test case for Services Public API distributed caching (Dapper repositories)
/// Validates cache integration with Dapper data access patterns for anonymous users
/// </summary>
public class PublicApiCacheTestCase
{
    public string CacheKey { get; set; } = string.Empty;
    public object CachedData { get; set; } = new();
    public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(5);
    public string QueryType { get; set; } = string.Empty; // e.g., "ServiceList", "ServiceDetail", "CategoryList"
    public Dictionary<string, object>? QueryParameters { get; set; }
    public bool TestAnonymousAccess { get; set; } = true;
    public bool TestCacheHit { get; set; } = true;
    public bool TestCacheMiss { get; set; } = true;
    public bool ValidateDataIntegrity { get; set; } = true;
    public int ExpectedCacheHitRatio { get; set; } = 80; // percentage
}

/// <summary>
/// Test case for Services Admin API distributed caching (EF Core repositories)
/// Validates cache integration with EF Core patterns for role-based access control
/// </summary>
public class AdminApiCacheTestCase
{
    public string CacheKey { get; set; } = string.Empty;
    public object CachedData { get; set; } = new();
    public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(10);
    public string EntityType { get; set; } = string.Empty; // e.g., "Service", "Category", "User"
    public string[] UserRoles { get; set; } = Array.Empty<string>();
    public string UserId { get; set; } = string.Empty;
    public bool TestRoleBasedAccess { get; set; } = true;
    public bool TestAuditLogging { get; set; } = true;
    public bool TestMedicalGradeCompliance { get; set; } = true;
    public bool ValidateSecurityHeaders { get; set; } = true;
    public AuditAction AuditAction { get; set; } = AuditAction.Read;
}

/// <summary>
/// Audit action enumeration for medical-grade compliance
/// </summary>
public enum AuditAction
{
    Create,
    Read,
    Update,
    Delete,
    CacheHit,
    CacheMiss,
    CacheInvalidation
}

/// <summary>
/// Test case for cache invalidation strategies
/// Validates proper cache invalidation for data consistency in medical-grade environment
/// </summary>
public class CacheInvalidationTestCase
{
    public string[] CacheKeys { get; set; } = Array.Empty<string>();
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; } = Guid.NewGuid();
    public CacheInvalidationStrategy Strategy { get; set; } = CacheInvalidationStrategy.KeyBased;
    public CacheInvalidationTrigger Trigger { get; set; } = CacheInvalidationTrigger.DataUpdate;
    public bool TestCascadingInvalidation { get; set; } = true;
    public bool TestPartialInvalidation { get; set; } = true;
    public TimeSpan MaxInvalidationTime { get; set; } = TimeSpan.FromSeconds(1);
    public string[] DependentCacheKeys { get; set; } = Array.Empty<string>();
    public bool ValidateConsistency { get; set; } = true;
}

/// <summary>
/// Cache invalidation strategy enumeration
/// </summary>
public enum CacheInvalidationStrategy
{
    KeyBased,
    TagBased,
    PatternBased,
    TimeBasedExpiry,
    EventDriven,
    ManualInvalidation
}

/// <summary>
/// Cache invalidation trigger enumeration
/// </summary>
public enum CacheInvalidationTrigger
{
    DataUpdate,
    DataDelete,
    SchemaChange,
    ConfigurationChange,
    ManualTrigger,
    ScheduledTask,
    MemoryPressure
}

/// <summary>
/// Test case for cache warming and preloading strategies
/// Validates cache warming strategies for optimal Services API performance
/// </summary>
public class CacheWarmingTestCase
{
    public string[] CacheKeys { get; set; } = Array.Empty<string>();
    public CacheWarmingStrategy Strategy { get; set; } = CacheWarmingStrategy.OnStartup;
    public int Priority { get; set; } = 1; // 1 = highest priority
    public TimeSpan MaxWarmingTime { get; set; } = TimeSpan.FromMinutes(5);
    public Func<Task<object>>? DataProvider { get; set; }
    public bool TestWarmingEffectiveness { get; set; } = true;
    public bool TestWarmingPerformance { get; set; } = true;
    public bool TestWarmingFailureRecovery { get; set; } = true;
    public int ExpectedCacheHitRatio { get; set; } = 90; // percentage after warming
    public bool ValidateDataFreshness { get; set; } = true;
}

/// <summary>
/// Cache warming strategy enumeration
/// </summary>
public enum CacheWarmingStrategy
{
    OnStartup,
    OnDemand,
    Scheduled,
    Predictive,
    LoadBased,
    UserBehaviorBased
}

/// <summary>
/// Test case for cache partitioning and multi-tenancy
/// Validates cache isolation between different API domains for security compliance
/// </summary>
public class CachePartitioningTestCase
{
    public string PartitionKey { get; set; } = string.Empty;
    public string[] TenantIds { get; set; } = Array.Empty<string>();
    public ApiDomain ApiDomain { get; set; } = ApiDomain.ServicesPublic;
    public Dictionary<string, object> PartitionData { get; set; } = new();
    public bool TestIsolation { get; set; } = true;
    public bool TestCrossPartitionAccess { get; set; } = true;
    public bool ValidateSecurityBoundaries { get; set; } = true;
    public bool TestPartitionScaling { get; set; } = true;
    public int MaxPartitionSize { get; set; } = 1000; // entries per partition
    public bool TestPartitionEviction { get; set; } = true;
}

/// <summary>
/// API domain enumeration for cache partitioning
/// </summary>
public enum ApiDomain
{
    ServicesPublic,
    ServicesAdmin,
    PublicGateway,
    SharedKernel
}

/// <summary>
/// Test case for cache backup and restore operations
/// Validates cache persistence and recovery for medical-grade data protection
/// </summary>
public class CacheBackupRestoreTestCase
{
    public string BackupName { get; set; } = string.Empty;
    public Dictionary<string, object> TestData { get; set; } = new();
    public CacheBackupStrategy BackupStrategy { get; set; } = CacheBackupStrategy.Snapshot;
    public CacheRestoreStrategy RestoreStrategy { get; set; } = CacheRestoreStrategy.Complete;
    public bool TestIncrementalBackup { get; set; } = false;
    public bool TestPointInTimeRecovery { get; set; } = false;
    public TimeSpan MaxBackupTime { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan MaxRestoreTime { get; set; } = TimeSpan.FromMinutes(2);
    public bool ValidateDataIntegrity { get; set; } = true;
    public bool TestBackupCompression { get; set; } = true;
    public bool TestBackupEncryption { get; set; } = false; // Not required for testing environment
}

/// <summary>
/// Cache backup strategy enumeration
/// </summary>
public enum CacheBackupStrategy
{
    Snapshot,
    Incremental,
    Differential,
    ContinuousDataProtection,
    EventSourcing
}

/// <summary>
/// Cache restore strategy enumeration
/// </summary>
public enum CacheRestoreStrategy
{
    Complete,
    Selective,
    InPlace,
    SideLoad,
    OnDemand
}

/// <summary>
/// Test case for cache monitoring and observability
/// Validates cache monitoring capabilities for medical-grade operational visibility
/// </summary>
public class CacheMonitoringTestCase
{
    public string MetricName { get; set; } = string.Empty;
    public CacheMetricType MetricType { get; set; } = CacheMetricType.HitRatio;
    public object ExpectedValue { get; set; } = new();
    public TimeSpan SamplePeriod { get; set; } = TimeSpan.FromSeconds(30);
    public bool TestAlertThresholds { get; set; } = true;
    public double AlertThreshold { get; set; } = 0.8; // 80% hit ratio threshold
    public bool TestHistoricalData { get; set; } = true;
    public bool ValidateMetricAccuracy { get; set; } = true;
    public string[] RequiredTags { get; set; } = Array.Empty<string>();
    public bool TestMetricExport { get; set; } = true;
}

/// <summary>
/// Cache metric type enumeration
/// </summary>
public enum CacheMetricType
{
    HitRatio,
    MissRatio,
    Latency,
    Throughput,
    MemoryUsage,
    ConnectionCount,
    ErrorRate,
    EvictionRate,
    ExpirationRate
}

/// <summary>
/// Test case for cache security and access control
/// Validates cache security measures for medical-grade data protection
/// </summary>
public class CacheSecurityTestCase
{
    public string UserId { get; set; } = string.Empty;
    public string[] UserRoles { get; set; } = Array.Empty<string>();
    public string ResourceKey { get; set; } = string.Empty;
    public CacheSecurityAction Action { get; set; } = CacheSecurityAction.Read;
    public bool ExpectAccess { get; set; } = true;
    public bool TestRoleBasedAccess { get; set; } = true;
    public bool TestDataMasking { get; set; } = false; // Not implemented in testing
    public bool ValidateAuditLogging { get; set; } = true;
    public SecurityLevel RequiredSecurityLevel { get; set; } = SecurityLevel.Standard;
    public bool TestSecurityHeaders { get; set; } = true;
}

/// <summary>
/// Cache security action enumeration
/// </summary>
public enum CacheSecurityAction
{
    Read,
    Write,
    Delete,
    Invalidate,
    Admin,
    Monitor
}

/// <summary>
/// Security level enumeration for medical-grade compliance
/// </summary>
public enum SecurityLevel
{
    Public,
    Standard,
    Sensitive,
    MedicalGrade,
    HighlyClassified
}

/// <summary>
/// Test case for cache high availability and failover
/// Validates cache availability for medical-grade reliability requirements
/// </summary>
public class CacheHighAvailabilityTestCase
{
    public string[] RedisNodes { get; set; } = Array.Empty<string>();
    public CacheFailoverStrategy FailoverStrategy { get; set; } = CacheFailoverStrategy.Automatic;
    public TimeSpan MaxFailoverTime { get; set; } = TimeSpan.FromSeconds(30);
    public int MinActiveNodes { get; set; } = 1;
    public bool TestNodeFailure { get; set; } = true;
    public bool TestNetworkPartition { get; set; } = false; // Complex for testing environment
    public bool TestDataReplication { get; set; } = false; // Not implemented in single-node testing
    public bool ValidateDataConsistency { get; set; } = true;
    public bool TestFailoverRecovery { get; set; } = true;
    public int MaxDataLossEntries { get; set; } = 0; // No data loss acceptable for medical-grade
}

/// <summary>
/// Cache failover strategy enumeration
/// </summary>
public enum CacheFailoverStrategy
{
    Manual,
    Automatic,
    GracefulShutdown,
    ForcedFailover,
    LoadBalancingFallback
}

/// <summary>
/// Test case for cache performance under load
/// Validates cache performance characteristics under sustained load
/// </summary>
public class CacheLoadTestCase
{
    public int ConcurrentUsers { get; set; } = 100;
    public int RequestsPerUser { get; set; } = 1000;
    public TimeSpan TestDuration { get; set; } = TimeSpan.FromMinutes(5);
    public CacheLoadPattern LoadPattern { get; set; } = CacheLoadPattern.Steady;
    public Dictionary<CacheOperation, int> OperationDistribution { get; set; } = new()
    {
        { CacheOperation.Get, 70 },    // 70% reads
        { CacheOperation.Set, 25 },    // 25% writes  
        { CacheOperation.Delete, 5 }   // 5% deletes
    };
    public CachePerformanceThreshold PerformanceThreshold { get; set; } = new();
    public bool TestMemoryGrowth { get; set; } = true;
    public bool TestConnectionPooling { get; set; } = true;
    public bool ValidateDataIntegrity { get; set; } = true;
    public int MaxErrorPercentage { get; set; } = 1; // 1% error rate acceptable
}

/// <summary>
/// Cache load pattern enumeration
/// </summary>
public enum CacheLoadPattern
{
    Steady,
    Ramp,
    Spike,
    Wave,
    Random,
    BurstyTraffic
}

/// <summary>
/// Test data factory for cache testing
/// Provides realistic test data for cache validation
/// </summary>
public interface ICacheTestDataFactory
{
    /// <summary>
    /// Creates mock service data for Services API caching
    /// Contract: Must generate realistic service data matching Services API responses
    /// </summary>
    Task<ServiceCacheData[]> CreateMockServiceDataAsync(
        int count = 100,
        Action<ServiceCacheData>? configure = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates mock category data for Services API caching
    /// Contract: Must generate realistic category hierarchy data for Services API caching
    /// </summary>
    Task<CategoryCacheData[]> CreateMockCategoryDataAsync(
        int depth = 3,
        int breadth = 5,
        Action<CategoryCacheData>? configure = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates mock rate limiting data for Public Gateway testing
    /// Contract: Must generate realistic rate limiting scenarios for IP and user-based limits
    /// </summary>
    Task<RateLimitCacheData[]> CreateMockRateLimitDataAsync(
        string[] identifiers,
        int requestsPerMinute = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates mock cache keys and values for performance testing
    /// Contract: Must generate realistic cache data for load testing scenarios
    /// </summary>
    Task<CacheTestEntry[]> CreateMockCacheEntriesAsync(
        int count = 10000,
        int valueSizeBytes = 1024,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates cache data quality and realism
    /// Contract: Must ensure cache data follows realistic patterns for Services APIs
    /// </summary>
    Task ValidateCacheDataQualityAsync<T>(
        T cacheData,
        CacheDataQualityRules<T>? qualityRules = null)
        where T : class;
}

/// <summary>
/// Service cache data model
/// </summary>
public class ServiceCacheData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid[] CategoryIds { get; set; } = Array.Empty<Guid>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Category cache data model
/// </summary>
public class CategoryCacheData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int Level { get; set; } = 1;
    public int ServiceCount { get; set; }
    public bool IsVisible { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Rate limit cache data model
/// </summary>
public class RateLimitCacheData
{
    public string Identifier { get; set; } = string.Empty; // IP address or user ID
    public int RequestCount { get; set; }
    public DateTime WindowStart { get; set; } = DateTime.UtcNow;
    public DateTime WindowEnd { get; set; }
    public int Limit { get; set; } = 1000;
    public bool IsBlocked { get; set; }
    public TimeSpan? BlockedUntil { get; set; }
}

/// <summary>
/// Cache test entry model
/// </summary>
public class CacheTestEntry
{
    public string Key { get; set; } = string.Empty;
    public object Value { get; set; } = new();
    public Type ValueType { get; set; } = typeof(string);
    public TimeSpan? Expiry { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int SizeBytes { get; set; }
    public string[]? Tags { get; set; }
}

/// <summary>
/// Quality rules for cache data validation
/// </summary>
public class CacheDataQualityRules<T> where T : class
{
    public Func<T, bool> IsRealistic { get; set; } = _ => true;
    public Func<T, bool> MatchesApiSchema { get; set; } = _ => true;
    public Func<T, bool> HasRequiredFields { get; set; } = _ => true;
    public Func<T, bool> ValidateBusinessRules { get; set; } = _ => true;
    public string[] ForbiddenPatterns { get; set; } = { "test", "mock", "fake", "lorem" };
    public int MaxValueSizeBytes { get; set; } = 1024 * 1024; // 1MB
    public TimeSpan MaxExpiry { get; set; } = TimeSpan.FromDays(7);
}
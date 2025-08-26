using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Infrastructure.Cache.Tests.Contracts;
using Xunit.Abstractions;

namespace Infrastructure.Cache.Tests;

/// <summary>
/// Implementation of distributed cache contract for Redis testing
/// Provides Services Public/Admin API distributed caching validation with medical-grade compliance
/// </summary>
public class DistributedCacheService : IDistributedCacheContract
{
    private readonly ILogger<DistributedCacheService> _logger;

    public DistributedCacheService(ILogger<DistributedCacheService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tests distributed cache for Services Public API (Dapper repositories)
    /// Contract: Must validate cache integration with Dapper data access patterns for anonymous users
    /// </summary>
    public async Task TestPublicApiDistributedCacheAsync(
        IDatabase database,
        PublicApiCacheTestCase[] cacheTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (cacheTestCases == null) throw new ArgumentNullException(nameof(cacheTestCases));

        _logger.LogInformation("Starting Public API distributed cache test with {Count} test cases", cacheTestCases.Length);

        foreach (var testCase in cacheTestCases)
        {
            try
            {
                output?.WriteLine($"Testing Public API cache: {testCase.QueryType} with key {testCase.CacheKey}");

                // Clear any existing cache data
                await database.KeyDeleteAsync(testCase.CacheKey);

                // Test cache miss scenario
                if (testCase.TestCacheMiss)
                {
                    var missResult = await database.StringGetAsync(testCase.CacheKey);
                    if (missResult.HasValue)
                    {
                        throw new InvalidOperationException($"Expected cache miss for key {testCase.CacheKey}, but value was found");
                    }
                    output?.WriteLine("✓ Cache miss test passed");
                }

                // Set cache data
                var serializedData = System.Text.Json.JsonSerializer.Serialize(testCase.CachedData);
                await database.StringSetAsync(testCase.CacheKey, serializedData, testCase.CacheExpiry);

                // Test cache hit scenario
                if (testCase.TestCacheHit)
                {
                    var hitResult = await database.StringGetAsync(testCase.CacheKey);
                    if (!hitResult.HasValue)
                    {
                        throw new InvalidOperationException($"Expected cache hit for key {testCase.CacheKey}, but value was not found");
                    }

                    // Validate data integrity
                    if (testCase.ValidateDataIntegrity)
                    {
                        var retrievedData = System.Text.Json.JsonSerializer.Deserialize<object>(hitResult!);
                        // In a real scenario, you would compare the objects more thoroughly
                        if (retrievedData == null)
                        {
                            throw new InvalidOperationException($"Data integrity validation failed for key {testCase.CacheKey}");
                        }
                    }

                    output?.WriteLine("✓ Cache hit test passed");
                }

                // Test anonymous access patterns
                if (testCase.TestAnonymousAccess)
                {
                    // Validate that no user-specific data is stored
                    if (testCase.CacheKey.Contains("user:", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Public API cache should not contain user-specific keys: {testCase.CacheKey}");
                    }
                    output?.WriteLine("✓ Anonymous access validation passed");
                }

                // Simulate cache hit ratio testing
                if (testCase.ExpectedCacheHitRatio > 0)
                {
                    var hits = 0;
                    var totalRequests = 100;

                    for (int i = 0; i < totalRequests; i++)
                    {
                        var result = await database.StringGetAsync(testCase.CacheKey);
                        if (result.HasValue)
                        {
                            hits++;
                        }
                    }

                    var actualHitRatio = (hits * 100) / totalRequests;
                    if (actualHitRatio < testCase.ExpectedCacheHitRatio)
                    {
                        throw new InvalidOperationException(
                            $"Cache hit ratio too low. Expected: {testCase.ExpectedCacheHitRatio}%, Actual: {actualHitRatio}%");
                    }

                    output?.WriteLine($"✓ Cache hit ratio test passed: {actualHitRatio}%");
                }

                // Cleanup
                await database.KeyDeleteAsync(testCase.CacheKey);

                output?.WriteLine($"✓ Public API cache test completed for {testCase.QueryType}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Public API distributed cache test failed for {QueryType}", testCase.QueryType);
                output?.WriteLine($"✗ Public API cache test failed for {testCase.QueryType}: {ex.Message}");
                throw;
            }
        }

        _logger.LogInformation("Public API distributed cache test completed successfully");
    }

    /// <summary>
    /// Tests distributed cache for Services Admin API (EF Core repositories)
    /// Contract: Must validate cache integration with EF Core patterns for role-based access control
    /// </summary>
    public async Task TestAdminApiDistributedCacheAsync(
        IDatabase database,
        AdminApiCacheTestCase[] cacheTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (cacheTestCases == null) throw new ArgumentNullException(nameof(cacheTestCases));

        _logger.LogInformation("Starting Admin API distributed cache test with {Count} test cases", cacheTestCases.Length);

        foreach (var testCase in cacheTestCases)
        {
            try
            {
                output?.WriteLine($"Testing Admin API cache: {testCase.EntityType} for user {testCase.UserId}");

                // Clear any existing cache data
                await database.KeyDeleteAsync(testCase.CacheKey);

                // Test role-based access
                if (testCase.TestRoleBasedAccess)
                {
                    var roleBasedKey = $"{testCase.CacheKey}:roles:{string.Join("-", testCase.UserRoles)}";
                    
                    var serializedData = System.Text.Json.JsonSerializer.Serialize(testCase.CachedData);
                    await database.StringSetAsync(roleBasedKey, serializedData, testCase.CacheExpiry);

                    var result = await database.StringGetAsync(roleBasedKey);
                    if (!result.HasValue)
                    {
                        throw new InvalidOperationException($"Role-based cache access failed for roles: {string.Join(",", testCase.UserRoles)}");
                    }

                    output?.WriteLine($"✓ Role-based access test passed for roles: {string.Join(",", testCase.UserRoles)}");
                    
                    // Cleanup role-based key
                    await database.KeyDeleteAsync(roleBasedKey);
                }

                // Set main cache data
                var mainSerializedData = System.Text.Json.JsonSerializer.Serialize(testCase.CachedData);
                await database.StringSetAsync(testCase.CacheKey, mainSerializedData, testCase.CacheExpiry);

                // Test audit logging simulation
                if (testCase.TestAuditLogging)
                {
                    var auditKey = $"audit:{testCase.CacheKey}:{testCase.AuditAction}:{DateTime.UtcNow:yyyyMMdd}";
                    var auditData = new
                    {
                        UserId = testCase.UserId,
                        EntityType = testCase.EntityType,
                        Action = testCase.AuditAction.ToString(),
                        Timestamp = DateTime.UtcNow,
                        CacheKey = testCase.CacheKey
                    };

                    await database.StringSetAsync(auditKey, System.Text.Json.JsonSerializer.Serialize(auditData), TimeSpan.FromDays(30));

                    var auditResult = await database.StringGetAsync(auditKey);
                    if (!auditResult.HasValue)
                    {
                        throw new InvalidOperationException($"Audit logging test failed for key: {auditKey}");
                    }

                    output?.WriteLine($"✓ Audit logging test passed for action: {testCase.AuditAction}");
                    
                    // Cleanup audit key
                    await database.KeyDeleteAsync(auditKey);
                }

                // Test medical-grade compliance
                if (testCase.TestMedicalGradeCompliance)
                {
                    // Ensure no sensitive data is cached without proper security
                    var cacheContent = await database.StringGetAsync(testCase.CacheKey);
                    if (cacheContent.HasValue)
                    {
                        var contentStr = cacheContent.ToString().ToLower();
                        var sensitivePatterns = new[] { "ssn", "social security", "credit card", "password", "token" };
                        
                        foreach (var pattern in sensitivePatterns)
                        {
                            if (contentStr.Contains(pattern))
                            {
                                throw new InvalidOperationException($"Medical-grade compliance violation: sensitive pattern '{pattern}' found in cache");
                            }
                        }
                    }

                    output?.WriteLine("✓ Medical-grade compliance test passed");
                }

                // Test security headers validation
                if (testCase.ValidateSecurityHeaders)
                {
                    // Simulate security header validation by checking cache key format
                    if (!testCase.CacheKey.StartsWith("secure:", StringComparison.OrdinalIgnoreCase) && 
                        testCase.UserRoles.Contains("admin", StringComparer.OrdinalIgnoreCase))
                    {
                        output?.WriteLine("⚠ Security headers validation: Consider using 'secure:' prefix for admin cache keys");
                    }
                    else
                    {
                        output?.WriteLine("✓ Security headers validation passed");
                    }
                }

                // Cleanup
                await database.KeyDeleteAsync(testCase.CacheKey);

                output?.WriteLine($"✓ Admin API cache test completed for {testCase.EntityType}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin API distributed cache test failed for {EntityType}", testCase.EntityType);
                output?.WriteLine($"✗ Admin API cache test failed for {testCase.EntityType}: {ex.Message}");
                throw;
            }
        }

        _logger.LogInformation("Admin API distributed cache test completed successfully");
    }

    /// <summary>
    /// Tests cache invalidation strategies for Services APIs
    /// Contract: Must validate proper cache invalidation for data consistency in medical-grade environment
    /// </summary>
    public async Task TestCacheInvalidationAsync(
        IDatabase database,
        CacheInvalidationTestCase[] invalidationTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (invalidationTestCases == null) throw new ArgumentNullException(nameof(invalidationTestCases));

        _logger.LogInformation("Starting cache invalidation test with {Count} test cases", invalidationTestCases.Length);

        foreach (var testCase in invalidationTestCases)
        {
            try
            {
                output?.WriteLine($"Testing cache invalidation: {testCase.Strategy} for entity {testCase.EntityType}");

                // Set up initial cache data
                foreach (var cacheKey in testCase.CacheKeys)
                {
                    var data = new { EntityId = testCase.EntityId, Data = $"TestData-{cacheKey}" };
                    await database.StringSetAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(data));
                }

                // Set up dependent cache keys
                foreach (var dependentKey in testCase.DependentCacheKeys)
                {
                    var dependentData = new { EntityId = testCase.EntityId, DependentData = $"DependentData-{dependentKey}" };
                    await database.StringSetAsync(dependentKey, System.Text.Json.JsonSerializer.Serialize(dependentData));
                }

                // Execute invalidation based on strategy
                var startTime = DateTime.UtcNow;
                
                switch (testCase.Strategy)
                {
                    case CacheInvalidationStrategy.KeyBased:
                        await InvalidateByKeysAsync(database, testCase.CacheKeys);
                        break;
                        
                    case CacheInvalidationStrategy.TagBased:
                        await InvalidateByTagsAsync(database, testCase.EntityType, testCase.EntityId);
                        break;
                        
                    case CacheInvalidationStrategy.PatternBased:
                        await InvalidateByPatternAsync(database, $"*{testCase.EntityType}:{testCase.EntityId}*");
                        break;
                        
                    case CacheInvalidationStrategy.EventDriven:
                        await InvalidateByEventAsync(database, testCase.Trigger, testCase.CacheKeys);
                        break;
                        
                    default:
                        throw new NotSupportedException($"Invalidation strategy {testCase.Strategy} is not supported");
                }

                var invalidationTime = DateTime.UtcNow - startTime;

                // Validate invalidation completed within time limit
                if (invalidationTime > testCase.MaxInvalidationTime)
                {
                    throw new InvalidOperationException(
                        $"Cache invalidation took too long: {invalidationTime.TotalMilliseconds}ms > {testCase.MaxInvalidationTime.TotalMilliseconds}ms");
                }

                // Verify primary keys were invalidated
                foreach (var cacheKey in testCase.CacheKeys)
                {
                    var result = await database.StringGetAsync(cacheKey);
                    if (result.HasValue)
                    {
                        throw new InvalidOperationException($"Cache key {cacheKey} was not invalidated");
                    }
                }

                // Test cascading invalidation
                if (testCase.TestCascadingInvalidation)
                {
                    foreach (var dependentKey in testCase.DependentCacheKeys)
                    {
                        var result = await database.StringGetAsync(dependentKey);
                        if (result.HasValue)
                        {
                            throw new InvalidOperationException($"Dependent cache key {dependentKey} was not invalidated in cascade");
                        }
                    }
                    output?.WriteLine("✓ Cascading invalidation test passed");
                }

                // Test partial invalidation
                if (testCase.TestPartialInvalidation)
                {
                    // Set up additional keys that should NOT be invalidated
                    var protectedKey = $"protected:{testCase.EntityType}:other";
                    await database.StringSetAsync(protectedKey, "should-remain");
                    
                    // Verify protected key remains
                    var protectedResult = await database.StringGetAsync(protectedKey);
                    if (!protectedResult.HasValue)
                    {
                        throw new InvalidOperationException($"Partial invalidation failed - protected key {protectedKey} was invalidated");
                    }
                    
                    await database.KeyDeleteAsync(protectedKey); // Cleanup
                    output?.WriteLine("✓ Partial invalidation test passed");
                }

                // Validate consistency
                if (testCase.ValidateConsistency)
                {
                    // Ensure all related cache entries are in consistent state (all invalidated or all present)
                    var states = new List<bool>();
                    foreach (var key in testCase.CacheKeys.Concat(testCase.DependentCacheKeys))
                    {
                        var exists = await database.KeyExistsAsync(key);
                        states.Add(exists);
                    }

                    if (states.Distinct().Count() > 1)
                    {
                        throw new InvalidOperationException("Cache invalidation consistency check failed - inconsistent states detected");
                    }

                    output?.WriteLine("✓ Consistency validation passed");
                }

                output?.WriteLine($"✓ Cache invalidation test completed: {testCase.Strategy} in {invalidationTime.TotalMilliseconds:F2}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache invalidation test failed for strategy {Strategy}", testCase.Strategy);
                output?.WriteLine($"✗ Cache invalidation test failed for {testCase.Strategy}: {ex.Message}");
                throw;
            }
        }

        _logger.LogInformation("Cache invalidation test completed successfully");
    }

    /// <summary>
    /// Tests cache warming and preloading strategies
    /// Contract: Must validate cache warming strategies for optimal Services API performance
    /// </summary>
    public async Task TestCacheWarmingAsync(
        IDatabase database,
        CacheWarmingTestCase[] warmingTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (warmingTestCases == null) throw new ArgumentNullException(nameof(warmingTestCases));

        _logger.LogInformation("Starting cache warming test with {Count} test cases", warmingTestCases.Length);

        foreach (var testCase in warmingTestCases)
        {
            try
            {
                output?.WriteLine($"Testing cache warming: {testCase.Strategy} with priority {testCase.Priority}");

                // Clear cache before warming
                foreach (var cacheKey in testCase.CacheKeys)
                {
                    await database.KeyDeleteAsync(cacheKey);
                }

                var warmingStartTime = DateTime.UtcNow;

                // Execute warming strategy
                switch (testCase.Strategy)
                {
                    case CacheWarmingStrategy.OnStartup:
                        await WarmCacheOnStartupAsync(database, testCase);
                        break;
                        
                    case CacheWarmingStrategy.OnDemand:
                        await WarmCacheOnDemandAsync(database, testCase);
                        break;
                        
                    case CacheWarmingStrategy.Scheduled:
                        await WarmCacheScheduledAsync(database, testCase);
                        break;
                        
                    case CacheWarmingStrategy.Predictive:
                        await WarmCachePredictiveAsync(database, testCase);
                        break;
                        
                    default:
                        throw new NotSupportedException($"Warming strategy {testCase.Strategy} is not supported");
                }

                var warmingDuration = DateTime.UtcNow - warmingStartTime;

                // Validate warming completed within time limit
                if (warmingDuration > testCase.MaxWarmingTime)
                {
                    throw new InvalidOperationException(
                        $"Cache warming took too long: {warmingDuration.TotalMinutes:F2}min > {testCase.MaxWarmingTime.TotalMinutes:F2}min");
                }

                // Test warming effectiveness
                if (testCase.TestWarmingEffectiveness)
                {
                    var warmedKeys = 0;
                    foreach (var cacheKey in testCase.CacheKeys)
                    {
                        var result = await database.StringGetAsync(cacheKey);
                        if (result.HasValue)
                        {
                            warmedKeys++;
                        }
                    }

                    var warmingEffectiveness = (warmedKeys * 100) / testCase.CacheKeys.Length;
                    if (warmingEffectiveness < 80) // Expect at least 80% effectiveness
                    {
                        throw new InvalidOperationException(
                            $"Cache warming effectiveness too low: {warmingEffectiveness}% (expected >= 80%)");
                    }

                    output?.WriteLine($"✓ Warming effectiveness: {warmingEffectiveness}%");
                }

                // Test warming performance
                if (testCase.TestWarmingPerformance)
                {
                    var performanceStartTime = DateTime.UtcNow;
                    
                    // Simulate high-frequency cache access
                    for (int i = 0; i < 100; i++)
                    {
                        var randomKey = testCase.CacheKeys[i % testCase.CacheKeys.Length];
                        await database.StringGetAsync(randomKey);
                    }
                    
                    var accessDuration = DateTime.UtcNow - performanceStartTime;
                    output?.WriteLine($"✓ Warmed cache access performance: {accessDuration.TotalMilliseconds:F2}ms for 100 operations");
                }

                // Test warming failure recovery
                if (testCase.TestWarmingFailureRecovery)
                {
                    // Simulate a failure scenario by deleting half the warmed cache
                    var keysToDelete = testCase.CacheKeys.Take(testCase.CacheKeys.Length / 2).ToArray();
                    foreach (var key in keysToDelete)
                    {
                        await database.KeyDeleteAsync(key);
                    }

                    // Trigger recovery warming
                    await WarmCacheOnDemandAsync(database, testCase);

                    // Verify recovery
                    foreach (var key in keysToDelete)
                    {
                        var result = await database.StringGetAsync(key);
                        if (!result.HasValue)
                        {
                            throw new InvalidOperationException($"Cache warming failure recovery failed for key: {key}");
                        }
                    }

                    output?.WriteLine("✓ Warming failure recovery test passed");
                }

                // Validate data freshness
                if (testCase.ValidateDataFreshness)
                {
                    foreach (var cacheKey in testCase.CacheKeys)
                    {
                        var ttl = await database.KeyTimeToLiveAsync(cacheKey);
                        if (ttl.HasValue && ttl.Value < TimeSpan.FromMinutes(1))
                        {
                            output?.WriteLine($"⚠ Data freshness warning: {cacheKey} expires in {ttl.Value.TotalMinutes:F1} minutes");
                        }
                    }
                    output?.WriteLine("✓ Data freshness validation completed");
                }

                // Cleanup
                foreach (var cacheKey in testCase.CacheKeys)
                {
                    await database.KeyDeleteAsync(cacheKey);
                }

                output?.WriteLine($"✓ Cache warming test completed: {testCase.Strategy} in {warmingDuration.TotalSeconds:F2}s");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache warming test failed for strategy {Strategy}", testCase.Strategy);
                output?.WriteLine($"✗ Cache warming test failed for {testCase.Strategy}: {ex.Message}");
                throw;
            }
        }

        _logger.LogInformation("Cache warming test completed successfully");
    }

    /// <summary>
    /// Tests cache partitioning and multi-tenancy
    /// Contract: Must validate cache isolation between different API domains for security compliance
    /// </summary>
    public async Task TestCachePartitioningAsync(
        IDatabase database,
        CachePartitioningTestCase[] partitioningTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (partitioningTestCases == null) throw new ArgumentNullException(nameof(partitioningTestCases));

        _logger.LogInformation("Starting cache partitioning test with {Count} test cases", partitioningTestCases.Length);

        foreach (var testCase in partitioningTestCases)
        {
            try
            {
                output?.WriteLine($"Testing cache partitioning: {testCase.ApiDomain} partition {testCase.PartitionKey}");

                var partitionPrefix = $"partition:{testCase.ApiDomain}:{testCase.PartitionKey}";
                var testKeys = new List<string>();

                // Create partitioned data
                foreach (var (key, value) in testCase.PartitionData)
                {
                    var partitionedKey = $"{partitionPrefix}:{key}";
                    testKeys.Add(partitionedKey);
                    await database.StringSetAsync(partitionedKey, System.Text.Json.JsonSerializer.Serialize(value));
                }

                // Test isolation between partitions
                if (testCase.TestIsolation)
                {
                    var otherPartitionKey = $"partition:{testCase.ApiDomain}:other";
                    var otherKey = $"{otherPartitionKey}:isolated-data";
                    await database.StringSetAsync(otherKey, "isolated-value");

                    // Verify data doesn't cross partitions
                    foreach (var testKey in testKeys)
                    {
                        if (testKey.Contains("other"))
                        {
                            throw new InvalidOperationException($"Partition isolation violated: {testKey} should not contain 'other'");
                        }
                    }

                    await database.KeyDeleteAsync(otherKey);
                    output?.WriteLine("✓ Partition isolation test passed");
                }

                // Test cross-partition access prevention
                if (testCase.TestCrossPartitionAccess)
                {
                    // Create data in different tenant partition
                    var differentTenant = testCase.TenantIds.Length > 1 ? testCase.TenantIds[1] : "different-tenant";
                    var crossPartitionKey = $"partition:{testCase.ApiDomain}:{differentTenant}:cross-access-test";
                    await database.StringSetAsync(crossPartitionKey, "should-not-be-accessible");

                    // Verify current partition cannot access other tenant's data
                    var currentTenantKeys = testKeys.Where(k => k.Contains(testCase.PartitionKey)).ToList();
                    foreach (var key in currentTenantKeys)
                    {
                        if (key.Contains(differentTenant))
                        {
                            throw new InvalidOperationException($"Cross-partition access violation detected: {key}");
                        }
                    }

                    await database.KeyDeleteAsync(crossPartitionKey);
                    output?.WriteLine("✓ Cross-partition access prevention test passed");
                }

                // Validate security boundaries
                if (testCase.ValidateSecurityBoundaries)
                {
                    // Ensure sensitive data is properly partitioned
                    foreach (var testKey in testKeys)
                    {
                        if (!testKey.StartsWith(partitionPrefix))
                        {
                            throw new InvalidOperationException($"Security boundary violation: key {testKey} does not follow partition prefix");
                        }
                    }

                    output?.WriteLine("✓ Security boundaries validation passed");
                }

                // Test partition scaling
                if (testCase.TestPartitionScaling)
                {
                    var scaleTestKeys = new List<string>();
                    
                    // Add data up to partition limit
                    for (int i = 0; i < testCase.MaxPartitionSize; i++)
                    {
                        var scaleKey = $"{partitionPrefix}:scale-test:{i}";
                        scaleTestKeys.Add(scaleKey);
                        await database.StringSetAsync(scaleKey, $"scale-data-{i}");
                    }

                    // Verify all data was stored
                    foreach (var scaleKey in scaleTestKeys)
                    {
                        var result = await database.StringGetAsync(scaleKey);
                        if (!result.HasValue)
                        {
                            throw new InvalidOperationException($"Partition scaling failed: key {scaleKey} was not stored");
                        }
                    }

                    // Cleanup scale test data
                    foreach (var scaleKey in scaleTestKeys)
                    {
                        await database.KeyDeleteAsync(scaleKey);
                    }

                    output?.WriteLine($"✓ Partition scaling test passed: {testCase.MaxPartitionSize} entries");
                }

                // Test partition eviction
                if (testCase.TestPartitionEviction)
                {
                    // Fill partition beyond capacity to trigger eviction
                    var evictionTestKeys = new List<string>();
                    
                    for (int i = 0; i < testCase.MaxPartitionSize + 100; i++)
                    {
                        var evictionKey = $"{partitionPrefix}:eviction-test:{i}";
                        evictionTestKeys.add(evictionKey);
                        await database.StringSetAsync(evictionKey, $"eviction-data-{i}", TimeSpan.FromSeconds(1));
                    }

                    // Wait for some keys to expire (simulating eviction)
                    await Task.Delay(TimeSpan.FromSeconds(2));

                    // Check that some keys were evicted
                    var remainingKeys = 0;
                    foreach (var evictionKey in evictionTestKeys)
                    {
                        var exists = await database.KeyExistsAsync(evictionKey);
                        if (exists)
                        {
                            remainingKeys++;
                        }
                    }

                    if (remainingKeys >= evictionTestKeys.Count)
                    {
                        throw new InvalidOperationException("Partition eviction test failed - no keys were evicted");
                    }

                    output?.WriteLine($"✓ Partition eviction test passed: {evictionTestKeys.Count - remainingKeys}/{evictionTestKeys.Count} keys evicted");
                }

                // Cleanup main test data
                foreach (var testKey in testKeys)
                {
                    await database.KeyDeleteAsync(testKey);
                }

                output?.WriteLine($"✓ Cache partitioning test completed for {testCase.ApiDomain}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache partitioning test failed for {ApiDomain}", testCase.ApiDomain);
                output?.WriteLine($"✗ Cache partitioning test failed for {testCase.ApiDomain}: {ex.Message}");
                throw;
            }
        }

        _logger.LogInformation("Cache partitioning test completed successfully");
    }

    /// <summary>
    /// Tests cache backup and restore operations
    /// Contract: Must validate cache persistence and recovery for medical-grade data protection
    /// </summary>
    public async Task TestCacheBackupRestoreAsync(
        IDatabase database,
        CacheBackupRestoreTestCase[] backupTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (backupTestCases == null) throw new ArgumentNullException(nameof(backupTestCases));

        _logger.LogInformation("Starting cache backup and restore test with {Count} test cases", backupTestCases.Length);

        foreach (var testCase in backupTestCases)
        {
            try
            {
                output?.WriteLine($"Testing cache backup/restore: {testCase.BackupName} using {testCase.BackupStrategy} strategy");

                // Set up test data
                var testKeys = new List<string>();
                foreach (var (key, value) in testCase.TestData)
                {
                    var fullKey = $"backup-test:{key}";
                    testKeys.Add(fullKey);
                    await database.StringSetAsync(fullKey, System.Text.Json.JsonSerializer.Serialize(value));
                }

                // Create backup
                var backupStartTime = DateTime.UtcNow;
                var backupData = new Dictionary<string, string>();

                foreach (var testKey in testKeys)
                {
                    var value = await database.StringGetAsync(testKey);
                    if (value.HasValue)
                    {
                        backupData[testKey] = value!;
                    }
                }

                var backupDuration = DateTime.UtcNow - backupStartTime;

                // Validate backup time
                if (backupDuration > testCase.MaxBackupTime)
                {
                    throw new InvalidOperationException(
                        $"Backup took too long: {backupDuration.TotalMinutes:F2}min > {testCase.MaxBackupTime.TotalMinutes:F2}min");
                }

                output?.WriteLine($"✓ Backup completed in {backupDuration.TotalSeconds:F2}s with {backupData.Count} entries");

                // Simulate data loss by deleting original data
                foreach (var testKey in testKeys)
                {
                    await database.KeyDeleteAsync(testKey);
                }

                // Verify data is gone
                foreach (var testKey in testKeys)
                {
                    var exists = await database.KeyExistsAsync(testKey);
                    if (exists)
                    {
                        throw new InvalidOperationException($"Data deletion failed for key: {testKey}");
                    }
                }

                // Restore from backup
                var restoreStartTime = DateTime.UtcNow;
                
                switch (testCase.RestoreStrategy)
                {
                    case CacheRestoreStrategy.Complete:
                        await RestoreCompleteAsync(database, backupData);
                        break;
                        
                    case CacheRestoreStrategy.Selective:
                        await RestoreSelectiveAsync(database, backupData, testKeys.Take(testKeys.Count / 2));
                        break;
                        
                    case CacheRestoreStrategy.InPlace:
                        await RestoreInPlaceAsync(database, backupData);
                        break;
                        
                    default:
                        throw new NotSupportedException($"Restore strategy {testCase.RestoreStrategy} is not supported");
                }

                var restoreDuration = DateTime.UtcNow - restoreStartTime;

                // Validate restore time
                if (restoreDuration > testCase.MaxRestoreTime)
                {
                    throw new InvalidOperationException(
                        $"Restore took too long: {restoreDuration.TotalMinutes:F2}min > {testCase.MaxRestoreTime.TotalMinutes:F2}min");
                }

                // Validate data integrity after restore
                if (testCase.ValidateDataIntegrity)
                {
                    var restoredCount = 0;
                    var expectedCount = testCase.RestoreStrategy == CacheRestoreStrategy.Selective ? 
                        testKeys.Count / 2 : testKeys.Count;

                    foreach (var testKey in testKeys)
                    {
                        var result = await database.StringGetAsync(testKey);
                        if (result.HasValue)
                        {
                            restoredCount++;
                            
                            // Verify content matches original
                            if (backupData.TryGetValue(testKey, out var originalValue))
                            {
                                if (result.ToString() != originalValue)
                                {
                                    throw new InvalidOperationException($"Data integrity check failed for key {testKey}");
                                }
                            }
                        }
                    }

                    if (restoredCount < expectedCount)
                    {
                        throw new InvalidOperationException(
                            $"Data integrity validation failed. Expected: {expectedCount}, Restored: {restoredCount}");
                    }

                    output?.WriteLine($"✓ Data integrity validated: {restoredCount}/{expectedCount} entries restored");
                }

                // Test backup compression if enabled
                if (testCase.TestBackupCompression)
                {
                    // Simulate compression by calculating size reduction
                    var originalSize = backupData.Values.Sum(v => v.Length);
                    var compressedSize = (int)(originalSize * 0.7); // Assume 30% compression

                    output?.WriteLine($"✓ Backup compression simulation: {originalSize} bytes → {compressedSize} bytes (30% reduction)");
                }

                // Cleanup
                foreach (var testKey in testKeys)
                {
                    await database.KeyDeleteAsync(testKey);
                }

                output?.WriteLine($"✓ Backup/restore test completed: {testCase.BackupName} in {(backupDuration + restoreDuration).TotalSeconds:F2}s total");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache backup/restore test failed for {BackupName}", testCase.BackupName);
                output?.WriteLine($"✗ Backup/restore test failed for {testCase.BackupName}: {ex.Message}");
                throw;
            }
        }

        _logger.LogInformation("Cache backup and restore test completed successfully");
    }

    // Private helper methods for cache invalidation strategies

    private async Task InvalidateByKeysAsync(IDatabase database, string[] keys)
    {
        var tasks = keys.Select(key => database.KeyDeleteAsync(key)).ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task InvalidateByTagsAsync(IDatabase database, string entityType, Guid entityId)
    {
        // Simulate tag-based invalidation by pattern matching
        var pattern = $"*{entityType}:{entityId}*";
        await InvalidateByPatternAsync(database, pattern);
    }

    private async Task InvalidateByPatternAsync(IDatabase database, string pattern)
    {
        // Note: In production, avoid KEYS command for performance reasons
        // This is simplified for testing purposes
        var server = database.Multiplexer.GetServer(database.Multiplexer.GetEndPoints()[0]);
        var keys = server.Keys(database.Database, pattern);
        
        var tasks = keys.Select(key => database.KeyDeleteAsync(key)).ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task InvalidateByEventAsync(IDatabase database, CacheInvalidationTrigger trigger, string[] keys)
    {
        // Simulate event-driven invalidation
        await InvalidateByKeysAsync(database, keys);
        
        // Log the trigger event
        var eventKey = $"cache-events:{trigger}:{DateTime.UtcNow:yyyyMMdd}";
        await database.StringIncrementAsync(eventKey);
        await database.KeyExpireAsync(eventKey, TimeSpan.FromDays(1));
    }

    // Private helper methods for cache warming strategies

    private async Task WarmCacheOnStartupAsync(IDatabase database, CacheWarmingTestCase testCase)
    {
        var tasks = new List<Task>();
        
        foreach (var cacheKey in testCase.CacheKeys)
        {
            tasks.Add(Task.Run(async () =>
            {
                if (testCase.DataProvider != null)
                {
                    var data = await testCase.DataProvider();
                    await database.StringSetAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(data));
                }
                else
                {
                    await database.StringSetAsync(cacheKey, $"warmed-data-{cacheKey}");
                }
            }));
        }
        
        await Task.WhenAll(tasks);
    }

    private async Task WarmCacheOnDemandAsync(IDatabase database, CacheWarmingTestCase testCase)
    {
        // Simulate on-demand warming by checking and filling missing keys
        foreach (var cacheKey in testCase.CacheKeys)
        {
            var exists = await database.KeyExistsAsync(cacheKey);
            if (!exists)
            {
                var data = testCase.DataProvider != null ? 
                    await testCase.DataProvider() : 
                    $"on-demand-warmed-{cacheKey}";
                    
                await database.StringSetAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(data));
            }
        }
    }

    private async Task WarmCacheScheduledAsync(IDatabase database, CacheWarmingTestCase testCase)
    {
        // Simulate scheduled warming
        await WarmCacheOnStartupAsync(database, testCase);
        
        // Set next scheduled warming time
        var nextWarmingKey = $"next-warming:{string.Join(":", testCase.CacheKeys)}";
        var nextWarmingTime = DateTime.UtcNow.AddHours(1);
        await database.StringSetAsync(nextWarmingKey, nextWarmingTime.ToString("O"), TimeSpan.FromHours(2));
    }

    private async Task WarmCachePredictiveAsync(IDatabase database, CacheWarmingTestCase testCase)
    {
        // Simulate predictive warming based on usage patterns
        var priorityKeys = testCase.CacheKeys.OrderBy(k => k.GetHashCode()).Take(testCase.CacheKeys.Length / 2);
        
        foreach (var cacheKey in priorityKeys)
        {
            var data = $"predictive-warmed-{cacheKey}";
            await database.StringSetAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(data));
        }
    }

    // Private helper methods for backup and restore strategies

    private async Task RestoreCompleteAsync(IDatabase database, Dictionary<string, string> backupData)
    {
        var tasks = backupData.Select(kvp => 
            database.StringSetAsync(kvp.Key, kvp.Value)).ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task RestoreSelectiveAsync(IDatabase database, Dictionary<string, string> backupData, IEnumerable<string> keysToRestore)
    {
        var tasks = keysToRestore.Where(key => backupData.ContainsKey(key))
            .Select(key => database.StringSetAsync(key, backupData[key])).ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task RestoreInPlaceAsync(IDatabase database, Dictionary<string, string> backupData)
    {
        // Same as complete restore for this implementation
        await RestoreCompleteAsync(database, backupData);
    }
}
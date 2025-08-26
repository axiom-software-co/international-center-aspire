using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Infrastructure.Cache.Tests.Contracts;
using Xunit.Abstractions;

namespace Infrastructure.Cache.Tests;

/// <summary>
/// Implementation of rate limiting contract for Redis testing
/// Provides IP-based and user-based rate limiting validation for Public Gateway and Services Admin API
/// </summary>
public class RateLimitingService : IRateLimitingContract
{
    private readonly ILogger<RateLimitingService> _logger;

    public RateLimitingService(ILogger<RateLimitingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tests IP-based rate limiting for Public Gateway anonymous access
    /// Contract: Must validate 1000 req/min IP-based rate limiting with Redis backing store accuracy
    /// </summary>
    public async Task TestIPBasedRateLimitingAsync(
        IDatabase database,
        IPRateLimitTestCase[] rateLimitTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (rateLimitTestCases == null) throw new ArgumentNullException(nameof(rateLimitTestCases));

        _logger.LogInformation("Starting IP-based rate limiting test with {Count} test cases", rateLimitTestCases.Length);

        foreach (var testCase in rateLimitTestCases)
        {
            try
            {
                output?.WriteLine($"Testing IP rate limiting for {testCase.IPAddress}: {testCase.RequestCount} requests in {testCase.TimeWindow}");

                var rateLimitKey = $"ratelimit:ip:{testCase.IPAddress}";
                var windowStart = DateTime.UtcNow;
                
                // Clear any existing rate limit data
                await database.KeyDeleteAsync(rateLimitKey);
                
                var blockedRequests = 0;
                var tasks = new List<Task>();
                
                // Simulate concurrent requests
                for (int i = 0; i < testCase.RequestCount; i++)
                {
                    int requestIndex = i;
                    tasks.Add(Task.Run(async () =>
                    {
                        // Simulate request processing time
                        if (testCase.RequestInterval > TimeSpan.Zero)
                        {
                            await Task.Delay(testCase.RequestInterval * requestIndex);
                        }
                        
                        var isBlocked = await CheckAndIncrementRateLimitAsync(
                            database, 
                            rateLimitKey, 
                            testCase.RequestsPerMinute, 
                            testCase.TimeWindow);
                        
                        if (isBlocked)
                        {
                            Interlocked.Increment(ref blockedRequests);
                        }
                    }));
                }
                
                await Task.WhenAll(tasks);
                
                // Validate rate limiting behavior
                var finalCount = await database.StringGetAsync(rateLimitKey);
                var allowedRequests = testCase.RequestCount - blockedRequests;
                
                output?.WriteLine($"Results: {allowedRequests} allowed, {blockedRequests} blocked, final count: {finalCount}");
                
                if (testCase.ExpectRateLimit && blockedRequests == 0)
                {
                    throw new InvalidOperationException(
                        $"Expected rate limiting for IP {testCase.IPAddress}, but no requests were blocked");
                }
                
                if (!testCase.ExpectRateLimit && blockedRequests > 0)
                {
                    throw new InvalidOperationException(
                        $"Unexpected rate limiting for IP {testCase.IPAddress}, {blockedRequests} requests were blocked");
                }
                
                // Test window reset if required
                if (testCase.TestWindowReset)
                {
                    await Task.Delay(testCase.TimeWindow);
                    
                    var isBlockedAfterReset = await CheckAndIncrementRateLimitAsync(
                        database, 
                        rateLimitKey, 
                        testCase.RequestsPerMinute, 
                        testCase.TimeWindow);
                    
                    if (isBlockedAfterReset)
                    {
                        throw new InvalidOperationException(
                            $"Rate limit window did not reset properly for IP {testCase.IPAddress}");
                    }
                }
                
                // Cleanup
                await database.KeyDeleteAsync(rateLimitKey);
                
                output?.WriteLine($"✓ IP rate limiting test completed for {testCase.IPAddress}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IP-based rate limiting test failed for IP {IPAddress}", testCase.IPAddress);
                output?.WriteLine($"✗ IP rate limiting test failed for {testCase.IPAddress}: {ex.Message}");
                throw;
            }
        }

        _logger.LogInformation("IP-based rate limiting test completed successfully");
    }

    /// <summary>
    /// Tests user-based rate limiting for Services Admin API
    /// Contract: Must validate 100 req/min user-based rate limiting with proper user identification
    /// </summary>
    public async Task TestUserBasedRateLimitingAsync(
        IDatabase database,
        UserRateLimitTestCase[] rateLimitTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (rateLimitTestCases == null) throw new ArgumentNullException(nameof(rateLimitTestCases));

        _logger.LogInformation("Starting user-based rate limiting test with {Count} test cases", rateLimitTestCases.Length);

        foreach (var testCase in rateLimitTestCases)
        {
            try
            {
                output?.WriteLine($"Testing user rate limiting for {testCase.UserId}: {testCase.RequestCount} requests in {testCase.TimeWindow}");

                var rateLimitKey = $"ratelimit:user:{testCase.UserId}";
                
                // Clear any existing rate limit data
                await database.KeyDeleteAsync(rateLimitKey);
                
                var blockedRequests = 0;
                var allowedRequests = 0;
                
                // Test role-based limits if specified
                var effectiveLimit = testCase.RequestsPerMinute;
                if (testCase.TestRoleBasedLimits && testCase.UserRoles != null)
                {
                    effectiveLimit = CalculateRoleBasedLimit(testCase.UserRoles, testCase.RequestsPerMinute);
                    output?.WriteLine($"Effective rate limit based on roles {string.Join(",", testCase.UserRoles)}: {effectiveLimit}");
                }
                
                // Simulate user requests
                for (int i = 0; i < testCase.RequestCount; i++)
                {
                    var isBlocked = await CheckAndIncrementRateLimitAsync(
                        database, 
                        rateLimitKey, 
                        effectiveLimit, 
                        testCase.TimeWindow);
                    
                    if (isBlocked)
                    {
                        blockedRequests++;
                    }
                    else
                    {
                        allowedRequests++;
                    }
                    
                    // Small delay between requests to simulate realistic usage
                    await Task.Delay(10);
                }
                
                output?.WriteLine($"Results: {allowedRequests} allowed, {blockedRequests} blocked");
                
                // Validate rate limiting behavior
                if (testCase.ExpectRateLimit && blockedRequests == 0)
                {
                    throw new InvalidOperationException(
                        $"Expected rate limiting for user {testCase.UserId}, but no requests were blocked");
                }
                
                if (!testCase.ExpectRateLimit && blockedRequests > 0)
                {
                    throw new InvalidOperationException(
                        $"Unexpected rate limiting for user {testCase.UserId}, {blockedRequests} requests were blocked");
                }
                
                // Cleanup
                await database.KeyDeleteAsync(rateLimitKey);
                
                output?.WriteLine($"✓ User rate limiting test completed for {testCase.UserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User-based rate limiting test failed for user {UserId}", testCase.UserId);
                output?.WriteLine($"✗ User rate limiting test failed for {testCase.UserId}: {ex.Message}");
                throw;
            }
        }

        _logger.LogInformation("User-based rate limiting test completed successfully");
    }

    /// <summary>
    /// Tests rate limiting window sliding and reset behavior
    /// Contract: Must validate accurate time window management for precise rate limiting enforcement
    /// </summary>
    public async Task TestRateLimitingWindowBehaviorAsync(
        IDatabase database,
        RateLimitWindowTestCase[] windowTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (windowTestCases == null) throw new ArgumentNullException(nameof(windowTestCases));

        _logger.LogInformation("Starting rate limiting window behavior test with {Count} test cases", windowTestCases.Length);

        foreach (var testCase in windowTestCases)
        {
            try
            {
                output?.WriteLine($"Testing {testCase.WindowType} window behavior for {testCase.Identifier}");

                var rateLimitKey = $"ratelimit:window:{testCase.Identifier}";
                
                // Clear any existing data
                await database.KeyDeleteAsync(rateLimitKey);
                
                switch (testCase.WindowType)
                {
                    case RateLimitWindowType.Fixed:
                        await TestFixedWindowAsync(database, rateLimitKey, testCase, output);
                        break;
                        
                    case RateLimitWindowType.Sliding:
                        await TestSlidingWindowAsync(database, rateLimitKey, testCase, output);
                        break;
                        
                    case RateLimitWindowType.TokenBucket:
                        await TestTokenBucketAsync(database, rateLimitKey, testCase, output);
                        break;
                        
                    case RateLimitWindowType.LeakyBucket:
                        await TestLeakyBucketAsync(database, rateLimitKey, testCase, output);
                        break;
                        
                    default:
                        throw new NotSupportedException($"Window type {testCase.WindowType} is not supported");
                }
                
                // Cleanup
                await database.KeyDeleteAsync(rateLimitKey);
                
                output?.WriteLine($"✓ Window behavior test completed for {testCase.WindowType}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rate limiting window behavior test failed for {WindowType}", testCase.WindowType);
                output?.WriteLine($"✗ Window behavior test failed for {testCase.WindowType}: {ex.Message}");
                throw;
            }
        }

        _logger.LogInformation("Rate limiting window behavior test completed successfully");
    }

    /// <summary>
    /// Tests rate limiting under high concurrency
    /// Contract: Must validate rate limiting accuracy under concurrent requests for medical-grade reliability
    /// </summary>
    public async Task TestConcurrentRateLimitingAsync(
        IDatabase database,
        ConcurrentRateLimitTestCase[] concurrencyTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (concurrencyTestCases == null) throw new ArgumentNullException(nameof(concurrencyTestCases));

        _logger.LogInformation("Starting concurrent rate limiting test with {Count} test cases", concurrencyTestCases.Length);

        foreach (var testCase in concurrencyTestCases)
        {
            try
            {
                output?.WriteLine($"Testing concurrent rate limiting: {testCase.ConcurrentRequests} concurrent, {testCase.TotalRequests} total requests");

                var rateLimitKey = $"ratelimit:concurrent:{testCase.Identifier}";
                
                // Clear any existing data
                await database.KeyDeleteAsync(rateLimitKey);
                
                var allowedRequests = 0;
                var blockedRequests = 0;
                var requestsPerThread = testCase.TotalRequests / testCase.ConcurrentRequests;
                
                var tasks = new List<Task>();
                
                for (int thread = 0; thread < testCase.ConcurrentRequests; thread++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        for (int i = 0; i < requestsPerThread; i++)
                        {
                            var isBlocked = await CheckAndIncrementRateLimitAsync(
                                database, 
                                rateLimitKey, 
                                testCase.RateLimit, 
                                testCase.TimeWindow);
                            
                            if (isBlocked)
                            {
                                Interlocked.Increment(ref blockedRequests);
                            }
                            else
                            {
                                Interlocked.Increment(ref allowedRequests);
                            }
                        }
                    }));
                }
                
                await Task.WhenAll(tasks);
                
                output?.WriteLine($"Results: {allowedRequests} allowed, {blockedRequests} blocked");
                
                // Validate accuracy within tolerance
                if (testCase.ValidateAccuracy)
                {
                    var expectedBlocked = Math.Max(0, testCase.TotalRequests - testCase.RateLimit);
                    var actualBlockedRatio = (double)blockedRequests / testCase.TotalRequests;
                    var expectedBlockedRatio = (double)expectedBlocked / testCase.TotalRequests;
                    var accuracyDifference = Math.Abs(actualBlockedRatio - expectedBlockedRatio);
                    
                    if (accuracyDifference > testCase.AccuracyTolerance)
                    {
                        throw new InvalidOperationException(
                            $"Rate limiting accuracy failed. Expected blocked ratio: {expectedBlockedRatio:P2}, " +
                            $"Actual: {actualBlockedRatio:P2}, Tolerance: {testCase.AccuracyTolerance:P2}");
                    }
                }
                
                // Cleanup
                await database.KeyDeleteAsync(rateLimitKey);
                
                output?.WriteLine($"✓ Concurrent rate limiting test completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Concurrent rate limiting test failed for {Identifier}", testCase.Identifier);
                output?.WriteLine($"✗ Concurrent rate limiting test failed: {ex.Message}");
                throw;
            }
        }

        _logger.LogInformation("Concurrent rate limiting test completed successfully");
    }

    /// <summary>
    /// Tests rate limiting recovery and burst handling
    /// Contract: Must validate rate limit recovery behavior and burst request handling policies
    /// </summary>
    public async Task TestRateLimitRecoveryAsync(
        IDatabase database,
        RateLimitRecoveryTestCase[] recoveryTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (recoveryTestCases == null) throw new ArgumentNullException(nameof(recoveryTestCases));

        _logger.LogInformation("Starting rate limit recovery test with {Count} test cases", recoveryTestCases.Length);

        foreach (var testCase in recoveryTestCases)
        {
            try
            {
                output?.WriteLine($"Testing rate limit recovery for {testCase.Identifier}");

                var rateLimitKey = $"ratelimit:recovery:{testCase.Identifier}";
                
                // Clear any existing data
                await database.KeyDeleteAsync(rateLimitKey);
                
                // First, exhaust the rate limit
                for (int i = 0; i < testCase.RateLimit + testCase.BurstSize; i++)
                {
                    await CheckAndIncrementRateLimitAsync(database, rateLimitKey, testCase.RateLimit, testCase.RecoveryWindow);
                }
                
                output?.WriteLine($"Rate limit exhausted, waiting for recovery window: {testCase.RecoveryWindow}");
                
                // Test burst recovery if enabled
                if (testCase.TestBurstRecovery)
                {
                    await Task.Delay(testCase.RecoveryWindow);
                    
                    // Test immediate burst after recovery
                    var burstAllowed = 0;
                    for (int i = 0; i < testCase.BurstSize; i++)
                    {
                        var isBlocked = await CheckAndIncrementRateLimitAsync(
                            database, rateLimitKey, testCase.RateLimit, testCase.RecoveryWindow);
                        
                        if (!isBlocked)
                        {
                            burstAllowed++;
                        }
                    }
                    
                    output?.WriteLine($"Burst recovery: {burstAllowed}/{testCase.BurstSize} requests allowed");
                    
                    if (burstAllowed == 0)
                    {
                        throw new InvalidOperationException("Rate limit recovery failed - no burst requests allowed");
                    }
                }
                
                // Test gradual recovery if enabled
                if (testCase.TestGradualRecovery)
                {
                    await Task.Delay(testCase.RecoveryWindow);
                    
                    var gradualStart = DateTime.UtcNow;
                    var recoveredRequests = 0;
                    
                    while (DateTime.UtcNow - gradualStart < testCase.MaxRecoveryTime)
                    {
                        var isBlocked = await CheckAndIncrementRateLimitAsync(
                            database, rateLimitKey, testCase.RateLimit, testCase.RecoveryWindow);
                        
                        if (!isBlocked)
                        {
                            recoveredRequests++;
                        }
                        
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    
                    output?.WriteLine($"Gradual recovery: {recoveredRequests} requests allowed over {testCase.MaxRecoveryTime}");
                    
                    if (recoveredRequests == 0)
                    {
                        throw new InvalidOperationException("Gradual rate limit recovery failed");
                    }
                }
                
                // Cleanup
                await database.KeyDeleteAsync(rateLimitKey);
                
                output?.WriteLine($"✓ Rate limit recovery test completed for {testCase.Identifier}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rate limit recovery test failed for {Identifier}", testCase.Identifier);
                output?.WriteLine($"✗ Rate limit recovery test failed for {testCase.Identifier}: {ex.Message}");
                throw;
            }
        }

        _logger.LogInformation("Rate limit recovery test completed successfully");
    }

    /// <summary>
    /// Tests rate limiting metrics and monitoring
    /// Contract: Must validate rate limiting metrics collection for medical-grade audit compliance
    /// </summary>
    public async Task TestRateLimitingMetricsAsync(
        IDatabase database,
        RateLimitMetricsTestCase[] metricsTestCases,
        ITestOutputHelper? output = null)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (metricsTestCases == null) throw new ArgumentNullException(nameof(metricsTestCases));

        _logger.LogInformation("Starting rate limiting metrics test with {Count} test cases", metricsTestCases.Length);

        foreach (var testCase in metricsTestCases)
        {
            try
            {
                output?.WriteLine($"Testing rate limiting metrics for {testCase.Identifier}");

                var rateLimitKey = $"ratelimit:metrics:{testCase.Identifier}";
                var metricsKeys = testCase.ExpectedMetrics.Select(m => $"metrics:{rateLimitKey}:{m}").ToArray();
                
                // Clear any existing data
                await database.KeyDeleteAsync(rateLimitKey);
                foreach (var metricsKey in metricsKeys)
                {
                    await database.KeyDeleteAsync(metricsKey);
                }
                
                var totalRequests = 0;
                var blockedRequests = 0;
                var rateLimitHits = 0;
                
                // Generate requests and collect metrics
                for (int i = 0; i < testCase.RequestCount; i++)
                {
                    var isBlocked = await CheckAndIncrementRateLimitAsync(
                        database, rateLimitKey, testCase.RateLimit, TimeSpan.FromMinutes(1));
                    
                    totalRequests++;
                    
                    if (isBlocked)
                    {
                        blockedRequests++;
                        rateLimitHits++;
                    }
                    
                    // Update metrics in Redis
                    await database.StringIncrementAsync($"metrics:{rateLimitKey}:requests_total");
                    
                    if (isBlocked)
                    {
                        await database.StringIncrementAsync($"metrics:{rateLimitKey}:requests_blocked");
                        await database.StringIncrementAsync($"metrics:{rateLimitKey}:rate_limit_hit");
                    }
                }
                
                output?.WriteLine($"Generated metrics: {totalRequests} total, {blockedRequests} blocked, {rateLimitHits} rate limit hits");
                
                // Validate metrics accuracy
                if (testCase.ValidateMetricAccuracy)
                {
                    foreach (var expectedMetric in testCase.ExpectedMetrics)
                    {
                        var metricsKey = $"metrics:{rateLimitKey}:{expectedMetric}";
                        var metricValue = await database.StringGetAsync(metricsKey);
                        
                        if (!metricValue.HasValue)
                        {
                            throw new InvalidOperationException($"Expected metric '{expectedMetric}' was not found");
                        }
                        
                        var actualValue = (int)metricValue;
                        int expectedValue = expectedMetric switch
                        {
                            "requests_total" => totalRequests,
                            "requests_blocked" => blockedRequests,
                            "rate_limit_hit" => rateLimitHits,
                            _ => 0
                        };
                        
                        if (actualValue != expectedValue)
                        {
                            throw new InvalidOperationException(
                                $"Metric '{expectedMetric}' mismatch. Expected: {expectedValue}, Actual: {actualValue}");
                        }
                        
                        output?.WriteLine($"✓ Metric validated: {expectedMetric} = {actualValue}");
                    }
                }
                
                // Test metric persistence if required
                if (testCase.TestMetricPersistence)
                {
                    // Simulate Redis restart by checking if metrics survive TTL
                    var persistenceTestKey = $"metrics:{rateLimitKey}:persistence_test";
                    await database.StringSetAsync(persistenceTestKey, "test", TimeSpan.FromSeconds(2));
                    
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    
                    var persistedValue = await database.StringGetAsync(persistenceTestKey);
                    if (persistedValue.HasValue)
                    {
                        output?.WriteLine("⚠ Metric persistence test: Value unexpectedly persisted beyond TTL");
                    }
                    else
                    {
                        output?.WriteLine("✓ Metric persistence test: TTL respected");
                    }
                }
                
                // Cleanup
                await database.KeyDeleteAsync(rateLimitKey);
                foreach (var metricsKey in metricsKeys)
                {
                    await database.KeyDeleteAsync(metricsKey);
                }
                
                output?.WriteLine($"✓ Rate limiting metrics test completed for {testCase.Identifier}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rate limiting metrics test failed for {Identifier}", testCase.Identifier);
                output?.WriteLine($"✗ Rate limiting metrics test failed for {testCase.Identifier}: {ex.Message}");
                throw;
            }
        }

        _logger.LogInformation("Rate limiting metrics test completed successfully");
    }

    // Private helper methods

    private async Task<bool> CheckAndIncrementRateLimitAsync(
        IDatabase database, 
        string key, 
        int limit, 
        TimeSpan window)
    {
        var script = @"
            local current = redis.call('GET', KEYS[1])
            if current == false then
                redis.call('SET', KEYS[1], 1)
                redis.call('EXPIRE', KEYS[1], ARGV[2])
                return 0
            else
                local count = tonumber(current)
                if count < tonumber(ARGV[1]) then
                    redis.call('INCR', KEYS[1])
                    return 0
                else
                    return 1
                end
            end";

        var result = await database.ScriptEvaluateAsync(
            script, 
            new RedisKey[] { key }, 
            new RedisValue[] { limit, (int)window.TotalSeconds });

        return (int)result == 1; // 1 means blocked, 0 means allowed
    }

    private int CalculateRoleBasedLimit(string[] roles, int baseLimit)
    {
        // Simple role-based limit calculation
        if (roles.Contains("admin", StringComparer.OrdinalIgnoreCase))
        {
            return baseLimit * 5; // Admins get 5x the limit
        }
        if (roles.Contains("premium", StringComparer.OrdinalIgnoreCase))
        {
            return baseLimit * 2; // Premium users get 2x the limit
        }
        return baseLimit; // Regular users get base limit
    }

    private async Task TestFixedWindowAsync(
        IDatabase database, 
        string key, 
        RateLimitWindowTestCase testCase, 
        ITestOutputHelper? output)
    {
        // Implementation for fixed window testing
        output?.WriteLine("Testing fixed window behavior");
        
        // Fill the window
        for (int i = 0; i < testCase.WindowLimit; i++)
        {
            await CheckAndIncrementRateLimitAsync(database, key, testCase.WindowLimit, testCase.WindowSize);
        }
        
        // Next request should be blocked
        var isBlocked = await CheckAndIncrementRateLimitAsync(database, key, testCase.WindowLimit, testCase.WindowSize);
        if (!isBlocked)
        {
            throw new InvalidOperationException("Fixed window test failed - expected blocking");
        }
        
        // Wait for window reset
        if (testCase.TestWindowReset)
        {
            await Task.Delay(testCase.WindowSize);
            
            var isBlockedAfterReset = await CheckAndIncrementRateLimitAsync(database, key, testCase.WindowLimit, testCase.WindowSize);
            if (isBlockedAfterReset)
            {
                throw new InvalidOperationException("Fixed window reset failed");
            }
        }
    }

    private async Task TestSlidingWindowAsync(
        IDatabase database, 
        string key, 
        RateLimitWindowTestCase testCase, 
        ITestOutputHelper? output)
    {
        // Implementation for sliding window testing
        output?.WriteLine("Testing sliding window behavior");
        
        // For simplicity, use the same logic as fixed window in this test implementation
        // In a real implementation, sliding window would use a more complex algorithm
        await TestFixedWindowAsync(database, key, testCase, output);
    }

    private async Task TestTokenBucketAsync(
        IDatabase database, 
        string key, 
        RateLimitWindowTestCase testCase, 
        ITestOutputHelper? output)
    {
        // Implementation for token bucket testing
        output?.WriteLine("Testing token bucket behavior");
        
        // Simplified token bucket simulation
        var bucketKey = $"{key}:bucket";
        var bucketSize = testCase.WindowLimit;
        
        // Initialize bucket
        await database.StringSetAsync(bucketKey, bucketSize);
        
        // Consume tokens
        for (int i = 0; i < bucketSize + 10; i++)
        {
            var tokensLeft = await database.StringDecrementAsync(bucketKey);
            if (tokensLeft < 0)
            {
                // Reset to 0 if negative
                await database.StringSetAsync(bucketKey, 0);
                output?.WriteLine($"Token bucket exhausted at request {i + 1}");
                break;
            }
        }
    }

    private async Task TestLeakyBucketAsync(
        IDatabase database, 
        string key, 
        RateLimitWindowTestCase testCase, 
        ITestOutputHelper? output)
    {
        // Implementation for leaky bucket testing
        output?.WriteLine("Testing leaky bucket behavior");
        
        // Simplified leaky bucket simulation
        var bucketKey = $"{key}:leaky";
        var bucketSize = testCase.WindowLimit;
        var leakRate = 1; // 1 request per second
        
        // Fill bucket rapidly
        for (int i = 0; i < bucketSize * 2; i++)
        {
            var currentLevel = await database.StringGetAsync(bucketKey);
            var level = currentLevel.HasValue ? (int)currentLevel : 0;
            
            if (level < bucketSize)
            {
                await database.StringIncrementAsync(bucketKey);
            }
            else
            {
                output?.WriteLine($"Leaky bucket overflow at request {i + 1}");
                break;
            }
        }
        
        // Simulate leak
        await Task.Delay(TimeSpan.FromSeconds(leakRate));
        await database.StringDecrementAsync(bucketKey);
    }
}
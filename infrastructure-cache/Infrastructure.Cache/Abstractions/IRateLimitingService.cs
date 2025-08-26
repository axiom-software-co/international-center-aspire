namespace Infrastructure.Cache.Abstractions;

/// <summary>
/// CONTRACT: Generic rate limiting service interface for protecting APIs and resources.
/// 
/// TDD PRINCIPLE: Interface drives the design of rate limiting architecture
/// DEPENDENCY INVERSION: Abstractions for variable rate limiting concerns
/// INFRASTRUCTURE: Generic rate limiting patterns reusable by any domain
/// DOMAIN AGNOSTIC: No knowledge of Services, News, Events, or any specific domain
/// </summary>
public interface IRateLimitingService
{
    /// <summary>
    /// CONTRACT: Check if request is allowed under rate limit
    /// 
    /// PRECONDITION: Valid client identifier and rate limit configuration
    /// POSTCONDITION: Returns rate limit result with current state
    /// INFRASTRUCTURE: Generic rate limiting for any domain
    /// PROTECTION: API and resource protection from abuse
    /// </summary>
    /// <param name="clientId">Unique client identifier (IP, user ID, etc.)</param>
    /// <param name="resource">Resource being accessed</param>
    /// <param name="limit">Maximum requests allowed in time window</param>
    /// <param name="window">Time window for rate limiting</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Rate limiting result with current state</returns>
    Task<RateLimitResult> IsRequestAllowedAsync(
        string clientId, 
        string resource, 
        int limit, 
        TimeSpan window, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Check rate limit with custom policy
    /// 
    /// PRECONDITION: Valid client identifier and rate limit policy
    /// POSTCONDITION: Returns rate limit result based on policy
    /// INFRASTRUCTURE: Flexible rate limiting with custom policies
    /// POLICY-DRIVEN: Support for different rate limiting algorithms
    /// </summary>
    /// <param name="clientId">Unique client identifier</param>
    /// <param name="policy">Rate limiting policy configuration</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Rate limiting result with policy application</returns>
    Task<RateLimitResult> CheckRateLimitAsync(
        string clientId, 
        RateLimitPolicy policy, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Record request attempt for rate limiting
    /// 
    /// PRECONDITION: Valid client identifier and resource
    /// POSTCONDITION: Request recorded in rate limiting store
    /// INFRASTRUCTURE: Request tracking for rate limit calculations
    /// ATOMIC: Thread-safe request counting
    /// </summary>
    /// <param name="clientId">Unique client identifier</param>
    /// <param name="resource">Resource being accessed</param>
    /// <param name="cost">Optional cost/weight of the request (default 1)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if request was recorded</returns>
    Task<bool> RecordRequestAsync(
        string clientId, 
        string resource, 
        int cost = 1, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Get current rate limit status for client
    /// 
    /// PRECONDITION: Valid client identifier and resource
    /// POSTCONDITION: Returns current rate limit state
    /// INFRASTRUCTURE: Rate limit status inquiry for any domain
    /// MONITORING: Current rate limit state monitoring
    /// </summary>
    /// <param name="clientId">Unique client identifier</param>
    /// <param name="resource">Resource being accessed</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Current rate limit status</returns>
    Task<RateLimitStatus> GetRateLimitStatusAsync(
        string clientId, 
        string resource, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Reset rate limit for client
    /// 
    /// PRECONDITION: Valid client identifier and resource
    /// POSTCONDITION: Rate limit state reset for client
    /// INFRASTRUCTURE: Rate limit reset capability for any domain
    /// ADMINISTRATION: Manual rate limit management
    /// </summary>
    /// <param name="clientId">Unique client identifier</param>
    /// <param name="resource">Resource being accessed</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if rate limit was reset</returns>
    Task<bool> ResetRateLimitAsync(
        string clientId, 
        string resource, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Get rate limiting statistics and metrics
    /// 
    /// POSTCONDITION: Returns rate limiting metrics for monitoring
    /// OBSERVABILITY: Rate limiting performance monitoring
    /// INFRASTRUCTURE: Generic rate limiting metrics for any domain
    /// </summary>
    /// <returns>Rate limiting statistics and metrics</returns>
    RateLimitingStatistics GetRateLimitingStatistics();

    /// <summary>
    /// CONTRACT: Clean up expired rate limit entries
    /// 
    /// POSTCONDITION: Expired entries removed from rate limiting store
    /// INFRASTRUCTURE: Maintenance for rate limiting data
    /// CLEANUP: Automatic cleanup of stale rate limit data
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Number of entries cleaned up</returns>
    Task<int> CleanupExpiredEntriesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Rate limiting result for request evaluation.
/// INFRASTRUCTURE: Generic rate limit result for any domain
/// </summary>
public sealed class RateLimitResult
{
    /// <summary>Whether the request is allowed</summary>
    public required bool IsAllowed { get; init; }
    
    /// <summary>Current request count in window</summary>
    public required int CurrentCount { get; init; }
    
    /// <summary>Maximum requests allowed in window</summary>
    public required int Limit { get; init; }
    
    /// <summary>Time until rate limit window resets</summary>
    public required TimeSpan ResetTime { get; init; }
    
    /// <summary>Time to wait before retry (if not allowed)</summary>
    public TimeSpan? RetryAfter { get; init; }
    
    /// <summary>Rate limit policy applied</summary>
    public string? PolicyName { get; init; }
    
    /// <summary>Additional rate limit metadata</summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>Evaluation timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>Create allowed result</summary>
    public static RateLimitResult Allowed(int currentCount, int limit, TimeSpan resetTime, string? policyName = null) =>
        new()
        {
            IsAllowed = true,
            CurrentCount = currentCount,
            Limit = limit,
            ResetTime = resetTime,
            PolicyName = policyName
        };
    
    /// <summary>Create denied result</summary>
    public static RateLimitResult Denied(int currentCount, int limit, TimeSpan resetTime, TimeSpan retryAfter, string? policyName = null) =>
        new()
        {
            IsAllowed = false,
            CurrentCount = currentCount,
            Limit = limit,
            ResetTime = resetTime,
            RetryAfter = retryAfter,
            PolicyName = policyName
        };
}

/// <summary>
/// Rate limiting policy configuration.
/// INFRASTRUCTURE: Generic rate limiting policy for any domain
/// </summary>
public sealed class RateLimitPolicy
{
    /// <summary>Policy name for identification</summary>
    public required string Name { get; init; }
    
    /// <summary>Resource pattern this policy applies to</summary>
    public required string ResourcePattern { get; init; }
    
    /// <summary>Maximum requests allowed</summary>
    public required int Limit { get; init; }
    
    /// <summary>Time window for rate limiting</summary>
    public required TimeSpan Window { get; init; }
    
    /// <summary>Rate limiting algorithm</summary>
    public RateLimitAlgorithm Algorithm { get; init; } = RateLimitAlgorithm.SlidingWindow;
    
    /// <summary>Burst allowance for requests</summary>
    public int? BurstAllowance { get; init; }
    
    /// <summary>Custom properties for policy</summary>
    public Dictionary<string, object> Properties { get; init; } = new();
}

/// <summary>
/// Rate limiting status for monitoring.
/// INFRASTRUCTURE: Rate limit status for any domain
/// </summary>
public sealed class RateLimitStatus
{
    /// <summary>Client identifier</summary>
    public required string ClientId { get; init; }
    
    /// <summary>Resource being accessed</summary>
    public required string Resource { get; init; }
    
    /// <summary>Current request count</summary>
    public required int CurrentCount { get; init; }
    
    /// <summary>Rate limit configured</summary>
    public required int Limit { get; init; }
    
    /// <summary>Time window for rate limiting</summary>
    public required TimeSpan Window { get; init; }
    
    /// <summary>Time until window resets</summary>
    public required TimeSpan ResetTime { get; init; }
    
    /// <summary>Whether client is currently rate limited</summary>
    public required bool IsLimited { get; init; }
    
    /// <summary>First request timestamp in current window</summary>
    public DateTime? FirstRequestTime { get; init; }
    
    /// <summary>Last request timestamp</summary>
    public DateTime? LastRequestTime { get; init; }
    
    /// <summary>Status timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Rate limiting algorithm types.
/// INFRASTRUCTURE: Rate limiting algorithm options
/// </summary>
public enum RateLimitAlgorithm
{
    /// <summary>Fixed time window algorithm</summary>
    FixedWindow = 0,
    
    /// <summary>Sliding time window algorithm</summary>
    SlidingWindow = 1,
    
    /// <summary>Token bucket algorithm</summary>
    TokenBucket = 2,
    
    /// <summary>Leaky bucket algorithm</summary>
    LeakyBucket = 3
}

/// <summary>
/// Rate limiting statistics for monitoring and observability.
/// INFRASTRUCTURE: Generic rate limiting metrics for any domain
/// </summary>
public sealed class RateLimitingStatistics
{
    /// <summary>Total requests evaluated</summary>
    public long TotalRequests { get; init; }
    
    /// <summary>Number of allowed requests</summary>
    public long AllowedRequests { get; init; }
    
    /// <summary>Number of denied requests</summary>
    public long DeniedRequests { get; init; }
    
    /// <summary>Allow ratio (percentage)</summary>
    public double AllowRatio { get; init; }
    
    /// <summary>Number of unique clients</summary>
    public int UniqueClients { get; init; }
    
    /// <summary>Number of active rate limit entries</summary>
    public int ActiveEntries { get; init; }
    
    /// <summary>Average evaluation time in milliseconds</summary>
    public double AverageEvaluationTimeMs { get; init; }
    
    /// <summary>Number of cleanup operations performed</summary>
    public int CleanupOperations { get; init; }
    
    /// <summary>Number of entries cleaned up</summary>
    public long EntriesCleanedUp { get; init; }
    
    /// <summary>Statistics timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
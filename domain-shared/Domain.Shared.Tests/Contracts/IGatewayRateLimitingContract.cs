namespace InternationalCenter.Tests.Shared.Contracts;

/// <summary>
/// Contract interface for gateway rate limiting behavior testing
/// Ensures proper rate limiting enforcement with differential policies
/// Contract-first testing approach without knowledge of concrete implementations
/// Public Gateway: Higher limits for website usage (1000/min)
/// Admin Gateway: Strict limits for medical compliance (100/min)
/// </summary>
public interface IGatewayRateLimitingContract
{
    /// <summary>
    /// Contract test: Verify gateway applies appropriate rate limiting policy
    /// Public Gateway: IP-based rate limiting with higher limits
    /// Admin Gateway: User-based rate limiting with strict limits
    /// </summary>
    Task VerifyRateLimitingContract_WithNormalUsage_AllowsRequests();
    
    /// <summary>
    /// Contract test: Verify gateway blocks requests that exceed rate limits
    /// </summary>
    Task VerifyRateLimitingContract_WithExcessiveRequests_ReturnsRateLimited();
    
    /// <summary>
    /// Contract test: Verify gateway adds rate limiting headers to responses
    /// </summary>
    Task VerifyRateLimitingContract_WithAnyRequest_AddsRateLimitHeaders();
    
    /// <summary>
    /// Contract test: Verify gateway uses Redis backing store for distributed rate limiting
    /// </summary>
    Task VerifyRateLimitingContract_WithDistributedGateways_SynchronizesLimits();
    
    /// <summary>
    /// Contract test: Verify gateway rate limiting resets correctly after time window
    /// </summary>
    Task VerifyRateLimitingContract_AfterTimeWindow_ResetsLimits();
    
    /// <summary>
    /// Contract test: Verify gateway rate limiting partitioning strategy
    /// Public Gateway: By IP address
    /// Admin Gateway: By user ID
    /// </summary>
    Task VerifyRateLimitingContract_WithPartitioning_IsolatesCorrectly();
    
    /// <summary>
    /// Contract test: Verify gateway logs rate limiting violations for audit
    /// </summary>
    Task VerifyRateLimitingContract_WithViolation_LogsAuditTrail();
    
    /// <summary>
    /// Contract test: Verify gateway provides rate limiting metrics endpoint
    /// </summary>
    Task VerifyRateLimitingContract_WithMetricsRequest_ProvidesStatistics();
    
    /// <summary>
    /// Contract test: Verify gateway handles Redis connection failures gracefully
    /// </summary>
    Task VerifyRateLimitingContract_WithRedisFailure_FallsBackGracefully();
    
    /// <summary>
    /// Contract test: Verify gateway rate limiting performance under load
    /// </summary>
    Task VerifyRateLimitingContract_WithHighLoad_MaintainsPerformance();
}
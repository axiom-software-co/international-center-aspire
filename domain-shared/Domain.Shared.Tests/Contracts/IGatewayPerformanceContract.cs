namespace InternationalCenter.Tests.Shared.Contracts;

/// <summary>
/// Contract interface for gateway performance behavior testing
/// Ensures minimal latency overhead and proper load balancing
/// Contract-first testing approach without knowledge of concrete implementations
/// Medical-grade systems require consistent performance under varying loads
/// </summary>
public interface IGatewayPerformanceContract
{
    /// <summary>
    /// Contract test: Verify gateway adds minimal latency overhead to API calls
    /// Target: <100ms additional latency for 95th percentile
    /// </summary>
    Task VerifyPerformanceContract_WithNormalLoad_MaintainsLowLatency();
    
    /// <summary>
    /// Contract test: Verify gateway handles concurrent requests efficiently
    /// </summary>
    Task VerifyPerformanceContract_WithConcurrentRequests_MaintainsPerformance();
    
    /// <summary>
    /// Contract test: Verify gateway memory usage stays within reasonable bounds
    /// </summary>
    Task VerifyPerformanceContract_WithExtendedUsage_MaintainsMemoryBounds();
    
    /// <summary>
    /// Contract test: Verify gateway connection pooling works efficiently
    /// </summary>
    Task VerifyPerformanceContract_WithConnectionPooling_ReusesConnections();
    
    /// <summary>
    /// Contract test: Verify gateway handles large request bodies efficiently
    /// </summary>
    Task VerifyPerformanceContract_WithLargeRequests_StreamsEfficiently();
    
    /// <summary>
    /// Contract test: Verify gateway responds to health checks quickly
    /// Target: <50ms for health check endpoints
    /// </summary>
    Task VerifyPerformanceContract_WithHealthCheck_RespondsQuickly();
    
    /// <summary>
    /// Contract test: Verify gateway graceful shutdown doesn't drop requests
    /// </summary>
    Task VerifyPerformanceContract_WithGracefulShutdown_CompletesInFlightRequests();
    
    /// <summary>
    /// Contract test: Verify gateway startup time meets requirements
    /// Target: <30 seconds for full initialization
    /// </summary>
    Task VerifyPerformanceContract_WithStartup_InitializesWithinTimeLimit();
    
    /// <summary>
    /// Contract test: Verify gateway load balancing distributes requests evenly
    /// </summary>
    Task VerifyPerformanceContract_WithLoadBalancing_DistributesEvenly();
    
    /// <summary>
    /// Contract test: Verify gateway circuit breaker protects against cascade failures
    /// </summary>
    Task VerifyPerformanceContract_WithCircuitBreaker_PreventsPointOfFailure();
    
    /// <summary>
    /// Contract test: Verify gateway caching improves response times
    /// </summary>
    Task VerifyPerformanceContract_WithCaching_ImprovesResponseTimes();
    
    /// <summary>
    /// Contract test: Verify gateway performance degrades gracefully under stress
    /// </summary>
    Task VerifyPerformanceContract_WithStressLoad_DegradesGracefully();
}
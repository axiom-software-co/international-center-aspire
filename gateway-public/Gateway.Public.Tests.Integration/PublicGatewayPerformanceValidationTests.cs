using InternationalCenter.Tests.Shared.Contracts;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace InternationalCenter.Gateway.Public.Tests.Integration;

/// <summary>
/// Performance validation tests for Public Gateway focusing on latency overhead and load balancing
/// Ensures minimal performance impact from gateway routing to Services Public API
/// Validates that gateway adds <100ms latency and handles concurrent requests efficiently
/// Medical-grade performance testing for public website integration patterns
/// </summary>
public class PublicGatewayPerformanceValidationTests : PublicGatewayContractTestBase
{
    private readonly ConcurrentBag<long> _latencyMeasurements = new();
    private readonly ConcurrentBag<(DateTime timestamp, HttpStatusCode status)> _requestResults = new();
    
    public PublicGatewayPerformanceValidationTests(ITestOutputHelper output) : base(output)
    {
    }
    
    #region Latency Overhead Validation
    
    /// <summary>
    /// Validate that Public Gateway adds minimal latency overhead compared to direct API access
    /// Target: Gateway overhead should be <100ms for 95th percentile
    /// </summary>
    [Fact]
    public async Task ValidateLatencyOverhead_WithGatewayRouting_ShouldAddMinimalLatency()
    {
        await InitializeDistributedApplicationAsync();
        ValidateServicesApiScope(ServicesApiBasePath);
        
        const int iterations = 20;
        var gatewayLatencies = new List<long>();
        var directApiLatencies = new List<long>();
        
        // Measure gateway routing latency
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = await CreateAuthenticatedRequest(ServicesApiBasePath, HttpMethod.Get);
            var response = await GatewayClient!.SendAsync(request);
            stopwatch.Stop();
            
            gatewayLatencies.Add(stopwatch.ElapsedMilliseconds);
            
            // Small delay between requests
            await Task.Delay(50);
        }
        
        // Measure direct API access latency (if available)
        if (ServicesApiClient != null)
        {
            for (int i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await ServicesApiClient.GetAsync(ServicesApiBasePath);
                stopwatch.Stop();
                
                directApiLatencies.Add(stopwatch.ElapsedMilliseconds);
                
                await Task.Delay(50);
            }
        }
        
        // Calculate performance metrics
        var gatewayP95 = gatewayLatencies.OrderBy(x => x).ElementAt((int)(iterations * 0.95));
        var gatewayAvg = gatewayLatencies.Average();
        var gatewayMax = gatewayLatencies.Max();
        
        // Performance validation
        Assert.True(gatewayP95 < 100, $"Gateway 95th percentile latency ({gatewayP95}ms) exceeds 100ms target");
        Assert.True(gatewayAvg < 50, $"Gateway average latency ({gatewayAvg:F1}ms) exceeds 50ms target");
        
        // Compare with direct API if available
        if (directApiLatencies.Any())
        {
            var directP95 = directApiLatencies.OrderBy(x => x).ElementAt((int)(iterations * 0.95));
            var overhead = gatewayP95 - directP95;
            
            Output.WriteLine($"📊 LATENCY ANALYSIS:");
            Output.WriteLine($"  Gateway: Avg {gatewayAvg:F1}ms, P95 {gatewayP95}ms, Max {gatewayMax}ms");
            Output.WriteLine($"  Direct:  Avg {directApiLatencies.Average():F1}ms, P95 {directP95}ms, Max {directApiLatencies.Max()}ms");
            Output.WriteLine($"  Overhead: {overhead}ms (P95)");
            
            Assert.True(overhead < 100, $"Gateway overhead ({overhead}ms) exceeds 100ms limit");
        }
        else
        {
            Output.WriteLine($"📊 GATEWAY LATENCY: Avg {gatewayAvg:F1}ms, P95 {gatewayP95}ms, Max {gatewayMax}ms");
        }
        
        Output.WriteLine("✅ PERFORMANCE: Public Gateway maintains minimal latency overhead");
    }
    
    /// <summary>
    /// Validate latency consistency under varying request patterns
    /// </summary>
    [Fact]
    public async Task ValidateLatencyConsistency_WithVariableLoad_ShouldMaintainStablePerformance()
    {
        await InitializeDistributedApplicationAsync();
        ValidateServicesApiScope(ServicesApiBasePath);
        
        var loadPatterns = new[]
        {
            ("Low Load", 1, 500), // 1 request every 500ms
            ("Medium Load", 5, 100), // 5 requests every 100ms  
            ("High Load", 10, 50), // 10 requests every 50ms
            ("Burst Load", 20, 25) // 20 requests every 25ms
        };
        
        foreach (var (patternName, requestCount, intervalMs) in loadPatterns)
        {
            var latencies = new List<long>();
            
            for (int batch = 0; batch < 3; batch++) // 3 batches per pattern
            {
                var tasks = new List<Task<long>>();
                
                for (int i = 0; i < requestCount; i++)
                {
                    tasks.Add(MeasureRequestLatency());
                    await Task.Delay(intervalMs / requestCount);
                }
                
                var batchLatencies = await Task.WhenAll(tasks);
                latencies.AddRange(batchLatencies);
            }
            
            var avgLatency = latencies.Average();
            var maxLatency = latencies.Max();
            var p95Latency = latencies.OrderBy(x => x).ElementAt((int)(latencies.Count * 0.95));
            
            // Performance validation for each load pattern
            Assert.True(avgLatency < 100, $"{patternName}: Average latency ({avgLatency:F1}ms) exceeds 100ms");
            Assert.True(p95Latency < 200, $"{patternName}: P95 latency ({p95Latency}ms) exceeds 200ms");
            
            Output.WriteLine($"📊 {patternName}: Avg {avgLatency:F1}ms, P95 {p95Latency}ms, Max {maxLatency}ms");
        }
        
        Output.WriteLine("✅ PERFORMANCE: Latency remains consistent across variable load patterns");
    }
    
    private async Task<long> MeasureRequestLatency()
    {
        var stopwatch = Stopwatch.StartNew();
        var request = await CreateAuthenticatedRequest(ServicesApiBasePath, HttpMethod.Get);
        var response = await GatewayClient!.SendAsync(request);
        stopwatch.Stop();
        
        return stopwatch.ElapsedMilliseconds;
    }
    
    #endregion
    
    #region Concurrent Performance Validation
    
    /// <summary>
    /// Validate gateway performance under concurrent request load
    /// Target: Handle 100 concurrent requests with <500ms average latency
    /// </summary>
    [Fact]
    public async Task ValidateConcurrentPerformance_With100ConcurrentRequests_ShouldMaintainPerformance()
    {
        await InitializeDistributedApplicationAsync();
        ValidateServicesApiScope(ServicesApiBasePath);
        
        const int concurrentRequests = 100;
        var stopwatch = Stopwatch.StartNew();
        
        // Create concurrent request tasks
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(async i =>
            {
                var requestStopwatch = Stopwatch.StartNew();
                try
                {
                    var request = await CreateAuthenticatedRequest(ServicesApiBasePath, HttpMethod.Get);
                    request.Headers.Add("X-Test-Request-Id", i.ToString());
                    
                    var response = await GatewayClient!.SendAsync(request);
                    requestStopwatch.Stop();
                    
                    _latencyMeasurements.Add(requestStopwatch.ElapsedMilliseconds);
                    _requestResults.Add((DateTime.UtcNow, response.StatusCode));
                    
                    return new { RequestId = i, Latency = requestStopwatch.ElapsedMilliseconds, Status = response.StatusCode };
                }
                catch (Exception ex)
                {
                    requestStopwatch.Stop();
                    _requestResults.Add((DateTime.UtcNow, HttpStatusCode.InternalServerError));
                    
                    Output.WriteLine($"Request {i} failed: {ex.Message}");
                    return new { RequestId = i, Latency = requestStopwatch.ElapsedMilliseconds, Status = HttpStatusCode.InternalServerError };
                }
            })
            .ToArray();
        
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Analyze concurrent performance
        var latencies = _latencyMeasurements.ToArray();
        var requestResults = _requestResults.ToArray();
        
        var successfulRequests = requestResults.Count(r => r.status.IsSuccessStatusCode());
        var failedRequests = requestResults.Count(r => !r.status.IsSuccessStatusCode());
        
        var avgLatency = latencies.Average();
        var p95Latency = latencies.OrderBy(x => x).ElementAt((int)(latencies.Length * 0.95));
        var maxLatency = latencies.Max();
        var throughput = (double)concurrentRequests / stopwatch.ElapsedMilliseconds * 1000; // requests per second
        
        // Performance assertions
        Assert.True(avgLatency < 500, $"Average latency ({avgLatency:F1}ms) under concurrent load exceeds 500ms");
        Assert.True(p95Latency < 1000, $"P95 latency ({p95Latency}ms) under concurrent load exceeds 1000ms");
        Assert.True(successfulRequests >= concurrentRequests * 0.95, $"Success rate ({successfulRequests}/{concurrentRequests}) below 95%");
        
        Output.WriteLine($"📊 CONCURRENT PERFORMANCE ({concurrentRequests} requests):");
        Output.WriteLine($"  Total Time: {stopwatch.ElapsedMilliseconds}ms");
        Output.WriteLine($"  Throughput: {throughput:F1} req/sec");
        Output.WriteLine($"  Success Rate: {successfulRequests}/{concurrentRequests} ({(double)successfulRequests/concurrentRequests*100:F1}%)");
        Output.WriteLine($"  Latency: Avg {avgLatency:F1}ms, P95 {p95Latency}ms, Max {maxLatency}ms");
        
        Output.WriteLine("✅ PERFORMANCE: Gateway handles concurrent load efficiently");
    }
    
    /// <summary>
    /// Validate performance degradation patterns under increasing load
    /// </summary>
    [Fact]
    public async Task ValidatePerformanceDegradation_WithIncreasingLoad_ShouldDegradeGracefully()
    {
        await InitializeDistributedApplicationAsync();
        ValidateServicesApiScope(ServicesApiBasePath);
        
        var loadLevels = new[] { 10, 25, 50, 75, 100 };
        var performanceMetrics = new List<(int load, double avgLatency, double throughput, double successRate)>();
        
        foreach (var load in loadLevels)
        {
            // Clear previous measurements
            _latencyMeasurements.Clear();
            _requestResults.Clear();
            
            var stopwatch = Stopwatch.StartNew();
            
            // Execute load test
            var tasks = Enumerable.Range(0, load)
                .Select(async i =>
                {
                    var requestStopwatch = Stopwatch.StartNew();
                    try
                    {
                        var request = await CreateAuthenticatedRequest(ServicesApiBasePath, HttpMethod.Get);
                        var response = await GatewayClient!.SendAsync(request);
                        requestStopwatch.Stop();
                        
                        _latencyMeasurements.Add(requestStopwatch.ElapsedMilliseconds);
                        _requestResults.Add((DateTime.UtcNow, response.StatusCode));
                        
                        return response.StatusCode;
                    }
                    catch
                    {
                        requestStopwatch.Stop();
                        _latencyMeasurements.Add(requestStopwatch.ElapsedMilliseconds);
                        _requestResults.Add((DateTime.UtcNow, HttpStatusCode.InternalServerError));
                        
                        return HttpStatusCode.InternalServerError;
                    }
                })
                .ToArray();
            
            await Task.WhenAll(tasks);
            stopwatch.Stop();
            
            // Calculate metrics
            var avgLatency = _latencyMeasurements.Average();
            var throughput = (double)load / stopwatch.ElapsedMilliseconds * 1000;
            var successRate = (double)_requestResults.Count(r => r.status.IsSuccessStatusCode()) / load;
            
            performanceMetrics.Add((load, avgLatency, throughput, successRate));
            
            Output.WriteLine($"📊 Load Level {load}: Avg Latency {avgLatency:F1}ms, Throughput {throughput:F1} req/sec, Success {successRate*100:F1}%");
            
            // Brief pause between load levels
            await Task.Delay(1000);
        }
        
        // Validate graceful degradation
        for (int i = 1; i < performanceMetrics.Count; i++)
        {
            var current = performanceMetrics[i];
            var previous = performanceMetrics[i - 1];
            
            // Latency should not increase dramatically (>3x)
            var latencyIncrease = current.avgLatency / previous.avgLatency;
            Assert.True(latencyIncrease < 3.0, $"Latency increased {latencyIncrease:F1}x from {previous.load} to {current.load} requests");
            
            // Success rate should remain reasonable (>80% even under high load)
            Assert.True(current.successRate > 0.8, $"Success rate dropped to {current.successRate*100:F1}% at load level {current.load}");
        }
        
        Output.WriteLine("✅ PERFORMANCE: Gateway degrades gracefully under increasing load");
    }
    
    #endregion
    
    #region Throughput Validation
    
    /// <summary>
    /// Validate maximum sustained throughput for Public Gateway
    /// Target: >500 requests per second sustained throughput
    /// </summary>
    [Fact]
    public async Task ValidateSustainedThroughput_WithContinuousLoad_ShouldMaintainHighThroughput()
    {
        await InitializeDistributedApplicationAsync();
        ValidateServicesApiScope(ServicesApiBasePath);
        
        const int testDurationSeconds = 30;
        const int targetRps = 500;
        var testEndTime = DateTime.UtcNow.AddSeconds(testDurationSeconds);
        
        var requestCounter = 0;
        var successCounter = 0;
        var errorCounter = 0;
        var latencies = new ConcurrentBag<long>();
        
        // Start continuous load generation
        var loadTasks = new List<Task>();
        for (int worker = 0; worker < Environment.ProcessorCount; worker++)
        {
            loadTasks.Add(Task.Run(async () =>
            {
                while (DateTime.UtcNow < testEndTime)
                {
                    try
                    {
                        var stopwatch = Stopwatch.StartNew();
                        var request = await CreateAuthenticatedRequest(ServicesApiBasePath, HttpMethod.Get);
                        var response = await GatewayClient!.SendAsync(request);
                        stopwatch.Stop();
                        
                        Interlocked.Increment(ref requestCounter);
                        latencies.Add(stopwatch.ElapsedMilliseconds);
                        
                        if (response.IsSuccessStatusCode)
                            Interlocked.Increment(ref successCounter);
                        else
                            Interlocked.Increment(ref errorCounter);
                    }
                    catch
                    {
                        Interlocked.Increment(ref requestCounter);
                        Interlocked.Increment(ref errorCounter);
                    }
                    
                    // Brief delay to prevent overwhelming
                    await Task.Delay(1);
                }
            }));
        }
        
        // Monitor performance during test
        var monitoringTask = Task.Run(async () =>
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow < testEndTime)
            {
                await Task.Delay(5000); // Report every 5 seconds
                
                var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                var currentRps = requestCounter / elapsed;
                var successRate = requestCounter > 0 ? (double)successCounter / requestCounter : 0;
                
                Output.WriteLine($"📊 {elapsed:F0}s: {currentRps:F0} RPS, {successRate*100:F1}% success, {requestCounter} total requests");
            }
        });
        
        // Wait for test completion
        await Task.WhenAll(loadTasks.Concat(new[] { monitoringTask }));
        
        // Final analysis
        var finalRps = (double)requestCounter / testDurationSeconds;
        var finalSuccessRate = (double)successCounter / requestCounter;
        var avgLatency = latencies.Any() ? latencies.Average() : 0;
        var p95Latency = latencies.Any() ? latencies.OrderBy(x => x).ElementAt((int)(latencies.Count * 0.95)) : 0;
        
        // Throughput validation
        Assert.True(finalRps > targetRps * 0.8, $"Sustained throughput ({finalRps:F0} RPS) below 80% of target ({targetRps} RPS)");
        Assert.True(finalSuccessRate > 0.95, $"Success rate ({finalSuccessRate*100:F1}%) below 95%");
        Assert.True(avgLatency < 100, $"Average latency ({avgLatency:F1}ms) under sustained load exceeds 100ms");
        
        Output.WriteLine($"📊 SUSTAINED THROUGHPUT RESULTS:");
        Output.WriteLine($"  Duration: {testDurationSeconds}s");
        Output.WriteLine($"  Total Requests: {requestCounter}");
        Output.WriteLine($"  Throughput: {finalRps:F0} RPS");
        Output.WriteLine($"  Success Rate: {finalSuccessRate*100:F1}%");
        Output.WriteLine($"  Latency: Avg {avgLatency:F1}ms, P95 {p95Latency}ms");
        
        Output.WriteLine("✅ PERFORMANCE: Gateway maintains high sustained throughput");
    }
    
    #endregion
    
    #region Resource Utilization Validation
    
    /// <summary>
    /// Validate memory usage remains stable under load
    /// </summary>
    [Fact]
    public async Task ValidateMemoryUsage_UnderLoad_ShouldRemainStable()
    {
        await InitializeDistributedApplicationAsync();
        ValidateServicesApiScope(ServicesApiBasePath);
        
        // Get baseline memory usage
        var process = Process.GetCurrentProcess();
        var initialMemory = process.WorkingSet64;
        
        // Generate load for memory testing
        const int requestCount = 500;
        var tasks = Enumerable.Range(0, requestCount)
            .Select(async i =>
            {
                var request = await CreateAuthenticatedRequest(ServicesApiBasePath, HttpMethod.Get);
                return await GatewayClient!.SendAsync(request);
            })
            .ToArray();
        
        await Task.WhenAll(tasks);
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Check memory usage after load
        var finalMemory = process.WorkingSet64;
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreaseMB = memoryIncrease / 1024.0 / 1024.0;
        
        // Memory validation - should not increase dramatically
        Assert.True(memoryIncreaseMB < 100, $"Memory increased by {memoryIncreaseMB:F1}MB, exceeds 100MB limit");
        
        Output.WriteLine($"📊 MEMORY USAGE:");
        Output.WriteLine($"  Initial: {initialMemory/1024.0/1024.0:F1}MB");
        Output.WriteLine($"  Final: {finalMemory/1024.0/1024.0:F1}MB");  
        Output.WriteLine($"  Increase: {memoryIncreaseMB:F1}MB");
        
        Output.WriteLine("✅ PERFORMANCE: Memory usage remains stable under load");
    }
    
    #endregion
    
    #region Health Check Performance
    
    /// <summary>
    /// Validate health check endpoint performance
    /// Target: Health checks should respond in <50ms
    /// </summary>
    [Fact]
    public async Task ValidateHealthCheckPerformance_WithRepeatedChecks_ShouldRespondQuickly()
    {
        await InitializeDistributedApplicationAsync();
        
        const int healthCheckCount = 50;
        var latencies = new List<long>();
        
        for (int i = 0; i < healthCheckCount; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await GatewayClient!.GetAsync("/health");
            stopwatch.Stop();
            
            latencies.Add(stopwatch.ElapsedMilliseconds);
            
            // Verify health check succeeded
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // Small delay between checks
            await Task.Delay(10);
        }
        
        var avgLatency = latencies.Average();
        var maxLatency = latencies.Max();
        var p95Latency = latencies.OrderBy(x => x).ElementAt((int)(latencies.Count * 0.95));
        
        // Health check performance validation
        Assert.True(avgLatency < 50, $"Health check average latency ({avgLatency:F1}ms) exceeds 50ms");
        Assert.True(p95Latency < 100, $"Health check P95 latency ({p95Latency}ms) exceeds 100ms");
        
        Output.WriteLine($"📊 HEALTH CHECK PERFORMANCE:");
        Output.WriteLine($"  Checks: {healthCheckCount}");
        Output.WriteLine($"  Avg: {avgLatency:F1}ms, P95: {p95Latency}ms, Max: {maxLatency}ms");
        
        Output.WriteLine("✅ PERFORMANCE: Health checks respond quickly and consistently");
    }
    
    #endregion
}
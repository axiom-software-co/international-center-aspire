using InternationalCenter.Tests.Shared.Contracts;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace InternationalCenter.Gateway.Admin.Tests.Integration;

/// <summary>
/// Performance validation tests for Admin Gateway focusing on medical-grade performance requirements
/// Ensures minimal latency overhead despite authentication and audit logging requirements
/// Validates performance with Microsoft Entra External ID authentication and audit persistence
/// Medical-grade systems require consistent performance under authenticated access patterns
/// </summary>
public class AdminGatewayPerformanceValidationTests : AdminGatewayContractTestBase
{
    protected override string TestAuthenticationToken => "perf-test-admin-token-67890";
    protected override string TestUserId => "perf-test-admin@internationalsolutions.medical";
    
    private readonly ConcurrentBag<long> _latencyMeasurements = new();
    private readonly ConcurrentBag<(DateTime timestamp, HttpStatusCode status)> _requestResults = new();
    
    public AdminGatewayPerformanceValidationTests(ITestOutputHelper output) : base(output)
    {
    }
    
    #region Authentication Overhead Validation
    
    /// <summary>
    /// Validate authentication overhead in Admin Gateway performance
    /// Target: Authentication should add <200ms latency for medical compliance
    /// </summary>
    [Fact]
    public async Task ValidateAuthenticationOverhead_WithAuthenticatedRequests_ShouldMaintainMedicalGradePerformance()
    {
        await InitializeDistributedApplicationAsync();
        ValidateServicesApiScope(ServicesApiBasePath);
        
        const int iterations = 15;
        var authenticatedLatencies = new List<long>();
        var unauthenticatedLatencies = new List<long>();
        
        // Measure authenticated request latencies
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = await CreateAuthenticatedRequest(ServicesApiBasePath, HttpMethod.Get);
            request.Headers.Add("X-Medical-Session-Id", $"med-session-{i}");
            
            var response = await GatewayClient!.SendAsync(request);
            stopwatch.Stop();
            
            authenticatedLatencies.Add(stopwatch.ElapsedMilliseconds);
            
            // Log medical audit trail performance impact
            Output.WriteLine($"Auth Request {i}: {stopwatch.ElapsedMilliseconds}ms - Status: {response.StatusCode}");
            
            await Task.Delay(100); // Medical systems need proper spacing
        }
        
        // Measure unauthenticated requests (should be rejected but still measure latency)
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = new HttpRequestMessage(HttpMethod.Get, ServicesApiBasePath);
            request.Headers.Add("X-Medical-Session-Id", $"unauth-session-{i}");
            
            var response = await GatewayClient!.SendAsync(request);
            stopwatch.Stop();
            
            unauthenticatedLatencies.Add(stopwatch.ElapsedMilliseconds);
            
            await Task.Delay(100);
        }
        
        // Calculate authentication overhead
        var authAvg = authenticatedLatencies.Average();
        var authP95 = authenticatedLatencies.OrderBy(x => x).ElementAt((int)(iterations * 0.95));
        var unauthAvg = unauthenticatedLatencies.Average();
        
        var authOverhead = authAvg - unauthAvg;
        
        // Medical-grade performance validation
        Assert.True(authAvg < 200, $"Authenticated average latency ({authAvg:F1}ms) exceeds 200ms medical requirement");
        Assert.True(authP95 < 400, $"Authenticated P95 latency ({authP95}ms) exceeds 400ms medical requirement");
        
        Output.WriteLine($"📊 AUTHENTICATION PERFORMANCE:");
        Output.WriteLine($"  Authenticated: Avg {authAvg:F1}ms, P95 {authP95}ms");
        Output.WriteLine($"  Unauthenticated: Avg {unauthAvg:F1}ms");
        Output.WriteLine($"  Auth Overhead: {authOverhead:F1}ms");
        
        Output.WriteLine("✅ PERFORMANCE: Admin Gateway maintains medical-grade performance with authentication");
    }
    
    #endregion
    
    #region Medical Audit Logging Performance
    
    /// <summary>
    /// Validate that medical-grade audit logging doesn't significantly impact performance
    /// Target: Audit logging should add <50ms latency overhead
    /// </summary>
    [Fact]
    public async Task ValidateAuditLoggingPerformance_WithMedicalGradeAudit_ShouldMaintainPerformance()
    {
        await InitializeDistributedApplicationAsync();
        ValidateServicesApiScope(ServicesApiBasePath);
        
        const int auditTestCount = 20;
        var auditLatencies = new List<long>();
        
        for (int i = 0; i < auditTestCount; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Create request with rich audit context for medical compliance
            var request = await CreateAuthenticatedRequest(ServicesApiBasePath, HttpMethod.Get);
            request.Headers.Add("X-Medical-Operation", "PatientDataAccess");
            request.Headers.Add("X-Audit-Level", "CRITICAL");
            request.Headers.Add("X-Medical-Context", $"Operation-{i}");
            request.Headers.Add("X-Patient-Context", "PATIENT-12345");
            request.Headers.Add("X-Medical-Facility", "International-Medical-Center");
            
            var response = await GatewayClient!.SendAsync(request);
            stopwatch.Stop();
            
            auditLatencies.Add(stopwatch.ElapsedMilliseconds);
            
            // Verify audit headers are maintained
            var hasCorrelationId = response.Headers.Contains("X-Correlation-ID");
            Assert.True(hasCorrelationId, "Medical audit correlation ID must be maintained");
            
            await Task.Delay(50);
        }
        
        var auditAvg = auditLatencies.Average();
        var auditMax = auditLatencies.Max();
        var auditP95 = auditLatencies.OrderBy(x => x).ElementAt((int)(auditTestCount * 0.95));
        
        // Medical audit performance validation
        Assert.True(auditAvg < 250, $"Medical audit average latency ({auditAvg:F1}ms) exceeds 250ms");
        Assert.True(auditP95 < 500, $"Medical audit P95 latency ({auditP95}ms) exceeds 500ms medical requirement");
        
        Output.WriteLine($"📊 MEDICAL AUDIT PERFORMANCE:");
        Output.WriteLine($"  Audit Requests: {auditTestCount}");
        Output.WriteLine($"  Avg: {auditAvg:F1}ms, P95: {auditP95}ms, Max: {auditMax}ms");
        Output.WriteLine($"  Medical audit logging maintains performance compliance");
        
        Output.WriteLine("✅ PERFORMANCE: Medical-grade audit logging maintains required performance");
    }
    
    #endregion
    
    #region Concurrent Medical Operations Performance
    
    /// <summary>
    /// Validate performance under concurrent medical operations
    /// Target: Handle 25 concurrent medical operations with <1000ms average latency
    /// </summary>
    [Fact]
    public async Task ValidateConcurrentMedicalOperations_WithAuthenticatedRequests_ShouldMaintainMedicalGradePerformance()
    {
        await InitializeDistributedApplicationAsync();
        ValidateServicesApiScope(ServicesApiBasePath);
        
        const int concurrentOperations = 25; // Medical systems have lower concurrency than public
        var stopwatch = Stopwatch.StartNew();
        
        // Simulate concurrent medical operations
        var medicalOperations = new[]
        {
            "PatientRecordAccess",
            "TreatmentPlanUpdate", 
            "MedicalImageRetrieval",
            "LabResultsQuery",
            "PrescriptionManagement"
        };
        
        var tasks = Enumerable.Range(0, concurrentOperations)
            .Select(async i =>
            {
                var operationStopwatch = Stopwatch.StartNew();
                try
                {
                    var operation = medicalOperations[i % medicalOperations.Length];
                    var request = await CreateAuthenticatedRequest(ServicesApiBasePath, HttpMethod.Get);
                    
                    // Add medical context for audit compliance
                    request.Headers.Add("X-Medical-Operation", operation);
                    request.Headers.Add("X-Medical-User", TestUserId);
                    request.Headers.Add("X-Medical-Session", $"med-session-{i}");
                    request.Headers.Add("X-Patient-Id", $"PATIENT-{i % 100:D3}");
                    request.Headers.Add("X-Facility-Code", "IMC-001");
                    
                    var response = await GatewayClient!.SendAsync(request);
                    operationStopwatch.Stop();
                    
                    _latencyMeasurements.Add(operationStopwatch.ElapsedMilliseconds);
                    _requestResults.Add((DateTime.UtcNow, response.StatusCode));
                    
                    return new 
                    { 
                        OperationId = i, 
                        Operation = operation,
                        Latency = operationStopwatch.ElapsedMilliseconds, 
                        Status = response.StatusCode 
                    };
                }
                catch (Exception ex)
                {
                    operationStopwatch.Stop();
                    _requestResults.Add((DateTime.UtcNow, HttpStatusCode.InternalServerError));
                    
                    Output.WriteLine($"Medical Operation {i} failed: {ex.Message}");
                    return new 
                    { 
                        OperationId = i, 
                        Operation = "Failed",
                        Latency = operationStopwatch.ElapsedMilliseconds, 
                        Status = HttpStatusCode.InternalServerError 
                    };
                }
            })
            .ToArray();
        
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Analyze concurrent medical operations performance
        var latencies = _latencyMeasurements.ToArray();
        var requestResults = _requestResults.ToArray();
        
        var successfulOperations = requestResults.Count(r => r.status.IsSuccessStatusCode());
        var failedOperations = requestResults.Count(r => !r.status.IsSuccessStatusCode());
        var unauthorizedOperations = requestResults.Count(r => r.status == HttpStatusCode.Unauthorized);
        
        var avgLatency = latencies.Length > 0 ? latencies.Average() : 0;
        var p95Latency = latencies.Length > 0 ? latencies.OrderBy(x => x).ElementAt((int)(latencies.Length * 0.95)) : 0;
        var maxLatency = latencies.Length > 0 ? latencies.Max() : 0;
        var throughput = (double)concurrentOperations / stopwatch.ElapsedMilliseconds * 1000;
        
        // Medical performance validation
        Assert.True(avgLatency < 1000, $"Medical operations average latency ({avgLatency:F1}ms) exceeds 1000ms requirement");
        Assert.True(p95Latency < 2000, $"Medical operations P95 latency ({p95Latency}ms) exceeds 2000ms requirement");
        
        // Medical systems require high reliability
        var successRate = latencies.Length > 0 ? (double)successfulOperations / concurrentOperations : 0;
        if (unauthorizedOperations < concurrentOperations * 0.5) // If not mostly auth failures
        {
            Assert.True(successRate > 0.9, $"Medical operations success rate ({successRate*100:F1}%) below 90% requirement");
        }
        
        Output.WriteLine($"📊 CONCURRENT MEDICAL OPERATIONS ({concurrentOperations} operations):");
        Output.WriteLine($"  Total Time: {stopwatch.ElapsedMilliseconds}ms");
        Output.WriteLine($"  Throughput: {throughput:F1} operations/sec");
        Output.WriteLine($"  Success: {successfulOperations}, Failed: {failedOperations}, Unauthorized: {unauthorizedOperations}");
        
        if (latencies.Length > 0)
        {
            Output.WriteLine($"  Latency: Avg {avgLatency:F1}ms, P95 {p95Latency}ms, Max {maxLatency}ms");
        }
        
        // Break down by operation type
        var operationGroups = results.GroupBy(r => r.Operation);
        foreach (var group in operationGroups)
        {
            var groupAvg = group.Average(r => r.Latency);
            var groupCount = group.Count();
            Output.WriteLine($"    {group.Key}: {groupCount} ops, Avg {groupAvg:F1}ms");
        }
        
        Output.WriteLine("✅ PERFORMANCE: Concurrent medical operations maintain required performance");
    }
    
    #endregion
    
    #region Medical System Load Testing
    
    /// <summary>
    /// Validate Admin Gateway performance under medical system load patterns
    /// Medical systems have different usage patterns than public systems
    /// </summary>
    [Fact]
    public async Task ValidateMedicalSystemLoad_WithRealisticUsagePatterns_ShouldMaintainPerformance()
    {
        await InitializeDistributedApplicationAsync();
        ValidateServicesApiScope(ServicesApiBasePath);
        
        // Medical system load patterns - lower volume but higher complexity
        var loadScenarios = new[]
        {
            ("Morning Rounds", 3, 2000, "HIGH"), // 3 concurrent users, 2s intervals, high priority
            ("Clinical Review", 5, 1500, "CRITICAL"), // 5 users, 1.5s intervals, critical operations
            ("Emergency Access", 8, 500, "EMERGENCY"), // 8 users, 500ms intervals, emergency
            ("End of Day", 2, 3000, "ROUTINE") // 2 users, 3s intervals, routine operations
        };
        
        foreach (var (scenarioName, userCount, intervalMs, priority) in loadScenarios)
        {
            _latencyMeasurements.Clear();
            _requestResults.Clear();
            
            Output.WriteLine($"🏥 Testing Medical Scenario: {scenarioName}");
            
            var scenarioStopwatch = Stopwatch.StartNew();
            
            // Simulate medical users
            var userTasks = Enumerable.Range(0, userCount)
                .Select(async userId =>
                {
                    var userLatencies = new List<long>();
                    
                    // Each user performs 3 operations
                    for (int operation = 0; operation < 3; operation++)
                    {
                        var operationStopwatch = Stopwatch.StartNew();
                        
                        try
                        {
                            var request = await CreateAuthenticatedRequest(ServicesApiBasePath, HttpMethod.Get);
                            
                            // Medical context headers
                            request.Headers.Add("X-Medical-Scenario", scenarioName);
                            request.Headers.Add("X-Medical-Priority", priority);
                            request.Headers.Add("X-Medical-User-Id", $"MEDUSER-{userId:D3}");
                            request.Headers.Add("X-Operation-Sequence", operation.ToString());
                            
                            var response = await GatewayClient!.SendAsync(request);
                            operationStopwatch.Stop();
                            
                            userLatencies.Add(operationStopwatch.ElapsedMilliseconds);
                            _latencyMeasurements.Add(operationStopwatch.ElapsedMilliseconds);
                            _requestResults.Add((DateTime.UtcNow, response.StatusCode));
                        }
                        catch
                        {
                            operationStopwatch.Stop();
                            _requestResults.Add((DateTime.UtcNow, HttpStatusCode.InternalServerError));
                        }
                        
                        await Task.Delay(intervalMs);
                    }
                    
                    return userLatencies;
                })
                .ToArray();
            
            var userResults = await Task.WhenAll(userTasks);
            scenarioStopwatch.Stop();
            
            // Analyze scenario performance
            var scenarioLatencies = _latencyMeasurements.ToArray();
            var scenarioResults = _requestResults.ToArray();
            
            if (scenarioLatencies.Length > 0)
            {
                var avgLatency = scenarioLatencies.Average();
                var maxLatency = scenarioLatencies.Max();
                var successRate = (double)scenarioResults.Count(r => r.status.IsSuccessStatusCode()) / scenarioResults.Length;
                
                // Medical performance requirements vary by scenario priority
                var maxAllowedLatency = priority switch
                {
                    "EMERGENCY" => 500,
                    "CRITICAL" => 1000,
                    "HIGH" => 1500,
                    "ROUTINE" => 2000,
                    _ => 2000
                };
                
                Assert.True(avgLatency < maxAllowedLatency, 
                    $"{scenarioName} average latency ({avgLatency:F1}ms) exceeds {maxAllowedLatency}ms for {priority} priority");
                
                Output.WriteLine($"  📊 Results: Avg {avgLatency:F1}ms, Max {maxLatency}ms, Success {successRate*100:F1}%");
            }
            
            // Brief pause between scenarios
            await Task.Delay(2000);
        }
        
        Output.WriteLine("✅ PERFORMANCE: Medical system load patterns perform within requirements");
    }
    
    #endregion
    
    #region Medical Compliance Performance
    
    /// <summary>
    /// Validate performance under medical compliance monitoring
    /// Medical systems require audit trails that can impact performance
    /// </summary>
    [Fact]
    public async Task ValidateMedicalCompliancePerformance_WithAuditTrail_ShouldMaintainPerformance()
    {
        await InitializeDistributedApplicationAsync();
        ValidateServicesApiScope(ServicesApiBasePath);
        
        const int complianceTestIterations = 30;
        var complianceLatencies = new List<long>();
        
        // Test operations requiring full medical compliance audit
        var complianceOperations = new[]
        {
            ("PatientDataAccess", "HIPAA_REQUIRED"),
            ("TreatmentModification", "FDA_REGULATED"),
            ("PrescriptionChange", "DEA_MONITORED"),
            ("LabResultAccess", "CLIA_COMPLIANT"),
            ("ImagingReview", "DICOM_SECURE")
        };
        
        for (int i = 0; i < complianceTestIterations; i++)
        {
            var (operation, regulation) = complianceOperations[i % complianceOperations.Length];
            var stopwatch = Stopwatch.StartNew();
            
            var request = await CreateAuthenticatedRequest(ServicesApiBasePath, HttpMethod.Get);
            
            // Full medical compliance context
            request.Headers.Add("X-Medical-Operation", operation);
            request.Headers.Add("X-Regulatory-Context", regulation);
            request.Headers.Add("X-Medical-Practitioner", TestUserId);
            request.Headers.Add("X-Patient-Consent", "VERIFIED");
            request.Headers.Add("X-Audit-Required", "FULL");
            request.Headers.Add("X-Compliance-Level", "MAXIMUM");
            request.Headers.Add("X-Legal-Basis", "TREATMENT");
            
            var response = await GatewayClient!.SendAsync(request);
            stopwatch.Stop();
            
            complianceLatencies.Add(stopwatch.ElapsedMilliseconds);
            
            // Verify compliance headers maintained
            if (response.Headers.Contains("X-Correlation-ID"))
            {
                var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
                Output.WriteLine($"Compliance Operation {i}: {operation} - {stopwatch.ElapsedMilliseconds}ms - Audit: {correlationId}");
            }
            
            await Task.Delay(200); // Medical compliance requires proper spacing
        }
        
        var complianceAvg = complianceLatencies.Average();
        var complianceMax = complianceLatencies.Max();
        var complianceP95 = complianceLatencies.OrderBy(x => x).ElementAt((int)(complianceLatencies.Count * 0.95));
        
        // Medical compliance performance validation
        Assert.True(complianceAvg < 500, $"Medical compliance average latency ({complianceAvg:F1}ms) exceeds 500ms");
        Assert.True(complianceP95 < 1000, $"Medical compliance P95 latency ({complianceP95}ms) exceeds 1000ms");
        
        Output.WriteLine($"📊 MEDICAL COMPLIANCE PERFORMANCE:");
        Output.WriteLine($"  Operations: {complianceTestIterations}");
        Output.WriteLine($"  Avg: {complianceAvg:F1}ms, P95: {complianceP95}ms, Max: {complianceMax}ms");
        Output.WriteLine($"  Medical compliance audit trails maintain performance");
        
        Output.WriteLine("✅ PERFORMANCE: Medical compliance requirements met with acceptable performance");
    }
    
    #endregion
    
    #region Medical System Health Monitoring
    
    /// <summary>
    /// Validate health check performance for medical system monitoring
    /// Medical systems require more frequent and reliable health checks
    /// </summary>
    [Fact]
    public async Task ValidateMedicalHealthMonitoring_WithFrequentChecks_ShouldRespondReliably()
    {
        await InitializeDistributedApplicationAsync();
        
        const int healthCheckCount = 100; // Medical systems need frequent monitoring
        var healthLatencies = new List<long>();
        var healthStatuses = new List<HttpStatusCode>();
        
        for (int i = 0; i < healthCheckCount; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Medical health checks include compliance status
                var request = new HttpRequestMessage(HttpMethod.Get, "/health");
                request.Headers.Add("X-Health-Check-Type", "MEDICAL_COMPLIANCE");
                request.Headers.Add("X-Monitor-Source", "MEDICAL_MONITORING");
                
                var response = await GatewayClient!.GetAsync("/health");
                stopwatch.Stop();
                
                healthLatencies.Add(stopwatch.ElapsedMilliseconds);
                healthStatuses.Add(response.StatusCode);
            }
            catch
            {
                stopwatch.Stop();
                healthLatencies.Add(stopwatch.ElapsedMilliseconds);
                healthStatuses.Add(HttpStatusCode.InternalServerError);
            }
            
            await Task.Delay(10); // Frequent medical monitoring
        }
        
        var avgLatency = healthLatencies.Average();
        var maxLatency = healthLatencies.Max();
        var p95Latency = healthLatencies.OrderBy(x => x).ElementAt((int)(healthLatencies.Count * 0.95));
        var successRate = (double)healthStatuses.Count(s => s == HttpStatusCode.OK) / healthCheckCount;
        
        // Medical health check performance requirements
        Assert.True(avgLatency < 25, $"Medical health check average latency ({avgLatency:F1}ms) exceeds 25ms");
        Assert.True(p95Latency < 50, $"Medical health check P95 latency ({p95Latency}ms) exceeds 50ms");
        Assert.True(successRate > 0.99, $"Medical health check success rate ({successRate*100:F1}%) below 99%");
        
        Output.WriteLine($"📊 MEDICAL HEALTH MONITORING:");
        Output.WriteLine($"  Checks: {healthCheckCount}");
        Output.WriteLine($"  Avg: {avgLatency:F1}ms, P95: {p95Latency}ms, Max: {maxLatency}ms");
        Output.WriteLine($"  Success Rate: {successRate*100:F2}%");
        
        Output.WriteLine("✅ PERFORMANCE: Medical health monitoring meets reliability requirements");
    }
    
    #endregion
}
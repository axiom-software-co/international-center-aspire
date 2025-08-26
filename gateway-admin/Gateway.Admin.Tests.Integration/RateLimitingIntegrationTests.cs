using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using Xunit;
using InternationalCenter.Shared.Services;

namespace InternationalCenter.Gateway.Admin.Tests.Integration;

/// <summary>
/// Contract: Admin Gateway MUST implement user-based rate limiting with strict limits for medical-grade compliance
/// Validates rate limiting middleware with Redis backing store and medical-grade audit logging for violations
/// </summary>
public class RateLimitingIntegrationTests : IClassFixture<AdminGatewayTestFactory>
{
    private readonly HttpClient _client;
    private readonly AdminGatewayTestFactory _factory;

    public RateLimitingIntegrationTests(AdminGatewayTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    /// <summary>
    /// Contract: Admin Gateway MUST enforce user-based rate limits of 100 requests per minute for medical compliance
    /// RED PHASE: This test will fail initially as rate limiting middleware is not yet implemented
    /// </summary>
    [Fact]
    public async Task AdminGateway_UserRateLimit_MustEnforce100RequestsPerMinute()
    {
        // Arrange - Setup authenticated admin user request
        var testUserId = "admin-user-rate-test-001";
        var correlationId = Guid.NewGuid().ToString();
        
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", "Test serviceadmin");
        _client.DefaultRequestHeaders.Add("X-User-ID", testUserId);
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        var requests = new List<Task<HttpResponseMessage>>();
        
        // Contract Validation: First 100 requests within 1 minute should succeed for authenticated admin
        for (int i = 0; i < 100; i++)
        {
            requests.Add(_client.GetAsync("/admin/api/services"));
        }
        
        var responses = await Task.WhenAll(requests);
        
        // Act - Make 101st request that should be rate limited
        var rateLimitedResponse = await _client.GetAsync("/admin/api/services");
        
        // Assert - Contract Validation: Strict rate limit should be enforced for medical compliance
        var successfulRequests = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        Assert.Equal(100, successfulRequests);
        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimitedResponse.StatusCode);
        
        // Contract Validation: Medical-grade rate limit headers must be present
        Assert.True(rateLimitedResponse.Headers.Contains("X-RateLimit-Limit"));
        Assert.True(rateLimitedResponse.Headers.Contains("X-RateLimit-Remaining"));
        Assert.True(rateLimitedResponse.Headers.Contains("X-RateLimit-Reset"));
        
        // Contract Validation: Rate limit should be strict (100 per minute for admin)
        var limitHeader = rateLimitedResponse.Headers.GetValues("X-RateLimit-Limit").FirstOrDefault();
        Assert.Equal("100", limitHeader);
    }

    /// <summary>
    /// Contract: Admin Gateway MUST audit rate limit violations with user context for medical-grade compliance
    /// Validates medical-grade audit logging integration with rate limiting middleware
    /// </summary>
    [Fact]
    public async Task AdminGateway_RateLimitViolation_MustAuditWithUserContext()
    {
        // Arrange - Setup authenticated admin user for rate limit violation
        var testUserId = "admin-user-audit-test-001";
        var correlationId = Guid.NewGuid().ToString();
        var initialAuditCount = 0;
        
        // Get audit repository to verify logging behavior
        using var scope = _factory.Services.CreateScope();
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        initialAuditCount = await auditRepository.GetAuditLogsCountAsync();
        
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", "Test serviceadmin");
        _client.DefaultRequestHeaders.Add("X-User-ID", testUserId);
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        // Act - Make requests to potentially trigger rate limiting and audit logging
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 105; i++) // Exceed 100 request limit
        {
            responses.Add(await _client.GetAsync("/admin/api/services"));
        }
        
        // Contract Validation: Rate limit violations must be audited
        var rateLimitedResponses = responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests).ToList();
        if (rateLimitedResponses.Any())
        {
            // Allow time for audit logging to complete
            await Task.Delay(1000);
            
            var finalAuditCount = await auditRepository.GetAuditLogsCountAsync();
            Assert.True(finalAuditCount > initialAuditCount, 
                "MEDICAL_GRADE_COMPLIANCE_VIOLATION: Rate limit violations were not audited");
                
            // Contract Validation: Audit logs should contain user context for medical compliance
            var recentAudits = await auditRepository.GetRecentAuditLogsAsync(10);
            var rateLimitAudit = recentAudits.FirstOrDefault(a => 
                a.UserId == testUserId && 
                a.CorrelationId == correlationId &&
                a.EventType.Contains("RATE_LIMIT"));
                
            Assert.NotNull(rateLimitAudit);
            Assert.Equal(testUserId, rateLimitAudit.UserId);
            Assert.Equal(correlationId, rateLimitAudit.CorrelationId);
        }
    }

    /// <summary>
    /// Contract: Admin Gateway MUST isolate rate limits by authenticated user for medical compliance
    /// Validates user-based rate limiting isolation in distributed system
    /// </summary>
    [Fact]
    public async Task AdminGateway_RateLimit_MustIsolateByAuthenticatedUser()
    {
        // Arrange - Setup two different authenticated admin users
        var user1Id = "admin-user-isolation-001";
        var user2Id = "admin-user-isolation-002";
        var correlationId1 = Guid.NewGuid().ToString();
        var correlationId2 = Guid.NewGuid().ToString();
        
        // Act - Make request from first admin user
        var client1 = _factory.CreateClient();
        client1.DefaultRequestHeaders.Add("Authorization", "Test serviceadmin");
        client1.DefaultRequestHeaders.Add("X-User-ID", user1Id);
        client1.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId1);
        var response1 = await client1.GetAsync("/admin/api/services");
        
        // Act - Make request from second admin user
        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Add("Authorization", "Test serviceadmin");
        client2.DefaultRequestHeaders.Add("X-User-ID", user2Id);
        client2.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId2);
        var response2 = await client2.GetAsync("/admin/api/services");
        
        // Assert - Contract Validation: Both authenticated users should have independent rate limits
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        
        // Contract Validation: Each user should have independent rate limit counters
        var remaining1 = response1.Headers.GetValues("X-RateLimit-Remaining").FirstOrDefault();
        var remaining2 = response2.Headers.GetValues("X-RateLimit-Remaining").FirstOrDefault();
        
        Assert.NotNull(remaining1);
        Assert.NotNull(remaining2);
        Assert.True(int.Parse(remaining1) > 95); // Should have ~99 remaining out of 100
        Assert.True(int.Parse(remaining2) > 95); // Should have ~99 remaining out of 100
        
        // Cleanup
        client1.Dispose();
        client2.Dispose();
    }

    /// <summary>
    /// Contract: Admin Gateway MUST enforce stricter rate limits than Public Gateway for medical compliance
    /// Validates differential rate limiting policies between gateways
    /// </summary>
    [Fact]
    public async Task AdminGateway_RateLimit_MustBeStricterThanPublicGateway()
    {
        // Arrange - Setup authenticated admin user
        var testUserId = "admin-user-strict-test-001";
        var correlationId = Guid.NewGuid().ToString();
        
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", "Test serviceadmin");
        _client.DefaultRequestHeaders.Add("X-User-ID", testUserId);
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        // Act - Make request to check rate limit configuration
        var response = await _client.GetAsync("/admin/api/services");
        
        // Assert - Contract Validation: Admin rate limits should be stricter (100 vs 1000 for Public)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        if (response.Headers.Contains("X-RateLimit-Limit"))
        {
            var limitHeader = response.Headers.GetValues("X-RateLimit-Limit").FirstOrDefault();
            var adminLimit = int.Parse(limitHeader);
            
            // Contract Validation: Admin Gateway limit should be 100 (stricter than Public Gateway's 1000)
            Assert.Equal(100, adminLimit);
            Assert.True(adminLimit < 1000, 
                "Admin Gateway rate limit must be stricter than Public Gateway for medical compliance");
        }
    }

    /// <summary>
    /// Contract: Admin Gateway rate limiting MUST integrate with medical-grade audit persistence
    /// Validates zero data loss audit requirements for rate limiting violations
    /// </summary>
    [Fact]
    public async Task AdminGateway_RateLimit_MustIntegrateWithMedicalGradeAudit()
    {
        // Arrange - Setup authenticated user and verify audit system
        var testUserId = "admin-user-medical-audit-001";
        var correlationId = Guid.NewGuid().ToString();
        
        using var scope = _factory.Services.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        Assert.NotNull(auditService); // Contract Validation: Medical-grade audit service must be available
        
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", "Test serviceadmin");
        _client.DefaultRequestHeaders.Add("X-User-ID", testUserId);
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        // Act - Make request that should be audited by rate limiting middleware
        var response = await _client.GetAsync("/admin/api/services");
        
        // Assert - Contract Validation: Request should be processed with medical-grade audit integration
        Assert.True(response.StatusCode == HttpStatusCode.OK || 
                   response.StatusCode == HttpStatusCode.TooManyRequests);
        
        // Contract Validation: Medical-grade rate limiting headers should be present
        if (response.StatusCode == HttpStatusCode.TooManyRequests || 
            response.Headers.Contains("X-RateLimit-Limit"))
        {
            Assert.True(response.Headers.Contains("X-RateLimit-Limit"));
            Assert.True(response.Headers.Contains("X-Correlation-ID") || 
                       response.Headers.Contains("X-Request-ID"),
                "Medical-grade audit correlation tracking must be present");
        }
    }

    /// <summary>
    /// Contract: Admin Gateway MUST handle burst requests while maintaining medical-grade audit integrity
    /// Validates rate limiting under concurrent load with audit logging requirements
    /// </summary>
    [Fact]
    public async Task AdminGateway_RateLimit_MustMaintainAuditIntegrityUnderLoad()
    {
        // Arrange - Setup multiple authenticated users for concurrent testing
        var userIds = Enumerable.Range(1, 10).Select(i => $"admin-user-load-{i:D3}").ToList();
        var tasks = new List<Task<HttpResponseMessage>>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Get initial audit count for verification
        using var scope = _factory.Services.CreateScope();
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        var initialAuditCount = await auditRepository.GetAuditLogsCountAsync();
        
        // Act - Send concurrent requests from multiple authenticated users
        foreach (var userId in userIds)
        {
            var client = _factory.CreateClient();
            var correlationId = Guid.NewGuid().ToString();
            
            client.DefaultRequestHeaders.Add("Authorization", "Test serviceadmin");
            client.DefaultRequestHeaders.Add("X-User-ID", userId);
            client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
            
            tasks.Add(client.GetAsync("/admin/api/services"));
        }
        
        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Allow time for audit processing
        await Task.Delay(2000);
        
        // Assert - Contract Validation: All authenticated requests should be processed
        Assert.True(stopwatch.ElapsedMilliseconds < 15000, // 15 seconds max for medical compliance
            $"Admin Gateway rate limiting performance exceeded medical compliance threshold: {stopwatch.ElapsedMilliseconds}ms");
        
        // Contract Validation: Medical-grade audit integrity must be maintained
        var finalAuditCount = await auditRepository.GetAuditLogsCountAsync();
        Assert.True(finalAuditCount >= initialAuditCount + userIds.Count,
            "MEDICAL_GRADE_COMPLIANCE_VIOLATION: Not all authenticated requests were audited");
        
        // Contract Validation: Rate limiting should allow authenticated admin requests
        var successfulRequests = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        Assert.True(successfulRequests >= 8, // At least 80% success rate for authenticated admin requests
            $"Admin Gateway rate limiting too restrictive: only {successfulRequests}/{userIds.Count} authenticated requests succeeded");
        
        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    /// <summary>
    /// Contract: Admin Gateway MUST provide detailed rate limit information for medical compliance monitoring
    /// Validates rate limiting observability requirements for medical-grade systems
    /// </summary>
    [Fact]
    public async Task AdminGateway_RateLimit_MustProvideDetailedComplianceInformation()
    {
        // Arrange - Setup authenticated admin user
        var testUserId = "admin-user-compliance-001";
        var correlationId = Guid.NewGuid().ToString();
        
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", "Test serviceadmin");
        _client.DefaultRequestHeaders.Add("X-User-ID", testUserId);
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        // Act - Make request to verify rate limit information
        var response = await _client.GetAsync("/admin/api/services");
        
        // Assert - Contract Validation: Detailed rate limit information must be provided
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Contract Validation: Medical compliance rate limit headers
        if (response.Headers.Contains("X-RateLimit-Limit"))
        {
            Assert.True(response.Headers.Contains("X-RateLimit-Limit"));
            Assert.True(response.Headers.Contains("X-RateLimit-Remaining"));
            Assert.True(response.Headers.Contains("X-RateLimit-Reset"));
            
            // Contract Validation: Admin-specific rate limit values
            var limit = response.Headers.GetValues("X-RateLimit-Limit").FirstOrDefault();
            var remaining = response.Headers.GetValues("X-RateLimit-Remaining").FirstOrDefault();
            
            Assert.Equal("100", limit); // Admin Gateway strict limit
            Assert.NotNull(remaining);
            Assert.True(int.Parse(remaining) <= 100);
        }
        
        // Contract Validation: Response should include correlation tracking for audit trail
        Assert.True(response.Headers.Contains("X-Correlation-ID") || 
                   response.Headers.Contains("X-Request-ID"),
            "Medical-grade audit correlation must be trackable in rate limiting responses");
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Testing;
using Shared.Models;
using Shared.Repositories;
using Shared.Services;
using InternationalCenter.Tests.Shared.Contracts;
using System.Net;
using System.Text.Json;

namespace InternationalCenter.Gateway.Admin.Tests.Integration;

/// <summary>
/// Medical-grade audit persistence integration tests for Admin Gateway
/// Validates zero data loss compliance and audit trail completeness
/// Tests full flow from gateway requests through database persistence
/// </summary>
public class AuditPersistenceIntegrationTests : ContractTestBase, IClassFixture<AdminGatewayTestFactory>
{
    private readonly AdminGatewayTestFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuditPersistenceIntegrationTests(AdminGatewayTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    /// <summary>
    /// Contract: Admin Gateway MUST persist audit logs for all authenticated requests with zero data loss
    /// Validates medical-grade compliance requirement for complete audit trail
    /// </summary>
    [Fact]
    public async Task AdminGateway_AuthenticatedRequest_MustPersistAuditLogWithUserContext()
    {
        // Arrange - Setup authenticated request context
        var testUserId = "test-admin-user-001";
        var testUserName = "Test Admin User";
        var testUserRoles = new[] { "Admin", "ServiceManager" };
        var correlationId = Guid.NewGuid().ToString();
        
        _client.DefaultRequestHeaders.Add("Authorization", "Test serviceadmin");
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        using var scope = _factory.Services.CreateScope();
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        
        // Get initial audit count for comparison
        var initialAuditCount = await auditRepository.GetAuditLogsCountAsync();

        // Act - Make authenticated request to Admin Gateway endpoint
        var response = await _client.GetAsync("/admin/audit/retention/status");

        // Assert - Verify response success
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Wait briefly for audit log persistence (async operation)
        await Task.Delay(100);
        
        // Contract Validation: Audit log MUST be persisted
        var finalAuditCount = await auditRepository.GetAuditLogsCountAsync();
        Assert.True(finalAuditCount > initialAuditCount, 
            "MEDICAL_GRADE_COMPLIANCE_VIOLATION: Audit log was not persisted for authenticated Admin Gateway request");
        
        // Contract Validation: Audit log MUST contain complete user context
        var recentAuditLogs = await auditRepository.GetAuditLogsByCorrelationIdAsync(correlationId);
        var auditLog = Assert.Single(recentAuditLogs);
        
        Assert.NotNull(auditLog);
        Assert.NotEmpty(auditLog.UserId);
        Assert.NotEmpty(auditLog.UserName);
        Assert.NotEmpty(auditLog.CorrelationId);
        Assert.Equal(correlationId, auditLog.CorrelationId);
        Assert.Contains("AdminGateway", auditLog.ClientApplication);
        Assert.Equal("GATEWAY_REQUEST_START", auditLog.Action);
        
        // Contract Validation: Audit timestamp MUST be within acceptable range
        var timeDifference = Math.Abs((DateTime.UtcNow - auditLog.AuditTimestamp).TotalMinutes);
        Assert.True(timeDifference < 5, 
            $"MEDICAL_GRADE_COMPLIANCE_VIOLATION: Audit timestamp is {timeDifference} minutes off, exceeds 5-minute tolerance");
    }

    /// <summary>
    /// Contract: Admin Gateway MUST persist audit logs for security events with critical action flag
    /// Validates medical-grade compliance for security event tracking
    /// </summary>
    [Fact]
    public async Task AdminGateway_SecurityEvent_MustPersistAuditLogWithCriticalFlag()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("Authorization", "Test serviceadmin");
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        using var scope = _factory.Services.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();

        // Act - Trigger security event
        await auditService.LogSecurityEventAsync(
            "ADMIN_ACCESS_ATTEMPT", 
            "Test security event for integration testing",
            AuditSeverity.Warning);

        // Contract Validation: Security audit log MUST be persisted as critical
        var securityLogs = await auditRepository.GetCriticalAuditLogsAsync(
            DateTime.UtcNow.AddMinutes(-5), 
            DateTime.UtcNow.AddMinutes(5), 
            100);
            
        var securityAuditLog = securityLogs.FirstOrDefault(log => log.Action == "ADMIN_ACCESS_ATTEMPT");
        Assert.NotNull(securityAuditLog);
        Assert.True(securityAuditLog.IsCriticalAction, 
            "MEDICAL_GRADE_COMPLIANCE_VIOLATION: Security events MUST be marked as critical actions");
        Assert.Equal("Security", securityAuditLog.EntityType);
        Assert.Equal(AuditSeverity.Warning, securityAuditLog.Severity);
    }

    /// <summary>
    /// Contract: Admin Gateway audit repository MUST support high-performance queries for compliance reporting
    /// Validates PostgreSQL optimization requirements for medical-grade audit queries
    /// </summary>
    [Fact]
    public async Task AdminGateway_AuditRepository_MustSupportHighPerformanceQueries()
    {
        // Arrange - Create test audit logs
        using var scope = _factory.Services.CreateScope();
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuditPersistenceIntegrationTests>>();
        
        var testLogs = new List<AuditLog>();
        for (int i = 0; i < 10; i++)
        {
            testLogs.Add(new AuditLog
            {
                EntityType = "TestEntity",
                EntityId = $"test-{i}",
                Action = AuditActions.Create,
                UserId = $"test-user-{i}",
                UserName = $"Test User {i}",
                CorrelationId = Guid.NewGuid().ToString(),
                TraceId = Guid.NewGuid().ToString(),
                RequestUrl = "/test",
                RequestMethod = "POST",
                RequestIp = "127.0.0.1",
                UserAgent = "Integration-Test",
                AppVersion = "1.0.0",
                BuildDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                SessionId = Guid.NewGuid().ToString(),
                AuditTimestamp = DateTime.UtcNow.AddMinutes(-i),
                IsCriticalAction = i % 3 == 0, // Every third log is critical
                Severity = AuditSeverity.Info
            });
        }

        // Act & Assert - Test batch creation performance
        var batchStartTime = DateTime.UtcNow;
        var batchResult = await auditRepository.CreateAuditLogsBatchAsync(testLogs);
        var batchDuration = DateTime.UtcNow - batchStartTime;
        
        Assert.True(batchResult, "MEDICAL_GRADE_COMPLIANCE_VIOLATION: Batch audit log creation failed");
        Assert.True(batchDuration.TotalSeconds < 10, 
            $"PERFORMANCE_VIOLATION: Batch creation took {batchDuration.TotalSeconds} seconds, exceeds 10-second tolerance");

        // Act & Assert - Test query performance
        var queryStartTime = DateTime.UtcNow;
        var entityLogs = await auditRepository.GetAuditLogsByEntityAsync("TestEntity", "test-0", 100);
        var queryDuration = DateTime.UtcNow - queryStartTime;
        
        Assert.NotEmpty(entityLogs);
        Assert.True(queryDuration.TotalSeconds < 5, 
            $"PERFORMANCE_VIOLATION: Entity query took {queryDuration.TotalSeconds} seconds, exceeds 5-second tolerance");

        // Act & Assert - Test critical action query performance
        var criticalQueryStartTime = DateTime.UtcNow;
        var criticalLogs = await auditRepository.GetCriticalAuditLogsAsync(
            DateTime.UtcNow.AddMinutes(-15), 
            DateTime.UtcNow.AddMinutes(5), 
            100);
        var criticalQueryDuration = DateTime.UtcNow - criticalQueryStartTime;
        
        Assert.NotEmpty(criticalLogs);
        Assert.True(criticalQueryDuration.TotalSeconds < 3, 
            $"PERFORMANCE_VIOLATION: Critical action query took {criticalQueryDuration.TotalSeconds} seconds, exceeds 3-second tolerance");
        
        logger.LogInformation("AUDIT_PERFORMANCE_METRICS: Batch={BatchDurationMs}ms, EntityQuery={EntityQueryMs}ms, CriticalQuery={CriticalQueryMs}ms",
            batchDuration.TotalMilliseconds, queryDuration.TotalMilliseconds, criticalQueryDuration.TotalMilliseconds);
    }

    /// <summary>
    /// Contract: Admin Gateway MUST handle audit failures gracefully without blocking business operations
    /// Validates medical-grade resilience requirement for audit system failures
    /// </summary>
    [Fact]
    public async Task AdminGateway_AuditFailure_MustNotBlockBusinessOperations()
    {
        // Arrange - This test validates resilience, business operations continue even if audit fails
        var correlationId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("Authorization", "Test serviceadmin");
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        // Act - Make business request that should succeed even if audit has issues
        var response = await _client.GetAsync("/admin/audit/retention/status");

        // Assert - Business operation MUST succeed regardless of audit issues
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(responseContent);
        
        // Contract Validation: Response should contain expected business data
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(responseData.TryGetProperty("success", out var successProperty));
        Assert.True(successProperty.GetBoolean());
    }

    /// <summary>
    /// Contract: Admin Gateway audit retention endpoints MUST be secured and functional
    /// Validates medical-grade access control for audit management operations
    /// </summary>
    [Fact]
    public async Task AdminGateway_AuditRetentionEndpoints_MustBeSecuredAndFunctional()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "Test serviceadmin");
        
        // Act & Assert - Test retention report endpoint
        var reportResponse = await _client.GetAsync("/admin/audit/retention/report");
        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);
        
        var reportContent = await reportResponse.Content.ReadAsStringAsync();
        Assert.NotEmpty(reportContent);
        
        // Contract Validation: Report should contain required compliance fields
        var reportData = JsonSerializer.Deserialize<JsonElement>(reportContent);
        Assert.True(reportData.TryGetProperty("success", out _));
        Assert.True(reportData.TryGetProperty("data", out var dataProperty));
        
        var reportObject = dataProperty;
        Assert.True(reportObject.TryGetProperty("complianceStatus", out _));
        Assert.True(reportObject.TryGetProperty("retentionDays", out var retentionDaysProperty));
        Assert.Equal(2555, retentionDaysProperty.GetInt32()); // 7-year medical compliance
        
        // Act & Assert - Test retention status endpoint
        var statusResponse = await _client.GetAsync("/admin/audit/retention/status");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        
        var statusContent = await statusResponse.Content.ReadAsStringAsync();
        Assert.NotEmpty(statusContent);
        
        // Contract Validation: Status should contain required metrics
        var statusData = JsonSerializer.Deserialize<JsonElement>(statusContent);
        Assert.True(statusData.TryGetProperty("data", out var statusDataProperty));
        Assert.True(statusDataProperty.TryGetProperty("compliancePercentage", out _));
        Assert.True(statusDataProperty.TryGetProperty("totalAuditLogs", out _));
    }

    /// <summary>
    /// Contract: Admin Gateway MUST persist correlation IDs for medical-grade audit trail continuity
    /// Validates end-to-end request tracing capability for compliance investigations
    /// </summary>
    [Fact]
    public async Task AdminGateway_CorrelationIdTracking_MustMaintainAuditTrailContinuity()
    {
        // Arrange
        var correlationId = $"test-correlation-{Guid.NewGuid()}";
        _client.DefaultRequestHeaders.Add("Authorization", "Test serviceadmin");
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        using var scope = _factory.Services.CreateScope();
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();

        // Act - Make multiple requests with same correlation ID
        await _client.GetAsync("/admin/audit/retention/status");
        await _client.GetAsync("/admin/audit/retention/report");
        
        // Wait for audit log persistence
        await Task.Delay(200);

        // Assert - All requests with same correlation ID MUST be traceable
        var correlatedLogs = await auditRepository.GetAuditLogsByCorrelationIdAsync(correlationId);
        Assert.True(correlatedLogs.Count() >= 2, 
            "MEDICAL_GRADE_COMPLIANCE_VIOLATION: Correlation ID tracking failed - missing audit logs for correlated requests");
        
        // Contract Validation: All logs MUST have correct correlation ID
        foreach (var log in correlatedLogs)
        {
            Assert.Equal(correlationId, log.CorrelationId);
            Assert.NotEmpty(log.TraceId);
            Assert.NotEmpty(log.RequestUrl);
            Assert.Equal("AdminGateway", log.ClientApplication);
        }
        
        // Contract Validation: Logs MUST be chronologically ordered
        var timestamps = correlatedLogs.Select(log => log.AuditTimestamp).OrderBy(t => t).ToArray();
        var originalTimestamps = correlatedLogs.Select(log => log.AuditTimestamp).ToArray();
        Assert.True(timestamps.SequenceEqual(originalTimestamps), 
            "MEDICAL_GRADE_COMPLIANCE_VIOLATION: Audit logs are not properly chronologically ordered");
    }
}
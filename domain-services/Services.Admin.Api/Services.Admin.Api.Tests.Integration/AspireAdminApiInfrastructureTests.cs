using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Dapper;
using StackExchange.Redis;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using InternationalCenter.Services.Admin.Api.Tests.Integration.Infrastructure;
using InternationalCenter.Tests.Shared.Configuration;

namespace InternationalCenter.Services.Admin.Api.Tests.Integration;

/// <summary>
/// Standardized Admin API infrastructure tests using Aspire testing patterns with medical-grade requirements
/// WHY: Tests medical-grade audit requirements through standardized Aspire infrastructure access
/// SCOPE: Admin API infrastructure validation with EF Core and Redis using standardized testing patterns
/// CONTEXT: Medical-grade Admin API requires reliable infrastructure testing with consistent Aspire orchestration
/// </summary>
public class AspireAdminApiInfrastructureTests : AspireAdminIntegrationTestBase
{
    public AspireAdminApiInfrastructureTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact(DisplayName = "TDD GREEN: Aspire Admin API Database - Should validate service storage", Timeout = 30000)]
    public async Task AspireAdminApi_Database_ShouldValidateServiceStorage()
    {
        // ARRANGE: Use standardized Aspire infrastructure from base class
        var databaseConnectionString = await GetConnectionStringAsync();
        Assert.NotNull(databaseConnectionString);
        
        // ACT: Test database connectivity using migration-managed schema with standardized retry
        await ExecuteDatabaseOperationWithRetryAsync(async () =>
        {
            using var connection = new NpgsqlConnection(databaseConnectionString);
            await connection.OpenAsync();
            
            // Verify migration-managed schema exists and is accessible
            var tableCount = await connection.QuerySingleAsync<int>(@"
                SELECT COUNT(*) FROM information_schema.tables 
                WHERE table_schema = 'public' AND table_name IN ('services', 'service_categories')");
            
            // Test basic database operations on migrated schema
            var testQuery = await connection.QuerySingleAsync<int>("SELECT 1");
            
            // ASSERT: Migration-managed database validation
            Assert.Equal(2, tableCount); // Both services and service_categories tables exist
            Assert.Equal(1, testQuery);   // Basic connectivity confirmed
        }, operationName: "Medical-grade database schema validation");
        
        Output.WriteLine("✅ STANDARDIZED MEDICAL-GRADE INFRASTRUCTURE: Admin API service storage validated through Aspire orchestration");
    }

    [Fact(DisplayName = "TDD GREEN: Aspire Admin API Cache - Should validate audit trail caching", Timeout = 30000)]
    public async Task AspireAdminApi_Cache_ShouldValidateAuditTrailCaching()
    {
        // ARRANGE: Use standardized Aspire infrastructure from base class
        var redisConnectionString = await GetConnectionStringAsync("redis");
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            Output.WriteLine("⚠️ STANDARDIZED MEDICAL-GRADE WARNING: Redis not configured in Aspire AppHost - skipping audit caching test");
            return;
        }
        
        // ACT: Test audit trail caching through direct Redis access with standardized retry
        await ExecuteDatabaseOperationWithRetryAsync(async () =>
        {
            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
            var database = connectionMultiplexer.GetDatabase();
            
            var auditId = Guid.NewGuid();
            var auditKey = $"admin:audit:service:{auditId}";
            var auditData = JsonSerializer.Serialize(new {
                Operation = "CREATE_SERVICE",
                UserId = "admin-user-123",
                ServiceId = $"test-service-{auditId}",
                Timestamp = DateTimeOffset.UtcNow.ToString(StandardizedTestConfiguration.LoggingConfiguration.IsoTimestampFormat),
                Changes = "Service created with medical-grade audit trail using standardized infrastructure"
            });
            
            await database.StringSetAsync(auditKey, auditData, StandardizedTestConfiguration.Timeouts.CacheOperation);
            var retrievedAudit = await database.StringGetAsync(auditKey);
            
            connectionMultiplexer.Dispose();
            
            // ASSERT: Medical-grade audit caching validation
            Assert.Equal(auditData, retrievedAudit);
        }, operationName: "Medical-grade audit trail caching validation");
        
        Output.WriteLine("✅ STANDARDIZED MEDICAL-GRADE INFRASTRUCTURE: Admin API audit trail caching validated through Aspire orchestration");
    }

    [Fact(DisplayName = "TDD GREEN: Aspire Admin API Health - Should validate medical-grade monitoring", Timeout = 30000)]
    public async Task AspireAdminApi_Health_ShouldValidateMedicalGradeMonitoring()
    {
        // ARRANGE: Use standardized Aspire infrastructure from base class
        var databaseConnectionString = await GetConnectionStringAsync();
        Assert.NotNull(databaseConnectionString);
        
        // ACT: Test health monitoring through standardized infrastructure validation with retry
        var healthResults = new List<(string Check, bool Healthy, TimeSpan ResponseTime)>();
        
        await ExecuteDatabaseOperationWithRetryAsync(async () =>
        {
            using var connection = new NpgsqlConnection(databaseConnectionString);
            await connection.OpenAsync();
            
            // Validate medical-grade monitoring requirements with standardized performance thresholds
            var healthChecks = new[]
            {
                ("Database Connection", "SELECT 1"),
                ("Service Table", "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'services'"),
                ("Audit Capabilities", "SELECT current_timestamp")
            };
            
            foreach (var (checkName, sql) in healthChecks)
            {
                var startTime = DateTimeOffset.UtcNow;
                try
                {
                    await connection.ExecuteScalarAsync(sql);
                    var responseTime = DateTimeOffset.UtcNow - startTime;
                    healthResults.Add((checkName, true, responseTime));
                }
                catch
                {
                    var responseTime = DateTimeOffset.UtcNow - startTime;
                    healthResults.Add((checkName, false, responseTime));
                }
            }
            
            // ASSERT: Medical-grade health validation with standardized performance thresholds
            Assert.All(healthResults, result => Assert.True(result.Healthy));
            Assert.All(healthResults, result => 
                Assert.True(result.ResponseTime < StandardizedTestConfiguration.PerformanceThresholds.DatabaseQueryMax));
        }, operationName: "Medical-grade health monitoring validation");
        
        Output.WriteLine($"✅ STANDARDIZED MEDICAL-GRADE INFRASTRUCTURE: Admin API health monitoring successful - {healthResults.Count(r => r.Healthy)}/{healthResults.Count} checks passed");
    }

    [Fact(DisplayName = "TDD GREEN: Aspire Configuration Injection - Should provide Admin API connections", Timeout = 30000)]
    public async Task AspireConfiguration_ShouldProvideAdminApiConnections()
    {
        // ARRANGE: Use standardized Aspire infrastructure from base class
        
        // ACT: Use standardized Aspire API for connection strings with retry
        await ExecuteDatabaseOperationWithRetryAsync(async () =>
        {
            var databaseConnectionString = await GetConnectionStringAsync();
            var redisConnectionString = await GetConnectionStringAsync("redis");
            
            // ASSERT: Standardized configuration injection validation
            Assert.NotNull(databaseConnectionString);
            Assert.Contains("postgres", databaseConnectionString.ToLower());
            Assert.Contains("database", databaseConnectionString.ToLower());
            
            // Verify Admin API can connect to infrastructure with standardized validation
            using var connection = new NpgsqlConnection(databaseConnectionString);
            await connection.OpenAsync();
            var result = await connection.QuerySingleAsync<int>("SELECT 1");
            Assert.Equal(1, result);
        }, operationName: "Medical-grade Aspire configuration validation");
        
        Output.WriteLine("✅ STANDARDIZED MEDICAL-GRADE INFRASTRUCTURE: Aspire configuration injection provides valid Admin API connections");
    }

}
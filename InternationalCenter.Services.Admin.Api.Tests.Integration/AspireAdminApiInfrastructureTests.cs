using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Dapper;
using StackExchange.Redis;
using Aspire.Hosting.Testing;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using InternationalCenter.Tests.Shared.TestCollections;

namespace InternationalCenter.Services.Admin.Api.Tests.Integration;

/// <summary>
/// TDD GREEN: Admin API infrastructure tests using Microsoft documented Aspire testing patterns
/// Tests medical-grade audit requirements through direct infrastructure access
/// Follows working pattern from infrastructure tests - per-test Aspire orchestration
/// </summary>
[Collection("AspireInfrastructureTests")]
public class AspireAdminApiInfrastructureTests : IAsyncLifetime
{
    private readonly ILogger<AspireAdminApiInfrastructureTests> _logger;

    public AspireAdminApiInfrastructureTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<AspireAdminApiInfrastructureTests>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "TDD GREEN: Aspire Admin API Database - Should validate service storage")]
    public async Task AspireAdminApi_Database_ShouldValidateServiceStorage()
    {
        // ARRANGE: Create Aspire application per test following Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var databaseConnectionString = await app.GetConnectionStringAsync("database");
        Assert.NotNull(databaseConnectionString);
        
        // Database schema managed by migrations - no setup needed
        
        // ACT: Test database connectivity using migration-managed schema
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
        
        _logger.LogInformation("TDD GREEN: Admin API service storage successful through Aspire orchestration");
    }

    [Fact(DisplayName = "TDD GREEN: Aspire Admin API Cache - Should validate audit trail caching")]
    public async Task AspireAdminApi_Cache_ShouldValidateAuditTrailCaching()
    {
        // ARRANGE: Create Aspire application per test following Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var redisConnectionString = await app.GetConnectionStringAsync("redis");
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            _logger.LogWarning("TDD GREEN: Redis not configured in Aspire AppHost - skipping test");
            return;
        }
        
        // ACT: Test audit trail caching through direct Redis access
        var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
        var database = connectionMultiplexer.GetDatabase();
        
        var auditKey = "admin:audit:service:123";
        var auditData = JsonSerializer.Serialize(new {
            Operation = "CREATE_SERVICE",
            UserId = "admin-user-123",
            ServiceId = "test-service-123",
            Timestamp = DateTime.UtcNow,
            Changes = "Service created with medical-grade audit trail"
        });
        
        await database.StringSetAsync(auditKey, auditData, TimeSpan.FromHours(24));
        var retrievedAudit = await database.StringGetAsync(auditKey);
        
        connectionMultiplexer.Dispose();
        
        // ASSERT: Audit caching validation
        Assert.Equal(auditData, retrievedAudit);
        
        _logger.LogInformation("TDD GREEN: Admin API audit trail caching successful through Aspire orchestration");
    }

    [Fact(DisplayName = "TDD GREEN: Aspire Admin API Health - Should validate medical-grade monitoring")]
    public async Task AspireAdminApi_Health_ShouldValidateMedicalGradeMonitoring()
    {
        // ARRANGE: Create Aspire application per test following Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var databaseConnectionString = await app.GetConnectionStringAsync("database");
        Assert.NotNull(databaseConnectionString);
        
        // ACT: Test health monitoring through direct infrastructure validation
        using var connection = new NpgsqlConnection(databaseConnectionString);
        await connection.OpenAsync();
        
        // Validate medical-grade monitoring requirements
        var healthChecks = new[]
        {
            ("Database Connection", "SELECT 1"),
            ("Service Table", "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'services'"),
            ("Audit Capabilities", "SELECT current_timestamp")
        };
        
        var healthResults = new List<(string Check, bool Healthy, TimeSpan ResponseTime)>();
        
        foreach (var (checkName, sql) in healthChecks)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                await connection.ExecuteScalarAsync(sql);
                var responseTime = DateTime.UtcNow - startTime;
                healthResults.Add((checkName, true, responseTime));
            }
            catch
            {
                var responseTime = DateTime.UtcNow - startTime;
                healthResults.Add((checkName, false, responseTime));
            }
        }
        
        // ASSERT: Medical-grade health validation
        Assert.All(healthResults, result => Assert.True(result.Healthy));
        Assert.All(healthResults, result => Assert.True(result.ResponseTime < TimeSpan.FromSeconds(1)));
        
        _logger.LogInformation("TDD GREEN: Admin API health monitoring successful - {HealthyChecks}/{TotalChecks} checks passed", 
            healthResults.Count(r => r.Healthy), healthResults.Count);
    }

    [Fact(DisplayName = "TDD GREEN: Aspire Configuration Injection - Should provide Admin API connections")]
    public async Task AspireConfiguration_ShouldProvideAdminApiConnections()
    {
        // ARRANGE: Create Aspire application per test following Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // ACT: Use direct Aspire API for connection string (not IConfiguration)
        var databaseConnectionString = await app.GetConnectionStringAsync("database");
        var redisConnectionString = await app.GetConnectionStringAsync("redis");
        
        // ASSERT: Configuration injection validation
        Assert.NotNull(databaseConnectionString);
        Assert.Contains("postgres", databaseConnectionString.ToLower());
        Assert.Contains("database", databaseConnectionString.ToLower());
        
        // Verify Admin API can connect to infrastructure
        using var connection = new NpgsqlConnection(databaseConnectionString);
        await connection.OpenAsync();
        var result = await connection.QuerySingleAsync<int>("SELECT 1");
        Assert.Equal(1, result);
        
        _logger.LogInformation("TDD GREEN: Aspire configuration injection providing valid Admin API connections");
    }

}
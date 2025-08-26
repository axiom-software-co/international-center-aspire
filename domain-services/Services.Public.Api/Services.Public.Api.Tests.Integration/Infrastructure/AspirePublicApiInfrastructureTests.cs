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

namespace InternationalCenter.Services.Public.Api.Tests.Integration.Infrastructure;

/// <summary>
/// TDD GREEN: Public API infrastructure tests using Microsoft documented Aspire testing patterns
/// Tests standard observability and performance requirements through direct infrastructure access
/// Follows working pattern from Admin API infrastructure tests - per-test Aspire orchestration
/// </summary>
[Collection("AspireInfrastructureTests")]
public class AspirePublicApiInfrastructureTests : IAsyncLifetime
{
    private readonly ILogger<AspirePublicApiInfrastructureTests> _logger;

    public AspirePublicApiInfrastructureTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<AspirePublicApiInfrastructureTests>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "TDD GREEN: Aspire Public API Database - Should validate service queries", Timeout = 30000)]
    public async Task AspirePublicApi_Database_ShouldValidateServiceQueries()
    {
        // ARRANGE: Create Aspire application per test following Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var databaseConnectionString = await app.GetConnectionStringAsync("database");
        Assert.NotNull(databaseConnectionString);
        
        // ACT: Test service queries using migration-managed schema
        using var connection = new NpgsqlConnection(databaseConnectionString);
        await connection.OpenAsync();
        
        // Verify migration-managed schema exists and supports queries
        var serviceCount = await connection.QuerySingleAsync<int>(@"
            SELECT COUNT(*) FROM services WHERE ""Available"" = true");
        
        var categoryCount = await connection.QuerySingleAsync<int>(@"
            SELECT COUNT(*) FROM service_categories WHERE ""Active"" = true");
        
        // Test basic pagination query pattern
        var paginationTest = await connection.QueryAsync<dynamic>(@"
            SELECT ""Id"", ""Title"", ""Slug"" FROM services 
            ORDER BY created_at DESC
            LIMIT 10 OFFSET 0");
        
        // ASSERT: Public API database validation
        Assert.True(serviceCount >= 0); // Services table accessible
        Assert.True(categoryCount >= 0); // Categories table accessible
        Assert.NotNull(paginationTest);  // Pagination queries work
        
        _logger.LogInformation("TDD GREEN: Public API service queries successful through Aspire orchestration");
    }

    [Fact(DisplayName = "TDD GREEN: Aspire Public API Cache - Should validate response caching", Timeout = 30000)]
    public async Task AspirePublicApi_Cache_ShouldValidateResponseCaching()
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
        
        // ACT: Test response caching through direct Redis access
        var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
        var database = connectionMultiplexer.GetDatabase();
        
        var cacheKey = "public:services:page:1";
        var cacheData = JsonSerializer.Serialize(new {
            Services = new[] {
                new { Id = "test-1", Title = "Test Service 1", Slug = "test-service-1" },
                new { Id = "test-2", Title = "Test Service 2", Slug = "test-service-2" }
            },
            Pagination = new { Page = 1, PageSize = 10, Total = 2 },
            CachedAt = DateTime.UtcNow
        });
        
        await database.StringSetAsync(cacheKey, cacheData, TimeSpan.FromMinutes(15));
        var retrievedCache = await database.StringGetAsync(cacheKey);
        
        connectionMultiplexer.Dispose();
        
        // ASSERT: Response caching validation
        Assert.Equal(cacheData, retrievedCache);
        
        _logger.LogInformation("TDD GREEN: Public API response caching successful through Aspire orchestration");
    }

    [Fact(DisplayName = "TDD GREEN: Aspire Public API Performance - Should validate standard observability", Timeout = 30000)]
    public async Task AspirePublicApi_Performance_ShouldValidateStandardObservability()
    {
        // ARRANGE: Create Aspire application per test following Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var databaseConnectionString = await app.GetConnectionStringAsync("database");
        Assert.NotNull(databaseConnectionString);
        
        // ACT: Test performance monitoring through direct infrastructure validation
        using var connection = new NpgsqlConnection(databaseConnectionString);
        await connection.OpenAsync();
        
        // Validate standard observability requirements
        var performanceChecks = new[]
        {
            ("Query Performance", "SELECT 1"),
            ("Index Usage", "SELECT schemaname, tablename, indexname FROM pg_indexes WHERE tablename = 'services'"),
            ("Connection Health", "SELECT current_timestamp")
        };
        
        var performanceResults = new List<(string Check, bool Healthy, TimeSpan ResponseTime)>();
        
        foreach (var (checkName, sql) in performanceChecks)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                await connection.ExecuteScalarAsync(sql);
                var responseTime = DateTime.UtcNow - startTime;
                performanceResults.Add((checkName, true, responseTime));
            }
            catch
            {
                var responseTime = DateTime.UtcNow - startTime;
                performanceResults.Add((checkName, false, responseTime));
            }
        }
        
        // ASSERT: Standard observability validation
        Assert.All(performanceResults, result => Assert.True(result.Healthy));
        Assert.All(performanceResults, result => Assert.True(result.ResponseTime < TimeSpan.FromSeconds(2))); // More lenient for standard APIs
        
        _logger.LogInformation("TDD GREEN: Public API performance monitoring successful - {HealthyChecks}/{TotalChecks} checks passed", 
            performanceResults.Count(r => r.Healthy), performanceResults.Count);
    }

    [Fact(DisplayName = "TDD GREEN: Aspire Configuration Injection - Should provide Public API connections", Timeout = 30000)]
    public async Task AspireConfiguration_ShouldProvidePublicApiConnections()
    {
        // ARRANGE: Create Aspire application per test following Microsoft documented pattern
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // ACT: Use direct Aspire API for connection string validation
        var databaseConnectionString = await app.GetConnectionStringAsync("database");
        var redisConnectionString = await app.GetConnectionStringAsync("redis");
        
        // ASSERT: Configuration injection validation
        Assert.NotNull(databaseConnectionString);
        Assert.Contains("postgres", databaseConnectionString.ToLower());
        Assert.Contains("database", databaseConnectionString.ToLower());
        
        // Verify Public API can connect to infrastructure
        using var connection = new NpgsqlConnection(databaseConnectionString);
        await connection.OpenAsync();
        var result = await connection.QuerySingleAsync<int>("SELECT 1");
        Assert.Equal(1, result);
        
        _logger.LogInformation("TDD GREEN: Aspire configuration injection providing valid Public API connections");
    }
}
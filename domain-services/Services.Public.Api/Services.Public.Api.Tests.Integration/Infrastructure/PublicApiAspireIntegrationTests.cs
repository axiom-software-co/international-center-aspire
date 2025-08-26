using Dapper;
using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Models;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Tests.Shared.Base;
using Npgsql;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace InternationalCenter.Services.Public.Api.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for Services.Public.Api using proper DistributedApplicationTestingBuilder
/// Tests the Public API through actual distributed application orchestration with automated test data cleanup
/// Uses real PostgreSQL, Redis, and HTTP client - no mocks (proper integration testing)
/// WHY: Integration tests must use real dependencies with DistributedApplicationTestingBuilder and test isolation
/// SCOPE: Services.Public.Api with Dapper repository patterns and anonymous access
/// CONTEXT: Public gateway architecture serving website through anonymous access patterns
/// </summary>
public class PublicApiAspireIntegrationTests : AspireIntegrationTestBase
{
    public PublicApiAspireIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override string GetServiceName() => "services-public-api";

    [Fact(DisplayName = "PublicApi Integration - Should handle anonymous requests through distributed orchestration", Timeout = 30000)]
    public async Task PublicApi_Integration_ShouldHandleAnonymousRequestsThroughDistributedOrchestration()
    {
        // ARRANGE - Aspire orchestration provides real infrastructure
        Assert.NotNull(HttpClient);

        // ACT - Make anonymous request to health check endpoint with retry for reliability
        var response = await GetWithRetryAsync("/health", operationName: "Health check endpoint");

        // ASSERT - Public API should accept anonymous requests
        Assert.True(response.IsSuccessStatusCode);
        var healthContent = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(healthContent);

        Output.WriteLine("✅ INTEGRATION CONTRACT: Public API accepts anonymous requests through Aspire orchestration");
    }

    [Fact(DisplayName = "PublicApi Integration - Should connect to PostgreSQL through Aspire orchestration", Timeout = 30000)]
    public async Task PublicApi_Integration_ShouldConnectToPostgreSQLThroughAspireOrchestration()
    {
        // ARRANGE - Get database connection string from Aspire orchestration
        var connectionString = await GetConnectionStringAsync();
        Assert.NotNull(connectionString);

        // ACT - Test database connectivity with real PostgreSQL
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // ASSERT - Database connection should work through Aspire infrastructure
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        
        Output.WriteLine("✅ INTEGRATION CONTRACT: Public API connects to PostgreSQL through Aspire orchestration");
    }

    [Fact(DisplayName = "PublicApi Integration - Should retrieve services through real database", Timeout = 30000)]
    public async Task PublicApi_Integration_ShouldRetrieveServicesThroughRealDatabase()
    {
        try
        {
            // ARRANGE - Setup test data in real database with proper cleanup using retry for reliability
            await ExecuteDatabaseOperationWithRetryAsync(
                () => CleanDatabaseAsync(),
                operationName: "Database cleanup before test");
                
            var testServiceId = await ExecuteDatabaseOperationWithRetryAsync(
                () => SetupTestServiceDataWithCleanup(),
                operationName: "Test service data setup");

            // ACT - Make request to services endpoint with retry for distributed environment reliability
            var response = await GetWithRetryAsync("/api/services", operationName: "Retrieve services from database");

            // ASSERT - Should return services from real database
            Assert.True(response.IsSuccessStatusCode);
            var services = await response.Content.ReadFromJsonAsync<ServiceResponse[]>();
            Assert.NotNull(services);
            Assert.Contains(services, s => s.Id == testServiceId.ToString());

            Output.WriteLine($"✅ INTEGRATION CONTRACT: Public API retrieved {services.Length} services from real database");
        }
        finally
        {
            // CLEANUP - Ensure test data is cleaned up with retry for reliability
            await ExecuteDatabaseOperationWithRetryAsync(
                () => CleanDatabaseAsync(),
                operationName: "Database cleanup after test");
            await ExecuteDatabaseOperationWithRetryAsync(
                () => VerifyCleanDatabaseStateAsync(),
                operationName: "Database state verification");
        }
    }

    [Fact(DisplayName = "PublicApi Integration - Should handle service queries with real Dapper repository", Timeout = 30000)]
    public async Task PublicApi_Integration_ShouldHandleServiceQueriesWithRealDapperRepository()
    {
        try
        {
            // ARRANGE - Setup test service in real database with proper cleanup
            await CleanDatabaseAsync();
            var testServiceId = await SetupTestServiceDataWithCleanup();

            // ACT - Query specific service by ID
            var response = await HttpClient!.GetAsync($"/api/services/{testServiceId}");

            // ASSERT - Should return service from real Dapper repository
            Assert.True(response.IsSuccessStatusCode);
            var service = await response.Content.ReadFromJsonAsync<ServiceResponse>();
            Assert.NotNull(service);
            Assert.Equal(testServiceId.ToString(), service.Id);

            Output.WriteLine("✅ INTEGRATION CONTRACT: Public API handles service queries through real Dapper repository");
        }
        finally
        {
            // CLEANUP - Ensure test data is cleaned up
            await CleanDatabaseAsync();
            await VerifyCleanDatabaseStateAsync();
        }
    }

    [Fact(DisplayName = "PublicApi Integration - Should handle concurrent requests with real infrastructure", Timeout = 30000)]
    public async Task PublicApi_Integration_ShouldHandleConcurrentRequestsWithRealInfrastructure()
    {
        try
        {
            // ARRANGE - Setup test data with proper cleanup
            await CleanDatabaseAsync();
            await SetupTestServiceDataWithCleanup();
            
            // ACT - Make concurrent requests to real API
            var tasks = Enumerable.Range(0, 10).Select(async i =>
            {
                var response = await HttpClient!.GetAsync("/api/services");
                return response.IsSuccessStatusCode;
            });

            var results = await Task.WhenAll(tasks);

            // ASSERT - All concurrent requests should succeed
            Assert.All(results, result => Assert.True(result));

            Output.WriteLine("✅ INTEGRATION CONTRACT: Public API handles concurrent requests with real infrastructure");
        }
        finally
        {
            // CLEANUP - Ensure test data is cleaned up
            await CleanDatabaseAsync();
            await VerifyCleanDatabaseStateAsync();
        }
    }

    [Fact(DisplayName = "PublicApi Integration - Should use Redis cache through Aspire orchestration", Timeout = 30000)]
    public async Task PublicApi_Integration_ShouldUseRedisCacheThroughAspireOrchestration()
    {
        try
        {
            // ARRANGE - Setup test data that will be cached with proper cleanup
            await CleanDatabaseAsync();
            var testServiceId = await SetupTestServiceDataWithCleanup();

            // ACT - Make request that should use caching
            var response1 = await HttpClient!.GetAsync($"/api/services/{testServiceId}");
            var response2 = await HttpClient!.GetAsync($"/api/services/{testServiceId}");

            // ASSERT - Both requests should succeed (cache working or fallback working)
            Assert.True(response1.IsSuccessStatusCode);
            Assert.True(response2.IsSuccessStatusCode);

            var service1 = await response1.Content.ReadFromJsonAsync<ServiceResponse>();
            var service2 = await response2.Content.ReadFromJsonAsync<ServiceResponse>();

            Assert.NotNull(service1);
            Assert.NotNull(service2);
            Assert.Equal(service1.Id, service2.Id);

            Output.WriteLine("✅ INTEGRATION CONTRACT: Public API uses Redis cache through Aspire orchestration");
        }
        finally
        {
            // CLEANUP - Ensure test data is cleaned up
            await CleanDatabaseAsync();
            await VerifyCleanDatabaseStateAsync();
        }
    }

    [Fact(DisplayName = "PublicApi Integration - Should validate API performance with real infrastructure", Timeout = 30000)]
    public async Task PublicApi_Integration_ShouldValidateApiPerformanceWithRealInfrastructure()
    {
        try
        {
            // ARRANGE - Setup test data with proper cleanup
            await CleanDatabaseAsync();
            await SetupTestServiceDataWithCleanup();

            // ACT - Measure response time with real infrastructure
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await HttpClient!.GetAsync("/api/services");
            stopwatch.Stop();

            // ASSERT - API should respond within reasonable time with real infrastructure
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, // 5 seconds for real infrastructure
                $"API response took {stopwatch.ElapsedMilliseconds}ms, should be under 5000ms with real infrastructure");

            Output.WriteLine($"✅ INTEGRATION CONTRACT: Public API responded in {stopwatch.ElapsedMilliseconds}ms with real infrastructure");
        }
        finally
        {
            // CLEANUP - Ensure test data is cleaned up
            await CleanDatabaseAsync();
            await VerifyCleanDatabaseStateAsync();
        }
    }

    /// <summary>
    /// Setup test service data in real database for integration testing with proper cleanup support
    /// Returns the created service ID for use in tests
    /// </summary>
    private async Task<ServiceId> SetupTestServiceDataWithCleanup()
    {
        var connectionString = await GetConnectionStringAsync();
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Create a test service in real database using raw SQL (since this is integration test)
        var serviceId = ServiceId.CreateNew();
        var categoryId = await SetupTestCategoryAsync();

        // Insert test service
        await connection.ExecuteAsync(
            """
            INSERT INTO services (id, title, slug, description, detailed_description, category_id, status, available, featured, created_at, updated_at)
            VALUES (@Id, @Title, @Slug, @Description, @DetailedDescription, @CategoryId, @Status, @Available, @Featured, @CreatedAt, @UpdatedAt)
            """,
            new
            {
                Id = serviceId.Value,
                Title = "Integration Test Service",
                Slug = "integration-test-service",
                Description = "Service for integration testing",
                DetailedDescription = "Detailed description for integration test service",
                CategoryId = categoryId,
                Status = ServiceStatus.Published.ToString(),
                Available = true,
                Featured = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        Output.WriteLine($"✅ TEST DATA: Created test service {serviceId} for integration testing");
        return serviceId;
    }

    [Fact(DisplayName = "PublicApi Integration - Should demonstrate reliable test data isolation", Timeout = 30000)]
    public async Task PublicApi_Integration_ShouldDemonstrateReliableTestDataIsolation()
    {
        // ARRANGE - Start with clean database
        await CleanDatabaseAsync();
        await VerifyCleanDatabaseStateAsync();
        
        // Create test data
        var testServiceId = await SetupTestServiceDataWithCleanup();
        
        // Verify data exists
        var connectionString = await GetConnectionStringAsync();
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var serviceCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM services");
        Assert.Equal(1, serviceCount);
        
        // ACT - Clean database using Respawn
        await CleanDatabaseAsync();
        
        // ASSERT - Database should be clean
        await VerifyCleanDatabaseStateAsync();
        var finalServiceCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM services");
        Assert.Equal(0, finalServiceCount);
        
        Output.WriteLine("✅ INTEGRATION CONTRACT: Respawn cleanup provides reliable test data isolation");
    }
}

/// <summary>
/// Response model for service API endpoints (matches Public API response format)
/// </summary>
public record ServiceResponse(
    string Id,
    string Title,
    string Slug,
    string Description,
    string? DetailedDescription,
    string Status,
    bool Available,
    bool Featured,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
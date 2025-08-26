using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Aspire.Hosting.Testing;
using System.Text.Json;
using System.Text;

namespace InternationalCenter.Services.Admin.Api.Benchmarks.Benchmarks;

/// <summary>
/// BenchmarkDotNet performance tests for Services.Admin.Api HTTP endpoints
/// WHY: End-to-end admin API performance includes HTTP pipeline, authentication, and medical-grade audit
/// SCOPE: Critical admin API endpoints with Entra External ID authentication simulation
/// CONTEXT: Full admin request/response cycle - most realistic benchmark for admin user experience
/// </summary>
[SimpleJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class AdminApiEndpointBenchmarks
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Initialize Aspire application with full HTTP infrastructure
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();

        // Get HTTP client for Services.Admin.Api
        _httpClient = _app.CreateHttpClient("services-admin-api");
        
        // Warm up the admin API to ensure consistent benchmarking
        await WarmUpAdminApiAsync();
        
        Console.WriteLine($"🏥 Admin API endpoint benchmarks setup complete - API base URL: {_httpClient.BaseAddress}");
    }
    
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        _httpClient?.Dispose();
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }
    
    private async Task WarmUpAdminApiAsync()
    {
        try
        {
            // Warm up with health check
            var warmupResponse = await _httpClient!.GetAsync("/health");
            Console.WriteLine($"🏥 Admin API warmup - Health check status: {warmupResponse.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🏥 Admin API warmup warning: {ex.Message}");
        }
    }

    /// <summary>
    /// Benchmark health check endpoint - Medical-grade infrastructure baseline
    /// Fast endpoint to establish baseline admin API performance
    /// </summary>
    [Benchmark(Description = "Health Check - Medical-grade infrastructure baseline")]
    public async Task<string> AdminHealthCheckEndpoint()
    {
        var response = await _httpClient!.GetAsync("/health");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark GET /api/admin/services - Admin service listing
    /// Critical for admin dashboard service management interface
    /// </summary>
    [Benchmark(Description = "GET /api/admin/services - Admin service listing")]
    public async Task<string> GetAdminServicesEndpoint()
    {
        var response = await _httpClient!.GetAsync("/api/admin/services");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark GET /api/admin/services/{id} - Admin service details
    /// Critical for admin service editing workflows
    /// </summary>
    [Benchmark(Description = "GET /api/admin/services/{id} - Admin service details")]
    public async Task<string> GetAdminServiceByIdEndpoint()
    {
        // Use realistic admin service ID lookup
        var response = await _httpClient!.GetAsync("/api/admin/services/test-service-id");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark POST /api/admin/services - Service creation
    /// Most critical admin operation - create new service with audit logging
    /// </summary>
    [Benchmark(Description = "POST /api/admin/services - Service creation")]
    public async Task<string> CreateAdminServiceEndpoint()
    {
        var createRequest = new
        {
            Title = "Benchmark Admin Service",
            Description = "Service created through admin API benchmark",
            DetailedDescription = "Detailed description for admin benchmarking",
            Slug = $"admin-benchmark-service-{Guid.NewGuid():N}",
            RequestId = Guid.NewGuid().ToString(),
            UserContext = "admin@benchmark.com",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Admin API Benchmark Agent"
        };

        var json = JsonSerializer.Serialize(createRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient!.PostAsync("/api/admin/services", content);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark PUT /api/admin/services/{id} - Service update
    /// Critical admin operation - update existing service with audit logging
    /// </summary>
    [Benchmark(Description = "PUT /api/admin/services/{id} - Service update")]
    public async Task<string> UpdateAdminServiceEndpoint()
    {
        var updateRequest = new
        {
            Id = "test-service-id",
            Title = "Updated Benchmark Service",
            Description = "Updated service description",
            DetailedDescription = "Updated detailed description",
            Slug = $"updated-benchmark-{Guid.NewGuid():N}",
            RequestId = Guid.NewGuid().ToString(),
            UserContext = "admin@benchmark.com",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Admin API Benchmark Agent"
        };

        var json = JsonSerializer.Serialize(updateRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient!.PutAsync("/api/admin/services/test-service-id", content);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark DELETE /api/admin/services/{id} - Service deletion
    /// Admin operation - delete service with audit logging and validation
    /// </summary>
    [Benchmark(Description = "DELETE /api/admin/services/{id} - Service deletion")]
    public async Task<string> DeleteAdminServiceEndpoint()
    {
        var response = await _httpClient!.DeleteAsync("/api/admin/services/test-service-id");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark GET /api/admin/categories - Admin category management
    /// Important for admin category management interface
    /// </summary>
    [Benchmark(Description = "GET /api/admin/categories - Admin category management")]
    public async Task<string> GetAdminCategoriesEndpoint()
    {
        var response = await _httpClient!.GetAsync("/api/admin/categories");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark POST /api/admin/categories - Category creation
    /// Admin operation - create new service category
    /// </summary>
    [Benchmark(Description = "POST /api/admin/categories - Category creation")]
    public async Task<string> CreateAdminCategoryEndpoint()
    {
        var createRequest = new
        {
            Name = "Benchmark Category",
            Description = "Category created through admin API benchmark",
            Slug = $"benchmark-category-{Guid.NewGuid():N}",
            RequestId = Guid.NewGuid().ToString(),
            UserContext = "admin@benchmark.com",
            ClientIpAddress = "127.0.0.1",
            UserAgent = "Admin API Benchmark Agent"
        };

        var json = JsonSerializer.Serialize(createRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient!.PostAsync("/api/admin/categories", content);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark authentication/authorization error handling
    /// Tests 401 Unauthorized performance - important for security
    /// </summary>
    [Benchmark(Description = "Authentication error - 401 Unauthorized handling")]
    public async Task<string> AuthenticationErrorHandlingEndpoint()
    {
        // Remove any auth headers to simulate unauthorized request
        using var client = new HttpClient { BaseAddress = _httpClient!.BaseAddress };
        var response = await client.GetAsync("/api/admin/services");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark validation error handling - Bad request performance
    /// Important for admin form validation and error response performance
    /// </summary>
    [Benchmark(Description = "Validation error - Bad request handling")]
    public async Task<string> ValidationErrorHandlingEndpoint()
    {
        // Invalid request to trigger validation error
        var invalidRequest = new
        {
            Title = "", // Empty title should trigger validation error
            Description = "",
            Slug = "",
            RequestId = Guid.NewGuid().ToString(),
            UserContext = "admin@benchmark.com"
        };

        var json = JsonSerializer.Serialize(invalidRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient!.PostAsync("/api/admin/services", content);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark concurrent admin requests
    /// Tests admin API performance under concurrent load from multiple admin users
    /// </summary>
    [Benchmark(Description = "Concurrent admin requests - Multi-user load simulation")]
    public async Task<string[]> ConcurrentAdminRequestsEndpoint()
    {
        // Simulate 3 concurrent admin requests (realistic admin load)
        var tasks = new Task<string>[3];
        for (int i = 0; i < 3; i++)
        {
            tasks[i] = GetAdminServiceListContent();
        }
        
        var results = await Task.WhenAll(tasks);
        return results;
    }
    
    private async Task<string> GetAdminServiceListContent()
    {
        var response = await _httpClient!.GetAsync("/api/admin/services");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark admin search functionality
    /// Tests admin service search performance with complex queries
    /// </summary>
    [Benchmark(Description = "Admin search - Complex query performance")]
    public async Task<string> AdminSearchEndpoint()
    {
        var response = await _httpClient!.GetAsync("/api/admin/services/search?q=benchmark&status=all&category=&page=1&size=20");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark admin pagination
    /// Tests paginated admin service listing performance
    /// </summary>
    [Benchmark(Description = "Admin pagination - Large dataset handling")]
    public async Task<string> AdminPaginationEndpoint()
    {
        var response = await _httpClient!.GetAsync("/api/admin/services?page=1&size=50&sort=title&order=asc");
        return await response.Content.ReadAsStringAsync();
    }
}
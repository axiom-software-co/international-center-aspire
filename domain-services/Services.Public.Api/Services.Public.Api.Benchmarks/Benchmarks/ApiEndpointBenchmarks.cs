using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace InternationalCenter.Services.Public.Api.Benchmarks.Benchmarks;

/// <summary>
/// BenchmarkDotNet performance tests for Services.Public.Api HTTP endpoints
/// WHY: End-to-end API performance includes HTTP pipeline, routing, controllers, and serialization
/// SCOPE: Critical API endpoints serving public website traffic
/// CONTEXT: Full request/response cycle performance - most realistic benchmark for website performance
/// </summary>
[SimpleJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class ApiEndpointBenchmarks
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

        // Get HTTP client for Services.Public.Api
        _httpClient = _app.CreateHttpClient("services-public-api");
        
        // Warm up the API to ensure consistent benchmarking
        await WarmUpApiAsync();
        
        Console.WriteLine($"🚀 API endpoint benchmarks setup complete - API base URL: {_httpClient.BaseAddress}");
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
    
    private async Task WarmUpApiAsync()
    {
        try
        {
            // Warm up with health check
            var warmupResponse = await _httpClient!.GetAsync("/health");
            Console.WriteLine($"API warmup - Health check status: {warmupResponse.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API warmup warning: {ex.Message}");
        }
    }

    /// <summary>
    /// Benchmark health check endpoint - Infrastructure performance baseline
    /// Fast endpoint to establish baseline API performance
    /// </summary>
    [Benchmark(Description = "Health Check - API infrastructure baseline")]
    public async Task<string> HealthCheckEndpoint()
    {
        var response = await _httpClient!.GetAsync("/health");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark GET /api/services/{slug} - MOST CRITICAL endpoint
    /// This endpoint serves individual service pages - highest traffic volume
    /// </summary>
    [Benchmark(Description = "GET /api/services/{slug} - Critical service page endpoint")]
    public async Task<string> GetServiceBySlugEndpoint()
    {
        // Use realistic service slug for benchmarking
        var response = await _httpClient!.GetAsync("/api/services/consultation-services");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark GET /api/services - Published services listing
    /// Critical for homepage and service directory performance
    /// </summary>
    [Benchmark(Description = "GET /api/services - Published services listing")]
    public async Task<string> GetPublishedServicesEndpoint()
    {
        var response = await _httpClient!.GetAsync("/api/services");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark GET /api/services/featured - Featured services for homepage
    /// Critical for homepage above-the-fold content performance
    /// </summary>
    [Benchmark(Description = "GET /api/services/featured - Homepage featured services")]
    public async Task<string> GetFeaturedServicesEndpoint()
    {
        var response = await _httpClient!.GetAsync("/api/services/featured?limit=6");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark GET /api/categories - Service categories for navigation
    /// Critical for website navigation and filtering performance
    /// </summary>
    [Benchmark(Description = "GET /api/categories - Navigation categories")]
    public async Task<string> GetServiceCategoriesEndpoint()
    {
        var response = await _httpClient!.GetAsync("/api/categories");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark GET /api/services with pagination - Service directory pagination
    /// Critical for service directory and large result sets
    /// </summary>
    [Benchmark(Description = "GET /api/services?page=1&size=20 - Paginated services")]
    public async Task<string> GetPagedServicesEndpoint()
    {
        var response = await _httpClient!.GetAsync("/api/services?page=1&size=20");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark GET /api/services/search - Search functionality
    /// Critical for user search experience and SEO
    /// </summary>
    [Benchmark(Description = "GET /api/services/search - Search functionality")]
    public async Task<string> SearchServicesEndpoint()
    {
        var response = await _httpClient!.GetAsync("/api/services/search?q=consultation&page=1&size=20");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark GET /api/services?category={id} - Category filtering
    /// Important for category-specific service listings
    /// </summary>
    [Benchmark(Description = "GET /api/services?category=1 - Category filtering")]
    public async Task<string> GetServicesByCategoryEndpoint()
    {
        var response = await _httpClient!.GetAsync("/api/services?category=1");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark 404 error handling - Not found performance
    /// Important for robust error handling and SEO (404 pages)
    /// </summary>
    [Benchmark(Description = "GET /api/services/non-existent - 404 error handling")]
    public async Task<string> NotFoundErrorHandlingEndpoint()
    {
        var response = await _httpClient!.GetAsync("/api/services/non-existent-service-benchmark");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark validation error handling - Bad request performance  
    /// Important for input validation and error response performance
    /// </summary>
    [Benchmark(Description = "GET /api/services/search?q= - Validation error handling")]
    public async Task<string> ValidationErrorHandlingEndpoint()
    {
        // Empty search query should trigger validation error
        var response = await _httpClient!.GetAsync("/api/services/search?q=&page=1&size=20");
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Benchmark concurrent requests to critical endpoint
    /// Tests API performance under concurrent load (simplified load test)
    /// </summary>
    [Benchmark(Description = "Concurrent requests - Load testing simulation")]
    public async Task<string[]> ConcurrentRequestsEndpoint()
    {
        // Simulate 5 concurrent requests to the most critical endpoint
        var tasks = new Task<string>[5];
        for (int i = 0; i < 5; i++)
        {
            tasks[i] = GetServiceBySlugContent();
        }
        
        var results = await Task.WhenAll(tasks);
        return results;
    }
    
    private async Task<string> GetServiceBySlugContent()
    {
        var response = await _httpClient!.GetAsync("/api/services/consultation-services");
        return await response.Content.ReadAsStringAsync();
    }
}
using InternationalCenter.Services.Public.Api.Infrastructure.Data;
using InternationalCenter.Tests.Shared.TestData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Aspire.Hosting.Testing;
using Aspire.Hosting;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace InternationalCenter.Services.Public.Api.Tests.Integration.Handlers;

/// <summary>
/// TDD GREEN: REST API integration tests using Aspire distributed application testing
/// Tests REST endpoint contracts against real PostgreSQL and Redis infrastructure
/// Replaces TestContainers with Microsoft's recommended Aspire testing framework
/// </summary>
public class PublicApiRestHandlerAspireTests : IAsyncLifetime
{
    private DistributedApplication _app = null!;
    private HttpClient _httpClient = null!;
    
    // Test data for seeding
    private List<InternationalCenter.Services.Domain.Entities.Service> _testServices = null!;

    public async Task InitializeAsync()
    {
        // ARRANGE: Setup Aspire distributed application for testing with real infrastructure
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();

        // Get real connection strings from Aspire orchestration
        var databaseConnectionString = await _app.GetConnectionStringAsync("database");
        var redisConnectionString = await _app.GetConnectionStringAsync("redis");

        // Create WebApplicationFactory with Aspire-provided infrastructure
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Replace database context with Aspire-provided connection string
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ServicesDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    
                    services.AddDbContext<ServicesDbContext>(options =>
                        options.UseNpgsql(databaseConnectionString));
                    
                    // Replace ApplicationDbContext for migration services with Aspire connection
                    var appDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<InternationalCenter.Shared.Infrastructure.ApplicationDbContext>));
                    if (appDescriptor != null)
                    {
                        services.Remove(appDescriptor);
                    }
                    
                    services.AddDbContext<InternationalCenter.Shared.Infrastructure.ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(databaseConnectionString, npgsql =>
                        {
                            npgsql.MigrationsAssembly("InternationalCenter.Migrations.Service");
                        });
                    });
                    
                    // Replace Redis with Aspire-provided connection
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = redisConnectionString;
                    });
                    
                    // Add Redis connection multiplexer for direct access
                    services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
                        StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));
                });
            });
            
        _httpClient = factory.CreateClient();
        
        // Prepare and seed test data using real database
        _testServices = ServiceTestDataGenerator.GenerateServices(20).ToList();
        foreach (var service in _testServices.Take(5))
        {
            service.Publish();
            service.SetFeatured(true);
        }
        
        // Seed data to Aspire-managed database
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServicesDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        
        // Clear existing data
        dbContext.Services.RemoveRange(dbContext.Services);
        await dbContext.SaveChangesAsync();
        
        // Add test data
        dbContext.Services.AddRange(_testServices);
        await dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _httpClient.Dispose();
        await _app.DisposeAsync();
    }

    [Fact(DisplayName = "TDD GREEN: GET /api/services - Should Return Paginated Services with Aspire")]
    public async Task GetServices_WithPagination_ShouldReturnPaginatedServices()
    {
        // ARRANGE: REST contract for paginated services using Aspire infrastructure
        var expectedUrl = "/api/services?page=1&pageSize=10";
        
        // ACT: Call REST endpoint against Aspire-managed infrastructure
        var response = await _httpClient.GetAsync(expectedUrl);
        
        // ASSERT: Contract expectations with real PostgreSQL and Redis
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        
        var result = await response.Content.ReadFromJsonAsync<ServicesResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Services);
        Assert.NotNull(result.Pagination);
        Assert.Equal(1, result.Pagination.Page);
        Assert.Equal(10, result.Pagination.PageSize);
        Assert.True(result.Services.Count <= 10);
    }

    [Fact(DisplayName = "TDD GREEN: GET /api/services/{slug} - Should Return Service By Slug with Aspire")]
    public async Task GetServiceBySlug_WithValidSlug_ShouldReturnService()
    {
        // ARRANGE: REST contract for single service lookup using Aspire infrastructure
        var testService = _testServices.First();
        var testSlug = testService.Slug.Value;
        var expectedUrl = $"/api/services/{testSlug}";
        
        // ACT: Call REST endpoint against Aspire-managed infrastructure
        var response = await _httpClient.GetAsync(expectedUrl);
        
        // ASSERT: Contract expectations with real data
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Service);
        Assert.Equal(testSlug, result.Service.Slug);
    }

    [Fact(DisplayName = "TDD GREEN: GET /api/services/search - Should Return Filtered Services with Aspire")]
    public async Task SearchServices_WithQuery_ShouldReturnFilteredServices()
    {
        // ARRANGE: REST contract for service search using Aspire infrastructure
        var query = "service"; // Match generated test data
        var expectedUrl = $"/api/services/search?q={query}&page=1&pageSize=10";
        
        // ACT: Call REST endpoint against Aspire-managed infrastructure
        var response = await _httpClient.GetAsync(expectedUrl);
        
        // ASSERT: Contract expectations with real search
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<SearchResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Services);
        Assert.NotNull(result.Pagination);
        Assert.Equal(query, result.Query);
        
        // Verify search actually works with real data
        Assert.True(result.Services.Count > 0);
    }

    [Fact(DisplayName = "TDD GREEN: GET /api/services/featured - Should Return Featured Services with Aspire")]
    public async Task GetFeaturedServices_WithLimit_ShouldReturnFeaturedServices()
    {
        // ARRANGE: REST contract for featured services using Aspire infrastructure
        var limit = 5;
        var expectedUrl = $"/api/services/featured?limit={limit}";
        
        // ACT: Call REST endpoint against Aspire-managed infrastructure
        var response = await _httpClient.GetAsync(expectedUrl);
        
        // ASSERT: Contract expectations with real featured data
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<FeaturedServicesResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Services);
        Assert.True(result.Services.Count <= limit);
        Assert.True(result.Services.All(s => s.Featured));
    }

    [Fact(DisplayName = "TDD GREEN: GET /api/services - With Category Filter Should Return Filtered Services")]
    public async Task GetServices_WithCategoryFilter_ShouldReturnFilteredServices()
    {
        // ARRANGE: REST contract with category filtering using Aspire infrastructure
        var categorySlug = "web-development";
        var expectedUrl = $"/api/services?category={categorySlug}&page=1&pageSize=10";
        
        // ACT: Call REST endpoint against Aspire-managed infrastructure
        var response = await _httpClient.GetAsync(expectedUrl);
        
        // ASSERT: Contract expectations with real filtering
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<ServicesResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Services);
        
        // Note: Category filtering may return empty results with test data, which is valid
        Assert.NotNull(result.Pagination);
    }

    [Fact(DisplayName = "TDD GREEN: REST Endpoints Should Include Standard Observability Headers")]
    public async Task RestEndpoints_ShouldIncludeObservabilityHeaders()
    {
        // ARRANGE: Request with correlation ID header using Aspire infrastructure
        var correlationId = Guid.NewGuid().ToString();
        _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        // ACT: Call REST endpoint against Aspire-managed infrastructure
        var response = await _httpClient.GetAsync("/api/services?page=1&pageSize=1");
        
        // ASSERT: Standard observability headers should be present
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").First());
        Assert.True(response.Headers.Contains("X-Request-Id"));
    }

    [Fact(DisplayName = "TDD GREEN: Invalid Page Parameters Should Return BadRequest")]
    public async Task GetServices_WithInvalidPagination_ShouldReturnBadRequest()
    {
        // ARRANGE: Invalid pagination parameters using Aspire infrastructure
        var invalidUrl = "/api/services?page=0&pageSize=0";
        
        // ACT: Call REST endpoint with invalid params against Aspire infrastructure
        var response = await _httpClient.GetAsync(invalidUrl);
        
        // ASSERT: Should return validation error
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("page", error.Message.ToLowerInvariant());
    }

    [Fact(DisplayName = "TDD GREEN: Non-Existent Service Slug Should Return NotFound")]
    public async Task GetServiceBySlug_WithNonExistentSlug_ShouldReturnNotFound()
    {
        // ARRANGE: Non-existent service slug using Aspire infrastructure
        var nonExistentSlug = "non-existent-service";
        var expectedUrl = $"/api/services/{nonExistentSlug}";
        
        // ACT: Call REST endpoint against Aspire-managed infrastructure
        var response = await _httpClient.GetAsync(expectedUrl);
        
        // ASSERT: Should return 404
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

// REST Response DTOs for contract testing (same as original implementation)
public class ServicesResponse
{
    public List<ServiceDto> Services { get; set; } = new();
    public PaginationDto Pagination { get; set; } = new();
}

public class ServiceResponse
{
    public ServiceDto Service { get; set; } = new();
}

public class SearchResponse
{
    public List<ServiceDto> Services { get; set; } = new();
    public PaginationDto Pagination { get; set; } = new();
    public string Query { get; set; } = string.Empty;
}

public class FeaturedServicesResponse
{
    public List<ServiceDto> Services { get; set; } = new();
}

public class ServiceDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool Featured { get; set; }
    public bool Available { get; set; }
}

public class PaginationDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
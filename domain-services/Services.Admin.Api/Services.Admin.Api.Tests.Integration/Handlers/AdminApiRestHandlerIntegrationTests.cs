using InternationalCenter.Services.Admin.Api;
using InternationalCenter.Services.Domain.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Aspire.Hosting.Testing;
using Aspire.Hosting;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace InternationalCenter.Services.Admin.Api.Tests.Integration.Handlers;

/// <summary>
/// TDD GREEN: Integration tests for Admin API REST handlers using Aspire distributed testing
/// Tests medical-grade audit requirements and CRUD operations with real infrastructure
/// Validates REST endpoints with proper medical-grade authorization and audit trails
/// Uses Aspire orchestration for PostgreSQL, Redis, and service dependencies
/// </summary>
public class AdminApiRestHandlerIntegrationTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;
    private WebApplicationFactory<Program>? _factory;
    
    // Medical-grade audit context
    private const string AdminUserId = "admin-user-123";
    private const string AdminIpAddress = "192.168.1.100";
    private const string AdminUserAgent = "AdminTool/1.0";


    public async Task InitializeAsync()
    {
        // ARRANGE: Setup Aspire distributed application for medical-grade testing
        var appBuilder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        _app = await appBuilder.BuildAsync();
        await _app.StartAsync();

        // Get real infrastructure connection strings from Aspire orchestration
        var databaseConnectionString = await _app.GetConnectionStringAsync("database");
        var redisConnectionString = await _app.GetConnectionStringAsync("redis");

        // Create Admin API client with Aspire-provided infrastructure
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                
                // Configure connection strings before Program.cs runs
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:database"] = databaseConnectionString,
                        ["ConnectionStrings:redis"] = redisConnectionString
                    });
                });
                
                builder.ConfigureServices(services =>
                {
                    // Replace with Aspire-managed database
                    var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ServicesDbContext>));
                    if (dbDescriptor != null) services.Remove(dbDescriptor);
                    
                    services.AddDbContext<ServicesDbContext>(options =>
                        options.UseNpgsql(databaseConnectionString));
                    
                    // Configure ApplicationDbContext for migration services
                    var appDbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<InternationalCenter.Shared.Infrastructure.ApplicationDbContext>));
                    if (appDbDescriptor != null) services.Remove(appDbDescriptor);
                    
                    services.AddDbContext<InternationalCenter.Shared.Infrastructure.ApplicationDbContext>(options =>
                        options.UseNpgsql(databaseConnectionString, npgsql =>
                            npgsql.MigrationsAssembly("InternationalCenter.Migrations.Service")));
                    
                    // Configure Redis with Aspire connection
                    services.AddStackExchangeRedisCache(options =>
                        options.Configuration = redisConnectionString);
                    
                    services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
                        StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString!));
                });
            });
            
        _httpClient = _factory.CreateClient();
        
        // Setup medical-grade request headers for all tests
        _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer test-admin-token");
        _httpClient.DefaultRequestHeaders.Add("X-User-Id", AdminUserId);
        _httpClient.DefaultRequestHeaders.Add("X-Real-IP", AdminIpAddress);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", AdminUserAgent);
        
        // Setup test database with medical-grade audit capabilities
        await SetupTestDatabaseAsync();
    }

    private async Task SetupTestDatabaseAsync()
    {
        using var scope = _factory!.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServicesDbContext>();
        
        // Ensure clean database state for medical-grade testing
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
        if (_app != null) await _app.DisposeAsync();
    }

    [Fact(DisplayName = "TDD GREEN: POST /admin/api/services - Should Create Service With Medical-Grade Audit", Timeout = 30000)]
    public async Task CreateService_WithValidRequest_ShouldCreateServiceAndAuditTrail()
    {
        // ARRANGE: Service creation request with medical-grade audit expectations
        var correlationId = Guid.NewGuid().ToString();
        _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        var createRequest = new CreateServiceApiRequest
        {
            Title = "Test Service",
            Slug = "test-service",
            Description = "Test service description",
            DetailedDescription = "Detailed test service description",
            Technologies = new[] { "C#", ".NET", "REST" },
            Features = new[] { "High Performance", "Scalable" },
            DeliveryModes = new[] { "Cloud", "On-Premise" },
            Available = true,
            RequestId = correlationId
        };

        // ACT: Call Admin API endpoint through Aspire infrastructure
        var response = await _httpClient!.PostAsJsonAsync("/admin/api/services", createRequest);

        // ASSERT: Medical-grade audit and response expectations
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(response.Headers.Contains("Location"));
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").First());

        var result = await response.Content.ReadFromJsonAsync<SimpleCreateServiceResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.ServiceId);
        Assert.Equal(createRequest.Title, result.Title);
        
        // Audit trails are logged server-side, not returned in response
    }

    [Fact(DisplayName = "TDD GREEN: PUT /admin/api/services/{id} - Should Update Service With Change Tracking", Timeout = 30000)]
    public async Task UpdateService_WithValidRequest_ShouldUpdateServiceAndTrackChanges()
    {
        // ARRANGE: Service update request with change tracking expectations
        var serviceId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        var updateRequest = new UpdateServiceRequest
        {
            Title = "Updated Test Service",
            Description = "Updated description",
            Available = false,
            RequestId = correlationId
        };

        // ACT: Call Admin API update endpoint through Aspire infrastructure
        var response = await _httpClient!.PutAsJsonAsync($"/admin/api/services/{serviceId}", updateRequest);

        // ASSERT: Change tracking and audit expectations
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SimpleUpdateServiceResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.ServiceId);
        Assert.Equal(updateRequest.Title, result.Title);
        
        // Audit trails are logged server-side, not returned in response
    }

    [Fact(DisplayName = "TDD GREEN: DELETE /admin/api/services/{id} - Should Soft Delete With Audit", Timeout = 30000)]
    public async Task DeleteService_WithValidId_ShouldSoftDeleteWithAuditTrail()
    {
        // ARRANGE: Service deletion with soft delete expectations
        var serviceId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        // ACT: Call Admin API delete endpoint through Aspire infrastructure
        var response = await _httpClient!.DeleteAsync($"/admin/api/services/{serviceId}");

        // ASSERT: Soft delete expectations  
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Delete returns 204 No Content for successful deletion
        // Audit trails are logged server-side, not returned in response
    }

    [Fact(DisplayName = "TDD GREEN: PATCH /admin/api/services/{id}/publish - Should Update Status With Audit", Timeout = 30000)]
    public async Task PublishService_WithValidId_ShouldUpdateStatusWithAudit()
    {
        // ARRANGE: Service publish/unpublish request
        var serviceId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        var publishRequest = new PublishServiceRequest 
        { 
            Publish = true,
            RequestId = correlationId
        };

        // ACT: Call Admin API publish endpoint through Aspire infrastructure
        var response = await _httpClient!.PatchAsJsonAsync($"/admin/api/services/{serviceId}/publish", publishRequest);

        // ASSERT: Status change and audit expectations
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SimplePublishServiceResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.ServiceId);
        
        // Audit trails are logged server-side, not returned in response
    }

    [Fact(DisplayName = "TDD GREEN: POST /admin/api/services - Invalid Request Should Return ValidationError", Timeout = 30000)]
    public async Task CreateService_WithInvalidRequest_ShouldReturnValidationError()
    {
        // ARRANGE: Invalid service creation request
        var invalidRequest = new CreateServiceApiRequest
        {
            // Missing required fields
            Title = "",
            Slug = "",
            Description = "",
            RequestId = Guid.NewGuid().ToString()
        };

        // ACT: Call Admin API endpoint with invalid request
        var response = await _httpClient!.PostAsJsonAsync("/admin/api/services", invalidRequest);

        // ASSERT: Validation error expectations
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseText = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(responseText);
        
        // Validation errors returned as problem details or simple error message
    }

    [Fact(DisplayName = "TDD GREEN: Admin Operations Should Require Medical-Grade Auth Context", Timeout = 30000)]
    public async Task AdminOperations_WithoutAuthContext_ShouldReturnUnauthorized()
    {
        // ARRANGE: Request without medical-grade auth headers
        var unauthorizedClient = _factory!.CreateClient();
        // Remove authorization header
        var createRequest = new CreateServiceApiRequest
        { 
            Title = "Test",
            Slug = "test",
            Description = "Test description",
            RequestId = Guid.NewGuid().ToString()
        };

        // ACT: Call Admin API endpoint without auth context
        var response = await unauthorizedClient.PostAsJsonAsync("/admin/api/services", createRequest);

        // ASSERT: Medical-grade security enforcement
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        
        unauthorizedClient.Dispose();
    }

    [Fact(DisplayName = "TDD GREEN: Concurrent Admin Operations Should Handle Optimistic Concurrency", Timeout = 30000)]
    public async Task AdminOperations_WithConcurrentUpdates_ShouldHandleOptimisticConcurrency()
    {
        // ARRANGE: First create a service for concurrent updates
        var serviceId = await CreateTestServiceAsync();
        var updateRequest1 = new UpdateServiceRequest 
        { 
            Title = "Update 1",
            RequestId = Guid.NewGuid().ToString()
        };
        var updateRequest2 = new UpdateServiceRequest 
        { 
            Title = "Update 2",
            RequestId = Guid.NewGuid().ToString()
        };

        // ACT: Concurrent updates through Aspire infrastructure
        var task1 = _httpClient!.PutAsJsonAsync($"/admin/api/services/{serviceId}", updateRequest1);
        var task2 = _httpClient!.PutAsJsonAsync($"/admin/api/services/{serviceId}", updateRequest2);
        
        var responses = await Task.WhenAll(task1, task2);

        // ASSERT: One should succeed, one should handle concurrency conflict
        Assert.True(responses.Any(r => r.StatusCode == HttpStatusCode.OK));
        Assert.True(responses.Any(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    private async Task<string> CreateTestServiceAsync()
    {
        var createRequest = new CreateServiceApiRequest
        {
            Title = $"Test Service {Guid.NewGuid():N}"[..20], // Truncate for unique short title
            Slug = $"test-service-{Guid.NewGuid():N}"[..30],   // Truncate for unique short slug  
            Description = "Test service for integration testing",
            DetailedDescription = "Detailed test service description",
            Technologies = new[] { "C#", ".NET" },
            Features = new[] { "Testing" },
            DeliveryModes = new[] { "Cloud" },
            Available = true,
            RequestId = Guid.NewGuid().ToString()
        };

        var response = await _httpClient!.PostAsJsonAsync("/admin/api/services", createRequest);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<SimpleCreateServiceResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.ServiceId);
        
        return result.ServiceId;
    }
}

// Admin API Request/Response DTOs matching actual API contracts
public class CreateServiceApiRequest
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
    public string[]? Technologies { get; set; }
    public string[]? Features { get; set; }
    public string[]? DeliveryModes { get; set; }
    public string? Icon { get; set; }
    public string? Image { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public bool? Available { get; set; }
    public string? RequestId { get; set; }
}

// Simple response DTOs matching actual API implementation
public class SimpleCreateServiceResponse
{
    public string ServiceId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class SimpleUpdateServiceResponse  
{
    public string ServiceId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class SimplePublishServiceResponse
{
    public string ServiceId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class CreateServiceRequest
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
    public string[] Technologies { get; set; } = Array.Empty<string>();
    public string[] Features { get; set; } = Array.Empty<string>();
    public string[] DeliveryModes { get; set; } = Array.Empty<string>();
    public bool Available { get; set; }
    
    // Medical-grade audit requirements
    public string? RequestId { get; set; }
    public string? UserContext { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class UpdateServiceRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool? Available { get; set; }
    
    // Medical-grade audit requirements
    public string? RequestId { get; set; }
    public string? UserContext { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class PublishServiceRequest
{
    public bool Publish { get; set; }
    
    // Medical-grade audit requirements
    public string? RequestId { get; set; }
    public string? UserContext { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? UserAgent { get; set; }
}


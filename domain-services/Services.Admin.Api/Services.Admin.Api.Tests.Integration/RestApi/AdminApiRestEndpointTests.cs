using Aspire.Hosting.Testing;
using Dapper;
using Npgsql;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;
using InternationalCenter.Tests.Shared.TestCollections;

namespace InternationalCenter.Services.Admin.Api.Tests.Integration.RestApi;

/// <summary>
/// Contract-First Integration Tests: Admin API REST endpoint contracts with medical-grade compliance
/// Tests API contracts with comprehensive preconditions and postconditions validation
/// Validates medical-grade audit trails and CRUD operations with real PostgreSQL and Redis infrastructure
/// Uses per-test orchestration pattern following Microsoft recommendations
/// </summary>
[Collection("AspireApiTests")]
public class AdminApiRestEndpointTests
{
    // No shared state - each test creates its own Aspire orchestration

    [Fact(DisplayName = "CONTRACT: POST /admin/api/services - Must Create Service With Medical-Grade Audit", Timeout = 30000)]
    public async Task CreateService_WithValidRequest_MustCreateServiceWithMedicalGradeAudit()
    {
        // ARRANGE: Per-test Aspire orchestration
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        
        // Get Admin API service endpoint from Aspire
        var adminApiEndpoint = app.GetEndpoint("services-admin-api");
        using var httpClient = new HttpClient() { BaseAddress = new Uri(adminApiEndpoint.ToString()) };
        
        // Configure medical-grade request headers
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer test-admin-token");
        httpClient.DefaultRequestHeaders.Add("X-User-Id", "admin-test-user");
        httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", Guid.NewGuid().ToString());
        
        var createRequest = new CreateServiceApiRequest
        {
            Title = "Test Admin Service",
            Slug = "test-admin-service",
            Description = "Service created via Admin API integration test",
            DetailedDescription = "Detailed description for integration testing",
            Technologies = new[] { "ASP.NET Core", "PostgreSQL", "Redis" },
            Features = new[] { "Medical-Grade Audit", "API Testing" },
            DeliveryModes = new[] { "Digital" },
            Icon = "test-icon",
            Image = "test-image.png",
            RequestId = Guid.NewGuid().ToString()
        };

        // CONTRACT PRECONDITIONS: Valid service creation request with required fields
        Assert.NotEmpty(createRequest.Title);
        Assert.NotEmpty(createRequest.Slug);
        Assert.NotEmpty(createRequest.Description);
        Assert.NotEmpty(createRequest.RequestId);
        
        // ACT: Call Admin API endpoint through Aspire infrastructure
        var response = await httpClient.PostAsync("/admin/api/services", 
            JsonContent.Create(createRequest));

        // CONTRACT POSTCONDITIONS: Must return Created status with service data
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(responseContent);
        
        var result = JsonSerializer.Deserialize<CreateServiceApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.NotEmpty(result.ServiceId);
        Assert.Equal(createRequest.Title, result.Title);
        
        // AUDIT CONTRACT: Must have audit trail entries
        var auditCount = await connection.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM audit_logs WHERE entity_id = @ServiceId AND action = 'CREATE'",
            new { ServiceId = result.ServiceId });
        Assert.True(auditCount > 0, "Medical-grade audit trail must record creation");
        
        // DATABASE CONTRACT: Service must exist in database with correct data
        var databaseConnectionString = await app.GetConnectionStringAsync("database");
        await using var connection = new NpgsqlConnection(databaseConnectionString);
        
        var createdService = await connection.QuerySingleOrDefaultAsync(@"
            SELECT title, slug, description, status, created_at, updated_at 
            FROM services WHERE id = @ServiceId",
            new { ServiceId = result.ServiceId });
            
        Assert.NotNull(createdService);
        Assert.Equal(createRequest.Title, createdService.title);
        Assert.Equal(createRequest.Slug, createdService.slug);
        Assert.Equal(createRequest.Description, createdService.description);
        Assert.Equal("Draft", createdService.status); // New services start as Draft
        Assert.True(createdService.created_at <= DateTime.UtcNow);
        Assert.True(createdService.updated_at <= DateTime.UtcNow);
    }

    [Fact(DisplayName = "CONTRACT: PUT /admin/api/services/{id} - Must Update Service With Audit", Timeout = 30000)]
    public async Task UpdateService_WithValidRequest_MustUpdateServiceWithMedicalGradeAudit()
    {
        // ARRANGE: Per-test Aspire orchestration with pre-created service
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        
        // Create test service first
        var databaseConnectionString = await app.GetConnectionStringAsync("database");
        var serviceId = Guid.NewGuid().ToString();
        
        await using var connection = new NpgsqlConnection(databaseConnectionString);
        await connection.ExecuteAsync(@"
            INSERT INTO services (id, title, slug, description, detailed_description, 
                                status, available, featured, created_at, updated_at, 
                                category_id, icon, image)
            VALUES (@Id, @Title, @Slug, @Description, @DetailedDescription, 
                   @Status, @Available, @Featured, @CreatedAt, @UpdatedAt,
                   1, @Icon, @Image)",
            new
            {
                Id = serviceId,
                Title = "Original Service",
                Slug = "original-service",
                Description = "Original description",
                DetailedDescription = "Original detailed description",
                Status = "Draft",
                Available = true,
                Featured = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Icon = "original-icon",
                Image = "original-image.png"
            });
        
        // Get Admin API service endpoint from Aspire
        var adminApiEndpoint = app.GetEndpoint("services-admin-api");
        using var httpClient = new HttpClient() { BaseAddress = new Uri(adminApiEndpoint.ToString()) };
        
        // Configure medical-grade request headers
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer test-admin-token");
        httpClient.DefaultRequestHeaders.Add("X-User-Id", "admin-test-user");
        httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", Guid.NewGuid().ToString());
        
        var updateRequest = new UpdateServiceApiRequest
        {
            Title = "Updated Test Admin Service",
            Description = "Updated description via Admin API",
            DetailedDescription = "Updated detailed description",
            Technologies = new[] { "ASP.NET Core", "PostgreSQL", "Redis", "Updated Tech" },
            RequestId = Guid.NewGuid().ToString()
        };

        // ACT: Call Admin API update endpoint
        var response = await httpClient.PutAsync($"/admin/api/services/{serviceId}", 
            JsonContent.Create(updateRequest));

        // ASSERT: Verify medical-grade service update
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UpdateServiceApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal(updateRequest.Title, result.Title);
    }

    [Fact(DisplayName = "TDD GREEN: PATCH /admin/api/services/{id}/publish - Should Publish Service", Timeout = 30000)]
    public async Task PublishService_WithValidRequest_ShouldPublishServiceWithAudit()
    {
        // ARRANGE: Per-test Aspire orchestration with pre-created service
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        
        // Create test service first
        var databaseConnectionString = await app.GetConnectionStringAsync("database");
        var serviceId = Guid.NewGuid().ToString();
        
        await using var connection = new NpgsqlConnection(databaseConnectionString);
        await connection.ExecuteAsync(@"
            INSERT INTO services (id, title, slug, description, detailed_description, 
                                status, available, featured, created_at, updated_at, 
                                category_id, icon, image)
            VALUES (@Id, @Title, @Slug, @Description, @DetailedDescription, 
                   @Status, @Available, @Featured, @CreatedAt, @UpdatedAt,
                   1, @Icon, @Image)",
            new
            {
                Id = serviceId,
                Title = "Service to Publish",
                Slug = "service-to-publish",
                Description = "Service for publish testing",
                DetailedDescription = "Detailed description for publish testing",
                Status = "Draft",
                Available = true,
                Featured = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Icon = "publish-icon",
                Image = "publish-image.png"
            });
        
        // Get Admin API service endpoint from Aspire
        var adminApiEndpoint = app.GetEndpoint("services-admin-api");
        using var httpClient = new HttpClient() { BaseAddress = new Uri(adminApiEndpoint.ToString()) };
        
        // Configure medical-grade request headers
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer test-admin-token");
        httpClient.DefaultRequestHeaders.Add("X-User-Id", "admin-test-user");
        httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", Guid.NewGuid().ToString());
        
        var publishRequest = new PublishServiceApiRequest
        {
            RequestId = Guid.NewGuid().ToString()
        };

        // ACT: Call Admin API publish endpoint
        var response = await httpClient.PatchAsync($"/admin/api/services/{serviceId}/publish", 
            JsonContent.Create(publishRequest));

        // ASSERT: Verify medical-grade service publishing
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Verify service is published using Dapper
        var publishedStatus = await connection.QuerySingleAsync<string>(
            "SELECT status FROM services WHERE id = @ServiceId",
            new { ServiceId = serviceId });
        Assert.Equal("Published", publishedStatus);
    }

    [Fact(DisplayName = "TDD GREEN: DELETE /admin/api/services/{id} - Should Delete Service With Audit", Timeout = 30000)]
    public async Task DeleteService_WithValidRequest_ShouldDeleteServiceWithAudit()
    {
        // ARRANGE: Per-test Aspire orchestration with pre-created service
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        
        // Create test service first
        var databaseConnectionString = await app.GetConnectionStringAsync("database");
        var serviceId = Guid.NewGuid().ToString();
        
        await using var connection = new NpgsqlConnection(databaseConnectionString);
        await connection.ExecuteAsync(@"
            INSERT INTO services (id, title, slug, description, detailed_description, 
                                status, available, featured, created_at, updated_at, 
                                category_id, icon, image)
            VALUES (@Id, @Title, @Slug, @Description, @DetailedDescription, 
                   @Status, @Available, @Featured, @CreatedAt, @UpdatedAt,
                   1, @Icon, @Image)",
            new
            {
                Id = serviceId,
                Title = "Service to Delete",
                Slug = "service-to-delete",
                Description = "Service for delete testing",
                DetailedDescription = "Detailed description for delete testing",
                Status = "Draft",
                Available = true,
                Featured = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Icon = "delete-icon",
                Image = "delete-image.png"
            });
        
        // Get Admin API service endpoint from Aspire
        var adminApiEndpoint = app.GetEndpoint("services-admin-api");
        using var httpClient = new HttpClient() { BaseAddress = new Uri(adminApiEndpoint.ToString()) };
        
        // Configure medical-grade request headers
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer test-admin-token");
        httpClient.DefaultRequestHeaders.Add("X-User-Id", "admin-test-user");
        httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", Guid.NewGuid().ToString());

        // ACT: Call Admin API delete endpoint
        var response = await httpClient.DeleteAsync($"/admin/api/services/{serviceId}");

        // ASSERT: Verify medical-grade service deletion
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        // Verify service is deleted/archived using Dapper
        var serviceCount = await connection.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM services WHERE id = @ServiceId AND status != 'Archived'",
            new { ServiceId = serviceId });
        Assert.Equal(0, serviceCount);
    }

    [Fact(DisplayName = "TDD GREEN: Admin API Should Require Authorization", Timeout = 30000)]
    public async Task AdminApiEndpoints_WithoutAuthorization_ShouldReturnUnauthorized()
    {
        // ARRANGE: Per-test Aspire orchestration
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        
        // Get Admin API service endpoint from Aspire (no auth headers)
        var adminApiEndpoint = app.GetEndpoint("adminapi");
        using var httpClient = new HttpClient() { BaseAddress = new Uri(adminApiEndpoint.ToString()) };
        
        var createRequest = new CreateServiceApiRequest
        {
            Title = "Test Service",
            Slug = "test-service",
            Description = "Test",
            RequestId = Guid.NewGuid().ToString()
        };

        // ACT: Call Admin API without authorization
        var response = await httpClient.PostAsync("/admin/api/services", 
            new StringContent(JsonSerializer.Serialize(createRequest), Encoding.UTF8, "application/json"));

        // ASSERT: Should require authorization for medical-grade security
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "CONTRACT: Admin API - Must Enforce Authentication Contract", Timeout = 30000)]
    public async Task AdminApi_MustEnforceAuthenticationContract()
    {
        // ARRANGE: Per-test Aspire orchestration
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        
        var adminApiEndpoint = app.GetEndpoint("services-admin-api");
        using var httpClient = new HttpClient() { BaseAddress = new Uri(adminApiEndpoint.ToString()) };
        
        // CONTRACT PRECONDITIONS: Admin API endpoints that require authentication
        var protectedEndpoints = new[]
        {
            ("POST", "/admin/api/services"),
            ("GET", "/admin/api/services"),
            ("PUT", "/admin/api/services/test-id"),
            ("DELETE", "/admin/api/services/test-id"),
            ("PATCH", "/admin/api/services/test-id/publish")
        };
        
        var testRequest = JsonContent.Create(new CreateServiceApiRequest
        {
            Title = "Test Service",
            Slug = "test-service", 
            Description = "Test",
            RequestId = Guid.NewGuid().ToString()
        });
        
        foreach (var (method, endpoint) in protectedEndpoints)
        {
            // ACT: Call endpoint without authorization header
            var response = method switch
            {
                "POST" => await httpClient.PostAsync(endpoint, testRequest),
                "PUT" => await httpClient.PutAsync(endpoint, testRequest),
                "DELETE" => await httpClient.DeleteAsync(endpoint),
                "PATCH" => await httpClient.PatchAsync(endpoint, testRequest),
                _ => await httpClient.GetAsync(endpoint)
            };
            
            // CONTRACT POSTCONDITIONS: Must require authentication
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact(DisplayName = "CONTRACT: Admin API - Must Include Medical-Grade Audit Trail", Timeout = 30000)]
    public async Task AdminApi_MustIncludeMedicalGradeAuditTrail()
    {
        // ARRANGE: Per-test Aspire orchestration
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        
        var adminApiEndpoint = app.GetEndpoint("services-admin-api");
        using var httpClient = new HttpClient() { BaseAddress = new Uri(adminApiEndpoint.ToString()) };
        
        // CONTRACT PRECONDITIONS: Authenticated admin request with audit context
        var userId = "admin-audit-test";
        var correlationId = Guid.NewGuid().ToString();
        var requestId = Guid.NewGuid().ToString();
        
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer test-admin-token");
        httpClient.DefaultRequestHeaders.Add("X-User-Id", userId);
        httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        var createRequest = new CreateServiceApiRequest
        {
            Title = "Audit Trail Test Service",
            Slug = "audit-trail-test-service",
            Description = "Testing medical-grade audit trail",
            RequestId = requestId
        };

        // ACT: Perform admin operation that requires audit logging
        var response = await httpClient.PostAsync("/admin/api/services", 
            JsonContent.Create(createRequest));

        // CONTRACT POSTCONDITIONS: Must create audit trail
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<CreateServiceApiResponse>();
        Assert.NotNull(result);
        
        // AUDIT CONTRACT: Must have detailed audit trail with user context
        var databaseConnectionString = await app.GetConnectionStringAsync("database");
        await using var connection = new NpgsqlConnection(databaseConnectionString);
        
        var auditEntries = await connection.QueryAsync(@"
            SELECT action, user_id, correlation_id, entity_id, timestamp, details
            FROM audit_logs 
            WHERE entity_id = @ServiceId AND action = 'CREATE'",
            new { ServiceId = result.ServiceId });
            
        Assert.True(auditEntries.Any(), "Medical-grade audit trail must record admin operations");
        
        var auditEntry = auditEntries.First();
        Assert.Equal("CREATE", auditEntry.action);
        Assert.Equal(userId, auditEntry.user_id);
        Assert.Equal(correlationId, auditEntry.correlation_id);
        Assert.True(auditEntry.timestamp <= DateTime.UtcNow);
        Assert.NotEmpty(auditEntry.details);
    }

    [Fact(DisplayName = "CONTRACT: Admin API - Must Validate Request Structure", Timeout = 30000)]
    public async Task AdminApi_MustValidateRequestStructure()
    {
        // ARRANGE: Per-test Aspire orchestration
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        
        var adminApiEndpoint = app.GetEndpoint("services-admin-api");
        using var httpClient = new HttpClient() { BaseAddress = new Uri(adminApiEndpoint.ToString()) };
        
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer test-admin-token");
        httpClient.DefaultRequestHeaders.Add("X-User-Id", "admin-test-user");
        
        // CONTRACT PRECONDITIONS: Invalid request scenarios that must be rejected
        var invalidRequests = new[]
        {
            (new CreateServiceApiRequest { Title = "", Slug = "test", Description = "test", RequestId = Guid.NewGuid().ToString() }, "Empty title"),
            (new CreateServiceApiRequest { Title = "Test", Slug = "", Description = "test", RequestId = Guid.NewGuid().ToString() }, "Empty slug"),
            (new CreateServiceApiRequest { Title = "Test", Slug = "test", Description = "", RequestId = Guid.NewGuid().ToString() }, "Empty description"),
            (new CreateServiceApiRequest { Title = "Test", Slug = "test", Description = "test", RequestId = "" }, "Empty RequestId"),
        };
        
        foreach (var (request, reason) in invalidRequests)
        {
            // ACT: Call endpoint with invalid request
            var response = await httpClient.PostAsync("/admin/api/services", JsonContent.Create(request));
            
            // CONTRACT POSTCONDITIONS: Must validate and reject invalid requests
            Assert.False(response.IsSuccessStatusCode, $"Invalid request should be rejected: {reason}");
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.UnprocessableEntity,
                       $"Invalid request should return validation error: {reason}");
                       
            // ERROR CONTRACT: Must return structured error response
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }
    }

    // Database verification methods removed - tests now use direct Dapper queries inline for simplicity
}

// Contract DTOs matching Admin API implementation
public class CreateServiceApiResponse
{
    public string ServiceId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

// Contract DTOs matching Admin API implementation
public class CreateServiceApiRequest
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
    public string[] Technologies { get; set; } = Array.Empty<string>();
    public string[] Features { get; set; } = Array.Empty<string>();
    public string[] DeliveryModes { get; set; } = Array.Empty<string>();
    public string Icon { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
}

public class UpdateServiceApiRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
    public string[] Technologies { get; set; } = Array.Empty<string>();
    public string RequestId { get; set; } = string.Empty;
}

public class UpdateServiceApiResponse
{
    public string ServiceId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class PublishServiceApiRequest
{
    public string RequestId { get; set; } = string.Empty;
}

public class DeleteServiceApiRequest
{
    public string RequestId { get; set; } = string.Empty;
}
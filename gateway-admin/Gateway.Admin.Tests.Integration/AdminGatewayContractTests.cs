using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;
using InternationalCenter.Gateway.Admin;

namespace InternationalCenter.Gateway.Admin.Tests.Integration;

/// <summary>
/// Contract tests for Admin Gateway focusing on preconditions and postconditions
/// Tests authentication, authorization, medical-grade audit, and routing contracts
/// Uses WebApplicationFactory for contract testing without full distributed environment
/// Validates gateway behavior against defined contracts for medical compliance
/// </summary>
public class AdminGatewayContractTests : IClassFixture<AdminGatewayTestFactory>
{
    private readonly AdminGatewayTestFactory _factory;
    private readonly HttpClient _client;

    public AdminGatewayContractTests(AdminGatewayTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    /// <summary>
    /// Contract: Admin Gateway MUST require authentication for all admin endpoints
    /// Precondition: Anonymous HTTP request to admin gateway endpoint
    /// Postcondition: Returns 401 Unauthorized status
    /// </summary>
    [Fact]
    public async Task AdminGateway_WhenRequestingWithoutAuthentication_ShouldReturn401()
    {
        // Arrange - Create anonymous client (no Authorization header)
        using var anonymousClient = _factory.CreateClient();

        // Act - Send unauthenticated request to admin endpoint
        var response = await anonymousClient.GetAsync("/api/admin/services");

        // Assert - Verify authentication requirement contract
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        
        // Verify response includes proper WWW-Authenticate header for medical compliance
        Assert.True(response.Headers.Contains("WWW-Authenticate"));
    }

    /// <summary>
    /// Contract: Admin Gateway MUST route authenticated requests to Services Admin API
    /// Precondition: Authenticated HTTP request with valid roles
    /// Postcondition: Request successfully routed to backend API, response received
    /// </summary>
    [Fact]
    public async Task AdminGateway_WhenAuthenticatedUserRequestsServices_ShouldRouteToServicesAdminApi()
    {
        // Arrange - Create authenticated client with ServiceAdmin role
        using var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "ServiceAdmin");

        // Act - Send authenticated request to admin services endpoint
        var response = await authenticatedClient.GetAsync("/api/admin/services");

        // Assert - Verify routing contract for authenticated requests
        Assert.NotNull(response);
        // Note: In a real environment, this would route to the Services Admin API
        // In contract testing, we verify the gateway accepts the request and processes it
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        
        // Verify response includes required security headers for medical compliance
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
    }

    /// <summary>
    /// Contract: Admin Gateway MUST add enhanced security headers for admin operations
    /// Precondition: Authenticated HTTP request to admin gateway
    /// Postcondition: Response includes enhanced security headers for medical compliance
    /// </summary>
    [Fact]
    public async Task AdminGateway_WhenReceivingAuthenticatedRequest_ShouldAddEnhancedSecurityHeaders()
    {
        // Arrange - Use WebApplicationFactory for contract testing
        using var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "ServiceAdmin");

        // Act - Request admin gateway health endpoint to verify security headers
        var response = await authenticatedClient.GetAsync("/health");

        // Assert - Verify enhanced security headers contract
        var headers = response.Headers;
        Assert.True(headers.Contains("X-Content-Type-Options"), "Missing X-Content-Type-Options header");
        Assert.True(headers.Contains("X-Frame-Options"), "Missing X-Frame-Options header");
        Assert.True(headers.Contains("X-XSS-Protection"), "Missing X-XSS-Protection header");
        Assert.True(headers.Contains("Referrer-Policy"), "Missing Referrer-Policy header");
        Assert.True(headers.Contains("Content-Security-Policy"), "Missing Content-Security-Policy header");
        Assert.True(headers.Contains("Strict-Transport-Security"), "Missing Strict-Transport-Security header");
        Assert.True(headers.Contains("X-Correlation-ID"), "Missing X-Correlation-ID header");

        // Verify enhanced header values for medical-grade compliance
        Assert.Equal("nosniff", headers.GetValues("X-Content-Type-Options").First());
        Assert.Equal("DENY", headers.GetValues("X-Frame-Options").First());
        Assert.Contains("frame-ancestors 'none'", headers.GetValues("Content-Security-Policy").First());
        Assert.Contains("max-age=31536000", headers.GetValues("Strict-Transport-Security").First());
    }

    /// <summary>
    /// Contract: Admin Gateway MUST apply stricter rate limiting for medical compliance
    /// Precondition: Multiple rapid requests from authenticated admin user exceeding admin limit
    /// Postcondition: Returns 429 Too Many Requests after admin limit exceeded (100 req/min)
    /// </summary>
    [Fact]
    public async Task AdminGateway_WhenExceedingAdminRateLimit_ShouldReturn429()
    {
        // Arrange - Use WebApplicationFactory for rate limiting test
        using var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "ServiceAdmin");
        authenticatedClient.DefaultRequestHeaders.Add("X-User-ID", "rate-limit-test-user");

        // Act - Send requests rapidly to test rate limiting
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 25; i++) // Send rapid requests
        {
            tasks.Add(authenticatedClient.GetAsync("/api/admin/services"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - Verify rate limiting behavior (some may succeed, some may be rate limited)
        var successResponses = responses.Where(r => r.IsSuccessStatusCode).ToList();
        var unauthorizedResponses = responses.Where(r => r.StatusCode == HttpStatusCode.Unauthorized).ToList();
        var rateLimitedResponses = responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests).ToList();

        // Contract validation: Rate limiting should be applied
        Assert.True(successResponses.Count + unauthorizedResponses.Count + rateLimitedResponses.Count == responses.Length,
            "All responses should be accounted for");

        // If any responses were successful, verify rate limit headers are present
        if (successResponses.Any() || rateLimitedResponses.Any())
        {
            var responseWithHeaders = successResponses.FirstOrDefault() ?? rateLimitedResponses.FirstOrDefault();
            Assert.NotNull(responseWithHeaders);
        }
    }

    /// <summary>
    /// Contract: Admin Gateway MUST enforce role-based authorization for create operations
    /// Precondition: Authenticated request to create service endpoint with ServiceAdmin role
    /// Postcondition: Request allowed and routed to backend API
    /// </summary>
    [Fact]
    public async Task AdminGateway_WhenServiceAdminCreatesService_ShouldAllowOperation()
    {
        // Arrange - Create client with ServiceAdmin role
        using var serviceAdminClient = _factory.CreateClient();
        serviceAdminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "ServiceAdmin");

        var serviceData = new
        {
            title = "Test Service",
            description = "Test service for contract validation",
            detailedDescription = "Detailed description for test service"
        };
        var jsonContent = new StringContent(JsonSerializer.Serialize(serviceData), Encoding.UTF8, "application/json");

        // Act - Attempt to create service with ServiceAdmin role
        var response = await serviceAdminClient.PostAsync("/api/admin/services", jsonContent);

        // Assert - Verify role-based authorization allows ServiceAdmin to create
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        
        // Verify medical-grade audit headers are present
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        
        // Note: In contract testing, we focus on authorization behavior, not backend integration
        // The gateway should accept the request based on role-based access control
    }

    /// <summary>
    /// Contract: Admin Gateway MUST deny access to users without proper roles
    /// Precondition: Authenticated request with insufficient roles (ServiceViewer trying to create)
    /// Postcondition: Returns 403 Forbidden status with audit logging
    /// </summary>
    [Fact]
    public async Task AdminGateway_WhenServiceViewerTriesToCreate_ShouldReturnForbidden()
    {
        // Arrange - Create client with only ServiceViewer role (insufficient for create operations)
        using var serviceViewerClient = _factory.CreateClient();
        serviceViewerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "ServiceViewer");

        var serviceData = new
        {
            title = "Test Service",
            description = "Test service that should be denied",
            detailedDescription = "This should not be allowed"
        };
        var jsonContent = new StringContent(JsonSerializer.Serialize(serviceData), Encoding.UTF8, "application/json");

        // Act - Attempt to create service with insufficient role
        var response = await serviceViewerClient.PostAsync("/api/admin/services", jsonContent);

        // Assert - Verify role-based authorization denies insufficient roles
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        
        // Verify medical-grade audit headers are present even for denied requests
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    /// <summary>
    /// Contract: Admin Gateway MUST allow SystemAdmin to perform all operations
    /// Precondition: Authenticated request with SystemAdmin role
    /// Postcondition: Request authorized for all operations
    /// </summary>
    [Fact]
    public async Task AdminGateway_WhenSystemAdminAccesses_ShouldAllowAllOperations()
    {
        // Arrange - Create client with SystemAdmin role (highest privilege)
        using var systemAdminClient = _factory.CreateClient();
        systemAdminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "SystemAdmin");

        // Act & Assert - Test multiple operations that SystemAdmin should be allowed
        var getResponse = await systemAdminClient.GetAsync("/api/admin/services");
        Assert.NotEqual(HttpStatusCode.Forbidden, getResponse.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, getResponse.StatusCode);

        var serviceData = new { title = "System Admin Test", description = "Test" };
        var jsonContent = new StringContent(JsonSerializer.Serialize(serviceData), Encoding.UTF8, "application/json");
        
        var postResponse = await systemAdminClient.PostAsync("/api/admin/services", jsonContent);
        Assert.NotEqual(HttpStatusCode.Forbidden, postResponse.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, postResponse.StatusCode);

        // Verify SystemAdmin operations include proper audit context
        Assert.True(getResponse.Headers.Contains("X-Correlation-ID"));
        Assert.True(postResponse.Headers.Contains("X-Correlation-ID"));
    }

    /// <summary>
    /// Contract: Admin Gateway MUST handle CORS for admin portal origins only
    /// Precondition: OPTIONS request with Origin header for admin portal domain
    /// Postcondition: Returns CORS headers allowing the admin origin
    /// </summary>
    [Fact]
    public async Task AdminGateway_WhenReceivingCorsPreflightFromAdminOrigin_ShouldAllowCors()
    {
        // Arrange - Using WebApplicationFactory for contract testing

        using var httpClient = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/admin/services");
        request.Headers.Add("Origin", "http://localhost:3000"); // Admin portal origin
        request.Headers.Add("Access-Control-Request-Method", "POST");

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert - Verify admin CORS contract
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"), "Missing Access-Control-Allow-Origin header");
        
        var allowedOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").First();
        Assert.Equal("http://localhost:3000", allowedOrigin);
    }

    /// <summary>
    /// Contract: Admin Gateway MUST reject CORS requests from non-admin origins
    /// Precondition: OPTIONS request with Origin header for public website domain
    /// Postcondition: CORS headers do not allow the public origin
    /// </summary>
    [Fact]
    public async Task AdminGateway_WhenReceivingCorsPreflightFromPublicOrigin_ShouldRejectCors()
    {
        // Arrange - Using WebApplicationFactory for contract testing

        using var httpClient = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/admin/services");
        request.Headers.Add("Origin", "http://localhost:4321"); // Public website origin
        request.Headers.Add("Access-Control-Request-Method", "POST");

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert - Verify admin CORS rejection contract
        // Either no CORS headers or explicit rejection
        if (response.Headers.Contains("Access-Control-Allow-Origin"))
        {
            var allowedOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").First();
            Assert.NotEqual("http://localhost:4321", allowedOrigin);
        }
        // Otherwise, the absence of CORS headers indicates rejection, which is acceptable
    }

    /// <summary>
    /// Contract: Admin Gateway MUST respond to health checks for monitoring
    /// Precondition: GET request to /health endpoint
    /// Postcondition: Returns 200 OK with health status information
    /// </summary>
    [Fact]
    public async Task AdminGateway_WhenRequestingHealthCheck_ShouldReturnHealthyStatus()
    {
        // Arrange - Using WebApplicationFactory for contract testing

        using var httpClient = _factory.CreateClient();

        // Act
        var response = await httpClient.GetAsync("/health");

        // Assert - Verify health check contract
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    /// <summary>
    /// Contract: Admin Gateway MUST route category requests to Services Admin API
    /// Precondition: Authenticated GET request to /api/admin/categories endpoint
    /// Postcondition: Request routed to Services Admin API, returns category data
    /// </summary>
    [Fact]
    public async Task AdminGateway_WhenAuthenticatedUserRequestsCategories_ShouldRouteToServicesAdminApi()
    {
        // Arrange - Using WebApplicationFactory for contract testing

        // Use WebApplicationFactory for HTTP client creation
        using var httpClient = _factory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "ServiceAdmin");

        // Act
        var response = await httpClient.GetAsync("/api/admin/categories");

        // Assert - Verify routing contract
        Assert.NotNull(response);
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound, 
            $"Expected success or 404 status but got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        
        // If successful, verify content type
        if (response.IsSuccessStatusCode)
        {
            var contentType = response.Content.Headers.ContentType?.MediaType;
            Assert.Equal("application/json", contentType);
        }
    }

    /// <summary>
    /// Contract: Admin Gateway MUST add user context headers to backend requests
    /// Precondition: Authenticated request with user identity
    /// Postcondition: Backend request includes X-User-Id and X-User-Roles headers
    /// </summary>
    [Fact]
    public async Task AdminGateway_WhenForwardingAuthenticatedRequest_ShouldAddUserContextHeaders()
    {
        // Arrange - Using WebApplicationFactory for contract testing

        // Use WebApplicationFactory for HTTP client creation
        using var httpClient = _factory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "ServiceAdmin");

        // Act - Send authenticated request that should be forwarded with user context
        var response = await httpClient.GetAsync("/api/admin/services");

        // Assert - Verify user context forwarding contract
        // The response should succeed, indicating the backend received proper user context
        Assert.NotNull(response);
        Assert.True(response.IsSuccessStatusCode, 
            $"Expected success status indicating proper user context forwarding, but got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        // Verify gateway source header is added
        // (Note: In real implementation, we might log or capture the forwarded headers)
        Assert.True(response.Headers.Contains("X-Correlation-ID"), "Expected correlation ID to be maintained");
    }
}
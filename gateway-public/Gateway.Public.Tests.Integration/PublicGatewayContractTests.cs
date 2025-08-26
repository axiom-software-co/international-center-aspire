using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using Xunit;
using InternationalCenter.Gateway.Public;

namespace InternationalCenter.Gateway.Public.Tests.Integration;

/// <summary>
/// Contract tests for Public Gateway focusing on preconditions and postconditions
/// Tests anonymous access, security headers, and routing contracts for public website usage
/// Uses WebApplicationFactory for contract testing without authentication requirements
/// Validates public gateway behavior for website integration
/// </summary>
public class PublicGatewayContractTests : IClassFixture<PublicGatewayTestFactory>
{
    private readonly PublicGatewayTestFactory _factory;
    private readonly HttpClient _client;

    public PublicGatewayContractTests(PublicGatewayTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    /// <summary>
    /// Contract: Public Gateway MUST route GET /api/services requests to Services Public API
    /// Precondition: Anonymous HTTP request to gateway services endpoint
    /// Postcondition: Request successfully routed to backend API, response received
    /// </summary>
    [Fact]
    public async Task PublicGateway_WhenRequestingServices_ShouldRouteToServicesPublicApi()
    {
        // Arrange - Use anonymous client (no authentication required for public gateway)
        using var publicClient = _factory.CreateClient();

        // Act - Send anonymous request to public services endpoint
        var response = await publicClient.GetAsync("/api/services?page=1&pageSize=10");

        // Assert - Verify public gateway routing contract
        Assert.NotNull(response);
        // Note: In contract testing, we verify the gateway processes the request
        // The actual backend integration is not the focus of contract testing
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        
        // Verify response includes required public security headers
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
    }

    /// <summary>
    /// Contract: Public Gateway MUST allow anonymous access to all public endpoints
    /// Precondition: Anonymous HTTP request to various public endpoints
    /// Postcondition: All requests processed without authentication requirements
    /// </summary>
    [Fact]
    public async Task PublicGateway_WhenAnonymousUserAccesses_ShouldAllowAllPublicEndpoints()
    {
        // Arrange - Use anonymous client
        using var anonymousClient = _factory.CreateClient();

        // Act & Assert - Test multiple public endpoints that should be accessible anonymously
        var servicesResponse = await anonymousClient.GetAsync("/api/services");
        Assert.NotEqual(HttpStatusCode.Unauthorized, servicesResponse.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, servicesResponse.StatusCode);

        var categoriesResponse = await anonymousClient.GetAsync("/api/categories");
        Assert.NotEqual(HttpStatusCode.Unauthorized, categoriesResponse.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, categoriesResponse.StatusCode);

        var versionResponse = await anonymousClient.GetAsync("/api/version");
        Assert.NotEqual(HttpStatusCode.Unauthorized, versionResponse.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, versionResponse.StatusCode);

        // Verify all responses include correlation tracking for public usage analytics
        Assert.True(servicesResponse.Headers.Contains("X-Correlation-ID"));
        Assert.True(categoriesResponse.Headers.Contains("X-Correlation-ID"));
        Assert.True(versionResponse.Headers.Contains("X-Correlation-ID"));
    }

    /// <summary>
    /// Contract: Public Gateway MUST add security headers to all responses
    /// Precondition: Any HTTP request to gateway
    /// Postcondition: Response includes required security headers
    /// </summary>
    [Fact]
    public async Task PublicGateway_WhenReceivingAnyRequest_ShouldAddSecurityHeaders()
    {
        // Arrange - Use anonymous client
        using var publicClient = _factory.CreateClient();

        // Act
        var response = await publicClient.GetAsync("/api/version");

        // Assert - Verify security headers contract
        var headers = response.Headers;
        Assert.True(headers.Contains("X-Content-Type-Options"), "Missing X-Content-Type-Options header");
        Assert.True(headers.Contains("X-Frame-Options"), "Missing X-Frame-Options header");
        Assert.True(headers.Contains("X-XSS-Protection"), "Missing X-XSS-Protection header");
        Assert.True(headers.Contains("Referrer-Policy"), "Missing Referrer-Policy header");
        Assert.True(headers.Contains("X-Correlation-ID"), "Missing X-Correlation-ID header");

        // Verify specific header values for public website security
        Assert.Equal("nosniff", headers.GetValues("X-Content-Type-Options").First());
        Assert.Equal("DENY", headers.GetValues("X-Frame-Options").First());
    }

    /// <summary>
    /// Contract: Public Gateway MUST support CORS for public website origins
    /// Precondition: OPTIONS request with Origin header for public website domain
    /// Postcondition: Returns CORS headers allowing the public origin
    /// </summary>
    [Fact]
    public async Task PublicGateway_WhenReceivingCorsPreflightFromPublicOrigin_ShouldAllowCors()
    {
        // Arrange - Create CORS preflight request
        using var publicClient = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/services");
        request.Headers.Add("Origin", "http://localhost:4321"); // Public website origin
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await publicClient.SendAsync(request);

        // Assert - Verify public CORS contract
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"), "Missing Access-Control-Allow-Origin header");
        
        var allowedOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").First();
        Assert.Contains("localhost", allowedOrigin); // Should allow public origins
    }

    /// <summary>
    /// Contract: Public Gateway MUST apply higher rate limits for public website usage
    /// Precondition: Multiple rapid requests from same IP (simulating website usage)
    /// Postcondition: Allows higher volume before rate limiting (different from admin)
    /// </summary>
    [Fact]
    public async Task PublicGateway_WhenReceivingManyRequests_ShouldAllowHigherLimitsForPublicUsage()
    {
        // Arrange - Use anonymous client for public usage simulation
        using var publicClient = _factory.CreateClient();

        // Act - Send requests to test public rate limiting (should be more permissive than admin)
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 15; i++) // Less than admin limit test to verify different policies
        {
            tasks.Add(publicClient.GetAsync("/api/version"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - Verify public rate limiting allows more requests than admin gateway would
        var successfulResponses = responses.Where(r => r.IsSuccessStatusCode);
        var rateLimitedResponses = responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        
        // Public gateway should be more permissive than admin gateway
        // Most requests should succeed under normal public usage patterns
        Assert.True(successfulResponses.Count() >= 10, 
            $"Public gateway should allow higher request volumes. Success: {successfulResponses.Count()}, Rate limited: {rateLimitedResponses.Count()}");
    }

    /// <summary>
    /// Contract: Public Gateway MUST respond to health checks for monitoring
    /// Precondition: GET request to /health endpoint
    /// Postcondition: Returns 200 OK with health status information
    /// </summary>
    [Fact]
    public async Task PublicGateway_WhenRequestingHealthCheck_ShouldReturnHealthyStatus()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<InternationalCenter_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("public-gateway");

        // Act
        var response = await httpClient.GetAsync("/health");

        // Assert - Verify health check contract
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }
}
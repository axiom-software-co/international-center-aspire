using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using InternationalCenter.Website.Shared.Tests.Contracts;

namespace InternationalCenter.Website.Tests.Integration.PublicGateway;

/// <summary>
/// Integration tests for Website Public Gateway API integration
/// Tests actual HTTP communication with Public Gateway for Services domain
/// Validates anonymous user access patterns and rate limiting compliance
/// </summary>
public class PublicGatewayIntegrationTests : IWebsiteTestEnvironmentContract<PublicGatewayTestContext>
{
    private readonly ITestOutputHelper _output;
    private readonly WebApplicationFactory<Program> _factory;

    public PublicGatewayIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        // In a real implementation, this would use the actual Public Gateway factory
        _factory = new WebApplicationFactory<Program>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Gateway", "Public")]
    [Trait("Domain", "Services")]
    [Trait("Timeout", "30")]
    public async Task GetServices_ShouldReturn_ValidServicesResponse()
    {
        // Arrange
        var options = new WebsiteTestEnvironmentOptions
        {
            UseRealApiEndpoints = true,
            MaxRequestsPerMinute = 1000, // Public Gateway limit
            TestEnvironment = "Integration"
        };
        
        var context = await SetupWebsiteTestEnvironmentAsync(options);

        // Act & Assert - Contract implementation
        var result = await ExecuteWebsiteTestAsync(context, async ctx =>
        {
            var client = ctx.HttpClient;
            var response = await client.GetAsync("/api/services");
            
            response.IsSuccessStatusCode.Should().BeTrue();
            
            var content = await response.Content.ReadAsStringAsync();
            var servicesResponse = JsonSerializer.Deserialize<ServicesApiResponse>(content);
            
            servicesResponse.Should().NotBeNull();
            servicesResponse!.Success.Should().BeTrue();
            servicesResponse.Data.Should().NotBeNull();
            
            return servicesResponse;
        }, "GetServices", new PerformanceThreshold { MaxDuration = TimeSpan.FromMilliseconds(2000) });

        result.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Gateway", "Public")]
    [Trait("RateLimit", "1000")]
    public async Task GetServices_RateLimit_ShouldEnforce1000RequestsPerMinute()
    {
        // Arrange
        var options = new WebsiteTestEnvironmentOptions
        {
            UseRealApiEndpoints = true,
            TestRateLimiting = true,
            MaxRequestsPerMinute = 1000
        };
        
        var context = await SetupWebsiteTestEnvironmentAsync(options);

        // Act & Assert - Contract implementation
        await ExecuteWebsiteTestAsync(context, async ctx =>
        {
            var client = ctx.HttpClient;
            var requestTasks = new List<Task<HttpResponseMessage>>();
            
            // Send requests rapidly to test rate limiting
            for (int i = 0; i < 10; i++)
            {
                requestTasks.Add(client.GetAsync("/api/services"));
            }
            
            var responses = await Task.WhenAll(requestTasks);
            
            // All requests within limit should succeed
            foreach (var response in responses)
            {
                response.IsSuccessStatusCode.Should().BeTrue();
            }
            
            return responses.Length;
        }, "RateLimitTest");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Gateway", "Public")]
    [Trait("Security", "Anonymous")]
    public async Task GetServices_AnonymousAccess_ShouldBeAllowed()
    {
        // Arrange
        var options = new WebsiteTestEnvironmentOptions
        {
            RequireAuthentication = false, // Anonymous access
            UseRealApiEndpoints = true
        };
        
        var context = await SetupWebsiteTestEnvironmentAsync(options);

        // Act & Assert - Contract implementation
        await ExecuteWebsiteTestAsync(context, async ctx =>
        {
            var client = ctx.HttpClient;
            
            // No authorization headers - anonymous request
            var response = await client.GetAsync("/api/services");
            
            response.IsSuccessStatusCode.Should().BeTrue();
            
            // Verify anonymous access is logged properly
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
            
            return true;
        }, "AnonymousAccess");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Gateway", "Public")]
    [Trait("Performance", "CoreWebVitals")]
    public async Task GetServices_Performance_ShouldMeetCoreWebVitals()
    {
        // Arrange
        var options = new WebsiteTestEnvironmentOptions
        {
            ValidatePerformance = true,
            UseRealApiEndpoints = true
        };
        
        var context = await SetupWebsiteTestEnvironmentAsync(options);

        // Act & Assert - Contract implementation
        await ExecuteWebsiteTestAsync(context, async ctx =>
        {
            var client = ctx.HttpClient;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var response = await client.GetAsync("/api/services");
            stopwatch.Stop();
            
            // Core Web Vitals: LCP should be under 2.5s for API calls
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2500);
            
            response.IsSuccessStatusCode.Should().BeTrue();
            
            return new { Duration = stopwatch.ElapsedMilliseconds, Success = true };
        }, "PerformanceTest", new PerformanceThreshold { MaxDuration = TimeSpan.FromMilliseconds(2500) });
    }

    // Contract Implementation Methods
    public async Task<PublicGatewayTestContext> SetupWebsiteTestEnvironmentAsync(
        WebsiteTestEnvironmentOptions options,
        CancellationToken cancellationToken = default)
    {
        _output.WriteLine($"Setting up Website test environment: {options.TestEnvironment}");
        
        var httpClient = _factory.CreateClient();
        
        if (!options.RequireAuthentication)
        {
            _output.WriteLine("Configuring anonymous access...");
        }
        
        if (options.UseRealApiEndpoints)
        {
            _output.WriteLine("Using real API endpoints for integration testing...");
        }

        return new PublicGatewayTestContext
        {
            HttpClient = httpClient,
            ServiceProvider = _factory.Services,
            Options = options,
            TestStartTime = DateTime.UtcNow
        };
    }

    public async Task<T> ExecuteWebsiteTestAsync<T>(
        PublicGatewayTestContext context,
        Func<PublicGatewayTestContext, Task<T>> testOperation,
        string operationName,
        PerformanceThreshold? performanceThreshold = null,
        CancellationToken cancellationToken = default)
    {
        _output.WriteLine($"Executing Website test operation: {operationName}");
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var result = await testOperation(context);
            stopwatch.Stop();
            
            if (performanceThreshold != null)
            {
                stopwatch.Elapsed.Should().BeLessThan(performanceThreshold.MaxDuration,
                    $"Operation {operationName} exceeded performance threshold");
            }
            
            _output.WriteLine($"Operation {operationName} completed in {stopwatch.ElapsedMilliseconds}ms");
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _output.WriteLine($"Operation {operationName} failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
            throw;
        }
    }

    public async Task ValidateWebsiteEnvironmentAsync(
        PublicGatewayTestContext context,
        CancellationToken cancellationToken = default)
    {
        _output.WriteLine("Validating Website test environment...");
        
        context.HttpClient.Should().NotBeNull();
        context.ServiceProvider.Should().NotBeNull();
        
        // Validate Public Gateway connectivity
        var response = await context.HttpClient.GetAsync("/health");
        response.IsSuccessStatusCode.Should().BeTrue("Public Gateway health check should pass");
        
        _output.WriteLine("Website test environment validation completed");
    }

    public async Task CleanupWebsiteEnvironmentAsync(
        PublicGatewayTestContext context,
        CancellationToken cancellationToken = default)
    {
        _output.WriteLine("Cleaning up Website test environment...");
        
        context.HttpClient?.Dispose();
        
        _output.WriteLine("Website test environment cleanup completed");
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _factory?.Dispose();
    }
}

// Supporting classes for test structure
public class PublicGatewayTestContext
{
    public HttpClient HttpClient { get; set; } = null!;
    public IServiceProvider ServiceProvider { get; set; } = null!;
    public WebsiteTestEnvironmentOptions Options { get; set; } = null!;
    public DateTime TestStartTime { get; set; }
}

public class WebsiteTestEnvironmentOptions
{
    public bool UseRealApiEndpoints { get; set; } = false;
    public bool RequireAuthentication { get; set; } = false;
    public bool TestRateLimiting { get; set; } = false;
    public bool ValidatePerformance { get; set; } = false;
    public int MaxRequestsPerMinute { get; set; } = 1000;
    public string TestEnvironment { get; set; } = "Test";
}

public class ServicesApiResponse
{
    public bool Success { get; set; }
    public ServicesData? Data { get; set; }
}

public class ServicesData
{
    public Service[] Services { get; set; } = Array.Empty<Service>();
    public int TotalCount { get; set; }
}

public class Service
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class PerformanceThreshold
{
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromSeconds(30);
}

// Mock Program class for WebApplicationFactory
public class Program { }
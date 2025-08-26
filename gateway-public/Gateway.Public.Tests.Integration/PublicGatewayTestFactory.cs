using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InternationalCenter.Gateway.Public;

namespace InternationalCenter.Gateway.Public.Tests.Integration;

/// <summary>
/// Test factory for Public Gateway contract testing
/// Configures test environment for anonymous access, security headers, and routing testing
/// Validates public gateway behavior without authentication requirements
/// </summary>
public class PublicGatewayTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices(services =>
        {
            // Configure test logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise in tests
            });

            // Public Gateway doesn't require authentication
            // All endpoints should be accessible anonymously

            // Configure test YARP proxy destinations to mock backend Services Public API
            // In contract tests, we focus on gateway behavior, not backend integration

            // Disable real database connections for contract tests
            services.RemoveAll(typeof(Microsoft.EntityFrameworkCore.DbContext));

            // Configure test Redis cache (in-memory)
            services.RemoveAll<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
            services.AddDistributedMemoryCache();

            // Configure test audit service for anonymous logging
            services.Configure<InternationalCenter.Shared.Models.AuditConfiguration>(options =>
            {
                options.EnableAuditing = true;
                options.AuditCreates = false; // Public API is read-only
                options.AuditUpdates = false;
                options.AuditDeletes = false;
                options.AuditReads = true; // Track public access patterns
                options.MaxRetentionDays = 30; // Shorter retention for public logs
                options.EncryptSensitiveData = false; // No sensitive data in public API
                options.ExcludedProperties = new List<string>();
                options.SensitiveProperties = new List<string>();
            });
        });

        // Override configuration for test environment
        builder.UseSetting("ConnectionStrings:database", "Host=localhost;Database=test;");
        builder.UseSetting("ConnectionStrings:redis", "localhost:6379");
        builder.UseSetting("PublicAllowedOrigins:0", "http://localhost:4321");
        builder.UseSetting("PublicAllowedOrigins:1", "https://localhost:4321");
        builder.UseSetting("PublicAllowedOrigins:2", "http://localhost:3000");

        // Configure YARP reverse proxy with test destinations
        builder.UseSetting("ReverseProxy:Routes:public-services:ClusterId", "services-public-cluster");
        builder.UseSetting("ReverseProxy:Routes:public-services:Match:Path", "/api/{**catch-all}");
        builder.UseSetting("ReverseProxy:Clusters:services-public-cluster:Destinations:primary:Address", "http://localhost:5000");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure test environment is properly set
        builder.UseEnvironment("Testing");
        
        return base.CreateHost(builder);
    }
}
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using InternationalCenter.Shared.Infrastructure.Testing;
using InternationalCenter.Shared.Infrastructure;
using InternationalCenter.Shared.Extensions;
using InternationalCenter.Gateway.Admin;

namespace InternationalCenter.Gateway.Admin.Tests.Integration;

/// <summary>
/// Test factory for Admin Gateway contract testing
/// Configures test environment for authentication, authorization, and routing testing
/// Replaces external dependencies with test implementations for contract validation
/// </summary>
public class AdminGatewayTestFactory : WebApplicationFactory<Program>
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

            // Replace authentication with test authentication handler
            // This allows testing different authentication scenarios
            services.RemoveAll<IAuthenticationSchemeProvider>();
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });

            // Configure test database for audit persistence testing
            services.RemoveAll(typeof(DbContext));
            services.RemoveAll<ApplicationDbContext>();
            
            // Add in-memory database for audit persistence testing
            var testDatabaseName = $"AdminGatewayAuditTest_{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                options.UseInMemoryDatabase(testDatabaseName);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Configure test Redis cache (in-memory)
            services.RemoveAll<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
            services.AddMemoryCache();
            services.AddSingleton<Microsoft.Extensions.Caching.Distributed.IDistributedCache, 
                Microsoft.Extensions.Caching.Memory.MemoryDistributedCache>();

            // Configure medical-grade audit services for integration testing
            services.AddMedicalGradeAuditWithDefaults();
            
            // Override audit configuration for testing
            services.Configure<InternationalCenter.Shared.Models.AuditConfiguration>(options =>
            {
                options.EnableAuditing = true;
                options.AuditCreates = true;
                options.AuditUpdates = true;
                options.AuditDeletes = true;
                options.AuditReads = true; // Enable for testing
                options.MaxRetentionDays = 2555; // Medical compliance even in tests
                options.EncryptSensitiveData = false; // Disable encryption for easier testing
                options.BatchSize = 100; // Smaller batches for testing
                options.EnableArchiving = true;
                options.ExcludedProperties = new List<string> { "UpdatedAt" };
                options.SensitiveProperties = new List<string> { "Password", "Token" };
            });
        });

        // Override configuration for test environment
        builder.UseSetting("EntraExternalId:ClientId", "test-client-id");
        builder.UseSetting("EntraExternalId:Domain", "test.onmicrosoft.com");
        builder.UseSetting("ConnectionStrings:database", "Host=localhost;Database=test;");
        builder.UseSetting("ConnectionStrings:redis", "localhost:6379");
        builder.UseSetting("AdminAllowedOrigins:0", "http://localhost:3000");
        builder.UseSetting("AdminAllowedOrigins:1", "https://localhost:3000");

        // Configure YARP reverse proxy with test destinations
        builder.UseSetting("ReverseProxy:Routes:admin-services:ClusterId", "services-admin-cluster");
        builder.UseSetting("ReverseProxy:Routes:admin-services:Match:Path", "/api/admin/{**catch-all}");
        builder.UseSetting("ReverseProxy:Clusters:services-admin-cluster:Destinations:primary:Address", "http://localhost:5001");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure test environment is properly set
        builder.UseEnvironment("Testing");
        
        var host = base.CreateHost(builder);
        
        // Ensure test database is created and audit tables are ready
        using (var scope = host.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
            if (context != null)
            {
                context.Database.EnsureCreated();
            }
        }
        
        return host;
    }

    /// <summary>
    /// Ensures the test database is properly seeded with required data for audit persistence tests
    /// </summary>
    public async Task EnsureTestDatabaseSeededAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (context != null)
        {
            await context.Database.EnsureCreatedAsync();
            await context.SaveChangesAsync();
        }
    }
}
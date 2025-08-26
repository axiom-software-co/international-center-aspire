using InternationalCenter.Services.Public.Api.Infrastructure.Data;
using InternationalCenter.Tests.Shared.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace InternationalCenter.Services.Public.Api.Tests.Integration.Infrastructure;

/// <summary>
/// DEPRECATED: Custom WebApplicationFactory using old patterns
/// 
/// USE INSTEAD: DistributedApplicationTestingBuilder with proper Aspire integration testing
/// 
/// The new approach:
/// - Uses DistributedApplicationTestingBuilder for proper distributed application testing
/// - Tests real infrastructure integration through Aspire orchestration
/// - No manual service replacement - uses real PostgreSQL, Redis, and HTTP clients
/// - Follows medical-grade integration testing with real dependencies
/// 
/// Migration Guide:
/// 1. Replace WebApplicationFactory usage with DistributedApplicationTestingBuilder
/// 2. Remove manual service replacements
/// 3. Use _app.CreateHttpClient() for HTTP clients
/// 4. Use _app.GetConnectionStringAsync() for database connections
/// 5. Test against real infrastructure instead of in-memory substitutes
/// </summary>
[Obsolete("Use DistributedApplicationTestingBuilder instead. This class uses manual service replacement which violates integration testing principles.", true)]
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DatabaseFixture _databaseFixture;

    public TestWebApplicationFactory(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Configure test database connection string BEFORE host is built
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var connectionString = _databaseFixture.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DatabaseFixture connection string is not ready");
            }

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:database"] = connectionString,
                ["ConnectionStrings:redis"] = "localhost:6379" // Dummy Redis connection for testing
            });
        });

        // Replace production services with test-specific ones
        builder.ConfigureServices(services =>
        {
            // Replace DbContext configuration with test database
            services.RemoveAll<DbContextOptions<ServicesDbContext>>();
            services.RemoveAll<ServicesDbContext>();
            services.AddDbContext<ServicesDbContext>(options =>
                options.UseNpgsql(_databaseFixture.ConnectionString));

            // Remove Redis cache and replace with in-memory cache
            services.RemoveAll<IDistributedCache>();
            services.AddMemoryCache();
            services.AddSingleton<IDistributedCache, MemoryDistributedCache>();

            // Remove all health checks to prevent duplicate registration conflicts
            services.RemoveAll<Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck>();
            services.RemoveAll<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
            
            // Add minimal health checks for testing
            services.AddHealthChecks()
                .AddCheck("test", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Test environment"));
        });
    }
}
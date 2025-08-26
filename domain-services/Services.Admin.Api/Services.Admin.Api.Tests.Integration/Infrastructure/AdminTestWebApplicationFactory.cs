using InternationalCenter.Services.Domain.Infrastructure.Data;
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

namespace InternationalCenter.Services.Admin.Api.Tests.Integration.Infrastructure;

/// <summary>
/// DEPRECATED: Custom WebApplicationFactory for Services Admin API integration testing
/// 
/// USE INSTEAD: DistributedApplicationTestingBuilder with proper Aspire integration testing
/// 
/// The new approach:
/// - Uses DistributedApplicationTestingBuilder for proper distributed application testing
/// - Tests real infrastructure integration through Aspire orchestration
/// - No manual service replacement - uses real PostgreSQL, Redis, and HTTP clients
/// - Follows medical-grade integration testing with real dependencies and audit trails
/// - Supports proper EF Core integration for Admin API medical-grade compliance
/// 
/// Migration Guide:
/// 1. Replace WebApplicationFactory usage with DistributedApplicationTestingBuilder
/// 2. Remove manual EF Core DbContext replacements  
/// 3. Use _app.CreateHttpClient() for authenticated HTTP clients
/// 4. Use _app.GetConnectionStringAsync() for EF Core database connections
/// 5. Test against real infrastructure for medical-grade compliance validation
/// </summary>
[Obsolete("Use DistributedApplicationTestingBuilder instead. This class uses manual service replacement which violates medical-grade integration testing principles.", true)]
public class AdminTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DatabaseFixture _databaseFixture;

    public AdminTestWebApplicationFactory(DatabaseFixture databaseFixture)
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
            // Remove Redis cache and replace with in-memory cache
            services.RemoveAll<IDistributedCache>();
            services.AddMemoryCache();
            services.AddSingleton<IDistributedCache, MemoryDistributedCache>();
        });
    }
}
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
/// Custom WebApplicationFactory for Services Admin API integration testing
/// Uses Microsoft recommended patterns with real PostgreSQL infrastructure
/// </summary>
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
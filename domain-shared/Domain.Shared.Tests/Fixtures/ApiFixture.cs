using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Grpc.Net.Client;
using StackExchange.Redis;

namespace InternationalCenter.Tests.Shared.Fixtures;

/// <summary>
/// WebApplicationFactory-based fixture for API integration testing
/// Provides TestServer configuration with real database and cache integration
/// Microsoft recommended pattern for API testing
/// </summary>
public sealed class ApiFixture<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime 
    where TProgram : class
{
    private readonly DatabaseFixture _databaseFixture;
    private readonly CacheFixture _cacheFixture;
    
    public ApiFixture(DatabaseFixture databaseFixture, CacheFixture cacheFixture)
    {
        _databaseFixture = databaseFixture;
        _cacheFixture = cacheFixture;
    }

    public string DatabaseConnectionString => _databaseFixture.ConnectionString;
    public string CacheConnectionString => _cacheFixture.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registrations
            services.RemoveAll(typeof(DbContext));
            
            // Configure test database connection
            services.AddDbContext<DbContext>(options =>
                options.UseNpgsql(_databaseFixture.ConnectionString));

            // Configure test Redis connection
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton<IConnectionMultiplexer>(_ => _cacheFixture.Connection);
            
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = _cacheFixture.ConnectionString;
                options.InstanceName = "TestInstance";
            });

            // Configure logging for test output
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning);
                builder.AddConsole();
            });
        });

        // Use test environment
        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Creates a gRPC client for testing gRPC services
    /// Microsoft recommended pattern for gRPC testing
    /// </summary>
    public GrpcChannel CreateGrpcChannel()
    {
        var httpClient = CreateClient();
        return GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions
        {
            HttpClient = httpClient
        });
    }

    /// <summary>
    /// Creates a typed gRPC client for service testing
    /// </summary>
    public T CreateGrpcClient<T>() where T : class
    {
        var channel = CreateGrpcChannel();
        return (T)Activator.CreateInstance(typeof(T), channel)!;
    }

    /// <summary>
    /// Ensures database is migrated and ready for testing
    /// </summary>
    public async Task EnsureDatabaseReadyAsync<TDbContext>() where TDbContext : DbContext
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Resets test state between test methods
    /// </summary>
    public async Task ResetTestStateAsync()
    {
        await _databaseFixture.ResetDatabaseAsync();
        await _cacheFixture.ClearCacheAsync();
    }

    /// <summary>
    /// Validates service health endpoints
    /// </summary>
    public async Task<bool> ValidateServiceHealthAsync()
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task InitializeAsync()
    {
        // Fixtures are initialized by test class
        await Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
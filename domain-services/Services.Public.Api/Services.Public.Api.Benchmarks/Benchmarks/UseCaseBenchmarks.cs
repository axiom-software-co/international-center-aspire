using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using InternationalCenter.Services.Public.Api.Application.UseCases;
using InternationalCenter.Services.Public.Api.Infrastructure.Repositories.Dapper;
using InternationalCenter.Services.Public.Api.Infrastructure.Repositories.Interfaces;
using InternationalCenter.Shared.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InternationalCenter.Services.Public.Api.Benchmarks.Benchmarks;

/// <summary>
/// BenchmarkDotNet performance tests for Services.Public.Api use cases
/// WHY: Use case performance includes validation, business logic, and repository access
/// SCOPE: GetServiceBySlugUseCase, GetServiceCategoriesUseCase, ServiceQueryUseCase
/// CONTEXT: Public API use cases serve website traffic - performance critical for user experience
/// </summary>
[SimpleJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class UseCaseBenchmarks
{
    private DistributedApplication? _app;
    private GetServiceBySlugUseCase? _getServiceBySlugUseCase;
    private GetServiceCategoriesUseCase? _getServiceCategoriesUseCase;
    private ServiceQueryUseCase? _serviceQueryUseCase;
    
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Initialize Aspire application for realistic testing
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();

        // Get database connection string
        var connectionString = await _app.GetConnectionStringAsync("database");
        
        // Build service provider with use case dependencies
        var services = new ServiceCollection();
        
        // Configuration
        services.AddSingleton<IConfiguration>(sp =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    KeyValuePair.Create("ConnectionStrings:database", connectionString)
                })
                .Build();
            return config;
        });
        
        // Logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Caching (in-memory for benchmarking)
        services.AddMemoryCache();
        services.AddSingleton<IDistributedCache, Microsoft.Extensions.Caching.Memory.MemoryDistributedCache>();
        
        // Infrastructure
        services.TryAddSingleton<IVersionService, VersionService>();
        
        // Repositories
        services.AddSingleton<IServiceReadRepository, ServiceReadRepository>();
        services.AddSingleton<IServiceCategoryReadRepository, ServiceCategoryReadRepository>();
        
        // Use Cases
        services.AddSingleton<GetServiceBySlugUseCase>();
        services.AddSingleton<GetServiceCategoriesUseCase>();
        services.AddSingleton<ServiceQueryUseCase>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        _getServiceBySlugUseCase = serviceProvider.GetRequiredService<GetServiceBySlugUseCase>();
        _getServiceCategoriesUseCase = serviceProvider.GetRequiredService<GetServiceCategoriesUseCase>();
        _serviceQueryUseCase = serviceProvider.GetRequiredService<ServiceQueryUseCase>();
        
        Console.WriteLine("🚀 Use case benchmarks setup complete - using real Aspire infrastructure");
    }
    
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }

    /// <summary>
    /// Benchmark GetServiceBySlugUseCase - MOST CRITICAL for website performance
    /// This use case serves individual service pages with validation and error handling
    /// </summary>
    [Benchmark(Description = "GetServiceBySlug UseCase - Critical path for service pages")]
    public async Task<object> GetServiceBySlugUseCase_ExecuteAsync()
    {
        // Test with realistic service slug
        return await _getServiceBySlugUseCase!.ExecuteAsync(
            "consultation-services", 
            CancellationToken.None);
    }

    /// <summary>
    /// Benchmark GetServiceCategoriesUseCase - Navigation and filtering performance
    /// Critical for website navigation and category filtering
    /// </summary>
    [Benchmark(Description = "GetServiceCategories UseCase - Navigation and filtering")]
    public async Task<object> GetServiceCategoriesUseCase_ExecuteAsync()
    {
        return await _getServiceCategoriesUseCase!.ExecuteAsync(
            activeOnly: true, 
            CancellationToken.None);
    }

    /// <summary>
    /// Benchmark ServiceQueryUseCase with published filter
    /// Critical for homepage and service listings
    /// </summary>
    [Benchmark(Description = "ServiceQuery UseCase - Published services")]
    public async Task<object> ServiceQueryUseCase_GetPublished()
    {
        var request = new ServicesQueryRequest
        {
            Page = 1,
            PageSize = 20,
            AvailableOnly = true,
            UserContext = "benchmark-user",
            RequestId = Guid.NewGuid().ToString(),
            ClientIpAddress = "127.0.0.1",
            UserAgent = "BenchmarkDotNet"
        };
        return await _serviceQueryUseCase!.ExecuteAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark ServiceQueryUseCase with featured filter
    /// Critical for homepage featured content
    /// </summary>
    [Benchmark(Description = "ServiceQuery UseCase - Featured services")]
    public async Task<object> ServiceQueryUseCase_GetFeatured()
    {
        var request = new ServicesQueryRequest
        {
            Page = 1,
            PageSize = 6,
            Featured = true,
            AvailableOnly = true,
            UserContext = "benchmark-user",
            RequestId = Guid.NewGuid().ToString(),
            ClientIpAddress = "127.0.0.1",
            UserAgent = "BenchmarkDotNet"
        };
        return await _serviceQueryUseCase!.ExecuteAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark ServiceQueryUseCase pagination
    /// Critical for service directory and search results
    /// </summary>
    [Benchmark(Description = "ServiceQuery UseCase - Paginated results")]
    public async Task<object> ServiceQueryUseCase_GetPaged()
    {
        var request = new ServicesQueryRequest
        {
            Page = 1,
            PageSize = 20,
            AvailableOnly = true,
            UserContext = "benchmark-user",
            RequestId = Guid.NewGuid().ToString(),
            ClientIpAddress = "127.0.0.1",
            UserAgent = "BenchmarkDotNet"
        };
        return await _serviceQueryUseCase!.ExecuteAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark ServiceQueryUseCase search functionality
    /// Critical for user search experience
    /// </summary>
    [Benchmark(Description = "ServiceQuery UseCase - Search functionality")]
    public async Task<object> ServiceQueryUseCase_Search()
    {
        var request = new ServicesQueryRequest
        {
            Page = 1,
            PageSize = 20,
            SearchTerm = "consultation",
            AvailableOnly = true,
            UserContext = "benchmark-user",
            RequestId = Guid.NewGuid().ToString(),
            ClientIpAddress = "127.0.0.1",
            UserAgent = "BenchmarkDotNet"
        };
        return await _serviceQueryUseCase!.ExecuteAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark GetServiceBySlugUseCase with validation error
    /// Tests error handling performance - important for robustness
    /// </summary>
    [Benchmark(Description = "GetServiceBySlug UseCase - Validation error handling")]
    public async Task<object> GetServiceBySlugUseCase_ValidationError()
    {
        // Test with invalid slug to benchmark validation error path
        return await _getServiceBySlugUseCase!.ExecuteAsync(
            "", // Empty slug should trigger validation error
            CancellationToken.None);
    }

    /// <summary>
    /// Benchmark GetServiceBySlugUseCase with not found scenario
    /// Tests not found handling performance - important for 404 pages
    /// </summary>
    [Benchmark(Description = "GetServiceBySlug UseCase - Not found handling")]
    public async Task<object> GetServiceBySlugUseCase_NotFound()
    {
        // Test with non-existent slug to benchmark not found path
        return await _getServiceBySlugUseCase!.ExecuteAsync(
            "non-existent-service-benchmark", 
            CancellationToken.None);
    }
}
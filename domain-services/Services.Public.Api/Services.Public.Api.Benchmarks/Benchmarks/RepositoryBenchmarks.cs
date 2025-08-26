using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InternationalCenter.Services.Public.Api.Infrastructure.Repositories.Dapper;
using InternationalCenter.Services.Public.Api.Infrastructure.Repositories.Interfaces;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Shared.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InternationalCenter.Services.Public.Api.Benchmarks.Benchmarks;

/// <summary>
/// BenchmarkDotNet performance tests for Services.Public.Api Dapper repositories
/// WHY: Repository performance directly impacts public website responsiveness
/// SCOPE: Critical Dapper operations - GetBySlug, GetPublished, Search, etc.
/// CONTEXT: Public gateway architecture - these operations serve website traffic
/// </summary>
[SimpleJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class RepositoryBenchmarks
{
    private DistributedApplication? _app;
    private IServiceReadRepository? _repository;
    private IServiceCategoryRepository? _categoryRepository;
    private ServiceId _testServiceId = null!;
    private Slug _testSlug = null!;
    private ServiceCategoryId _testCategoryId = null!;
    
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Initialize Aspire application for realistic database testing
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();

        // Get database connection string
        var connectionString = await _app.GetConnectionStringAsync("database");
        
        // Build service provider with repository dependencies
        var services = new ServiceCollection();
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
        services.AddLogging(builder => builder.AddConsole());
        services.TryAddSingleton<IVersionService, VersionService>();
        services.AddSingleton<IServiceReadRepository, ServiceReadRepository>();
        services.AddSingleton<IServiceCategoryRepository, ServiceCategoryReadRepository>();
        
        var serviceProvider = services.BuildServiceProvider();
        _repository = serviceProvider.GetRequiredService<IServiceReadRepository>();
        _categoryRepository = serviceProvider.GetRequiredService<IServiceCategoryRepository>();
        
        // Setup test data
        await SetupTestDataAsync();
        
        Console.WriteLine("🚀 Repository benchmarks setup complete - using real Aspire PostgreSQL");
    }
    
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }
    
    private async Task SetupTestDataAsync()
    {
        // Create test data for consistent benchmarking
        // Note: In real scenarios, this would use the actual seeded test data
        _testServiceId = ServiceId.NewServiceId();
        _testSlug = Slug.Create("test-benchmark-service");
        _testCategoryId = ServiceCategoryId.Create(1);
    }

    /// <summary>
    /// Benchmark GetBySlugAsync - CRITICAL PATH for public website
    /// This operation serves individual service pages - most frequent operation
    /// </summary>
    [Benchmark(Description = "GetBySlug - Critical path for public website service pages")]
    public async Task<object?> GetBySlugAsync()
    {
        // Benchmark with realistic slug lookup
        var slug = Slug.Create("consultation-services"); // Realistic slug
        return await _repository!.GetBySlugAsync(slug, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark GetPublishedAsync - Homepage and service listing performance
    /// Critical for homepage load times and service directory
    /// </summary>
    [Benchmark(Description = "GetPublished - Homepage and service directory performance")]
    public async Task<object> GetPublishedAsync()
    {
        return await _repository!.GetPublishedAsync(CancellationToken.None);
    }

    /// <summary>
    /// Benchmark GetFeaturedAsync - Homepage featured services performance
    /// Critical for homepage above-the-fold content
    /// </summary>
    [Benchmark(Description = "GetFeatured - Homepage featured services")]
    public async Task<object> GetFeaturedAsync()
    {
        return await _repository!.GetFeaturedAsync(limit: 6, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark GetPagedAsync - Service listing pagination performance
    /// Critical for service directory and category pages
    /// </summary>
    [Benchmark(Description = "GetPaged - Service directory pagination")]
    public async Task<object> GetPagedAsync()
    {
        return await _repository!.GetPagedAsync(
            page: 1, 
            pageSize: 20, 
            categoryId: null, 
            publishedOnly: true, 
            CancellationToken.None);
    }

    /// <summary>
    /// Benchmark GetByCategoryAsync - Category filtering performance
    /// Critical for category-specific service listings
    /// </summary>
    [Benchmark(Description = "GetByCategory - Category filtering")]
    public async Task<object> GetByCategoryAsync()
    {
        return await _repository!.GetByCategoryAsync(
            _testCategoryId, 
            publishedOnly: true, 
            CancellationToken.None);
    }

    /// <summary>
    /// Benchmark SearchAsync - Search functionality performance
    /// Critical for user search experience
    /// </summary>
    [Benchmark(Description = "Search - Full-text search performance")]
    public async Task<object> SearchAsync()
    {
        return await _repository!.SearchAsync(
            "consultation", 
            page: 1, 
            pageSize: 20, 
            publishedOnly: true, 
            CancellationToken.None);
    }

    /// <summary>
    /// Benchmark CountAsync - Count operations for pagination
    /// Used for pagination metadata and statistics
    /// </summary>
    [Benchmark(Description = "Count - Pagination and statistics")]
    public async Task<int> CountAsync()
    {
        return await _repository!.CountAsync(
            categoryId: null, 
            publishedOnly: true, 
            CancellationToken.None);
    }

    /// <summary>
    /// Benchmark ExistsAsync - Existence checks for validation
    /// Used for slug validation and duplicate prevention
    /// </summary>
    [Benchmark(Description = "SlugExists - Validation and duplicate checking")]
    public async Task<bool> SlugExistsAsync()
    {
        return await _repository!.SlugExistsAsync(_testSlug, CancellationToken.None);
    }
}

/// <summary>
/// Simple version service implementation for benchmarking
/// </summary>
public class VersionService : IVersionService
{
    public string Version => "1.0.0-benchmark";
    public string Environment => "benchmark";
    public string BuildDate => DateTime.UtcNow.ToString("yyyy-MM-dd");
}
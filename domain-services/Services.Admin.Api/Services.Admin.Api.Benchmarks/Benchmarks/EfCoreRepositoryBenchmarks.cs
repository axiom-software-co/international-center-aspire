using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using InternationalCenter.Services.Admin.Api.Infrastructure.Repositories;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.Infrastructure.Data;
using InternationalCenter.Services.Domain.Infrastructure.Interfaces;
using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Domain.Specifications;

namespace InternationalCenter.Services.Admin.Api.Benchmarks.Benchmarks;

/// <summary>
/// BenchmarkDotNet performance tests for Services.Admin.Api EF Core repositories
/// WHY: EF Core repository performance directly impacts medical-grade admin operations
/// SCOPE: Critical EF Core operations with medical-grade audit logging
/// CONTEXT: Admin gateway architecture - these operations serve admin workflows with audit compliance
/// </summary>
[SimpleJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class EfCoreRepositoryBenchmarks
{
    private DistributedApplication? _app;
    private IServiceRepository? _serviceRepository;
    private IServiceCategoryRepository? _categoryRepository;
    private IServicesDbContext? _dbContext;
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
        
        // Build service provider with EF Core repository dependencies
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
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // EF Core setup
        services.AddDbContext<ServicesDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IServicesDbContext>(provider => provider.GetRequiredService<ServicesDbContext>());
        
        // Repositories
        services.AddScoped<IServiceRepository, AdminServiceRepository>();
        services.AddScoped<IServiceCategoryRepository, AdminServiceCategoryRepository>();
        
        var serviceProvider = services.BuildServiceProvider();
        var scope = serviceProvider.CreateScope();
        
        _serviceRepository = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        _categoryRepository = scope.ServiceProvider.GetRequiredService<IServiceCategoryRepository>();
        _dbContext = scope.ServiceProvider.GetRequiredService<IServicesDbContext>();
        
        // Setup test data
        await SetupTestDataAsync();
        
        Console.WriteLine("🏥 EF Core repository benchmarks setup complete - using real Aspire PostgreSQL");
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
        _testServiceId = ServiceId.NewServiceId();
        _testSlug = Slug.Create("test-benchmark-admin-service");
        _testCategoryId = ServiceCategoryId.Create(1);

        // Ensure database is created and ready
        await _dbContext!.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Benchmark GetByIdAsync with EF Core Include - Critical for admin service editing
    /// Tests EF Core navigation property loading performance with audit logging
    /// </summary>
    [Benchmark(Description = "GetById with Include - Critical for admin service editing")]
    public async Task<Service?> GetByIdAsync()
    {
        // Use realistic service ID lookup
        var serviceId = ServiceId.NewServiceId(); // This will likely return null, testing the query path
        return await _serviceRepository!.GetByIdAsync(serviceId, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark GetBySlugAsync with EF Core Include - Admin service lookup
    /// Tests slug-based lookup performance with navigation properties
    /// </summary>
    [Benchmark(Description = "GetBySlug with Include - Admin service lookup")]
    public async Task<Service?> GetBySlugAsync()
    {
        var slug = Slug.Create("admin-benchmark-service"); // Realistic admin slug
        return await _serviceRepository!.GetBySlugAsync(slug, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark GetAllAsync with EF Core Include and OrderBy - Admin service listing
    /// Critical for admin dashboard service management interface
    /// </summary>
    [Benchmark(Description = "GetAll with Include and OrderBy - Admin service listing")]
    public async Task<IReadOnlyList<Service>> GetAllAsync()
    {
        return await _serviceRepository!.GetAllAsync(CancellationToken.None);
    }

    /// <summary>
    /// Benchmark GetBySpecificationAsync - Advanced admin filtering
    /// Tests specification pattern performance with complex queries
    /// </summary>
    [Benchmark(Description = "GetBySpecification - Advanced admin filtering")]
    public async Task<IReadOnlyList<Service>> GetBySpecificationAsync()
    {
        // Create specification for published services (common admin filter)
        var specification = new PublishedServicesSpecification();
        return await _serviceRepository!.GetBySpecificationAsync(specification, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark GetPagedAsync - Admin pagination with complex queries
    /// Critical for admin service management with large datasets
    /// </summary>
    [Benchmark(Description = "GetPaged - Admin pagination with complex queries")]
    public async Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync()
    {
        var specification = new PublishedServicesSpecification();
        return await _serviceRepository!.GetPagedAsync(
            specification, 
            page: 1, 
            pageSize: 20, 
            CancellationToken.None);
    }

    /// <summary>
    /// Benchmark CountAsync - Statistics and pagination metadata
    /// Used for admin dashboard statistics and pagination
    /// </summary>
    [Benchmark(Description = "Count - Statistics and pagination metadata")]
    public async Task<int> CountAsync()
    {
        var specification = new PublishedServicesSpecification();
        return await _serviceRepository!.CountAsync(specification, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark ExistsAsync - Validation and duplicate checking
    /// Critical for admin form validation and business rule enforcement
    /// </summary>
    [Benchmark(Description = "Exists - Validation and duplicate checking")]
    public async Task<bool> ExistsAsync()
    {
        return await _serviceRepository!.ExistsAsync(_testServiceId, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark SlugExistsAsync - Slug uniqueness validation
    /// Critical for admin service creation/editing workflows
    /// </summary>
    [Benchmark(Description = "SlugExists - Slug uniqueness validation")]
    public async Task<bool> SlugExistsAsync()
    {
        return await _serviceRepository!.SlugExistsAsync(_testSlug, excludeId: null, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark AddAsync with medical-grade audit - Service creation performance
    /// Tests EF Core entity creation with audit logging overhead
    /// </summary>
    [Benchmark(Description = "Add with audit - Service creation performance")]
    public async Task AddAsync()
    {
        // Create test service for benchmarking
        var serviceId = ServiceId.NewServiceId();
        var slug = Slug.Create($"benchmark-service-{Guid.NewGuid():N}");
        var metadata = ServiceMetadata.Create("icon", "image", "title", "description", 
            new[] {"tech1"}, new[] {"feature1"}, new[] {"online"});
        
        var service = new Service(serviceId, "Benchmark Service", slug, 
            "Service for performance benchmarking", "Detailed benchmark description", metadata);
        
        await _serviceRepository!.AddAsync(service, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark UpdateAsync with medical-grade audit - Service modification performance
    /// Tests EF Core entity update with change tracking and audit logging
    /// </summary>
    [Benchmark(Description = "Update with audit - Service modification performance")]
    public async Task UpdateAsync()
    {
        // Create and track a service for updating
        var serviceId = ServiceId.NewServiceId();
        var slug = Slug.Create($"update-benchmark-{Guid.NewGuid():N}");
        var metadata = ServiceMetadata.Create("icon", "image", "title", "description", 
            new[] {"tech1"}, new[] {"feature1"}, new[] {"online"});
        
        var service = new Service(serviceId, "Update Benchmark Service", slug, 
            "Service for update benchmarking", "Detailed update description", metadata);
        
        await _serviceRepository!.UpdateAsync(service, CancellationToken.None);
    }

    /// <summary>
    /// Benchmark SaveChangesAsync - EF Core transaction performance
    /// Critical for admin operations - tests change tracking and database commit
    /// </summary>
    [Benchmark(Description = "SaveChanges - EF Core transaction performance")]
    public async Task SaveChangesAsync()
    {
        await _serviceRepository!.SaveChangesAsync(CancellationToken.None);
    }
}

/// <summary>
/// Sample specification for benchmarking - published services filter
/// </summary>
public class PublishedServicesSpecification : Specification<Service>
{
    public PublishedServicesSpecification()
    {
        AddCriteria(s => s.Status == ServiceStatus.Published && s.IsActive);
        AddInclude(s => s.Category);
        AddOrderBy(s => s.SortOrder);
        AddThenBy(s => s.Title);
    }
}

/// <summary>
/// Base specification class for benchmarking
/// </summary>
public abstract class Specification<T> : ISpecification<T>
{
    protected Specification() { }
    protected Specification(System.Linq.Expressions.Expression<Func<T, bool>> criteria) 
    { 
        Criteria = criteria; 
    }

    public System.Linq.Expressions.Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<System.Linq.Expressions.Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public System.Linq.Expressions.Expression<Func<T, object>>? OrderBy { get; private set; }
    public System.Linq.Expressions.Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public List<System.Linq.Expressions.Expression<Func<T, object>>> ThenByList { get; } = new();
    public List<System.Linq.Expressions.Expression<Func<T, object>>> ThenByDescendingList { get; } = new();

    public int Take { get; private set; }
    public int Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    protected virtual void AddCriteria(System.Linq.Expressions.Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    protected virtual void AddInclude(System.Linq.Expressions.Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    protected virtual void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    protected virtual void AddOrderBy(System.Linq.Expressions.Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    protected virtual void AddOrderByDescending(System.Linq.Expressions.Expression<Func<T, object>> orderByDescExpression)
    {
        OrderByDescending = orderByDescExpression;
    }

    protected virtual void AddThenBy(System.Linq.Expressions.Expression<Func<T, object>> thenByExpression)
    {
        ThenByList.Add(thenByExpression);
    }

    protected virtual void AddThenByDescending(System.Linq.Expressions.Expression<Func<T, object>> thenByDescExpression)
    {
        ThenByDescendingList.Add(thenByDescExpression);
    }

    protected virtual void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
}
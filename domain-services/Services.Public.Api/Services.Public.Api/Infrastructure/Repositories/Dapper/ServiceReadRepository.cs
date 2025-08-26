using System.Data;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Public.Api.Infrastructure.Repositories.Interfaces;
using InternationalCenter.Shared.Infrastructure.Observability;
using InternationalCenter.Shared.Services;

namespace InternationalCenter.Services.Public.Api.Infrastructure.Repositories.Dapper;

/// <summary>
/// High-performance read-only repository using Dapper for Services
/// Optimized for Public API read operations (per user rule: Dapper for public APIs)
/// </summary>
public sealed class ServiceReadRepository : IServiceReadRepository
{
    private readonly string _connectionString;
    private readonly ILogger<ServiceReadRepository> _logger;
    private readonly IVersionService _versionService;

    public ServiceReadRepository(
        IConfiguration configuration, 
        ILogger<ServiceReadRepository> logger,
        IVersionService versionService)
    {
        _connectionString = configuration.GetConnectionString("database") 
            ?? throw new InvalidOperationException("Database connection string not found");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _versionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
    }

    public async Task<Service?> GetByIdAsync(ServiceId id, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceReadRepository", "GetByIdAsync", null, null, _versionService);
        
        const string sql = """
            SELECT s.*, sc.id as CategoryId, sc.name as CategoryName, sc.slug as CategorySlug, 
                   sc.description as CategoryDescription, sc.active as CategoryActive
            FROM services s
            LEFT JOIN service_categories sc ON s.categoryid = sc.id
            WHERE s.id = @Id
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var result = await connection.QueryAsync<ServiceDto, ServiceCategoryDto, ServiceDto>(
                sql,
                (service, category) =>
                {
                    service.Category = category;
                    return service;
                },
                new { Id = id.Value },
                splitOn: "CategoryId");

            var serviceDto = result.FirstOrDefault();
            var domainService = serviceDto?.ToDomainEntity();
            
            _logger.LogInformation("Retrieved service {ServiceId}: {Found}", id.Value, domainService != null ? "found" : "not found");
            return domainService;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service by ID {ServiceId}", id.Value);
            throw;
        }
    }

    public async Task<Service?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceReadRepository", "GetBySlugAsync", null, null, _versionService);
        
        const string sql = """
            SELECT s.*, sc.id as CategoryId, sc.name as CategoryName, sc.slug as CategorySlug, 
                   sc.description as CategoryDescription, sc.active as CategoryActive
            FROM services s
            LEFT JOIN service_categories sc ON s.categoryid = sc.id
            WHERE s.slug = @Slug
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var result = await connection.QueryAsync<ServiceDto, ServiceCategoryDto, ServiceDto>(
                sql,
                (service, category) =>
                {
                    service.Category = category;
                    return service;
                },
                new { Slug = slug.Value },
                splitOn: "CategoryId");

            var serviceDto = result.FirstOrDefault();
            var domainService = serviceDto?.ToDomainEntity();
            
            _logger.LogInformation("Retrieved service by slug {Slug}: {Found}", slug.Value, domainService != null ? "found" : "not found");
            return domainService;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service by slug {Slug}", slug.Value);
            throw;
        }
    }

    public async Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceReadRepository", "GetAllAsync", null, null, _versionService);
        
        const string sql = """
            SELECT s.*, sc.id as CategoryId, sc.name as CategoryName, sc.slug as CategorySlug, 
                   sc.description as CategoryDescription, sc.active as CategoryActive
            FROM services s
            LEFT JOIN service_categories sc ON s.categoryid = sc.id
            ORDER BY s.priority, s.title
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var result = await connection.QueryAsync<ServiceDto, ServiceCategoryDto, ServiceDto>(
                sql,
                (service, category) =>
                {
                    service.Category = category;
                    return service;
                },
                splitOn: "CategoryId");

            var services = result.Select(dto => dto.ToDomainEntity()).ToList();
            
            _logger.LogInformation("Retrieved {ServiceCount} services", services.Count);
            return services;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all services");
            throw;
        }
    }

    public async Task<IReadOnlyList<Service>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceReadRepository", "GetPublishedAsync", null, null, _versionService);
        
        const string sql = """
            SELECT s.*, sc.id as CategoryId, sc.name as CategoryName, sc.slug as CategorySlug, 
                   sc.description as CategoryDescription, sc.active as CategoryActive
            FROM services s
            LEFT JOIN service_categories sc ON s.categoryid = sc.id
            WHERE s.status = 'published' AND s.available = true
            ORDER BY s.priority, s.title
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var result = await connection.QueryAsync<ServiceDto, ServiceCategoryDto, ServiceDto>(
                sql,
                (service, category) =>
                {
                    service.Category = category;
                    return service;
                },
                splitOn: "CategoryId");

            var services = result.Select(dto => dto.ToDomainEntity()).ToList();
            
            _logger.LogInformation("Retrieved {ServiceCount} published services", services.Count);
            return services;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving published services");
            throw;
        }
    }

    public async Task<IReadOnlyList<Service>> GetFeaturedAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceReadRepository", "GetFeaturedAsync", null, null, _versionService);
        
        const string sql = """
            SELECT s.*, sc.id as CategoryId, sc.name as CategoryName, sc.slug as CategorySlug, 
                   sc.description as CategoryDescription, sc.active as CategoryActive
            FROM services s
            LEFT JOIN service_categories sc ON s.categoryid = sc.id
            WHERE s.status = 'published' AND s.available = true AND s.featured = true
            ORDER BY s.priority, s.title
            LIMIT @Limit
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var result = await connection.QueryAsync<ServiceDto, ServiceCategoryDto, ServiceDto>(
                sql,
                (service, category) =>
                {
                    service.Category = category;
                    return service;
                },
                new { Limit = limit },
                splitOn: "CategoryId");

            var services = result.Select(dto => dto.ToDomainEntity()).ToList();
            
            _logger.LogInformation("Retrieved {ServiceCount} featured services (limit: {Limit})", services.Count, limit);
            return services;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving featured services");
            throw;
        }
    }

    public async Task<IReadOnlyList<Service>> GetByCategoryAsync(ServiceCategoryId categoryId, bool publishedOnly = true, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceReadRepository", "GetByCategoryAsync", null, null, _versionService);
        
        var whereClause = publishedOnly 
            ? "WHERE s.categoryid = @CategoryId AND s.status = 'published' AND s.available = true"
            : "WHERE s.categoryid = @CategoryId";
            
        var sql = $"""
            SELECT s.*, sc.id as CategoryId, sc.name as CategoryName, sc.slug as CategorySlug, 
                   sc.description as CategoryDescription, sc.active as CategoryActive
            FROM services s
            LEFT JOIN service_categories sc ON s.categoryid = sc.id
            {whereClause}
            ORDER BY s.priority, s.title
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var result = await connection.QueryAsync<ServiceDto, ServiceCategoryDto, ServiceDto>(
                sql,
                (service, category) =>
                {
                    service.Category = category;
                    return service;
                },
                new { CategoryId = categoryId.Value },
                splitOn: "CategoryId");

            var services = result.Select(dto => dto.ToDomainEntity()).ToList();
            
            _logger.LogInformation("Retrieved {ServiceCount} services for category {CategoryId} (publishedOnly: {PublishedOnly})", 
                services.Count, categoryId.Value, publishedOnly);
            return services;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving services for category {CategoryId}", categoryId.Value);
            throw;
        }
    }

    public async Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, ServiceCategoryId? categoryId = null, bool publishedOnly = true, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceReadRepository", "GetPagedAsync", null, null, _versionService);
        
        var whereClause = publishedOnly ? "WHERE s.status = 'published' AND s.available = true" : "";
        if (categoryId != null)
        {
            whereClause = publishedOnly 
                ? "WHERE s.categoryid = @CategoryId AND s.status = 'published' AND s.available = true"
                : "WHERE s.categoryid = @CategoryId";
        }

        var countSql = $"SELECT COUNT(*) FROM services s {whereClause}";
        
        var dataSql = $"""
            SELECT s.*, sc.id as CategoryId, sc.name as CategoryName, sc.slug as CategorySlug, 
                   sc.description as CategoryDescription, sc.active as CategoryActive
            FROM services s
            LEFT JOIN service_categories sc ON s.categoryid = sc.id
            {whereClause}
            ORDER BY s.priority, s.title
            LIMIT @PageSize OFFSET @Offset
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var parameters = new { 
                CategoryId = categoryId?.Value, 
                PageSize = pageSize, 
                Offset = (page - 1) * pageSize 
            };

            var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

            var result = await connection.QueryAsync<ServiceDto, ServiceCategoryDto, ServiceDto>(
                dataSql,
                (service, category) =>
                {
                    service.Category = category;
                    return service;
                },
                parameters,
                splitOn: "CategoryId");

            var services = result.Select(dto => dto.ToDomainEntity()).ToList();
            
            _logger.LogInformation("Retrieved page {Page} of services: {ServiceCount} items, {TotalCount} total", 
                page, services.Count, totalCount);
            return (services, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged services");
            throw;
        }
    }

    public async Task<(IReadOnlyList<Service> Items, int TotalCount)> SearchAsync(string searchTerm, int page, int pageSize, bool publishedOnly = true, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceReadRepository", "SearchAsync", null, null, _versionService);
        
        var whereClause = publishedOnly 
            ? "WHERE (s.title ILIKE @SearchTerm OR s.description ILIKE @SearchTerm) AND s.status = 'published' AND s.available = true"
            : "WHERE (s.title ILIKE @SearchTerm OR s.description ILIKE @SearchTerm)";

        var countSql = $"SELECT COUNT(*) FROM services s {whereClause}";
        
        var dataSql = $"""
            SELECT s.*, sc.id as CategoryId, sc.name as CategoryName, sc.slug as CategorySlug, 
                   sc.description as CategoryDescription, sc.active as CategoryActive
            FROM services s
            LEFT JOIN service_categories sc ON s.categoryid = sc.id
            {whereClause}
            ORDER BY s.priority, s.title
            LIMIT @PageSize OFFSET @Offset
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var parameters = new { 
                SearchTerm = $"%{searchTerm}%", 
                PageSize = pageSize, 
                Offset = (page - 1) * pageSize 
            };

            var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

            var result = await connection.QueryAsync<ServiceDto, ServiceCategoryDto, ServiceDto>(
                dataSql,
                (service, category) =>
                {
                    service.Category = category;
                    return service;
                },
                parameters,
                splitOn: "CategoryId");

            var services = result.Select(dto => dto.ToDomainEntity()).ToList();
            
            _logger.LogInformation("Search for '{SearchTerm}': {ServiceCount} items found, {TotalCount} total", 
                searchTerm, services.Count, totalCount);
            return (services, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching services with term '{SearchTerm}'", searchTerm);
            throw;
        }
    }

    public async Task<int> CountAsync(ServiceCategoryId? categoryId = null, bool publishedOnly = true, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceReadRepository", "CountAsync", null, null, _versionService);
        
        var whereClause = publishedOnly ? "WHERE status = 'published' AND available = true" : "";
        if (categoryId != null)
        {
            whereClause = publishedOnly 
                ? "WHERE categoryid = @CategoryId AND status = 'published' AND available = true"
                : "WHERE categoryid = @CategoryId";
        }

        var sql = $"SELECT COUNT(*) FROM services {whereClause}";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var count = await connection.QuerySingleAsync<int>(sql, new { CategoryId = categoryId?.Value });
            
            _logger.LogInformation("Service count: {Count} (categoryId: {CategoryId}, publishedOnly: {PublishedOnly})", 
                count, categoryId?.Value, publishedOnly);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting services");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(ServiceId id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM services WHERE id = @Id)";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var exists = await connection.QuerySingleAsync<bool>(sql, new { Id = id.Value });
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking service existence for ID {ServiceId}", id.Value);
            throw;
        }
    }

    public async Task<bool> SlugExistsAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM services WHERE slug = @Slug)";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var exists = await connection.QuerySingleAsync<bool>(sql, new { Slug = slug.Value });
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking service slug existence for {Slug}", slug.Value);
            throw;
        }
    }
}

// DTOs for Dapper mapping
internal class ServiceDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long Priority { get; set; }
    public int? CategoryId { get; set; }
    public bool Available { get; set; }
    public bool Featured { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string[] Technologies { get; set; } = Array.Empty<string>();
    public string[] Features { get; set; } = Array.Empty<string>();
    public string[] DeliveryModes { get; set; } = Array.Empty<string>();
    public string Icon { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string MetaTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    
    public ServiceCategoryDto? Category { get; set; }

    public Service ToDomainEntity()
    {
        var serviceId = ServiceId.FromString(Id);
        var slug = Slug.Create(Slug);
        var serviceStatus = ServiceStatusExtensions.FromString(Status);
        var metadata = ServiceMetadata.Create(
            icon: Icon,
            image: Image,
            metaTitle: MetaTitle,
            metaDescription: MetaDescription,
            technologies: Technologies,
            features: Features,
            deliveryModes: DeliveryModes);
        
        var service = new Service(serviceId, Title, slug, Description, DetailedDescription, metadata);
        
        // Set additional properties using domain methods
        if (CategoryId.HasValue)
        {
            service.SetCategory(ServiceCategoryId.Create(CategoryId.Value));
        }
        
        service.SetSortOrder((int)Priority);
        service.SetAvailability(Available);
        service.SetFeatured(Featured);
        
        if (serviceStatus == ServiceStatus.Published)
        {
            service.Publish();
        }
        else if (serviceStatus == ServiceStatus.Archived)
        {
            service.Archive();
        }
        
        return service;
    }
}

internal class ServiceCategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string CategoryDescription { get; set; } = string.Empty;
    public bool CategoryActive { get; set; }
    
    public ServiceCategory? ToDomainEntity()
    {
        if (CategoryId <= 0) return null;
        
        var categoryId = ServiceCategoryId.Create(CategoryId);
        var slug = Slug.Create(CategorySlug);
        var category = new ServiceCategory(categoryId, CategoryName, CategoryDescription, slug);
        
        if (!CategoryActive)
        {
            category.Deactivate();
        }
        
        return category;
    }
}
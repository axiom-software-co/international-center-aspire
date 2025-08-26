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
/// High-performance read-only repository using Dapper for Service Categories
/// Optimized for Public API read operations (per user rule: Dapper for public APIs)
/// </summary>
public sealed class ServiceCategoryReadRepository : IServiceCategoryReadRepository
{
    private readonly string _connectionString;
    private readonly ILogger<ServiceCategoryReadRepository> _logger;
    private readonly IVersionService _versionService;

    public ServiceCategoryReadRepository(
        IConfiguration configuration, 
        ILogger<ServiceCategoryReadRepository> logger,
        IVersionService versionService)
    {
        _connectionString = configuration.GetConnectionString("database") 
            ?? throw new InvalidOperationException("Database connection string not found");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _versionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
    }

    public async Task<ServiceCategory?> GetByIdAsync(ServiceCategoryId id, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceCategoryReadRepository", "GetByIdAsync", null, null, _versionService);
        
        const string sql = "SELECT * FROM service_categories WHERE id = @Id";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var categoryDto = await connection.QueryFirstOrDefaultAsync<ServiceCategoryDto>(sql, new { Id = id.Value });
            var domainCategory = categoryDto?.ToDomainEntity();
            
            _logger.LogInformation("Retrieved service category {CategoryId}: {Found}", id.Value, domainCategory != null ? "found" : "not found");
            return domainCategory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service category by ID {CategoryId}", id.Value);
            throw;
        }
    }

    public async Task<ServiceCategory?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceCategoryReadRepository", "GetBySlugAsync", null, null, _versionService);
        
        const string sql = "SELECT * FROM service_categories WHERE slug = @Slug";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var categoryDto = await connection.QueryFirstOrDefaultAsync<ServiceCategoryDto>(sql, new { Slug = slug.Value });
            var domainCategory = categoryDto?.ToDomainEntity();
            
            _logger.LogInformation("Retrieved service category by slug {Slug}: {Found}", slug.Value, domainCategory != null ? "found" : "not found");
            return domainCategory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service category by slug {Slug}", slug.Value);
            throw;
        }
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceCategoryReadRepository", "GetAllAsync", null, null, _versionService);
        
        var whereClause = activeOnly ? "WHERE active = true" : "";
        var sql = $"SELECT * FROM service_categories {whereClause} ORDER BY displayorder, name";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var categoryDtos = await connection.QueryAsync<ServiceCategoryDto>(sql);
            var categories = categoryDtos.Select(dto => dto.ToDomainEntity()).ToList();
            
            _logger.LogInformation("Retrieved {CategoryCount} service categories (activeOnly: {ActiveOnly})", categories.Count, activeOnly);
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all service categories");
            throw;
        }
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetActiveOrderedAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceCategoryReadRepository", "GetActiveOrderedAsync", null, null, _versionService);
        
        const string sql = "SELECT * FROM service_categories WHERE active = true ORDER BY displayorder, name";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var categoryDtos = await connection.QueryAsync<ServiceCategoryDto>(sql);
            var categories = categoryDtos.Select(dto => dto.ToDomainEntity()).ToList();
            
            _logger.LogInformation("Retrieved {CategoryCount} active service categories", categories.Count);
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active service categories");
            throw;
        }
    }

    public async Task<ServiceCategory?> GetWithServicesAsync(ServiceCategoryId id, bool publishedOnly = true, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceCategoryReadRepository", "GetWithServicesAsync", null, null, _versionService);
        
        var serviceWhereClause = publishedOnly 
            ? "AND s.status = 'published' AND s.available = true"
            : "";
            
        var sql = $"""
            SELECT sc.*, s.id as ServiceId, s.title as ServiceTitle, s.slug as ServiceSlug, 
                   s.description as ServiceDescription, s.status as ServiceStatus, 
                   s.available as ServiceAvailable, s.featured as ServiceFeatured, 
                   s.priority as ServicePriority
            FROM service_categories sc
            LEFT JOIN services s ON sc.id = s.categoryid {serviceWhereClause}
            WHERE sc.id = @Id
            ORDER BY s.priority, s.title
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var result = await connection.QueryAsync<ServiceCategoryDto, ServiceSummaryDto, ServiceCategoryDto>(
                sql,
                (category, service) =>
                {
                    if (service != null)
                    {
                        category.Services ??= new List<ServiceSummaryDto>();
                        category.Services.Add(service);
                    }
                    return category;
                },
                new { Id = id.Value },
                splitOn: "ServiceId");

            var categoryDto = result.GroupBy(r => r.Id).Select(g =>
            {
                var first = g.First();
                first.Services = g.Where(r => r.Services?.Any() == true).SelectMany(r => r.Services!).ToList();
                return first;
            }).FirstOrDefault();

            var domainCategory = categoryDto?.ToDomainEntity();
            
            _logger.LogInformation("Retrieved service category {CategoryId} with {ServiceCount} services", 
                id.Value, categoryDto?.Services?.Count ?? 0);
            return domainCategory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service category with services for ID {CategoryId}", id.Value);
            throw;
        }
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetWithServiceCountsAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginServiceScope("ServiceCategoryReadRepository", "GetWithServiceCountsAsync", null, null, _versionService);
        
        var whereClause = activeOnly ? "WHERE sc.active = true" : "";
        var sql = $"""
            SELECT sc.*, 
                   COUNT(s.id) FILTER (WHERE s.status = 'published' AND s.available = true) as ServiceCount
            FROM service_categories sc
            LEFT JOIN services s ON sc.id = s.categoryid
            {whereClause}
            GROUP BY sc.id, sc.name, sc.slug, sc.description, sc.displayorder, sc.active, sc.created_at, sc.updated_at, 
                     sc.minpriorityorder, sc.maxpriorityorder, sc.featured1, sc.featured2
            ORDER BY sc.displayorder, sc.name
            """;

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var categoryDtos = await connection.QueryAsync<ServiceCategoryWithCountDto>(sql);
            var categories = categoryDtos.Select(dto => dto.ToDomainEntity()).ToList();
            
            _logger.LogInformation("Retrieved {CategoryCount} service categories with service counts", categories.Count);
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service categories with service counts");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(ServiceCategoryId id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM service_categories WHERE id = @Id)";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var exists = await connection.QuerySingleAsync<bool>(sql, new { Id = id.Value });
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking service category existence for ID {CategoryId}", id.Value);
            throw;
        }
    }

    public async Task<bool> SlugExistsAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM service_categories WHERE slug = @Slug)";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var exists = await connection.QuerySingleAsync<bool>(sql, new { Slug = slug.Value });
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking service category slug existence for {Slug}", slug.Value);
            throw;
        }
    }
}

// DTOs for Dapper mapping
internal class ServiceCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MinPriorityOrder { get; set; }
    public int MaxPriorityOrder { get; set; }
    public bool Featured1 { get; set; }
    public bool Featured2 { get; set; }
    
    public List<ServiceSummaryDto>? Services { get; set; }

    public ServiceCategory ToDomainEntity()
    {
        var categoryId = ServiceCategoryId.Create(Id);
        var slug = Slug.Create(Slug);
        var category = new ServiceCategory(categoryId, Name, Description, slug, DisplayOrder);
        
        if (!Active)
        {
            category.Deactivate();
        }
        
        return category;
    }
}

internal class ServiceCategoryWithCountDto : ServiceCategoryDto
{
    public int ServiceCount { get; set; }
}

internal class ServiceSummaryDto
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceTitle { get; set; } = string.Empty;
    public string ServiceSlug { get; set; } = string.Empty;
    public string ServiceDescription { get; set; } = string.Empty;
    public string ServiceStatus { get; set; } = string.Empty;
    public bool ServiceAvailable { get; set; }
    public bool ServiceFeatured { get; set; }
    public long ServicePriority { get; set; }
}
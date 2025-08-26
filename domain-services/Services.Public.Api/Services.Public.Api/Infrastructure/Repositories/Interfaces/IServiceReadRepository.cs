using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Domain.Specifications;

namespace InternationalCenter.Services.Public.Api.Infrastructure.Repositories.Interfaces;

/// <summary>
/// Read-only repository interface for Services using Dapper for high-performance reads
/// Used by Public API for read operations (per user rule: Dapper for public APIs)
/// </summary>
public interface IServiceReadRepository
{
    Task<Service?> GetByIdAsync(ServiceId id, CancellationToken cancellationToken = default);
    Task<Service?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetFeaturedAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetByCategoryAsync(ServiceCategoryId categoryId, bool publishedOnly = true, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, ServiceCategoryId? categoryId = null, bool publishedOnly = true, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Service> Items, int TotalCount)> SearchAsync(string searchTerm, int page, int pageSize, bool publishedOnly = true, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ServiceCategoryId? categoryId = null, bool publishedOnly = true, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(ServiceId id, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(Slug slug, CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only repository interface for Service Categories using Dapper for high-performance reads
/// Used by Public API for read operations (per user rule: Dapper for public APIs)
/// </summary>
public interface IServiceCategoryReadRepository
{
    Task<ServiceCategory?> GetByIdAsync(ServiceCategoryId id, CancellationToken cancellationToken = default);
    Task<ServiceCategory?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceCategory>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceCategory>> GetActiveOrderedAsync(CancellationToken cancellationToken = default);
    Task<ServiceCategory?> GetWithServicesAsync(ServiceCategoryId id, bool publishedOnly = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceCategory>> GetWithServiceCountsAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(ServiceCategoryId id, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(Slug slug, CancellationToken cancellationToken = default);
}
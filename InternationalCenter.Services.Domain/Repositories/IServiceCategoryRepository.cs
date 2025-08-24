using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.ValueObjects;

namespace InternationalCenter.Services.Domain.Repositories;

public interface IServiceCategoryRepository
{
    Task<ServiceCategory?> GetByIdAsync(ServiceCategoryId id, CancellationToken cancellationToken = default);
    Task<ServiceCategory?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceCategory>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceCategory>> GetActiveOrderedAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(ServiceCategoryId id, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(Slug slug, ServiceCategoryId? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(ServiceCategory category, CancellationToken cancellationToken = default);
    Task UpdateAsync(ServiceCategory category, CancellationToken cancellationToken = default);
    Task DeleteAsync(ServiceCategory category, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
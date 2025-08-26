using Services.Shared.Entities;
using Services.Shared.ValueObjects;
using Services.Shared.Specifications;

namespace Services.Shared.Repositories;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(ServiceId id, CancellationToken cancellationToken = default);
    Task<Service?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetBySpecificationAsync(ISpecification<Service> specification, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync(ISpecification<Service> specification, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<Service> specification, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(ServiceId id, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(Slug slug, ServiceId? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Service service, CancellationToken cancellationToken = default);
    Task UpdateAsync(Service service, CancellationToken cancellationToken = default);
    Task DeleteAsync(Service service, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
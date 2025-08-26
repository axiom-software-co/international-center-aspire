using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Domain.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InternationalCenter.Services.Public.Api.Infrastructure.Repositories;

public sealed class ServiceCategoryRepository : IServiceCategoryRepository
{
    private readonly IServicesDbContext _context;
    private readonly ILogger<ServiceCategoryRepository> _logger;

    public ServiceCategoryRepository(IServicesDbContext context, ILogger<ServiceCategoryRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ServiceCategory?> GetByIdAsync(ServiceCategoryId id, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceCategories
            .Include(sc => sc.Services)
            .FirstOrDefaultAsync(sc => sc.Id == id, cancellationToken);
    }

    public async Task<ServiceCategory?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceCategories
            .Include(sc => sc.Services)
            .FirstOrDefaultAsync(sc => sc.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceCategories.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(sc => sc.Active);
        }

        return await query
            .OrderBy(sc => sc.DisplayOrder)
            .ThenBy(sc => sc.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetActiveOrderedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ServiceCategories
            .Where(sc => sc.Active)
            .OrderBy(sc => sc.DisplayOrder)
            .ThenBy(sc => sc.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(ServiceCategoryId id, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceCategories
            .AnyAsync(sc => sc.Id == id, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(Slug slug, ServiceCategoryId? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceCategories.Where(sc => sc.Slug == slug);

        if (excludeId != null)
        {
            query = query.Where(sc => sc.Id != excludeId);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(ServiceCategory category, CancellationToken cancellationToken = default)
    {
        await _context.ServiceCategories.AddAsync(category, cancellationToken);
        _logger.LogDebug("Added service category {CategoryId} to context", category.Id);
    }

    public Task UpdateAsync(ServiceCategory category, CancellationToken cancellationToken = default)
    {
        _context.ServiceCategories.Update(category);
        _logger.LogDebug("Updated service category {CategoryId} in context", category.Id);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ServiceCategory category, CancellationToken cancellationToken = default)
    {
        _context.ServiceCategories.Remove(category);
        _logger.LogDebug("Removed service category {CategoryId} from context", category.Id);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var changes = await _context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Saved {ChangeCount} changes to database", changes);
    }
}
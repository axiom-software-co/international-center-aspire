using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.Specifications;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Domain.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InternationalCenter.Services.Public.Api.Infrastructure.Repositories;

public sealed class ServiceRepository : IServiceRepository
{
    private readonly IServicesDbContext _context;
    private readonly ILogger<ServiceRepository> _logger;

    public ServiceRepository(IServicesDbContext context, ILogger<ServiceRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Service?> GetByIdAsync(ServiceId id, CancellationToken cancellationToken = default)
    {
        return await _context.Services
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Service?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        return await _context.Services
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Services
            .Include(s => s.Category)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetBySpecificationAsync(ISpecification<Service> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync(ISpecification<Service> specification, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification, ignoreQueryFilters: false);
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> CountAsync(ISpecification<Service> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification, ignoreQueryFilters: false)
            .CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(ServiceId id, CancellationToken cancellationToken = default)
    {
        return await _context.Services
            .AnyAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(Slug slug, ServiceId? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Services.Where(s => s.Slug == slug);
        
        if (excludeId != null)
        {
            query = query.Where(s => s.Id != excludeId);
        }
        
        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Service service, CancellationToken cancellationToken = default)
    {
        await _context.Services.AddAsync(service, cancellationToken);
        _logger.LogDebug("Added service {ServiceId} to context", service.Id);
    }

    public Task UpdateAsync(Service service, CancellationToken cancellationToken = default)
    {
        _context.Services.Update(service);
        _logger.LogDebug("Updated service {ServiceId} in context", service.Id);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Service service, CancellationToken cancellationToken = default)
    {
        _context.Services.Remove(service);
        _logger.LogDebug("Removed service {ServiceId} from context", service.Id);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var changes = await _context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Saved {ChangeCount} changes to database", changes);
    }

    private IQueryable<Service> ApplySpecification(ISpecification<Service> specification, bool ignoreQueryFilters = true)
    {
        var query = _context.Services.AsQueryable();

        // Apply paging when enabled (regardless of ignoreQueryFilters)
        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Include strings
        foreach (var include in specification.IncludeStrings)
        {
            query = query.Include(include);
        }

        // Include expressions
        foreach (var include in specification.Includes)
        {
            query = query.Include(include);
        }

        // Apply ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply additional orderings
        foreach (var thenBy in specification.ThenByList)
        {
            query = ((IOrderedQueryable<Service>)query).ThenBy(thenBy);
        }

        foreach (var thenByDescending in specification.ThenByDescendingList)
        {
            query = ((IOrderedQueryable<Service>)query).ThenByDescending(thenByDescending);
        }

        return query;
    }
}
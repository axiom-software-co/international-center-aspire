using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.Specifications;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Domain.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InternationalCenter.Services.Admin.Api.Infrastructure.Repositories;

/// <summary>
/// Admin API service repository implementation with medical-grade audit logging
/// Optimized for write operations and comprehensive change tracking
/// </summary>
public sealed class AdminServiceRepository : IServiceRepository
{
    private readonly IServicesDbContext _context;
    private readonly ILogger<AdminServiceRepository> _logger;

    public AdminServiceRepository(IServicesDbContext context, ILogger<AdminServiceRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Service?> GetByIdAsync(ServiceId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ADMIN_READ: Retrieving service by ID: {ServiceId}", id);
        
        var service = await _context.Services
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        await _context.AuditReadOperationAsync("Service", "GetById", service != null ? 1 : 0, "admin");
        return service;
    }

    public async Task<Service?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ADMIN_READ: Retrieving service by slug: {Slug}", slug);
        
        var service = await _context.Services
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Slug == slug, cancellationToken);

        await _context.AuditReadOperationAsync("Service", "GetBySlug", service != null ? 1 : 0, "admin");
        return service;
    }

    public async Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ADMIN_READ: Retrieving all services");
        
        var services = await _context.Services
            .Include(s => s.Category)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Title)
            .ToListAsync(cancellationToken);

        await _context.AuditReadOperationAsync("Service", "GetAll", services.Count, "admin");
        return services.AsReadOnly();
    }

    public async Task<IReadOnlyList<Service>> GetBySpecificationAsync(ISpecification<Service> specification, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ADMIN_READ: Retrieving services by specification: {SpecificationType}", specification.GetType().Name);
        
        var query = ApplySpecification(_context.Services, specification);
        var services = await query.ToListAsync(cancellationToken);

        await _context.AuditReadOperationAsync("Service", "GetBySpecification", services.Count, "admin");
        return services.AsReadOnly();
    }

    public async Task<(IReadOnlyList<Service> Items, int TotalCount)> GetPagedAsync(ISpecification<Service> specification, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ADMIN_READ: Retrieving paged services: Page={Page}, PageSize={PageSize}", page, pageSize);
        
        var query = ApplySpecification(_context.Services, specification);
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        await _context.AuditReadOperationAsync("Service", "GetPaged", items.Count, "admin");
        return (items.AsReadOnly(), totalCount);
    }

    public async Task<int> CountAsync(ISpecification<Service> specification, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(_context.Services, specification);
        var count = await query.CountAsync(cancellationToken);

        await _context.AuditReadOperationAsync("Service", "Count", count, "admin");
        return count;
    }

    public async Task<bool> ExistsAsync(ServiceId id, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Services.AnyAsync(s => s.Id == id, cancellationToken);
        await _context.AuditReadOperationAsync("Service", "Exists", exists ? 1 : 0, "admin");
        return exists;
    }

    public async Task<bool> SlugExistsAsync(Slug slug, ServiceId? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Services.Where(s => s.Slug == slug);
        if (excludeId != null)
        {
            query = query.Where(s => s.Id != excludeId);
        }

        var exists = await query.AnyAsync(cancellationToken);
        await _context.AuditReadOperationAsync("Service", "SlugExists", exists ? 1 : 0, "admin");
        return exists;
    }

    public async Task AddAsync(Service service, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ADMIN_WRITE: Adding new service: {Title} ({Id})", service.Title, service.Id);
        
        await _context.Services.AddAsync(service, cancellationToken);
        await _context.AuditWriteOperationAsync("Service", "ADD", service.Id, "admin");
    }

    public async Task UpdateAsync(Service service, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ADMIN_WRITE: Updating service: {Title} ({Id})", service.Title, service.Id);
        
        _context.Services.Update(service);
        await _context.AuditWriteOperationAsync("Service", "UPDATE", service.Id, "admin");
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Service service, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("ADMIN_WRITE: Deleting service: {Title} ({Id})", service.Title, service.Id);
        
        _context.Services.Remove(service);
        await _context.AuditWriteOperationAsync("Service", "DELETE", service.Id, "admin");
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var changeCount = _context.ChangeTracker.Entries().Count(e => e.State != EntityState.Unchanged);
        _logger.LogInformation("ADMIN_WRITE: Saving {ChangeCount} changes to database", changeCount);
        
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Service> ApplySpecification(IQueryable<Service> query, ISpecification<Service> specification)
    {
        // Apply criteria
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes
        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply ThenBy ordering
        if (specification.ThenByList.Any())
        {
            var orderedQuery = query as IOrderedQueryable<Service>;
            if (orderedQuery != null)
            {
                query = specification.ThenByList.Aggregate(orderedQuery, (current, thenBy) => current.ThenBy(thenBy));
            }
        }

        // Apply ThenByDescending ordering
        if (specification.ThenByDescendingList.Any())
        {
            var orderedQuery = query as IOrderedQueryable<Service>;
            if (orderedQuery != null)
            {
                query = specification.ThenByDescendingList.Aggregate(orderedQuery, (current, thenByDesc) => current.ThenByDescending(thenByDesc));
            }
        }

        // Apply paging
        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        return query;
    }
}
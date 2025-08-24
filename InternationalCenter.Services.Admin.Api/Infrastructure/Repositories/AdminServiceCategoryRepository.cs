using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Domain.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InternationalCenter.Services.Admin.Api.Infrastructure.Repositories;

/// <summary>
/// Admin API service category repository implementation with medical-grade audit logging
/// Optimized for administrative operations with comprehensive change tracking
/// </summary>
public sealed class AdminServiceCategoryRepository : IServiceCategoryRepository
{
    private readonly IServicesDbContext _context;
    private readonly ILogger<AdminServiceCategoryRepository> _logger;

    public AdminServiceCategoryRepository(IServicesDbContext context, ILogger<AdminServiceCategoryRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ServiceCategory?> GetByIdAsync(ServiceCategoryId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ADMIN_READ: Retrieving service category by ID: {CategoryId}", id);
        
        var category = await _context.ServiceCategories
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        await _context.AuditReadOperationAsync("ServiceCategory", "GetById", category != null ? 1 : 0, "admin");
        return category;
    }

    public async Task<ServiceCategory?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ADMIN_READ: Retrieving service category by slug: {Slug}", slug);
        
        var category = await _context.ServiceCategories
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

        await _context.AuditReadOperationAsync("ServiceCategory", "GetBySlug", category != null ? 1 : 0, "admin");
        return category;
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ADMIN_READ: Retrieving all service categories, activeOnly: {ActiveOnly}", activeOnly);
        
        var query = _context.ServiceCategories.AsQueryable();
        
        if (activeOnly)
        {
            query = query.Where(c => c.Active);
        }
        
        var categories = await query
            .Include(c => c.Services)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        await _context.AuditReadOperationAsync("ServiceCategory", "GetAll", categories.Count, "admin");
        return categories.AsReadOnly();
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetActiveOrderedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ADMIN_READ: Retrieving active service categories ordered by display order");
        
        var categories = await _context.ServiceCategories
            .Where(c => c.Active)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        await _context.AuditReadOperationAsync("ServiceCategory", "GetActiveOrdered", categories.Count, "admin");
        return categories.AsReadOnly();
    }

    public async Task<bool> ExistsAsync(ServiceCategoryId id, CancellationToken cancellationToken = default)
    {
        var exists = await _context.ServiceCategories.AnyAsync(c => c.Id == id, cancellationToken);
        await _context.AuditReadOperationAsync("ServiceCategory", "Exists", exists ? 1 : 0, "admin");
        return exists;
    }

    public async Task<bool> SlugExistsAsync(Slug slug, ServiceCategoryId? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceCategories.Where(c => c.Slug == slug);
        if (excludeId != null)
        {
            query = query.Where(c => c.Id != excludeId);
        }

        var exists = await query.AnyAsync(cancellationToken);
        await _context.AuditReadOperationAsync("ServiceCategory", "SlugExists", exists ? 1 : 0, "admin");
        return exists;
    }

    public async Task AddAsync(ServiceCategory category, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ADMIN_WRITE: Adding new service category: {Name} ({Id})", category.Name, category.Id);
        
        await _context.ServiceCategories.AddAsync(category, cancellationToken);
        await _context.AuditWriteOperationAsync("ServiceCategory", "ADD", category.Id.ToString(), "admin");
    }

    public async Task UpdateAsync(ServiceCategory category, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ADMIN_WRITE: Updating service category: {Name} ({Id})", category.Name, category.Id);
        
        _context.ServiceCategories.Update(category);
        await _context.AuditWriteOperationAsync("ServiceCategory", "UPDATE", category.Id.ToString(), "admin");
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(ServiceCategory category, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("ADMIN_WRITE: Deleting service category: {Name} ({Id})", category.Name, category.Id);
        
        _context.ServiceCategories.Remove(category);
        await _context.AuditWriteOperationAsync("ServiceCategory", "DELETE", category.Id.ToString(), "admin");
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var changeCount = _context.ChangeTracker.Entries().Count(e => e.State != EntityState.Unchanged);
        _logger.LogInformation("ADMIN_WRITE: Saving {ChangeCount} changes to database", changeCount);
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
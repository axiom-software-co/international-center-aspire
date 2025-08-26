using Infrastructure.Database.Abstractions;
using Infrastructure.Database.Base;
using Infrastructure.Database.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Shared.Entities;
using Services.Shared.Infrastructure.EntityConfigurations;

namespace Services.Shared.Infrastructure.Data;

/// <summary>
/// Services domain-specific Admin API database context extending generic audit infrastructure.
/// DOMAIN: Services domain specific database operations
/// ADMIN API: EF Core implementation for Services Admin API operations
/// AUDIT: Medical-grade audit capabilities inherited from BaseAuditableDbContext
/// </summary>
public sealed class ServicesAdminDbContext : BaseAuditableDbContext, IAuditableDbContext
{
    private readonly ILogger<ServicesAdminDbContext> _logger;

    public ServicesAdminDbContext(
        DbContextOptions<ServicesAdminDbContext> options,
        IOptions<DatabaseConnectionOptions> databaseOptions,
        ILogger<ServicesAdminDbContext> logger) 
        : base(options, databaseOptions, logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Test-only constructor for integration tests
    /// </summary>
    public ServicesAdminDbContext(DbContextOptions<ServicesAdminDbContext> options) 
        : base(options)
    {
        _logger = null!;
    }

    /// <summary>
    /// Services entity set for Admin API operations
    /// </summary>
    public DbSet<Service> Services => Set<Service>();

    /// <summary>
    /// Service categories entity set for Admin API operations  
    /// </summary>
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply Services domain entity configurations
        modelBuilder.ApplyConfiguration(new ServiceEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceCategoryEntityConfiguration());

        // Apply domain-specific constraints and indexes optimized for Admin operations
        ApplyAdminOptimizedConstraints(modelBuilder);
    }

    /// <summary>
    /// Apply Admin API specific database constraints and indexes.
    /// ADMIN API: Optimized for frequent writes and complex queries
    /// </summary>
    private static void ApplyAdminOptimizedConstraints(ModelBuilder modelBuilder)
    {
        // Service admin optimizations
        modelBuilder.Entity<Service>(entity =>
        {
            // Admin-specific indexes for management operations
            entity.HasIndex(s => new { s.Status, s.CreatedAt })
                  .HasDatabaseName("IX_Services_Admin_StatusCreated");
            
            entity.HasIndex(s => new { s.Available, s.Featured, s.UpdatedAt })
                  .HasDatabaseName("IX_Services_Admin_Management");
                  
            entity.HasIndex(s => s.UpdatedAt)
                  .HasDatabaseName("IX_Services_Admin_RecentUpdates");
        });

        // ServiceCategory admin optimizations
        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasIndex(sc => new { sc.Active, sc.CreatedAt })
                  .HasDatabaseName("IX_ServiceCategories_Admin_ActiveCreated");
                  
            entity.HasIndex(sc => sc.UpdatedAt)
                  .HasDatabaseName("IX_ServiceCategories_Admin_RecentUpdates");
        });
    }

    /// <summary>
    /// Get published services for admin operations (includes unpublished for management).
    /// ADMIN API: Full access to all service states
    /// </summary>
    public IQueryable<Service> AllServices => Services.Include(s => s.Category);

    /// <summary>
    /// Get all categories for admin operations (includes inactive for management).
    /// ADMIN API: Full access to all category states
    /// </summary>
    public IQueryable<ServiceCategory> AllCategories => ServiceCategories.Include(c => c.Services);

    /// <summary>
    /// Get services by status for admin filtering.
    /// ADMIN API: Status-based filtering for management
    /// </summary>
    public IQueryable<Service> GetServicesByStatus(ServiceStatus status)
    {
        return Services.Where(s => s.Status == status)
                      .Include(s => s.Category);
    }

    /// <summary>
    /// Get recently updated services for admin dashboard.
    /// ADMIN API: Recent changes tracking
    /// </summary>
    public IQueryable<Service> GetRecentlyUpdatedServices(DateTime since)
    {
        return Services.Where(s => s.UpdatedAt >= since)
                      .OrderByDescending(s => s.UpdatedAt)
                      .Include(s => s.Category);
    }

    /// <summary>
    /// Medical-grade audit operation for Services domain read access.
    /// AUDIT: Domain-specific audit logging
    /// </summary>
    public async Task AuditServicesReadOperationAsync(string operation, int recordCount, string? userId = null)
    {
        await AuditReadOperationAsync("Services", operation, recordCount, userId);
        
        _logger?.LogInformation("SERVICES ADMIN READ: Operation={Operation}, RecordCount={RecordCount}, UserId={UserId}, Timestamp={Timestamp}",
            operation, recordCount, userId ?? "system", DateTime.UtcNow);
    }

    /// <summary>
    /// Medical-grade audit operation for Services domain write access.
    /// AUDIT: Domain-specific audit logging
    /// </summary>
    public async Task AuditServicesWriteOperationAsync(string operation, string entityId, string? userId = null)
    {
        await AuditWriteOperationAsync("Services", operation, entityId, userId);
        
        _logger?.LogInformation("SERVICES ADMIN WRITE: Operation={Operation}, EntityId={EntityId}, UserId={UserId}, Timestamp={Timestamp}",
            operation, entityId, userId ?? "system", DateTime.UtcNow);
    }

    /// <summary>
    /// Override SaveChangesAsync for Services domain-specific audit trails.
    /// AUDIT: Enhanced audit logging with Services context
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Capture Services domain changes before saving for enhanced audit trail
        var serviceEntries = ChangeTracker.Entries<Service>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
            
        var categoryEntries = ChangeTracker.Entries<ServiceCategory>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Log Services domain-specific audit entries after successful save
        foreach (var entry in serviceEntries)
        {
            var service = entry.Entity;
            await AuditServicesWriteOperationAsync(
                $"SERVICE_{entry.State.ToString().ToUpper()}", 
                service.Id.Value);
        }
        
        foreach (var entry in categoryEntries)
        {
            var category = entry.Entity;
            await AuditServicesWriteOperationAsync(
                $"CATEGORY_{entry.State.ToString().ToUpper()}", 
                category.Id.Value.ToString());
        }

        return result;
    }
}
using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Domain.Infrastructure.EntityConfigurations;
using InternationalCenter.Services.Domain.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;

namespace InternationalCenter.Services.Domain.Infrastructure.Data;

/// <summary>
/// Unified database context for Services domain with medical-grade audit capabilities
/// Implements IServicesDbContext interface for dependency inversion and testability
/// Pragmatic architecture that eliminates DI complexity while maintaining standards
/// </summary>
public sealed class ServicesDbContext : DbContext, IServicesDbContext
{
    private readonly ILogger<ServicesDbContext>? _logger;
    private readonly IConfiguration? _configuration;

    public ServicesDbContext(
        DbContextOptions<ServicesDbContext> options,
        ILogger<ServicesDbContext> logger,
        IConfiguration configuration) : base(options) 
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Test-only constructor for integration tests
    /// </summary>
    public ServicesDbContext(DbContextOptions<ServicesDbContext> options) : base(options) 
    {
        _logger = null;
        _configuration = null;
    }

    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new ServiceEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceCategoryEntityConfiguration());

        // Apply domain-specific constraints and indexes
        ApplyDomainConstraints(modelBuilder);
    }

    private static void ApplyDomainConstraints(ModelBuilder modelBuilder)
    {
        // Service constraints
        modelBuilder.Entity<Service>(entity =>
        {
            // Performance indexes following Microsoft EF Core recommendations
            entity.HasIndex(s => new { s.Status, s.Available, s.Featured })
                  .HasDatabaseName("IX_Services_Performance_StatusAvailableFeatured");
            
            entity.HasIndex(s => s.Slug)
                  .IsUnique()
                  .HasDatabaseName("IX_Services_Slug_Unique");
                  
            entity.HasIndex(s => new { s.SortOrder, s.Title })
                  .HasDatabaseName("IX_Services_Ordering");
                  
            entity.HasIndex(s => s.CategoryId)
                  .HasDatabaseName("IX_Services_CategoryId");

            // Full-text search support for PostgreSQL
            entity.HasIndex(s => new { s.Title, s.Description })
                  .HasDatabaseName("IX_Services_Search");
        });

        // ServiceCategory constraints
        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasIndex(sc => sc.Slug)
                  .IsUnique()
                  .HasDatabaseName("IX_ServiceCategories_Slug_Unique");
                  
            entity.HasIndex(sc => new { sc.Active, sc.DisplayOrder })
                  .HasDatabaseName("IX_ServiceCategories_ActiveOrder");
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // This should not happen in production as options are provided via DI
            throw new InvalidOperationException("DbContext is not configured. Ensure connection string is provided.");
        }

        // Enable sensitive data logging only in development
        optionsBuilder.EnableDetailedErrors();
        
        #if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
        #endif
    }

    /// <summary>
    /// Get published services for public operations (repository pattern approach)
    /// </summary>
    public IQueryable<Service> PublishedServices => Services.Where(s => s.Status == ServiceStatus.Published);

    /// <summary>
    /// Get active categories for public operations (repository pattern approach)
    /// </summary>
    public IQueryable<ServiceCategory> ActiveCategories => ServiceCategories.Where(c => c.Active == true);

    /// <summary>
    /// Medical-grade audit operation for read access
    /// </summary>
    public async Task AuditReadOperationAsync(string entityType, string operation, int recordCount, string? userId = null)
    {
        await Task.CompletedTask;
        
        _logger?.LogInformation("READ AUDIT: EntityType={EntityType}, Operation={Operation}, RecordCount={RecordCount}, UserId={UserId}, Timestamp={Timestamp}",
            entityType, operation, recordCount, userId ?? "system", DateTime.UtcNow);
    }

    /// <summary>
    /// Medical-grade audit operation for write access  
    /// </summary>
    public async Task AuditWriteOperationAsync(string entityType, string operation, string entityId, string? userId = null)
    {
        await Task.CompletedTask;
        
        _logger?.LogInformation("WRITE AUDIT: EntityType={EntityType}, Operation={Operation}, EntityId={EntityId}, UserId={UserId}, Timestamp={Timestamp}",
            entityType, operation, entityId, userId ?? "system", DateTime.UtcNow);
    }

    /// <summary>
    /// Override SaveChangesAsync for medical-grade audit trails
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Capture changes before saving for audit trail
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Log audit entries after successful save
        foreach (var entry in entries)
        {
            var entityId = GetEntityId(entry);
            await AuditWriteOperationAsync(entry.Entity.GetType().Name, entry.State.ToString().ToUpper(), entityId);
        }

        return result;
    }

    /// <summary>
    /// Extract entity ID for audit logging
    /// </summary>
    private string GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        try
        {
            // Try to get the primary key value(s)
            var keyValues = entry.Metadata.FindPrimaryKey()?.Properties
                .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
                .Where(v => v != null)
                .ToList();

            if (keyValues?.Any() == true)
            {
                return string.Join(",", keyValues);
            }

            // Fallback: try common ID property names
            var commonIdNames = new[] { "Id", "id", "ID" };
            foreach (var idName in commonIdNames)
            {
                var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == idName);
                if (property != null)
                {
                    return property.CurrentValue?.ToString() ?? "Unknown";
                }
            }

            return "Unknown";
        }
        catch (Exception)
        {
            return "Unknown";
        }
    }
}
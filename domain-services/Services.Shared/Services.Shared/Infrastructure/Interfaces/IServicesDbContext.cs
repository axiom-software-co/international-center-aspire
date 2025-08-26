using Services.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Services.Shared.Infrastructure.Interfaces;

/// <summary>
/// Database context contract following Interface Segregation Principle
/// Contains only essential operations needed by repositories for TDD unit testing
/// Maintains medical-grade audit logging interface contract
/// </summary>
public interface IServicesDbContext
{
    /// <summary>
    /// Services entity set for repository operations
    /// </summary>
    DbSet<Service> Services { get; }

    /// <summary>
    /// Service categories entity set for repository operations  
    /// </summary>
    DbSet<ServiceCategory> ServiceCategories { get; }

    /// <summary>
    /// Medical-grade audit operation for read access tracking
    /// Required for admin API compliance and security standards
    /// </summary>
    Task AuditReadOperationAsync(string entityType, string operation, int recordCount, string? userId = null);

    /// <summary>
    /// Medical-grade audit operation for write access tracking
    /// Required for admin API compliance and security standards
    /// </summary>
    Task AuditWriteOperationAsync(string entityType, string operation, string entityId, string? userId = null);

    /// <summary>
    /// Unit of Work pattern - commits all pending changes to database
    /// Essential for repository SaveChanges operations
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// EF Core change tracking for repository operations
    /// Required for audit logging and change detection
    /// </summary>
    ChangeTracker ChangeTracker { get; }
}
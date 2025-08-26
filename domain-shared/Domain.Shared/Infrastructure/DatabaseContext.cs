using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace Shared.Infrastructure;

public abstract class BaseDatabaseContext : DbContext
{
    protected BaseDatabaseContext(DbContextOptions options) : base(options) { }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (BaseEntity)entityEntry.Entity;
            
            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: Services entity configurations moved to ServicesDbContext - will be configured in domain-specific context
        // Services and ServiceCategory entities are now part of the Services domain, not shared infrastructure
        
        // Configure BaseEntity primary key for all derived entities
        // modelBuilder.Entity<Service>(entity =>
        // {
        //     // Configure primary key (removed [Key] attribute)
        //     entity.HasKey(e => e.Id);
        //     
        //     // Configure required fields (removed [Required] attributes)
        //     entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
        //     entity.Property(e => e.Slug).IsRequired().HasMaxLength(255);
        //     entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        //     entity.Property(e => e.Icon).HasMaxLength(255);
        //     entity.Property(e => e.Image).HasMaxLength(500);
        //     entity.Property(e => e.Category).HasMaxLength(100);
        //     entity.Property(e => e.MetaTitle).HasMaxLength(255);
        //     entity.Property(e => e.MetaDescription).HasMaxLength(500);
        //     
        //     // Configure PostgreSQL array columns
        //     entity.Property(e => e.Technologies).HasColumnType("text[]");
        //     entity.Property(e => e.Features).HasColumnType("text[]");
        //     entity.Property(e => e.DeliveryModes).HasColumnType("text[]");
        //     
        //     // Configure indexes
        //     entity.HasIndex(e => e.Slug).IsUnique();
        //     entity.HasIndex(e => e.Status);
        //     entity.HasIndex(e => e.CategoryId);
        //     entity.HasIndex(e => e.Featured);
        // });

        // modelBuilder.Entity<ServiceCategory>(entity =>
        // {
        //     // Configure primary key (removed [Key] attribute with DatabaseGenerated)
        //     entity.HasKey(e => e.Id);
        //     entity.Property(e => e.Id).ValueGeneratedOnAdd();
        //     
        //     // Configure required fields (removed [Required] and [MaxLength] attributes)
        //     entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        //     entity.Property(e => e.Slug).IsRequired().HasMaxLength(255);
        //     
        //     // Configure indexes
        //     entity.HasIndex(e => e.Name).IsUnique();
        //     entity.HasIndex(e => e.Slug).IsUnique();
        //     entity.HasIndex(e => e.Active);
        //     
        //     // Explicit column mappings to ensure EnsureCreatedAsync creates all properties
        //     entity.Property(e => e.Featured1).IsRequired();
        //     entity.Property(e => e.Featured2).IsRequired();
        //     entity.Property(e => e.DisplayOrder).IsRequired();
        //     entity.Property(e => e.MinPriorityOrder).IsRequired();
        //     entity.Property(e => e.MaxPriorityOrder).IsRequired();
        // });

        // Domain-specific entity configurations removed - each domain manages its own entities
        // News, Research, Events, Contacts, Search entities belong in their respective domain projects

        base.OnModelCreating(modelBuilder);
    }
}
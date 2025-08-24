using Microsoft.EntityFrameworkCore;
using InternationalCenter.Shared.Models;

namespace InternationalCenter.Shared.Infrastructure;

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
        // Configure PostgreSQL array columns
        modelBuilder.Entity<Service>(entity =>
        {
            entity.Property(e => e.Technologies).HasColumnType("text[]");
            entity.Property(e => e.Features).HasColumnType("text[]");
            entity.Property(e => e.DeliveryModes).HasColumnType("text[]");
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.Featured);
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Active);
            
            // Explicit column mappings to ensure EnsureCreatedAsync creates all properties
            entity.Property(e => e.Featured1).IsRequired();
            entity.Property(e => e.Featured2).IsRequired();
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.Property(e => e.MinPriorityOrder).IsRequired();
            entity.Property(e => e.MaxPriorityOrder).IsRequired();
        });

        modelBuilder.Entity<NewsArticle>(entity =>
        {
            entity.Property(e => e.Tags).HasColumnType("text[]");
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Featured);
            entity.HasIndex(e => e.PublishedAt);
        });

        // TODO: Add configurations for other models as they are implemented
        // Temporarily commented to focus on Services and News APIs
        /*
        modelBuilder.Entity<ResearchArticle>(entity =>
        {
            entity.Property(e => e.Tags).HasColumnType("text[]");
            entity.Property(e => e.Keywords).HasColumnType("text[]");
            entity.Property(e => e.Collaborators).HasColumnType("text[]");
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Featured);
            entity.HasIndex(e => e.PublishedAt);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.Property(e => e.Tags).HasColumnType("text[]");
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Featured);
            entity.HasIndex(e => e.StartDate);
            entity.HasIndex(e => e.EndDate);
        });

        modelBuilder.Entity<NewsletterSubscription>(entity =>
        {
            entity.Property(e => e.Preferences).HasColumnType("text[]");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<UnifiedSearchIndex>(entity =>
        {
            entity.Property(e => e.Tags).HasColumnType("text[]");
            entity.HasIndex(e => e.ContentType);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsPublished);
            entity.HasIndex(e => e.IsFeatured);
            entity.HasIndex(e => e.PublishedAt);
            entity.HasIndex(e => e.LastIndexed);
        });
        */

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatedAt);
        });

        base.OnModelCreating(modelBuilder);
    }
}
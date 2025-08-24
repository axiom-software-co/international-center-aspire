using Microsoft.EntityFrameworkCore;
using InternationalCenter.Shared.Models;
using InternationalCenter.Shared.Infrastructure;

namespace InternationalCenter.News.Migrations.Service;

/// <summary>
/// News domain-specific DbContext following vertical slice architecture
/// Handles only News-related entities for domain isolation
/// </summary>
public class NewsDbContext : BaseDatabaseContext
{
    public NewsDbContext(DbContextOptions<NewsDbContext> options) : base(options) { }

    // News domain entities only
    public DbSet<NewsArticle> NewsArticles { get; set; }
    public DbSet<NewsCategory> NewsCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure News domain-specific indexes and constraints
        modelBuilder.Entity<NewsArticle>(entity =>
        {
            // Index for published articles by date
            entity.HasIndex(n => new { n.IsPublished, n.PublishDate })
                  .HasDatabaseName("IX_NewsArticles_Published_PublishDate")
                  .HasFilter("[IsPublished] = 1");
            
            // Index for category filtering
            entity.HasIndex(n => n.CategoryId)
                  .HasDatabaseName("IX_NewsArticles_CategoryId");
            
            // Index for featured articles
            entity.HasIndex(n => new { n.IsFeatured, n.PublishDate })
                  .HasDatabaseName("IX_NewsArticles_Featured_PublishDate")
                  .HasFilter("[IsFeatured] = 1");
                  
            // Full-text search index on title and summary
            entity.HasIndex(n => new { n.Title, n.Summary })
                  .HasDatabaseName("IX_NewsArticles_Title_Summary");
        });

        modelBuilder.Entity<NewsCategory>(entity =>
        {
            // Index for active categories with ordering
            entity.HasIndex(nc => new { nc.IsActive, nc.SortOrder })
                  .HasDatabaseName("IX_NewsCategories_Active_SortOrder");
                  
            // Unique constraint on category name
            entity.HasIndex(nc => nc.Name)
                  .IsUnique()
                  .HasDatabaseName("IX_NewsCategories_Name_Unique");
        });
    }
}
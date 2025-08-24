using Microsoft.EntityFrameworkCore;
using InternationalCenter.Shared.Models;

namespace InternationalCenter.Shared.Infrastructure;

public class ApplicationDbContext : BaseDatabaseContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Services
    public DbSet<Service> Services { get; set; }
    public DbSet<ServiceCategory> ServiceCategories { get; set; }

    // News
    public DbSet<NewsArticle> NewsArticles { get; set; }
    public DbSet<NewsCategory> NewsCategories { get; set; }

    // Research
    public DbSet<ResearchArticle> ResearchArticles { get; set; }

    // Events
    public DbSet<Event> Events { get; set; }
    public DbSet<EventRegistration> EventRegistrations { get; set; }

    // Contacts
    public DbSet<Contact> Contacts { get; set; }

    // Newsletter
    public DbSet<NewsletterSubscription> NewsletterSubscriptions { get; set; }

    // Search
    public DbSet<UnifiedSearchIndex> UnifiedSearch { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure indexes for common query patterns following Microsoft EF Core best practices
        modelBuilder.Entity<Service>(entity =>
        {
            // Composite index for common filtering pattern: Status + Available + Featured
            entity.HasIndex(s => new { s.Status, s.Available, s.Featured })
                  .HasDatabaseName("IX_Services_Status_Available_Featured");
            
            // Index for category filtering
            entity.HasIndex(s => s.Category)
                  .HasDatabaseName("IX_Services_Category");
            
            // Index for sorting by priority (SortOrder) and title
            entity.HasIndex(s => new { s.SortOrder, s.Title })
                  .HasDatabaseName("IX_Services_SortOrder_Title");
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            // Index for active categories ordering
            entity.HasIndex(sc => new { sc.Active, sc.DisplayOrder })
                  .HasDatabaseName("IX_ServiceCategories_Active_DisplayOrder");
        });
    }
}
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Infrastructure.EntityConfigurations;

namespace Shared.Infrastructure;

public class ApplicationDbContext : AuditableDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider) { }

    // Medical-grade audit logging - zero data loss compliance
    public new DbSet<AuditLog> AuditLogs { get; set; }

    // Services removed - managed by ServicesDbContext in Services domain

    // NOTE: The following APIs are on hold - only Services APIs are active
    // Commenting out to allow Services API tests to compile properly
    
    // News (ON HOLD)
    // public DbSet<NewsArticle> NewsArticles { get; set; }
    // public DbSet<NewsCategory> NewsCategories { get; set; }

    // Research (ON HOLD)
    // public DbSet<ResearchArticle> ResearchArticles { get; set; }

    // Events (ON HOLD)
    // public DbSet<Event> Events { get; set; }
    // public DbSet<EventRegistration> EventRegistrations { get; set; }

    // Contacts (ON HOLD)
    // public DbSet<Contact> Contacts { get; set; }

    // Newsletter (ON HOLD)
    // public DbSet<NewsletterSubscription> NewsletterSubscriptions { get; set; }

    // Search (ON HOLD)
    // public DbSet<UnifiedSearchIndex> UnifiedSearch { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply medical-grade audit log configuration
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        
        // Service entity configuration removed - managed by ServicesDbContext in Services domain
    }
}
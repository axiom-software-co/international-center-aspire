using Infrastructure.Database.Base;
using Infrastructure.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Service.Audit.Data;

public sealed class AuditDbContext : BaseAuditableDbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        ConfigureAuditEvent(modelBuilder);
    }

    private static void ConfigureAuditEvent(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AuditEvent>();
        
        entity.ToTable("audit_events");
        
        entity.HasKey(e => e.Id);
        
        entity.Property(e => e.Id)
            .HasMaxLength(255)
            .IsRequired();
            
        entity.Property(e => e.EventType)
            .HasConversion<int>()
            .IsRequired();
            
        entity.Property(e => e.EntityType)
            .HasMaxLength(255)
            .IsRequired();
            
        entity.Property(e => e.EntityId)
            .HasMaxLength(255)
            .IsRequired();
            
        entity.Property(e => e.UserId)
            .HasMaxLength(255);
            
        entity.Property(e => e.UserName)
            .HasMaxLength(255);
            
        entity.Property(e => e.SessionId)
            .HasMaxLength(255);
            
        entity.Property(e => e.IpAddress)
            .HasMaxLength(45); // IPv6 max length
            
        entity.Property(e => e.UserAgent)
            .HasMaxLength(2000);
            
        entity.Property(e => e.Timestamp)
            .IsRequired();
            
        entity.Property(e => e.Reason)
            .HasMaxLength(1000);
            
        entity.Property(e => e.OldValues)
            .HasColumnType("jsonb");
            
        entity.Property(e => e.NewValues)
            .HasColumnType("jsonb");
            
        entity.Property(e => e.Signature)
            .HasMaxLength(500)
            .IsRequired();
            
        entity.Property(e => e.SignatureAlgorithm)
            .HasMaxLength(50)
            .IsRequired();
            
        entity.Property(e => e.CorrelationId)
            .HasMaxLength(255);
        
        // Indexes for common query patterns
        entity.HasIndex(e => new { e.EntityType, e.EntityId })
            .HasDatabaseName("ix_audit_events_entity");
            
        entity.HasIndex(e => e.Timestamp)
            .HasDatabaseName("ix_audit_events_timestamp");
            
        entity.HasIndex(e => e.EventType)
            .HasDatabaseName("ix_audit_events_event_type");
            
        entity.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_audit_events_user_id");
            
        entity.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("ix_audit_events_correlation_id");
    }
}
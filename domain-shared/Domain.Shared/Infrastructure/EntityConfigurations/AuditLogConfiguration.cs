using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Models;

namespace Shared.Infrastructure.EntityConfigurations;

/// <summary>
/// Medical-grade EF Core configuration for AuditLog entity
/// Optimized for PostgreSQL with comprehensive indexing strategy
/// Designed for zero data loss and high-performance audit queries
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Table configuration
        builder.ToTable("audit_logs");
        
        // Primary key configuration (inherited from BaseEntity)
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("Primary key for audit log entry");

        // Required string fields with appropriate lengths
        builder.Property(e => e.EntityType)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("Type of entity being audited (e.g., Service, User)");

        builder.Property(e => e.EntityId)
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("ID of the specific entity instance");

        builder.Property(e => e.Action)
            .HasMaxLength(20)
            .IsRequired()
            .HasComment("Action performed (CREATE, UPDATE, DELETE, READ)");

        builder.Property(e => e.UserId)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("ID of user performing the action");

        builder.Property(e => e.UserName)
            .HasMaxLength(200)
            .IsRequired()
            .HasComment("Name of user performing the action");

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("Correlation ID for tracing requests across services");

        builder.Property(e => e.TraceId)
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("Trace ID for distributed tracing");

        builder.Property(e => e.RequestUrl)
            .HasMaxLength(500)
            .IsRequired()
            .HasComment("URL of the request that triggered the audit");

        builder.Property(e => e.RequestMethod)
            .HasMaxLength(10)
            .IsRequired()
            .HasComment("HTTP method of the request");

        builder.Property(e => e.RequestIp)
            .HasMaxLength(45) // IPv6 length
            .IsRequired()
            .HasComment("IP address of the client making the request");

        builder.Property(e => e.UserAgent)
            .HasMaxLength(200) // Increased for modern user agents
            .IsRequired()
            .HasComment("User agent string of the client");

        builder.Property(e => e.AppVersion)
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("Version of the application that generated the audit");

        builder.Property(e => e.BuildDate)
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("Build date of the application version");

        // PostgreSQL JSONB columns for structured data
        builder.Property(e => e.OldValues)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}")
            .IsRequired()
            .HasComment("Previous values before change (JSON)");

        builder.Property(e => e.NewValues)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}")
            .IsRequired()
            .HasComment("New values after change (JSON)");

        builder.Property(e => e.ChangedProperties)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired()
            .HasComment("List of properties that changed (JSON array)");

        // Timestamp configuration
        builder.Property(e => e.AuditTimestamp)
            .HasColumnType("timestamptz") // PostgreSQL timestamp with timezone
            .HasDefaultValueSql("NOW()")
            .IsRequired()
            .HasComment("Timestamp when audit entry was created (UTC)");

        // Optional fields
        builder.Property(e => e.Reason)
            .HasMaxLength(500)
            .HasComment("Optional reason for the action");

        builder.Property(e => e.AdditionalData)
            .HasMaxLength(1000)
            .HasComment("Additional context data");

        // Critical action flag
        builder.Property(e => e.IsCriticalAction)
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("Flag indicating if this is a critical action requiring special attention");

        builder.Property(e => e.SessionId)
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("Session ID of the user performing the action");

        builder.Property(e => e.ClientApplication)
            .HasMaxLength(100)
            .HasComment("Client application identifier");

        // Processing duration as PostgreSQL interval
        builder.Property(e => e.ProcessingDuration)
            .HasColumnType("interval")
            .HasDefaultValue(TimeSpan.Zero)
            .IsRequired()
            .HasComment("Time taken to process the request");

        builder.Property(e => e.Severity)
            .HasMaxLength(20)
            .HasDefaultValue("INFO")
            .IsRequired()
            .HasComment("Severity level (INFO, WARN, ERROR, CRITICAL)");

        // Inherited BaseEntity properties
        builder.Property(e => e.CreatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired()
            .HasComment("Creation timestamp");

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired()
            .HasComment("Last update timestamp");

        // Comprehensive indexing strategy for medical-grade performance
        
        // Primary indexes for common queries
        builder.HasIndex(e => e.EntityType)
            .HasDatabaseName("IX_AuditLogs_EntityType");

        builder.HasIndex(e => new { e.EntityType, e.EntityId })
            .HasDatabaseName("IX_AuditLogs_Entity");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(e => e.AuditTimestamp)
            .HasDatabaseName("IX_AuditLogs_AuditTimestamp");

        builder.HasIndex(e => e.Action)
            .HasDatabaseName("IX_AuditLogs_Action");

        builder.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("IX_AuditLogs_CorrelationId");

        builder.HasIndex(e => e.Severity)
            .HasDatabaseName("IX_AuditLogs_Severity");

        // Partial index for critical actions (PostgreSQL optimization)
        builder.HasIndex(e => e.IsCriticalAction)
            .HasDatabaseName("IX_AuditLogs_IsCriticalAction")
            .HasFilter("is_critical_action = true");

        // Composite indexes for common query patterns
        builder.HasIndex(e => new { e.EntityType, e.AuditTimestamp })
            .HasDatabaseName("IX_AuditLogs_EntityType_AuditTimestamp");

        builder.HasIndex(e => new { e.UserId, e.AuditTimestamp })
            .HasDatabaseName("IX_AuditLogs_UserId_AuditTimestamp");

        builder.HasIndex(e => new { e.IsCriticalAction, e.AuditTimestamp })
            .HasDatabaseName("IX_AuditLogs_IsCriticalAction_AuditTimestamp")
            .HasFilter("is_critical_action = true");

        builder.HasIndex(e => new { e.EntityType, e.Action, e.AuditTimestamp })
            .HasDatabaseName("IX_AuditLogs_EntityType_Action_AuditTimestamp");

        // GIN index for JSONB columns (PostgreSQL-specific for JSON queries)
        builder.HasIndex(e => e.OldValues)
            .HasDatabaseName("IX_AuditLogs_OldValues_GIN")
            .HasMethod("gin");

        builder.HasIndex(e => e.NewValues)
            .HasDatabaseName("IX_AuditLogs_NewValues_GIN")
            .HasMethod("gin");

        builder.HasIndex(e => e.ChangedProperties)
            .HasDatabaseName("IX_AuditLogs_ChangedProperties_GIN")
            .HasMethod("gin");

        // Hash index for exact correlation ID lookups (PostgreSQL optimization)
        builder.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("IX_AuditLogs_CorrelationId_Hash")
            .HasMethod("hash");
    }
}
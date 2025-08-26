namespace Shared.Models;

/// <summary>
/// Medical-grade audit log entity for zero data loss compliance
/// Configured using EF Core Fluent API instead of data annotations
/// Stores comprehensive audit trail for all business operations
/// </summary>
public class AuditLog : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, READ
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string RequestUrl { get; set; } = string.Empty;
    public string RequestMethod { get; set; } = string.Empty;
    public string RequestIp { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public string BuildDate { get; set; } = string.Empty;
    public string OldValues { get; set; } = "{}";
    public string NewValues { get; set; } = "{}";
    public string ChangedProperties { get; set; } = "[]";
    public DateTime AuditTimestamp { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    public string? AdditionalData { get; set; }
    public bool IsCriticalAction { get; set; } = false;
    public string SessionId { get; set; } = string.Empty;
    public string? ClientApplication { get; set; }
    public TimeSpan ProcessingDuration { get; set; } = TimeSpan.Zero;
    public string Severity { get; set; } = AuditSeverity.Info;
}

/// <summary>
/// Medical-grade audit configuration for healthcare compliance
/// Ensures zero data loss and proper retention policies
/// </summary>
public class AuditConfiguration
{
    public bool EnableAuditing { get; set; } = true;
    public bool AuditCreates { get; set; } = true;
    public bool AuditUpdates { get; set; } = true;
    public bool AuditDeletes { get; set; } = true;
    public bool AuditReads { get; set; } = false; // Usually disabled for performance
    public List<string> ExcludedProperties { get; set; } = new() { "UpdatedAt", "LastAccessed" };
    public List<string> SensitiveProperties { get; set; } = new() 
    { 
        "Password", "Token", "Secret", "Key", "Hash",
        "SSN", "TaxId", "CreditCard", "BankAccount",
        "Phone", "Email" // Medical privacy compliance
    };
    public int MaxRetentionDays { get; set; } = 2555; // 7 years for medical-grade compliance
    public bool EncryptSensitiveData { get; set; } = true;
    public int BatchSize { get; set; } = 1000; // For bulk audit operations
    public bool EnableArchiving { get; set; } = true; // Archive old logs instead of deletion
    public string ArchiveConnectionString { get; set; } = string.Empty;
}

public interface IAuditable
{
    string Id { get; }
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
}

public interface IAuditableWithReason : IAuditable
{
    string? AuditReason { get; set; }
}

public static class AuditActions
{
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string Delete = "DELETE";
    public const string Read = "READ";
    public const string Login = "LOGIN";
    public const string Logout = "LOGOUT";
    public const string Export = "EXPORT";
    public const string Import = "IMPORT";
    public const string Archive = "ARCHIVE";
    public const string Restore = "RESTORE";
}

public static class AuditSeverity
{
    public const string Info = "INFO";
    public const string Warning = "WARN";
    public const string Error = "ERROR";
    public const string Critical = "CRITICAL";
}

public class AuditContext
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string RequestUrl { get; set; } = string.Empty;
    public string RequestMethod { get; set; } = string.Empty;
    public string RequestIp { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public string BuildDate { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string? ClientApplication { get; set; }
    public string? Reason { get; set; }
    public DateTime RequestStartTime { get; set; } = DateTime.UtcNow;
}
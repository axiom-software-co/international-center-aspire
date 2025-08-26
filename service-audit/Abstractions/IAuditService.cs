namespace Service.Audit.Abstractions;

public interface IAuditService
{
    Task<string> LogAsync(AuditEventType eventType, string entityType, string entityId, 
        object? oldValues, object? newValues, string? reason = null, 
        CancellationToken cancellationToken = default);
        
    Task<string> LogAsync(AuditEventType eventType, string entityType, string entityId, 
        string? reason = null, CancellationToken cancellationToken = default);
        
    Task<IReadOnlyList<AuditEvent>> GetAuditTrailAsync(string entityType, string entityId, 
        CancellationToken cancellationToken = default);
        
    Task<IReadOnlyList<AuditEvent>> GetAuditTrailAsync(string entityType, string entityId, 
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
        
    Task<bool> VerifyIntegrityAsync(string auditId, CancellationToken cancellationToken = default);
    
    Task<AuditIntegrityReport> VerifyEntityIntegrityAsync(string entityType, string entityId, 
        CancellationToken cancellationToken = default);
}
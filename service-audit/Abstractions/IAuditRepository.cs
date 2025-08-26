namespace Service.Audit.Abstractions;

public interface IAuditRepository
{
    Task<string> CreateAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
    
    Task<AuditEvent?> GetAuditEventAsync(string auditId, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<AuditEvent>> GetAuditTrailAsync(string entityType, string entityId, 
        CancellationToken cancellationToken = default);
        
    Task<IReadOnlyList<AuditEvent>> GetAuditTrailAsync(string entityType, string entityId, 
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
        
    Task<IReadOnlyList<AuditEvent>> GetAuditEventsAsync(DateTimeOffset from, DateTimeOffset to, 
        CancellationToken cancellationToken = default);
        
    Task<bool> ExistsAsync(string auditId, CancellationToken cancellationToken = default);
}
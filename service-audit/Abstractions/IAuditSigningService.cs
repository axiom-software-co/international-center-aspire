namespace Service.Audit.Abstractions;

public interface IAuditSigningService
{
    Task<string> GenerateSignatureAsync(string data, CancellationToken cancellationToken = default);
    
    Task<bool> VerifySignatureAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
    
    Task<bool> VerifySignatureAsync(string data, string signature, CancellationToken cancellationToken = default);
}
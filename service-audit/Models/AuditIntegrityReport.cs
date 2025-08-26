namespace Service.Audit.Models;

public sealed class AuditIntegrityReport
{
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public bool IsValid { get; init; }
    public int TotalEvents { get; init; }
    public int ValidEvents { get; init; }
    public int InvalidEvents { get; init; }
    public DateTimeOffset VerifiedAt { get; init; }
    public IReadOnlyList<AuditIntegrityViolation> Violations { get; init; } = Array.Empty<AuditIntegrityViolation>();
}

public sealed class AuditIntegrityViolation
{
    public string AuditId { get; init; } = string.Empty;
    public AuditEventType EventType { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string ViolationType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ExpectedSignature { get; init; } = string.Empty;
    public string ActualSignature { get; init; } = string.Empty;
}
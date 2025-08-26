namespace Service.Audit.Configuration;

public sealed class AuditServiceOptions
{
    public const string SectionName = "AuditService";
    
    public bool Enabled { get; set; } = true;
    public bool RequireSignatures { get; set; } = true;
    public string SigningAlgorithm { get; set; } = "HMACSHA256";
    public string? SigningKey { get; set; }
    public bool LogReadOperations { get; set; } = false;
    public bool LogSystemEvents { get; set; } = true;
    public int MaxAuditRetentionDays { get; set; } = 2555; // 7 years for medical compliance
    public bool EnableBackgroundCleanup { get; set; } = true;
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(30);
    public int BatchSize { get; set; } = 1000;
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
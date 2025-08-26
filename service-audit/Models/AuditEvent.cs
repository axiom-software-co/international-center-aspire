using System.Text.Json;

namespace Service.Audit.Models;

public sealed class AuditEvent
{
    public string Id { get; init; } = string.Empty;
    public AuditEventType EventType { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public string? SessionId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string? Reason { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string Signature { get; init; } = string.Empty;
    public string SignatureAlgorithm { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
    
    public T? GetOldValues<T>() where T : class
    {
        if (string.IsNullOrEmpty(OldValues)) return null;
        return JsonSerializer.Deserialize<T>(OldValues);
    }
    
    public T? GetNewValues<T>() where T : class
    {
        if (string.IsNullOrEmpty(NewValues)) return null;
        return JsonSerializer.Deserialize<T>(NewValues);
    }
    
    public string GetDataForSigning()
    {
        var data = new
        {
            Id,
            EventType = EventType.ToString(),
            EntityType,
            EntityId,
            UserId,
            UserName,
            SessionId,
            IpAddress,
            UserAgent,
            Timestamp = Timestamp.ToString("O"),
            Reason,
            OldValues,
            NewValues,
            CorrelationId
        };
        
        return JsonSerializer.Serialize(data, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
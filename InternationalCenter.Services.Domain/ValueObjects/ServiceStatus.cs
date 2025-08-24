namespace InternationalCenter.Services.Domain.ValueObjects;

public enum ServiceStatus
{
    Draft,
    Published,
    Archived
}

public static class ServiceStatusExtensions
{
    public static string ToStringValue(this ServiceStatus status)
    {
        return status switch
        {
            ServiceStatus.Draft => "draft",
            ServiceStatus.Published => "published",
            ServiceStatus.Archived => "archived",
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
    }

    public static ServiceStatus FromString(string status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "draft" => ServiceStatus.Draft,
            "published" => ServiceStatus.Published,
            "active" => ServiceStatus.Published, // Legacy support
            "archived" => ServiceStatus.Archived,
            _ => throw new ArgumentException($"Invalid service status: {status}")
        };
    }
}
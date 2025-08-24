using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace InternationalCenter.Shared.Infrastructure.Observability;

public static class LoggingExtensions
{
    public static IDisposable BeginServiceScope(this ILogger logger, string serviceName, string operation, string? correlationId = null)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["service.name"] = serviceName,
            ["operation.name"] = operation,
            ["correlation.id"] = correlationId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            ["trace.id"] = Activity.Current?.TraceId.ToString() ?? "",
            ["span.id"] = Activity.Current?.SpanId.ToString() ?? ""
        }) ?? throw new InvalidOperationException("Unable to create logging scope");
    }
    
    public static void LogServiceOperation(this ILogger logger, string serviceName, string operation, object? data = null, string? correlationId = null)
    {
        using var scope = logger.BeginServiceScope(serviceName, operation, correlationId);
        logger.LogInformation("Service operation completed: {Operation} {@Data}", operation, data);
    }
    
    public static void LogServiceError(this ILogger logger, string serviceName, Exception exception, string operation, object? data = null, string? correlationId = null)
    {
        using var scope = logger.BeginServiceScope(serviceName, operation, correlationId);
        logger.LogError(exception, "Service operation failed: {Operation} {@Data}", operation, data);
    }
    
    public static void LogBusinessEvent(this ILogger logger, string serviceName, string eventName, object? eventData = null, string? correlationId = null)
    {
        using var scope = logger.BeginServiceScope(serviceName, "BusinessEvent", correlationId);
        logger.LogInformation("Business event: {EventName} {@EventData}", eventName, eventData);
    }
    
    public static void LogPerformanceMetric(this ILogger logger, string serviceName, string metricName, double value, string unit = "", string? correlationId = null)
    {
        using var scope = logger.BeginServiceScope(serviceName, "Performance", correlationId);
        logger.LogInformation("Performance metric: {MetricName} = {Value} {Unit}", metricName, value, unit);
    }
}
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Services;

namespace Shared.Infrastructure.Observability;

public static class LoggingExtensions
{
    public static IDisposable BeginServiceScope(this ILogger logger, string serviceName, string operation, string? correlationId = null, IHttpContextAccessor? httpContextAccessor = null, IVersionService? versionService = null)
    {
        var scope = new Dictionary<string, object>
        {
            ["service.name"] = serviceName,
            ["operation.name"] = operation,
            ["correlation.id"] = correlationId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            ["trace.id"] = Activity.Current?.TraceId.ToString() ?? "",
            ["span.id"] = Activity.Current?.SpanId.ToString() ?? ""
        };

        // Add HTTP context information for medical-grade compliance
        if (httpContextAccessor?.HttpContext is HttpContext httpContext)
        {
            // Request URL for medical-grade audit trail
            scope["request.url"] = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}{httpContext.Request.QueryString}";
            scope["request.method"] = httpContext.Request.Method;
            scope["request.ip"] = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            // User ID for medical-grade compliance (zero data loss requirement)
            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                scope["user.id"] = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                                  user.FindFirst("sub")?.Value ?? 
                                  user.FindFirst("user_id")?.Value ?? "authenticated_unknown";
                scope["user.name"] = user.FindFirst(ClaimTypes.Name)?.Value ?? user.Identity.Name ?? "unknown";
            }
            else
            {
                scope["user.id"] = "anonymous";
                scope["user.name"] = "anonymous";
            }

            // Request ID for correlation
            if (httpContext.Request.Headers.ContainsKey("X-Request-ID"))
            {
                scope["request.id"] = httpContext.Request.Headers["X-Request-ID"].FirstOrDefault() ?? "";
            }
        }
        else
        {
            // Fallback values when HTTP context not available
            scope["request.url"] = "N/A";
            scope["request.method"] = "N/A";
            scope["request.ip"] = "N/A";
            scope["user.id"] = "system";
            scope["user.name"] = "system";
        }

        // App version for medical-grade compliance (version on every log)
        if (versionService != null)
        {
            scope["app.version"] = versionService.GetVersion();
            scope["app.build_date"] = versionService.BuildDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
        else
        {
            scope["app.version"] = "unknown";
            scope["app.build_date"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        // Timestamp for medical-grade audit requirements
        scope["log.timestamp"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        scope["log.level"] = "INFO"; // Default, will be overridden by actual log level

        return logger.BeginScope(scope) ?? throw new InvalidOperationException("Unable to create logging scope");
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
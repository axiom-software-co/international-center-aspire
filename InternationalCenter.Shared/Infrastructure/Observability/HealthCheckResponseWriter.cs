using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InternationalCenter.Shared.Infrastructure.Observability;

public static class HealthCheckResponseWriter
{
    public static Task WriteResponse(HttpContext context, HealthReport healthReport)
    {
        var serviceName = context.Items["ServiceName"]?.ToString() ?? "Unknown";
            
        var result = new
        {
            status = healthReport.Status.ToString().ToLowerInvariant(),
            duration = $"{healthReport.TotalDuration.TotalMilliseconds:F2}ms",
            timestamp = DateTimeOffset.UtcNow,
            service = serviceName,
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            checks = healthReport.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString().ToLowerInvariant(),
                duration = $"{e.Value.Duration.TotalMilliseconds:F2}ms",
                description = e.Value.Description,
                data = e.Value.Data,
                exception = e.Value.Exception?.Message,
                tags = e.Value.Tags
            }).ToArray()
        };
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 
                                      healthReport.Status == HealthStatus.Degraded ? 200 : 503;
                                      
        return context.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
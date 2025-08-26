using System.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using StackExchange.Redis;

namespace Shared.Infrastructure.Observability;

public static class ObservabilityServiceExtensions
{
    public static IServiceCollection AddProductionObservability<TDbContext>(
        this IServiceCollection services,
        string serviceName) where TDbContext : DbContext
    {
        // Register observability options directly (Microsoft recommended options pattern)
        services.AddSingleton(new ObservabilityOptions { ServiceName = serviceName });
        
        // Add custom metrics with service-specific name
        services.AddSingleton(provider => new ServiceMetrics(serviceName));
        
        // Add comprehensive health checks for medical-grade monitoring
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck<TDbContext>>("database", tags: new[] { "database", "critical" })
            .AddCheck<RedisHealthCheck>("redis", tags: new[] { "cache", "important" })
            .AddCheck<MedicalGradeAuditHealthCheck>("audit-system", tags: new[] { "audit", "critical" })
            .AddCheck<SecurityPolicyHealthCheck>("zero-trust-security", tags: new[] { "security", "critical" })
            .AddCheck<PerformanceHealthCheck>("performance-metrics", tags: new[] { "performance", "important" })
            .AddCheck($"{serviceName.ToLowerInvariant()}-service", () => HealthCheckResult.Healthy($"{serviceName} service is running"), tags: new[] { "service", "custom" });
            
        // Add OpenTelemetry with comprehensive instrumentation
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddSource($"InternationalCenter.{serviceName}")
                       .AddSource("InternationalCenter.Database")
                       .AddSource("InternationalCenter.Cache")
                       .AddAspNetCoreInstrumentation(options =>
                       {
                           options.RecordException = true;
                           options.EnrichWithHttpRequest = (activity, request) => EnrichWithHttpRequest(activity, request, serviceName);
                           options.EnrichWithHttpResponse = EnrichWithHttpResponse;
                       })
                       .AddHttpClientInstrumentation(options =>
                       {
                           options.RecordException = true;
                           options.EnrichWithHttpRequestMessage = EnrichWithHttpRequestMessage;
                           options.EnrichWithHttpResponseMessage = EnrichWithHttpResponseMessage;
                       })
                       .AddEntityFrameworkCoreInstrumentation(options =>
                       {
                           options.SetDbStatementForText = true;
                           options.SetDbStatementForStoredProcedure = true;
                           options.EnrichWithIDbCommand = EnrichWithDbCommand;
                       });
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter($"InternationalCenter.{serviceName}")
                       .AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddProcessInstrumentation()
                       .AddRuntimeInstrumentation();
            });
        
        return services;
    }
    
    public static IApplicationBuilder UseProductionObservability(
        this IApplicationBuilder app,
        string serviceName)
    {
        // Add performance monitoring middleware with service name
        app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<PerformanceMonitoringMiddleware>>();
            var metrics = context.RequestServices.GetRequiredService<ServiceMetrics>();
            var middleware = new PerformanceMonitoringMiddleware(next, logger, metrics, serviceName);
            await middleware.InvokeAsync(context);
        });
        
        // Add health checks endpoints with custom response writer
        app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = (context, report) =>
            {
                context.Items["ServiceName"] = serviceName;
                return HealthCheckResponseWriter.WriteResponse(context, report);
            },
            AllowCachingResponses = false
        });
        
        // Add health checks endpoint for readiness (critical checks only)
        app.UseHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("critical"),
            ResponseWriter = (context, report) =>
            {
                context.Items["ServiceName"] = serviceName;
                return HealthCheckResponseWriter.WriteResponse(context, report);
            },
            AllowCachingResponses = false
        });
        
        // Add health checks endpoint for liveness (basic checks only)
        app.UseHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("self"),
            ResponseWriter = (context, report) =>
            {
                context.Items["ServiceName"] = serviceName;
                return HealthCheckResponseWriter.WriteResponse(context, report);
            },
            AllowCachingResponses = false
        });
        
        return app;
    }
    
    // Overload for services that don't have database context
    public static IServiceCollection AddProductionObservability(
        this IServiceCollection services,
        string serviceName)
    {
        // Register observability options directly (Microsoft recommended options pattern)
        services.AddSingleton(new ObservabilityOptions { ServiceName = serviceName });
        
        // Add custom metrics
        services.AddSingleton(provider => new ServiceMetrics(serviceName));
        
        // Add basic health checks (without database)
        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>("redis", tags: new[] { "cache", "important" })
            .AddCheck("self", () => HealthCheckResult.Healthy("Service is running"), tags: new[] { "self" });
            
        // Add OpenTelemetry (same configuration as above but without EF Core instrumentation)
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddSource($"InternationalCenter.{serviceName}")
                       .AddSource("InternationalCenter.Cache")
                       .AddAspNetCoreInstrumentation(options =>
                       {
                           options.RecordException = true;
                           options.EnrichWithHttpRequest = (activity, request) => EnrichWithHttpRequest(activity, request, serviceName);
                           options.EnrichWithHttpResponse = EnrichWithHttpResponse;
                       })
                       .AddHttpClientInstrumentation(options =>
                       {
                           options.RecordException = true;
                           options.EnrichWithHttpRequestMessage = EnrichWithHttpRequestMessage;
                           options.EnrichWithHttpResponseMessage = EnrichWithHttpResponseMessage;
                       });
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter($"InternationalCenter.{serviceName}")
                       .AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddProcessInstrumentation()
                       .AddRuntimeInstrumentation();
            });
        
        return services;
    }
    
    private static void EnrichWithHttpRequest(Activity activity, HttpRequest request, string serviceName)
    {
        activity.AddTag("service.name", $"InternationalCenter.{serviceName}");
        activity.AddTag("http.user_agent", request.Headers.UserAgent.ToString());
        activity.AddTag("http.request_content_length", request.ContentLength);
        activity.AddTag("correlation.id", request.Headers["X-Correlation-Id"].FirstOrDefault());
        activity.AddTag("http.client_ip", GetClientIpAddress(request));
    }
    
    private static void EnrichWithHttpResponse(Activity activity, HttpResponse response)
    {
        activity.AddTag("http.response_content_length", response.ContentLength);
        activity.AddTag("http.response_content_type", response.ContentType);
    }
    
    private static void EnrichWithHttpRequestMessage(Activity activity, HttpRequestMessage request)
    {
        activity.AddTag("http.client.method", request.Method.Method);
        activity.AddTag("http.client.url", request.RequestUri?.ToString());
    }
    
    private static void EnrichWithHttpResponseMessage(Activity activity, HttpResponseMessage response)
    {
        activity.AddTag("http.client.status_code", (int)response.StatusCode);
        activity.AddTag("http.client.response_content_length", response.Content.Headers.ContentLength);
    }
    
    private static void EnrichWithDbCommand(Activity activity, IDbCommand command)
    {
        var operation = command.CommandText?.Split(' ').FirstOrDefault()?.ToUpper();
        activity.AddTag("db.operation", operation);
        activity.AddTag("db.table", ExtractTableName(command.CommandText));
        activity.AddTag("db.command_timeout", command.CommandTimeout);
        
        if (command.Parameters.Count > 0)
        {
            activity.AddTag("db.parameter_count", command.Parameters.Count);
        }
    }
    
    private static string ExtractTableName(string? commandText)
    {
        if (string.IsNullOrEmpty(commandText)) return "";
        
        var words = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length - 1; i++)
        {
            if (words[i].Equals("FROM", StringComparison.OrdinalIgnoreCase) ||
                words[i].Equals("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                words[i].Equals("INTO", StringComparison.OrdinalIgnoreCase))
            {
                var tableName = words[i + 1].Trim('`', '"', '[', ']');
                var parts = tableName.Split('.');
                return parts.Length > 1 ? parts[1] : parts[0];
            }
        }
        
        return "";
    }
    
    private static string GetClientIpAddress(HttpRequest request)
    {
        var forwardedFor = request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').FirstOrDefault()?.Trim() ?? "";
        }
        
        var realIp = request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }
        
        return request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
    }
}

public class ObservabilityOptions
{
    public string ServiceName { get; set; } = "";
}
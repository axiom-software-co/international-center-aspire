using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure.Observability;

public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly ServiceMetrics _metrics;
    private readonly string _serviceName;
    private readonly ActivitySource _activitySource;
    
    public PerformanceMonitoringMiddleware(
        RequestDelegate next, 
        ILogger<PerformanceMonitoringMiddleware> logger,
        ServiceMetrics metrics,
        string serviceName)
    {
        _next = next;
        _logger = logger;
        _metrics = metrics;
        _serviceName = serviceName;
        _activitySource = ActivitySources.GetByServiceName(serviceName);
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "";
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        
        // Add correlation ID to response headers
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        
        // Start activity for distributed tracing
        using var activity = _activitySource.StartActivity($"{method} {path}");
        activity?.AddTag("http.method", method);
        activity?.AddTag("http.route", path);
        activity?.AddTag("http.scheme", context.Request.Scheme);
        activity?.AddTag("correlation.id", correlationId);
        activity?.AddTag("service.name", _serviceName);
        activity?.AddTag("service.version", "1.0.0");
        
        // Add request information to activity
        if (context.Request.ContentLength.HasValue)
        {
            activity?.AddTag("http.request_content_length", context.Request.ContentLength.Value);
        }
        
        if (context.Request.Headers.UserAgent.Any())
        {
            activity?.AddTag("http.user_agent", context.Request.Headers.UserAgent.ToString());
        }
        
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Record the exception in activity and metrics
            activity?.AddTag("error", true);
            activity?.AddTag("error.type", ex.GetType().Name);
            activity?.AddTag("error.message", ex.Message);
            
            _logger.LogServiceError(_serviceName, ex, "HTTP Request", new { method, path }, correlationId);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalSeconds;
            var statusCode = context.Response.StatusCode;
            
            // Record metrics
            _metrics.RecordRequest(method, path, duration, statusCode);
            
            // Add response information to activity
            activity?.AddTag("http.status_code", statusCode);
            if (context.Response.ContentLength.HasValue)
            {
                activity?.AddTag("http.response_content_length", context.Response.ContentLength.Value);
            }
            
            // Log request completion
            if (statusCode >= 400)
            {
                _logger.LogWarning(
                    "{ServiceName}: HTTP {Method} {Path} responded {StatusCode} in {Duration:F3}s (CorrelationId: {CorrelationId})",
                    _serviceName, method, path, statusCode, duration, correlationId);
            }
            else
            {
                _logger.LogInformation(
                    "{ServiceName}: HTTP {Method} {Path} responded {StatusCode} in {Duration:F3}s",
                    _serviceName, method, path, statusCode, duration);
            }
            
            // Log slow requests
            if (duration > 1.0)
            {
                _logger.LogWarning(
                    "{ServiceName}: Slow request detected - {Method} {Path} took {Duration:F3}s (CorrelationId: {CorrelationId})",
                    _serviceName, method, path, duration, correlationId);
            }
            
            // Log performance metric
            _logger.LogPerformanceMetric(_serviceName, "request_duration", duration, "seconds", correlationId);
        }
    }
}
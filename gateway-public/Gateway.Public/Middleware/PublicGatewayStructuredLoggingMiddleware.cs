using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using InternationalCenter.Shared.Services;

namespace InternationalCenter.Gateway.Public.Middleware;

/// <summary>
/// Structured logging middleware for Public Gateway with anonymous user tracking
/// Adds required fields: anonymous user ID, correlation ID, request URL, app version
/// </summary>
public class PublicGatewayStructuredLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PublicGatewayStructuredLoggingMiddleware> _logger;

    public PublicGatewayStructuredLoggingMiddleware(RequestDelegate next, ILogger<PublicGatewayStructuredLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var versionService = context.RequestServices.GetService<IVersionService>();
        var appVersion = versionService?.GetVersion() ?? "unknown";
        
        var correlationId = context.TraceIdentifier;
        var requestUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? 
                      context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
                      "unknown";
        
        // Generate anonymous user ID for tracking (no PII)
        var anonymousUserId = $"anon_{clientIp.GetHashCode():X8}";

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["UserId"] = anonymousUserId,
            ["CorrelationId"] = correlationId,
            ["RequestUrl"] = requestUrl,
            ["AppVersion"] = appVersion,
            ["Gateway"] = "Public",
            ["ClientIp"] = clientIp,
            ["UserAgent"] = context.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown"
        });

        var startTime = DateTimeOffset.UtcNow;

        try
        {
            _logger.LogInformation("PUBLIC_GATEWAY: Request started - Method: {HttpMethod} | Path: {RequestPath}", 
                context.Request.Method, context.Request.Path);

            await _next(context);

            var duration = DateTimeOffset.UtcNow - startTime;
            
            _logger.LogInformation("PUBLIC_GATEWAY: Request completed - Status: {StatusCode} | Duration: {Duration}ms", 
                context.Response.StatusCode, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            
            _logger.LogError(ex, "PUBLIC_GATEWAY: Request failed - Status: {StatusCode} | Duration: {Duration}ms | Error: {ErrorMessage}", 
                context.Response.StatusCode, duration.TotalMilliseconds, ex.Message);
            
            throw;
        }
    }
}

/// <summary>
/// Extension methods for registering Public Gateway structured logging middleware
/// </summary>
public static class PublicGatewayStructuredLoggingExtensions
{
    /// <summary>
    /// Adds structured logging middleware to the Public Gateway pipeline
    /// </summary>
    public static IApplicationBuilder UsePublicGatewayStructuredLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PublicGatewayStructuredLoggingMiddleware>();
    }
}
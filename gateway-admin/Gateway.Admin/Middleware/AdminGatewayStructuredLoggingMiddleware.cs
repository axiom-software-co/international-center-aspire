using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using InternationalCenter.Shared.Services;

namespace InternationalCenter.Gateway.Admin.Middleware;

/// <summary>
/// Structured logging middleware for Admin Gateway with user context tracking
/// Adds required fields: user ID, correlation ID, request URL, app version
/// Provides medical-grade audit trail continuity
/// </summary>
public class AdminGatewayStructuredLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdminGatewayStructuredLoggingMiddleware> _logger;

    public AdminGatewayStructuredLoggingMiddleware(RequestDelegate next, ILogger<AdminGatewayStructuredLoggingMiddleware> logger)
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
        
        // Get authenticated user ID for medical-grade audit trail
        var userId = context.User?.Identity?.Name ?? 
                    context.User?.FindFirst("sub")?.Value ?? 
                    context.User?.FindFirst("oid")?.Value ?? 
                    context.Request.Headers["X-User-ID"].FirstOrDefault() ?? 
                    "unauthenticated";

        var userRoles = context.User?.Claims
            .Where(c => c.Type == "role" || c.Type == "roles")
            .Select(c => c.Value)
            .ToArray() ?? Array.Empty<string>();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["UserId"] = userId,
            ["CorrelationId"] = correlationId,
            ["RequestUrl"] = requestUrl,
            ["AppVersion"] = appVersion,
            ["Gateway"] = "Admin",
            ["ClientIp"] = clientIp,
            ["UserAgent"] = context.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown",
            ["UserRoles"] = string.Join(",", userRoles),
            ["IsAuthenticated"] = context.User?.Identity?.IsAuthenticated ?? false
        });

        var startTime = DateTimeOffset.UtcNow;

        try
        {
            _logger.LogInformation("ADMIN_GATEWAY: Request started - Method: {HttpMethod} | Path: {RequestPath} | User: {UserId}", 
                context.Request.Method, context.Request.Path, userId);

            await _next(context);

            var duration = DateTimeOffset.UtcNow - startTime;
            
            _logger.LogInformation("ADMIN_GATEWAY: Request completed - Status: {StatusCode} | Duration: {Duration}ms | User: {UserId}", 
                context.Response.StatusCode, duration.TotalMilliseconds, userId);
        }
        catch (Exception ex)
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            
            _logger.LogError(ex, "ADMIN_GATEWAY: Request failed - Status: {StatusCode} | Duration: {Duration}ms | User: {UserId} | Error: {ErrorMessage}", 
                context.Response.StatusCode, duration.TotalMilliseconds, userId, ex.Message);
            
            throw;
        }
    }
}

/// <summary>
/// Extension methods for registering Admin Gateway structured logging middleware
/// </summary>
public static class AdminGatewayStructuredLoggingExtensions
{
    /// <summary>
    /// Adds structured logging middleware to the Admin Gateway pipeline with medical-grade audit trail continuity
    /// </summary>
    public static IApplicationBuilder UseAdminGatewayStructuredLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AdminGatewayStructuredLoggingMiddleware>();
    }
}
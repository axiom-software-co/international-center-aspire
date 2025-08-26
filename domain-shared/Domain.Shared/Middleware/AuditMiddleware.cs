using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;
using Shared.Services;
using Shared.Models;
using Shared.Infrastructure.Observability;

namespace Shared.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip audit for health checks and version endpoints
        if (ShouldSkipAudit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var auditContext = CreateAuditContext(context);
        var auditService = context.RequestServices.GetService<IAuditService>();
        var versionService = context.RequestServices.GetService<IVersionService>();
        
        if (auditService != null && versionService != null)
        {
            try
            {
                // Set audit context for this request
                auditService.SetAuditContext(auditContext);
                
                // Create structured logging scope with medical-grade context
                using var loggingScope = _logger.BeginServiceScope(
                    "AuditMiddleware", 
                    "RequestProcessing", 
                    auditContext.CorrelationId,
                    context.RequestServices.GetService<IHttpContextAccessor>(),
                    versionService);
                
                // Log incoming request for high-security endpoints
                if (IsHighSecurityEndpoint(context.Request.Path))
                {
                    await auditService.LogBusinessEventAsync(
                        AuditActions.Read,
                        "AdminEndpoint", 
                        context.Request.Path.ToString(),
                        new
                        {
                            Method = context.Request.Method,
                            QueryString = context.Request.QueryString.ToString(),
                            UserAgent = context.Request.Headers.UserAgent.FirstOrDefault(),
                            RemoteIp = context.Connection.RemoteIpAddress?.ToString()
                        },
                        AuditSeverity.Info);
                }
                
                _logger.LogInformation("Processing {Method} request to {Path} by {UserId}",
                    context.Request.Method, context.Request.Path, auditContext.UserId);
                    
                await _next(context);
                
                // Log completion for critical actions
                if (IsCriticalAction(context.Request.Method, context.Request.Path))
                {
                    await auditService.LogBusinessEventAsync(
                        GetAuditAction(context.Request.Method),
                        "AdminEndpoint", 
                        context.Request.Path.ToString(),
                        new
                        {
                            StatusCode = context.Response.StatusCode,
                            ProcessingDurationMs = (DateTime.UtcNow - auditContext.RequestStartTime).TotalMilliseconds
                        },
                        context.Response.StatusCode >= 400 ? AuditSeverity.Warning : AuditSeverity.Info);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process audit middleware for {Path}", context.Request.Path);
                
                try
                {
                    // Emergency audit log for middleware failures
                    await auditService.LogSystemEventAsync(
                        "AUDIT_MIDDLEWARE_ERROR",
                        $"Audit middleware failed for {context.Request.Method} {context.Request.Path}: {ex.Message}",
                        AuditSeverity.Error);
                }
                catch
                {
                    // Last resort logging
                    _logger.LogCritical("AUDIT SYSTEM FAILURE: Unable to log audit middleware error for {Path}", context.Request.Path);
                }
                
                // Continue processing - don't fail the request due to audit issues
                await _next(context);
            }
        }
        else
        {
            // If audit service is not available, continue without auditing
            await _next(context);
        }
    }

    private AuditContext CreateAuditContext(HttpContext context)
    {
        var auditContext = new AuditContext
        {
            CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            TraceId = Activity.Current?.TraceId.ToString() ?? "",
            RequestUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}",
            RequestMethod = context.Request.Method,
            RequestIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            UserAgent = context.Request.Headers.UserAgent.FirstOrDefault()?.Substring(0, Math.Min(100, context.Request.Headers.UserAgent.FirstOrDefault()?.Length ?? 0)) ?? "unknown",
            SessionId = context.Session?.Id ?? context.Connection.Id,
            ClientApplication = context.Request.Headers["X-Client-Application"].FirstOrDefault(),
            RequestStartTime = DateTime.UtcNow
        };

        // User information
        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            auditContext.UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                 user.FindFirst("sub")?.Value ??
                                 user.FindFirst("user_id")?.Value ?? "authenticated_unknown";
            auditContext.UserName = user.FindFirst(ClaimTypes.Name)?.Value ?? user.Identity.Name ?? "unknown";
        }
        else
        {
            auditContext.UserId = "anonymous";
            auditContext.UserName = "anonymous";
        }

        // Request ID for correlation
        if (context.Request.Headers.ContainsKey("X-Request-ID"))
        {
            auditContext.CorrelationId = context.Request.Headers["X-Request-ID"].FirstOrDefault() ?? auditContext.CorrelationId;
        }

        return auditContext;
    }

    private bool ShouldSkipAudit(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? "";
        
        return pathValue.Contains("/health") ||
               pathValue.Contains("/metrics") ||
               pathValue.Contains("/api/version") ||
               pathValue.Contains("/favicon.ico") ||
               pathValue.Contains("/swagger") ||
               pathValue.Contains("/openapi");
    }

    private bool IsHighSecurityEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? "";
        
        return pathValue.Contains("/admin") ||
               pathValue.Contains("/services") ||
               pathValue.Contains("/users") ||
               pathValue.Contains("/settings") ||
               pathValue.Contains("/config");
    }

    private bool IsCriticalAction(string method, PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? "";
        var isCriticalPath = pathValue.Contains("/admin") ||
                            pathValue.Contains("/delete") ||
                            pathValue.Contains("/archive") ||
                            pathValue.Contains("/users") ||
                            pathValue.Contains("/settings");
        
        var isCriticalMethod = method.Equals("DELETE", StringComparison.OrdinalIgnoreCase) ||
                              method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
                              method.Equals("PATCH", StringComparison.OrdinalIgnoreCase) ||
                              method.Equals("POST", StringComparison.OrdinalIgnoreCase);
        
        return isCriticalPath || isCriticalMethod;
    }

    private string GetAuditAction(string httpMethod)
    {
        return httpMethod.ToUpperInvariant() switch
        {
            "POST" => AuditActions.Create,
            "PUT" => AuditActions.Update,
            "PATCH" => AuditActions.Update,
            "DELETE" => AuditActions.Delete,
            "GET" => AuditActions.Read,
            _ => "HTTP_" + httpMethod.ToUpperInvariant()
        };
    }
}
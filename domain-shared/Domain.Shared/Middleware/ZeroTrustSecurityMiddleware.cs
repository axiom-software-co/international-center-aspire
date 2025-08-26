using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Shared.Services;
using Shared.Models;
using Shared.Security;
using Shared.Infrastructure.Observability;

namespace Shared.Middleware;

public class ZeroTrustSecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ZeroTrustSecurityMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public ZeroTrustSecurityMiddleware(RequestDelegate next, ILogger<ZeroTrustSecurityMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip security validation for health checks and version endpoints
        if (ShouldSkipSecurityValidation(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var auditService = context.RequestServices.GetService<IAuditService>();
        var versionService = context.RequestServices.GetService<IVersionService>();

        using var scope = _logger.BeginServiceScope(
            "ZeroTrustSecurity", 
            "RequestValidation", 
            context.TraceIdentifier,
            context.RequestServices.GetService<IHttpContextAccessor>(),
            versionService);

        try
        {
            // Validate IP address (if configured)
            if (!await ValidateIpAddressAsync(context, auditService))
            {
                await CreateSecurityResponseAsync(context, HttpStatusCode.Forbidden, "IP_ADDRESS_BLOCKED", "Access from this IP address is not allowed");
                return;
            }

            // Validate request headers for security threats
            if (!await ValidateSecurityHeadersAsync(context, auditService))
            {
                await CreateSecurityResponseAsync(context, HttpStatusCode.BadRequest, "INVALID_HEADERS", "Request contains invalid security headers");
                return;
            }

            // Validate request size and rate limiting
            if (!await ValidateRequestLimitsAsync(context, auditService))
            {
                await CreateSecurityResponseAsync(context, HttpStatusCode.TooManyRequests, "RATE_LIMIT_EXCEEDED", "Request rate limit exceeded");
                return;
            }

            // Validate SSL/TLS requirements
            if (!await ValidateSecureConnectionAsync(context, auditService))
            {
                await CreateSecurityResponseAsync(context, HttpStatusCode.UpgradeRequired, "INSECURE_CONNECTION", "Secure connection required");
                return;
            }

            // Add security headers to response
            AddSecurityHeaders(context);

            // Continue to next middleware
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in zero-trust security middleware for {Path} from {IpAddress}",
                context.Request.Path, context.Connection.RemoteIpAddress?.ToString());
            
            if (auditService != null)
            {
                await auditService.LogSecurityEventAsync(
                    "SECURITY_MIDDLEWARE_ERROR",
                    $"Security middleware error for {context.Request.Path}: {ex.Message}",
                    AuditSeverity.Error);
            }

            await CreateSecurityResponseAsync(context, HttpStatusCode.InternalServerError, "SECURITY_ERROR", "Security validation failed");
        }
    }

    private bool ShouldSkipSecurityValidation(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? "";
        
        return pathValue.Contains("/health") ||
               pathValue.Contains("/metrics") ||
               pathValue.Contains("/api/version") ||
               pathValue.Contains("/favicon.ico") ||
               pathValue.Contains("/openapi") ||
               pathValue.Contains("/swagger");
    }

    private async Task<bool> ValidateIpAddressAsync(HttpContext context, IAuditService? auditService)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ipAddress))
        {
            _logger.LogWarning("No IP address available for request to {Path}", context.Request.Path);
            return true; // Allow if IP can't be determined
        }

        // Check IP blocklist
        var blockedIps = _configuration.GetSection("Security:BlockedIpAddresses").Get<string[]>() ?? Array.Empty<string>();
        if (blockedIps.Contains(ipAddress))
        {
            _logger.LogWarning("Blocked IP address {IpAddress} attempted access to {Path}", ipAddress, context.Request.Path);
            
            if (auditService != null)
            {
                await auditService.LogSecurityEventAsync(
                    "IP_ADDRESS_BLOCKED",
                    $"Blocked IP {ipAddress} attempted access to {context.Request.Path}",
                    AuditSeverity.Warning);
            }
            
            return false;
        }

        // Check IP allowlist for admin endpoints
        if (IsAdminEndpoint(context.Request.Path))
        {
            var allowedIps = _configuration.GetSection("Security:AdminAllowedIpAddresses").Get<string[]>();
            if (allowedIps?.Any() == true && !allowedIps.Contains(ipAddress) && !allowedIps.Contains("*"))
            {
                _logger.LogWarning("Unauthorized IP address {IpAddress} attempted admin access to {Path}", ipAddress, context.Request.Path);
                
                if (auditService != null)
                {
                    await auditService.LogSecurityEventAsync(
                        "ADMIN_IP_UNAUTHORIZED",
                        $"Unauthorized IP {ipAddress} attempted admin access to {context.Request.Path}",
                        AuditSeverity.Warning);
                }
                
                return false;
            }
        }

        return true;
    }

    private async Task<bool> ValidateSecurityHeadersAsync(HttpContext context, IAuditService? auditService)
    {
        var headers = context.Request.Headers;
        
        // Check for suspicious User-Agent patterns
        var userAgent = headers.UserAgent.FirstOrDefault() ?? "";
        var suspiciousPatterns = new[] { "sqlmap", "nikto", "nmap", "masscan", "zap", "burp" };
        
        if (suspiciousPatterns.Any(pattern => userAgent.ToLowerInvariant().Contains(pattern)))
        {
            _logger.LogWarning("Suspicious User-Agent detected: {UserAgent} from {IpAddress}",
                userAgent, context.Connection.RemoteIpAddress?.ToString());
            
            if (auditService != null)
            {
                await auditService.LogSecurityEventAsync(
                    "SUSPICIOUS_USER_AGENT",
                    $"Suspicious User-Agent: {userAgent}",
                    AuditSeverity.Warning);
            }
            
            return false;
        }

        // Check for SQL injection patterns in headers
        var allHeaderValues = headers.SelectMany(h => h.Value).ToList();
        var sqlInjectionPatterns = new[] { "union select", "drop table", "exec(", "script>", "<iframe" };
        
        foreach (var headerValue in allHeaderValues)
        {
            var lowerValue = headerValue?.ToLowerInvariant() ?? "";
            if (sqlInjectionPatterns.Any(pattern => lowerValue.Contains(pattern)))
            {
                _logger.LogWarning("Potential security threat in headers from {IpAddress}",
                    context.Connection.RemoteIpAddress?.ToString());
                
                if (auditService != null)
                {
                    await auditService.LogSecurityEventAsync(
                        "SECURITY_THREAT_HEADERS",
                        "Potential security threat detected in request headers",
                        AuditSeverity.Warning);
                }
                
                return false;
            }
        }

        return true;
    }

    private async Task<bool> ValidateRequestLimitsAsync(HttpContext context, IAuditService? auditService)
    {
        // Check request body size limit
        var maxRequestSize = _configuration.GetValue<long>("Security:MaxRequestSizeBytes", 10 * 1024 * 1024); // 10MB default
        if (context.Request.ContentLength > maxRequestSize)
        {
            _logger.LogWarning("Request size {Size} exceeds limit {Limit} from {IpAddress}",
                context.Request.ContentLength, maxRequestSize, context.Connection.RemoteIpAddress?.ToString());
            
            if (auditService != null)
            {
                await auditService.LogSecurityEventAsync(
                    "REQUEST_SIZE_EXCEEDED",
                    $"Request size {context.Request.ContentLength} exceeds limit {maxRequestSize}",
                    AuditSeverity.Warning);
            }
            
            return false;
        }

        // Basic rate limiting (could be enhanced with more sophisticated implementation)
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"rate_limit_{ipAddress}";
        
        // This is a simplified rate limiting implementation
        // In production, consider using Redis or a more sophisticated rate limiting library
        
        return true; // For now, always pass rate limiting
    }

    private async Task<bool> ValidateSecureConnectionAsync(HttpContext context, IAuditService? auditService)
    {
        var requireHttps = _configuration.GetValue<bool>("Security:RequireHttps", true);
        
        if (requireHttps && !context.Request.IsHttps && !IsLocalDevelopment(context))
        {
            _logger.LogWarning("Insecure connection attempt to {Path} from {IpAddress}",
                context.Request.Path, context.Connection.RemoteIpAddress?.ToString());
            
            if (auditService != null)
            {
                await auditService.LogSecurityEventAsync(
                    "INSECURE_CONNECTION",
                    $"Insecure HTTP connection attempt to {context.Request.Path}",
                    AuditSeverity.Warning);
            }
            
            return false;
        }

        return true;
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Security headers
        headers["X-Frame-Options"] = "DENY";
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-XSS-Protection"] = "1; mode=block";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'";
        
        // HSTS header for HTTPS
        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        // Remove server identification headers
        headers.Remove("Server");
        headers["X-Powered-By"] = ""; // Remove if present
        
        // Add correlation headers for tracing
        if (!headers.ContainsKey("X-Correlation-ID"))
        {
            headers["X-Correlation-ID"] = context.TraceIdentifier;
        }
    }

    private bool IsAdminEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? "";
        return pathValue.Contains("/admin") || 
               pathValue.Contains("/management") || 
               pathValue.Contains("/configuration");
    }

    private bool IsLocalDevelopment(HttpContext context)
    {
        var host = context.Request.Host.Host.ToLowerInvariant();
        return host == "localhost" || 
               host == "127.0.0.1" || 
               host.StartsWith("192.168.") || 
               host.StartsWith("10.0.") ||
               host.StartsWith("172.");
    }

    private async Task CreateSecurityResponseAsync(HttpContext context, HttpStatusCode statusCode, string errorCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            Error = errorCode,
            Message = message,
            Timestamp = DateTime.UtcNow,
            TraceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Shared.Middleware;

/// <summary>
/// Comprehensive security headers middleware for medical-grade compliance
/// Implements modern security headers with gateway-specific configurations and fallback policies
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IConfiguration configuration,
        SecurityHeadersOptions options)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip security headers for health checks and version endpoints
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (path.Contains("/health") || path.Contains("/api/version") || path.Contains("/favicon.ico") || 
            path.Contains("/openapi") || path.Contains("/swagger"))
        {
            await _next(context);
            return;
        }

        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        
        try
        {
            await _next(context);
        }
        finally
        {
            // Apply security headers after request processing
            ApplySecurityHeaders(context, correlationId);
        }
    }

    private void ApplySecurityHeaders(HttpContext context, string correlationId)
    {
        var response = context.Response;
        
        try
        {
            // Core Security Headers (MANDATORY for medical-grade compliance)
            SetHeaderWithFallback(response, "X-Content-Type-Options", "nosniff");
            SetHeaderWithFallback(response, "X-Frame-Options", "DENY");
            SetHeaderWithFallback(response, "X-XSS-Protection", "1; mode=block");
            SetHeaderWithFallback(response, "Referrer-Policy", "strict-origin-when-cross-origin");
            
            // Content Security Policy (gateway-specific)
            var csp = _options.IsAdminGateway 
                ? "default-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'; upgrade-insecure-requests;"
                : "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self';";
            
            SetHeaderWithFallback(response, "Content-Security-Policy", csp);
            
            // HSTS (MANDATORY for medical-grade compliance)
            if (context.Request.IsHttps || _options.AlwaysAddHSTS)
            {
                SetHeaderWithFallback(response, "Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
            }
            
            // Modern Security Headers
            SetHeaderWithFallback(response, "Cross-Origin-Opener-Policy", "same-origin");
            SetHeaderWithFallback(response, "Cross-Origin-Embedder-Policy", "require-corp");
            SetHeaderWithFallback(response, "Cross-Origin-Resource-Policy", "same-origin");
            
            // Permission Policy (restrict dangerous features)
            var permissionPolicy = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
            SetHeaderWithFallback(response, "Permissions-Policy", permissionPolicy);
            
            // Gateway identification and tracking headers
            SetHeaderWithFallback(response, "X-Gateway-Type", _options.IsAdminGateway ? "ADMIN" : "PUBLIC");
            SetHeaderWithFallback(response, "X-Compliance-Level", "MEDICAL_GRADE");
            SetHeaderWithFallback(response, "X-Correlation-ID", correlationId);
            
            // Remove server information headers for security
            response.Headers.Remove("Server");
            response.Headers.Remove("X-Powered-By");
            response.Headers.Remove("X-AspNet-Version");
            response.Headers.Remove("X-AspNetMvc-Version");
            
            _logger.LogDebug("Security headers applied successfully for {GatewayType} gateway - CorrelationId: {CorrelationId}, Path: {Path}",
                _options.IsAdminGateway ? "ADMIN" : "PUBLIC", correlationId, context.Request.Path);
        }
        catch (Exception ex)
        {
            // CRITICAL: Security headers failure should be logged but not block the response
            _logger.LogError(ex, "SECURITY_HEADERS_FAILURE: Failed to apply security headers for {GatewayType} gateway - CorrelationId: {CorrelationId}, Path: {Path}",
                _options.IsAdminGateway ? "ADMIN" : "PUBLIC", correlationId, context.Request.Path);
        }
    }
    
    private static void SetHeaderWithFallback(HttpResponse response, string headerName, string headerValue)
    {
        try
        {
            // Only set header if it doesn't already exist (allow override)
            if (!response.Headers.ContainsKey(headerName))
            {
                response.Headers.Append(headerName, headerValue);
            }
        }
        catch (InvalidOperationException)
        {
            // Headers already sent - this is expected in some scenarios
        }
    }
}

/// <summary>
/// Configuration options for security headers middleware
/// </summary>
public class SecurityHeadersOptions
{
    /// <summary>
    /// Whether this is an admin gateway (affects CSP and other policies)
    /// </summary>
    public bool IsAdminGateway { get; set; } = false;
    
    /// <summary>
    /// Whether to always add HSTS headers (even for non-HTTPS in development)
    /// </summary>
    public bool AlwaysAddHSTS { get; set; } = false;
    
    /// <summary>
    /// Additional custom security headers
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}

/// <summary>
/// Extension methods for registering security headers middleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds comprehensive security headers middleware for Public Gateway
    /// </summary>
    public static IApplicationBuilder UsePublicGatewaySecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>(new SecurityHeadersOptions
        {
            IsAdminGateway = false,
            AlwaysAddHSTS = false
        });
    }
    
    /// <summary>
    /// Adds comprehensive security headers middleware for Admin Gateway with medical-grade compliance
    /// </summary>
    public static IApplicationBuilder UseAdminGatewaySecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>(new SecurityHeadersOptions
        {
            IsAdminGateway = true,
            AlwaysAddHSTS = true // Medical-grade compliance requires HSTS
        });
    }
    
    /// <summary>
    /// Adds security headers middleware with custom options
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, SecurityHeadersOptions options)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>(options);
    }
}
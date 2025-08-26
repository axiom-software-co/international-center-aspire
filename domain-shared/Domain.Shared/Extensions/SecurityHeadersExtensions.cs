using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shared.Extensions;

/// <summary>
/// Enhanced security headers extensions for medical-grade compliance
/// Provides comprehensive security header management with fallback policies and modern security standards
/// </summary>
public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Applies comprehensive security headers for Public Gateway with modern standards
    /// Includes fallback policies if no other headers are specified
    /// </summary>
    public static void ApplyPublicGatewaySecurityHeaders(this HttpResponse response, HttpContext context, IConfiguration configuration)
    {
        try
        {
            // Core Security Headers (MANDATORY - fallback policies apply)
            ApplyHeaderWithFallback(response, "X-Content-Type-Options", "nosniff");
            ApplyHeaderWithFallback(response, "X-Frame-Options", "DENY");
            ApplyHeaderWithFallback(response, "X-XSS-Protection", "1; mode=block");
            ApplyHeaderWithFallback(response, "Referrer-Policy", "strict-origin-when-cross-origin");
            
            // Enhanced Content Security Policy for Public Gateway
            var publicCSP = configuration.GetValue<string>("Security:PublicGateway:ContentSecurityPolicy") ??
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'; upgrade-insecure-requests;";
            ApplyHeaderWithFallback(response, "Content-Security-Policy", publicCSP);
            
            // HSTS (HTTPS only with fallback configuration)
            if (context.Request.IsHttps)
            {
                var hstsValue = configuration.GetValue<string>("Security:HSTS:MaxAge") ?? "31536000";
                var includeSubDomains = configuration.GetValue<bool>("Security:HSTS:IncludeSubDomains", true);
                var preload = configuration.GetValue<bool>("Security:HSTS:Preload", false);
                
                var hstsHeader = $"max-age={hstsValue}";
                if (includeSubDomains) hstsHeader += "; includeSubDomains";
                if (preload) hstsHeader += "; preload";
                
                ApplyHeaderWithFallback(response, "Strict-Transport-Security", hstsHeader);
            }
            
            // Modern Cross-Origin Security Headers
            ApplyHeaderWithFallback(response, "Cross-Origin-Opener-Policy", "same-origin");
            ApplyHeaderWithFallback(response, "Cross-Origin-Embedder-Policy", "credentialless");
            ApplyHeaderWithFallback(response, "Cross-Origin-Resource-Policy", "same-site");
            
            // Permissions Policy (restrict dangerous features for public access)
            var permissionsPolicy = configuration.GetValue<string>("Security:PublicGateway:PermissionsPolicy") ??
                "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=(), interest-cohort=()";
            ApplyHeaderWithFallback(response, "Permissions-Policy", permissionsPolicy);
            
            // Gateway identification headers
            ApplyHeaderWithFallback(response, "X-Gateway-Type", "PUBLIC");
            ApplyHeaderWithFallback(response, "X-Security-Level", "STANDARD");
            
            // Remove server information headers (security through obscurity)
            RemoveServerHeaders(response);
        }
        catch (Exception)
        {
            // Fallback: If security header application fails, apply minimal required headers
            ApplyMinimalSecurityHeaders(response, isAdmin: false);
        }
    }
    
    /// <summary>
    /// Applies comprehensive security headers for Admin Gateway with medical-grade compliance
    /// Stricter policies and mandatory compliance headers with comprehensive fallback policies
    /// </summary>
    public static void ApplyAdminGatewaySecurityHeaders(this HttpResponse response, HttpContext context, IConfiguration configuration)
    {
        try
        {
            // Core Security Headers (MANDATORY - medical-grade compliance)
            ApplyHeaderWithFallback(response, "X-Content-Type-Options", "nosniff");
            ApplyHeaderWithFallback(response, "X-Frame-Options", "DENY");
            ApplyHeaderWithFallback(response, "X-XSS-Protection", "1; mode=block");
            ApplyHeaderWithFallback(response, "Referrer-Policy", "strict-origin-when-cross-origin");
            
            // Stricter Content Security Policy for Admin Gateway (medical-grade)
            var adminCSP = configuration.GetValue<string>("Security:AdminGateway:ContentSecurityPolicy") ??
                "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self'; font-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'; upgrade-insecure-requests;";
            ApplyHeaderWithFallback(response, "Content-Security-Policy", adminCSP);
            
            // HSTS (MANDATORY for admin - medical-grade compliance)
            var hstsValue = configuration.GetValue<string>("Security:AdminGateway:HSTS:MaxAge") ?? "31536000";
            var hstsHeader = $"max-age={hstsValue}; includeSubDomains; preload";
            ApplyHeaderWithFallback(response, "Strict-Transport-Security", hstsHeader);
            
            // Enhanced Cross-Origin Security Headers (stricter for admin)
            ApplyHeaderWithFallback(response, "Cross-Origin-Opener-Policy", "same-origin");
            ApplyHeaderWithFallback(response, "Cross-Origin-Embedder-Policy", "require-corp");
            ApplyHeaderWithFallback(response, "Cross-Origin-Resource-Policy", "same-origin");
            
            // Stricter Permissions Policy for admin operations
            var adminPermissionsPolicy = configuration.GetValue<string>("Security:AdminGateway:PermissionsPolicy") ??
                "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=(), interest-cohort=(), browsing-topics=()";
            ApplyHeaderWithFallback(response, "Permissions-Policy", adminPermissionsPolicy);
            
            // Medical-grade compliance headers
            ApplyHeaderWithFallback(response, "X-Gateway-Type", "ADMIN");
            ApplyHeaderWithFallback(response, "X-Security-Level", "MEDICAL_GRADE");
            ApplyHeaderWithFallback(response, "X-Compliance-Level", "MEDICAL_GRADE");
            
            // Remove server information headers (enhanced security for admin)
            RemoveServerHeaders(response);
        }
        catch (Exception)
        {
            // Fallback: If security header application fails, apply minimal required headers
            ApplyMinimalSecurityHeaders(response, isAdmin: true);
        }
    }
    
    /// <summary>
    /// Applies a header with fallback policy - only sets if not already present
    /// Ensures fallback security policies are applied when no explicit policy exists
    /// </summary>
    private static void ApplyHeaderWithFallback(HttpResponse response, string headerName, string headerValue)
    {
        try
        {
            if (!response.Headers.ContainsKey(headerName))
            {
                response.Headers.Append(headerName, headerValue);
            }
        }
        catch (InvalidOperationException)
        {
            // Headers already sent - this is expected and acceptable
        }
    }
    
    /// <summary>
    /// Removes server identification headers for enhanced security
    /// Security through obscurity - prevents information disclosure
    /// </summary>
    private static void RemoveServerHeaders(HttpResponse response)
    {
        try
        {
            response.Headers.Remove("Server");
            response.Headers.Remove("X-Powered-By");
            response.Headers.Remove("X-AspNet-Version");
            response.Headers.Remove("X-AspNetMvc-Version");
            response.Headers.Remove("X-SourceFiles");
        }
        catch (InvalidOperationException)
        {
            // Headers may already be sent or not present - acceptable
        }
    }
    
    /// <summary>
    /// Applies minimal security headers as absolute fallback policy
    /// Ensures basic security even if comprehensive header application fails
    /// </summary>
    private static void ApplyMinimalSecurityHeaders(HttpResponse response, bool isAdmin)
    {
        try
        {
            // Absolute minimum security headers (fallback policy)
            ApplyHeaderWithFallback(response, "X-Content-Type-Options", "nosniff");
            ApplyHeaderWithFallback(response, "X-Frame-Options", "DENY");
            ApplyHeaderWithFallback(response, "X-XSS-Protection", "1; mode=block");
            
            if (isAdmin)
            {
                ApplyHeaderWithFallback(response, "Strict-Transport-Security", "max-age=31536000; includeSubDomains");
                ApplyHeaderWithFallback(response, "X-Compliance-Level", "MEDICAL_GRADE");
            }
        }
        catch
        {
            // Even minimal fallback failed - log this as critical security issue
            // But don't throw - allow response to continue
        }
    }
    
    /// <summary>
    /// Validates that required security headers are present
    /// Used for security compliance verification
    /// </summary>
    public static bool ValidateSecurityHeaders(this HttpResponse response, bool isAdmin = false)
    {
        var requiredHeaders = new[]
        {
            "X-Content-Type-Options",
            "X-Frame-Options", 
            "X-XSS-Protection",
            "Referrer-Policy"
        };
        
        var adminRequiredHeaders = new[]
        {
            "Strict-Transport-Security",
            "X-Compliance-Level"
        };
        
        var allHeaders = isAdmin 
            ? requiredHeaders.Concat(adminRequiredHeaders).ToArray()
            : requiredHeaders;
            
        return allHeaders.All(header => response.Headers.ContainsKey(header));
    }
}
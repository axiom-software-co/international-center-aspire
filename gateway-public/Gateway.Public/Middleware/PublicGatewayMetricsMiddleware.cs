using Gateway.Public.Services;
using System.Diagnostics;

namespace Gateway.Public.Middleware;

public class PublicGatewayMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PublicGatewayMetricsService _metricsService;
    private readonly ILogger<PublicGatewayMetricsMiddleware> _logger;

    public PublicGatewayMetricsMiddleware(
        RequestDelegate next,
        PublicGatewayMetricsService metricsService,
        ILogger<PublicGatewayMetricsMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip metrics collection for certain paths to reduce noise
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (ShouldSkipMetrics(path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var clientIp = GetClientIpAddress(context);
        var method = context.Request.Method;
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault() ?? "";
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        // Record the incoming request
        _metricsService.RecordRequest(method, path, clientIp, userAgent);

        // Set up response metrics collection
        var originalStatusCode = 0;
        Exception? capturedException = null;

        try
        {
            await _next(context);
            originalStatusCode = context.Response.StatusCode;
        }
        catch (Exception ex)
        {
            capturedException = ex;
            originalStatusCode = 500; // Assume server error for unhandled exceptions
            
            // Record the exception as a proxy error if this was a proxy request
            if (IsProxyRequest(path))
            {
                _metricsService.RecordProxyLatency(GetTargetServiceFromPath(path), stopwatch.Elapsed.TotalSeconds, false);
            }
            
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var durationSeconds = stopwatch.Elapsed.TotalSeconds;

            // Record the response metrics
            _metricsService.RecordResponse(method, path, originalStatusCode, durationSeconds, clientIp);

            // Record proxy metrics for backend requests
            if (IsProxyRequest(path))
            {
                var targetService = GetTargetServiceFromPath(path);
                var success = originalStatusCode >= 200 && originalStatusCode < 400;
                
                _metricsService.RecordProxyRequest(targetService, method, path);
                _metricsService.RecordProxyLatency(targetService, durationSeconds, success);
            }

            // Record rate limiting metrics if the request was rate limited
            if (originalStatusCode == StatusCodes.Status429TooManyRequests)
            {
                _metricsService.RecordRateLimitCheck(clientIp, false, 0.001); // Assume minimal decision time for rejected requests
            }

            // Record security violations based on response codes and headers
            RecordSecurityMetricsFromResponse(context, originalStatusCode, clientIp, path, userAgent);

            _logger.LogDebug("Metrics recorded for request {CorrelationId}: {Method} {Path} -> {StatusCode} in {Duration}ms",
                correlationId, method, path, originalStatusCode, durationSeconds * 1000);
        }
    }

    private void RecordSecurityMetricsFromResponse(HttpContext context, int statusCode, string clientIp, string path, string userAgent)
    {
        // Record security violations based on status codes
        switch (statusCode)
        {
            case StatusCodes.Status403Forbidden:
                _metricsService.RecordSecurityViolation("ip_blocked", clientIp);
                _metricsService.RecordBlockedRequest("ip_blocked", clientIp, path);
                break;
                
            case StatusCodes.Status400BadRequest:
                // Check if this was a suspicious user agent or SQL injection attempt
                if (ContainsSuspiciousPatterns(userAgent))
                {
                    _metricsService.RecordSecurityViolation("suspicious_user_agent", clientIp);
                    _metricsService.RecordBlockedRequest("suspicious_user_agent", clientIp, path);
                }
                else
                {
                    _metricsService.RecordSecurityViolation("malformed_request", clientIp);
                    _metricsService.RecordBlockedRequest("malformed_request", clientIp, path);
                }
                break;
                
            case StatusCodes.Status413PayloadTooLarge:
                _metricsService.RecordSecurityViolation("request_size_exceeded", clientIp);
                _metricsService.RecordBlockedRequest("request_size_exceeded", clientIp, path);
                break;
                
            case StatusCodes.Status426UpgradeRequired:
                _metricsService.RecordSecurityViolation("insecure_connection", clientIp);
                _metricsService.RecordBlockedRequest("insecure_connection", clientIp, path);
                break;
                
            case StatusCodes.Status429TooManyRequests:
                // Rate limiting violations are handled separately
                break;
        }

        // Record suspicious activity patterns
        if (ContainsSuspiciousPatterns(userAgent))
        {
            _metricsService.RecordSuspiciousActivity("suspicious_user_agent", clientIp);
        }

        // Check for suspicious request patterns
        if (ContainsSecurityThreats(context.Request))
        {
            _metricsService.RecordSuspiciousActivity("security_threat_patterns", clientIp);
        }
    }

    private static bool ShouldSkipMetrics(string path)
    {
        // Skip metrics collection for these paths to reduce noise
        var skipPaths = new[]
        {
            "/favicon.ico",
            "/robots.txt", 
            "/sitemap.xml",
            "/.well-known/",
            "/metrics" // Don't collect metrics on the metrics endpoint itself
        };

        return skipPaths.Any(skipPath => path.Contains(skipPath));
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ??
               context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
               context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
               "unknown";
    }

    private static bool IsProxyRequest(string path)
    {
        // Determine if this request was proxied to a backend service
        // Based on the YARP configuration paths
        return path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/services/", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetTargetServiceFromPath(string path)
    {
        // Extract the target service from the request path
        // This should match your YARP routing configuration
        
        if (path.StartsWith("/api/services/", StringComparison.OrdinalIgnoreCase))
        {
            return "services-public-api";
        }
        
        if (path.StartsWith("/api/events/", StringComparison.OrdinalIgnoreCase))
        {
            return "events-public-api";
        }
        
        if (path.StartsWith("/api/news/", StringComparison.OrdinalIgnoreCase))
        {
            return "news-public-api";
        }
        
        if (path.StartsWith("/api/newsletter/", StringComparison.OrdinalIgnoreCase))
        {
            return "newsletter-public-api";
        }
        
        if (path.StartsWith("/api/research/", StringComparison.OrdinalIgnoreCase))
        {
            return "research-public-api";
        }
        
        if (path.StartsWith("/api/search/", StringComparison.OrdinalIgnoreCase))
        {
            return "search-public-api";
        }
        
        if (path.StartsWith("/api/contacts/", StringComparison.OrdinalIgnoreCase))
        {
            return "contacts-public-api";
        }

        // Default fallback
        return "unknown-service";
    }

    private static bool ContainsSuspiciousPatterns(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return false;
        
        var suspiciousPatterns = new[] { "sqlmap", "nikto", "nmap", "masscan", "zap", "burp", "havij", "pangolin" };
        var lowerAgent = userAgent.ToLowerInvariant();
        
        return suspiciousPatterns.Any(pattern => lowerAgent.Contains(pattern));
    }

    private static bool ContainsSecurityThreats(HttpRequest request)
    {
        // Check request headers for potential security threats
        var allHeaderValues = request.Headers.SelectMany(h => h.Value).ToList();
        var threatPatterns = new[] 
        { 
            "union select", "drop table", "exec(", "script>", "<iframe", 
            "javascript:", "vbscript:", "onload=", "onerror=",
            "../", "..\\", "%2e%2e", "etc/passwd", "boot.ini"
        };
        
        foreach (var headerValue in allHeaderValues)
        {
            var lowerValue = headerValue?.ToLowerInvariant() ?? "";
            if (threatPatterns.Any(pattern => lowerValue.Contains(pattern)))
            {
                return true;
            }
        }

        // Check query parameters for threats
        foreach (var queryParam in request.Query)
        {
            var lowerValue = queryParam.Value.ToString().ToLowerInvariant();
            if (threatPatterns.Any(pattern => lowerValue.Contains(pattern)))
            {
                return true;
            }
        }

        return false;
    }
}

public static class PublicGatewayMetricsMiddlewareExtensions
{
    public static IApplicationBuilder UsePublicGatewayMetrics(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PublicGatewayMetricsMiddleware>();
    }
}
using Gateway.Admin.Services;
using System.Diagnostics;
using System.Security.Claims;

namespace Gateway.Admin.Middleware;

public class AdminGatewayMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AdminGatewayMetricsService _metricsService;
    private readonly ILogger<AdminGatewayMetricsMiddleware> _logger;

    public AdminGatewayMetricsMiddleware(
        RequestDelegate next,
        AdminGatewayMetricsService metricsService,
        ILogger<AdminGatewayMetricsMiddleware> logger)
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
        var method = context.Request.Method;
        var clientIp = GetClientIpAddress(context);
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        // Extract user context for medical-grade compliance tracking
        var userId = GetUserId(context);
        var userRoles = GetUserRoles(context);
        var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;

        // Record the incoming admin request
        if (isAuthenticated)
        {
            _metricsService.RecordAdminRequest(method, path, userId, userRoles, clientIp);
            _metricsService.RecordUserActivity(userId, $"{method.ToUpper()}_REQUEST", path);
        }

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
            
            // Record the exception as a security/compliance issue
            if (isAuthenticated)
            {
                _metricsService.RecordSecurityViolation("unhandled_exception", userId, clientIp, ex.GetType().Name);
                _metricsService.RecordComplianceViolation("system_error", userId, $"Unhandled exception: {ex.GetType().Name}", "high");
            }
            
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var durationSeconds = stopwatch.Elapsed.TotalSeconds;

            // Record the response metrics
            if (isAuthenticated)
            {
                _metricsService.RecordAdminResponse(method, path, originalStatusCode, durationSeconds, userId);
            }

            // Record authentication/authorization specific metrics based on response codes
            RecordAuthZMetricsFromResponse(context, originalStatusCode, durationSeconds, userId, userRoles, clientIp, path);

            // Record security violations based on response codes
            RecordSecurityMetricsFromResponse(context, originalStatusCode, userId, userRoles, clientIp, path);

            // Record medical compliance events
            RecordComplianceMetricsFromResponse(context, originalStatusCode, userId, userRoles, clientIp, path, durationSeconds);

            _logger.LogDebug("Admin Gateway metrics recorded for request {CorrelationId}: {Method} {Path} -> {StatusCode} by {UserId} in {Duration}ms",
                correlationId, method, path, originalStatusCode, userId, durationSeconds * 1000);
        }
    }

    private void RecordAuthZMetricsFromResponse(HttpContext context, int statusCode, double durationSeconds, 
        string userId, string[] userRoles, string clientIp, string path)
    {
        // Record authentication metrics
        switch (statusCode)
        {
            case StatusCodes.Status401Unauthorized:
                // Authentication failed
                _metricsService.RecordAuthenticationResult("jwt", userId, false, durationSeconds, "unauthorized");
                _metricsService.RecordSecurityViolation("authentication_failure", userId, clientIp, "401 Unauthorized response");
                break;
                
            case StatusCodes.Status403Forbidden:
                // Authorization failed - user was authenticated but not authorized
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    _metricsService.RecordAuthenticationResult("jwt", userId, true, 0.001); // Auth succeeded
                    _metricsService.RecordAuthorizationCheck("default_policy", userId, userRoles, false, durationSeconds, path);
                    _metricsService.RecordComplianceViolation("authorization_failure", userId, $"Access denied to {path}", "medium");
                }
                else
                {
                    // Not authenticated at all
                    _metricsService.RecordAuthenticationResult("jwt", userId, false, durationSeconds, "forbidden");
                }
                break;
                
            case var code when code >= 200 && code < 300:
                // Success - record both auth and authz success if user is authenticated
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    _metricsService.RecordAuthenticationResult("jwt", userId, true, 0.001); // Minimal time for successful auth
                    _metricsService.RecordAuthorizationCheck("default_policy", userId, userRoles, true, durationSeconds, path);
                    
                    // Record token validation success
                    if (context.Request.Headers.Authorization.Any())
                    {
                        _metricsService.RecordTokenValidation("jwt", true, 0.001);
                    }
                }
                break;
        }
    }

    private void RecordSecurityMetricsFromResponse(HttpContext context, int statusCode, 
        string userId, string[] userRoles, string clientIp, string path)
    {
        // Record security violations based on status codes
        switch (statusCode)
        {
            case StatusCodes.Status400BadRequest:
                _metricsService.RecordSecurityViolation("bad_request", userId, clientIp, "Malformed admin request");
                break;
                
            case StatusCodes.Status403Forbidden:
                _metricsService.RecordSecurityViolation("access_denied", userId, clientIp, $"Access denied to {path}");
                break;
                
            case StatusCodes.Status413PayloadTooLarge:
                _metricsService.RecordSecurityViolation("payload_too_large", userId, clientIp, "Admin request exceeded size limit");
                break;
                
            case StatusCodes.Status426UpgradeRequired:
                _metricsService.RecordSecurityViolation("insecure_connection", userId, clientIp, "HTTPS required for admin operations");
                break;
                
            case StatusCodes.Status429TooManyRequests:
                _metricsService.RecordSecurityViolation("rate_limit_exceeded", userId, clientIp, "Admin rate limit exceeded");
                _metricsService.RecordComplianceViolation("rate_limit_violation", userId, "Medical-grade rate limits exceeded", "high");
                break;
                
            case var code when code >= 500:
                _metricsService.RecordSecurityViolation("server_error", userId, clientIp, $"Server error {code}");
                _metricsService.RecordComplianceViolation("system_error", userId, $"Server error {code}", "high");
                break;
        }
    }

    private void RecordComplianceMetricsFromResponse(HttpContext context, int statusCode,
        string userId, string[] userRoles, string clientIp, string path, double durationSeconds)
    {
        var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;

        // Record medical compliance events based on various criteria
        if (isAuthenticated)
        {
            // Check for potential compliance violations based on request patterns
            if (path.Contains("delete", StringComparison.OrdinalIgnoreCase) || 
                path.Contains("remove", StringComparison.OrdinalIgnoreCase))
            {
                _metricsService.RecordComplianceViolation("data_deletion_attempt", userId, 
                    $"Attempt to delete data via {path}", "high");
            }

            if (path.Contains("audit", StringComparison.OrdinalIgnoreCase) && 
                context.Request.Method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                _metricsService.RecordComplianceViolation("audit_tampering_attempt", userId,
                    $"Attempt to delete audit data via {path}", "critical");
            }

            // Record successful admin operations for compliance tracking
            if (statusCode >= 200 && statusCode < 300)
            {
                if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                    context.Request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
                    context.Request.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
                {
                    _metricsService.RecordUserActivity(userId, "DATA_MODIFICATION", path);
                }
            }

            // Check for long-running operations that might indicate issues
            if (durationSeconds > 10.0) // Operations taking longer than 10 seconds
            {
                _metricsService.RecordComplianceViolation("long_running_operation", userId,
                    $"Operation took {durationSeconds:F2} seconds", "medium");
            }

            // Record role-based access patterns for compliance monitoring
            if (userRoles.Any())
            {
                foreach (var role in userRoles)
                {
                    _metricsService.RecordRbacPolicyEvaluation($"role_{role}", userId, userRoles, 
                        statusCode < 400, durationSeconds);
                }
            }
        }
        else
        {
            // Unauthenticated access to admin endpoints is a compliance violation
            _metricsService.RecordComplianceViolation("unauthenticated_admin_access", "anonymous",
                $"Unauthenticated access attempt to {path}", "critical");
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

    private static string GetUserId(HttpContext context)
    {
        return context.User?.Identity?.Name ??
               context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               context.User?.FindFirst("sub")?.Value ??
               context.User?.FindFirst("user_id")?.Value ??
               context.Request.Headers["X-User-ID"].FirstOrDefault() ??
               "anonymous";
    }

    private static string[] GetUserRoles(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
            return Array.Empty<string>();

        var roles = new List<string>();

        // Try different claim types for roles
        var roleClaims = context.User.FindAll(ClaimTypes.Role)
            .Concat(context.User.FindAll("roles"))
            .Concat(context.User.FindAll("role"))
            .Select(c => c.Value)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct()
            .ToArray();

        return roleClaims;
    }
}

public static class AdminGatewayMetricsMiddlewareExtensions
{
    public static IApplicationBuilder UseAdminGatewayMetrics(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AdminGatewayMetricsMiddleware>();
    }
}
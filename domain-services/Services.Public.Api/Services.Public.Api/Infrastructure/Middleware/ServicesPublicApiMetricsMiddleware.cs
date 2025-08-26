using Services.Public.Api.Infrastructure.Services;
using System.Diagnostics;

namespace Services.Public.Api.Infrastructure.Middleware;

public class ServicesPublicApiMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ServicesPublicApiMetricsService _metricsService;
    private readonly ILogger<ServicesPublicApiMetricsMiddleware> _logger;

    public ServicesPublicApiMetricsMiddleware(
        RequestDelegate next,
        ServicesPublicApiMetricsService metricsService,
        ILogger<ServicesPublicApiMetricsMiddleware> logger)
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
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        // All requests to Services Public API are anonymous through Public Gateway
        var isAnonymousRequest = true;
        var originalStatusCode = 0;

        try
        {
            await _next(context);
            originalStatusCode = context.Response.StatusCode;
        }
        catch (Exception ex)
        {
            originalStatusCode = 500; // Assume server error for unhandled exceptions
            _logger.LogError(ex, "Unhandled exception in Services Public API: {Path} [{CorrelationId}]", path, correlationId);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var durationSeconds = stopwatch.Elapsed.TotalSeconds;

            // Record anonymous request metrics
            if (isAnonymousRequest)
            {
                _metricsService.RecordAnonymousRequest(path, durationSeconds, originalStatusCode);
            }

            // Record specific endpoint metrics based on the path
            RecordEndpointSpecificMetrics(context, path, method, durationSeconds, originalStatusCode, correlationId);

            _logger.LogDebug("Services Public API metrics recorded: {Method} {Path} -> {StatusCode} in {Duration}ms [{CorrelationId}]",
                method, path, originalStatusCode, durationSeconds * 1000, correlationId);
        }
    }

    private void RecordEndpointSpecificMetrics(HttpContext context, string path, string method, 
        double durationSeconds, int statusCode, string correlationId)
    {
        var success = statusCode >= 200 && statusCode < 400;
        
        // Extract result count from response headers if available
        var resultCountHeader = context.Response.Headers["X-Result-Count"].FirstOrDefault();
        var resultCount = int.TryParse(resultCountHeader, out var count) ? count : 0;

        try
        {
            // Services endpoints
            if (path.Contains("/api/services") && !path.Contains("/categories"))
            {
                if (path.Contains("/search"))
                {
                    // Service search endpoint
                    var searchTerm = context.Request.Query["q"].FirstOrDefault() ?? "";
                    _metricsService.RecordServiceSearch(searchTerm, durationSeconds, resultCount);
                }
                else if (IsServiceBySlugRequest(path))
                {
                    // Individual service by slug
                    var slug = ExtractSlugFromPath(path);
                    var found = statusCode != 404;
                    _metricsService.RecordServiceBySlugRequest(slug, durationSeconds, found);
                }
                else
                {
                    // General services listing
                    _metricsService.RecordServiceRequest("list", "", durationSeconds, success, resultCount);
                }
            }
            // Categories endpoints
            else if (path.Contains("/api/categories") || path.Contains("/categories"))
            {
                _metricsService.RecordCategoryRequest("list", durationSeconds, success, resultCount);
            }
            // Version endpoint
            else if (path.Contains("/api/version"))
            {
                // Version requests don't need special metrics beyond the anonymous request tracking
            }

            // Record cache metrics if cache headers are present
            RecordCacheMetrics(context, durationSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record endpoint-specific metrics for {Path} [{CorrelationId}]", path, correlationId);
        }
    }

    private void RecordCacheMetrics(HttpContext context, double durationSeconds)
    {
        // Check for cache hit/miss indicators in response headers
        var cacheStatus = context.Response.Headers["X-Cache-Status"].FirstOrDefault();
        
        if (!string.IsNullOrEmpty(cacheStatus))
        {
            var isHit = cacheStatus.Equals("HIT", StringComparison.OrdinalIgnoreCase);
            var operation = DetermineCacheOperation(context.Request.Path.Value ?? "");
            
            _metricsService.RecordCacheOperation(operation, isHit, durationSeconds);
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
            "/metrics", // Don't collect metrics on the metrics endpoint itself
            "/health",  // Health check endpoints
            "/openapi", // OpenAPI documentation
            "/swagger"  // Swagger UI
        };

        return skipPaths.Any(skipPath => path.Contains(skipPath));
    }

    private static bool IsServiceBySlugRequest(string path)
    {
        // Check if this is a request for a specific service by slug
        // Pattern: /api/services/{slug} where slug is not a known operation
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (segments.Length >= 3 && segments[0] == "api" && segments[1] == "services")
        {
            var lastSegment = segments[2];
            
            // Known operations that are not slugs
            var operations = new[] { "search", "categories", "health", "version" };
            return !operations.Contains(lastSegment.ToLowerInvariant());
        }
        
        return false;
    }

    private static string ExtractSlugFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (segments.Length >= 3 && segments[0] == "api" && segments[1] == "services")
        {
            return segments[2];
        }
        
        return "unknown";
    }

    private static string DetermineCacheOperation(string path)
    {
        if (path.Contains("/services") && !path.Contains("/categories"))
        {
            return IsServiceBySlugRequest(path) ? "service_by_slug" : "services_list";
        }
        
        if (path.Contains("/categories"))
        {
            return "categories_list";
        }
        
        return "other";
    }
}

public static class ServicesPublicApiMetricsMiddlewareExtensions
{
    public static IApplicationBuilder UseServicesPublicApiMetrics(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ServicesPublicApiMetricsMiddleware>();
    }
}
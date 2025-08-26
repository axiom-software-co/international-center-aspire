using Microsoft.AspNetCore.Builder;
using System.Net;
using System.Text;

namespace Infrastructure.Metrics.Middleware;

public sealed class MetricsEndpointMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IPrometheusMetricsExporter _metricsExporter;
    private readonly IMetricsEndpointSecurity _security;
    private readonly ILogger<MetricsEndpointMiddleware> _logger;
    private readonly MetricsOptions _options;

    public MetricsEndpointMiddleware(
        RequestDelegate next,
        IPrometheusMetricsExporter metricsExporter,
        IMetricsEndpointSecurity security,
        ILogger<MetricsEndpointMiddleware> logger,
        IOptions<MetricsOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _metricsExporter = metricsExporter ?? throw new ArgumentNullException(nameof(metricsExporter));
        _security = security ?? throw new ArgumentNullException(nameof(security));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsMetricsRequest(context.Request))
        {
            await _next(context);
            return;
        }

        if (!_options.Enabled)
        {
            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            await context.Response.WriteAsync("Metrics endpoint is disabled");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var clientIp = GetClientIpAddress(context);

        try
        {
            // Security validation
            var isAuthorized = await _security.IsAuthorizedAsync(context, context.RequestAborted);
            
            if (!isAuthorized)
            {
                await HandleUnauthorizedRequest(context, clientIp);
                return;
            }

            // Rate limiting check
            if (_security.ShouldRateLimitRequest(clientIp, context.Request.Path))
            {
                await HandleRateLimitedRequest(context, clientIp);
                return;
            }

            // Generate metrics
            var metricsContent = await _metricsExporter.GetMetricsAsync(context.RequestAborted);

            // Set response headers
            ConfigureResponseHeaders(context.Response);
            
            // Enable compression if requested and configured
            if (ShouldCompressResponse(context) && _options.EnableGzip)
            {
                await WriteCompressedResponse(context, metricsContent);
            }
            else
            {
                await WriteResponse(context, metricsContent);
            }

            stopwatch.Stop();

            // Record successful access attempt
            await _security.RecordAccessAttemptAsync(context.Request, true, context.RequestAborted);

            _logger.LogDebug("Served metrics to {ClientIp} in {Duration}ms, {Size} bytes",
                clientIp, stopwatch.Elapsed.TotalMilliseconds, metricsContent.Length);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Metrics request cancelled by client {ClientIp}", clientIp);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            await HandleErrorResponse(context, ex, clientIp, stopwatch.Elapsed);
        }
    }

    private bool IsMetricsRequest(HttpRequest request)
    {
        return string.Equals(request.Path, _options.MetricsPath, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(request.Method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check X-Forwarded-For header first (for reverse proxy scenarios)
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            var forwardedIps = xForwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (forwardedIps.Length > 0)
            {
                return forwardedIps[0].Trim();
            }
        }

        // Check X-Real-IP header
        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private async Task HandleUnauthorizedRequest(HttpContext context, string clientIp)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        
        await _security.LogSecurityEventAsync(SecurityEventType.UnauthorizedAccess, clientIp, 
            context.Request.Headers.UserAgent, "Unauthorized access to metrics endpoint");
        
        await _security.RecordAccessAttemptAsync(context.Request, false, context.RequestAborted);
        
        _logger.LogWarning("Unauthorized metrics access attempt from {ClientIp}", clientIp);
        
        await context.Response.WriteAsync("Forbidden");
    }

    private async Task HandleRateLimitedRequest(HttpContext context, string clientIp)
    {
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.Headers.Add("Retry-After", "60");
        
        await _security.LogSecurityEventAsync(SecurityEventType.RateLimitExceeded, clientIp,
            context.Request.Headers.UserAgent, "Rate limit exceeded for metrics endpoint");
        
        await _security.RecordAccessAttemptAsync(context.Request, false, context.RequestAborted);
        
        _logger.LogWarning("Rate limit exceeded for metrics access from {ClientIp}", clientIp);
        
        await context.Response.WriteAsync("Too Many Requests");
    }

    private void ConfigureResponseHeaders(HttpResponse response)
    {
        response.ContentType = "text/plain; version=0.0.4; charset=utf-8";
        
        if (_options.Security.EnableSecurityHeaders)
        {
            _security.GenerateSecurityHeaders(response);
        }

        // Add caching headers
        if (_options.Performance.EnableCaching && _options.Performance.CacheDuration > TimeSpan.Zero)
        {
            var maxAge = (int)_options.Performance.CacheDuration.TotalSeconds;
            response.Headers.Add("Cache-Control", $"public, max-age={maxAge}");
            response.Headers.Add("ETag", GenerateETag());
        }
        else
        {
            response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            response.Headers.Add("Pragma", "no-cache");
            response.Headers.Add("Expires", "0");
        }
    }

    private static bool ShouldCompressResponse(HttpContext context)
    {
        var acceptEncoding = context.Request.Headers.AcceptEncoding.ToString();
        return acceptEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase);
    }

    private async Task WriteCompressedResponse(HttpContext context, string content)
    {
        var contentBytes = Encoding.UTF8.GetBytes(content);
        
        if (contentBytes.Length > 1024) // Only compress if content is larger than 1KB
        {
            using var compressionStream = new System.IO.Compression.GZipStream(context.Response.Body, System.IO.Compression.CompressionLevel.Fastest);
            context.Response.Headers.Add("Content-Encoding", "gzip");
            await compressionStream.WriteAsync(contentBytes, context.RequestAborted);
        }
        else
        {
            await WriteResponse(context, content);
        }
    }

    private async Task WriteResponse(HttpContext context, string content)
    {
        // Check max response size
        if (content.Length > _options.Performance.MaxResponseSize)
        {
            _logger.LogWarning("Metrics response size ({Size}) exceeds maximum ({Max}), truncating",
                content.Length, _options.Performance.MaxResponseSize);
            
            content = content[..(_options.Performance.MaxResponseSize - 100)] + "\n# [Response truncated due to size limit]";
        }

        await context.Response.WriteAsync(content, context.RequestAborted);
    }

    private async Task HandleErrorResponse(HttpContext context, Exception ex, string clientIp, TimeSpan duration)
    {
        _logger.LogError(ex, "Error serving metrics to {ClientIp} after {Duration}ms", 
            clientIp, duration.TotalMilliseconds);

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "text/plain";
            
            var errorMessage = _options.Environment == "Development" 
                ? $"Internal Server Error: {ex.Message}" 
                : "Internal Server Error";
                
            await context.Response.WriteAsync(errorMessage);
        }

        await _security.LogSecurityEventAsync(SecurityEventType.SystemEvent, clientIp,
            context.Request.Headers.UserAgent, $"Error serving metrics: {ex.Message}");
        
        await _security.RecordAccessAttemptAsync(context.Request, false, context.RequestAborted);
    }

    private string GenerateETag()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var hash = timestamp.GetHashCode();
        return $"\"{hash:x}\"";
    }
}

public static class MetricsEndpointMiddlewareExtensions
{
    public static IApplicationBuilder UseMetricsEndpoint(this IApplicationBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.UseMiddleware<MetricsEndpointMiddleware>();
    }

    public static IApplicationBuilder UseMetricsEndpoint(this IApplicationBuilder builder, string path)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        return builder.Map(path, app => app.UseMiddleware<MetricsEndpointMiddleware>());
    }
}
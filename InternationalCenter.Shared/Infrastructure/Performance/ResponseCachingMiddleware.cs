using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace InternationalCenter.Shared.Infrastructure.Performance;

public sealed class ResponseCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseCachingMiddleware> _logger;
    private readonly ResponseCachingOptions _options;

    public ResponseCachingMiddleware(
        RequestDelegate next,
        ILogger<ResponseCachingMiddleware> logger,
        ResponseCachingOptions? options = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? ResponseCachingOptions.Default;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip caching for non-GET requests
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var cachePolicy = GetCachePolicy(context);
        if (cachePolicy == null)
        {
            await _next(context);
            return;
        }

        // Check if client has valid cached version
        if (HasValidClientCache(context, cachePolicy))
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            SetCacheHeaders(context, cachePolicy);
            _logger.LogDebug("Returning 304 Not Modified for {Path}", context.Request.Path);
            return;
        }

        var originalBodyStream = context.Response.Body;

        try
        {
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status200OK && memoryStream.Length > 0)
            {
                SetCacheHeaders(context, cachePolicy);
                SetETagHeader(context, memoryStream.ToArray());
                SetLastModifiedHeader(context);
            }

            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private CachePolicy? GetCachePolicy(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (string.IsNullOrEmpty(path))
            return null;

        // gRPC services caching
        if (path.Contains("/services."))
        {
            if (path.Contains("getservices") || path.Contains("searchservices"))
                return _options.ShortTermCache;
            
            if (path.Contains("getservicecategories") || path.Contains("getfeaturedservices"))
                return _options.MediumTermCache;
            
            if (path.Contains("getservicebyslug"))
                return _options.LongTermCache;
        }

        // Static content caching
        if (IsStaticContent(path))
            return _options.StaticContentCache;

        return null;
    }

    private static bool IsStaticContent(string path)
    {
        var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".woff", ".woff2", ".ttf" };
        return staticExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasValidClientCache(HttpContext context, CachePolicy policy)
    {
        // Check If-None-Match (ETag)
        var ifNoneMatch = context.Request.Headers.IfNoneMatch.FirstOrDefault();
        if (!string.IsNullOrEmpty(ifNoneMatch))
        {
            // We'll validate this after generating response ETag
            return false;
        }

        // Check If-Modified-Since
        var ifModifiedSince = context.Request.Headers.IfModifiedSince.FirstOrDefault();
        if (!string.IsNullOrEmpty(ifModifiedSince) && 
            DateTime.TryParse(ifModifiedSince, out var clientDate))
        {
            var lastModified = DateTime.UtcNow.AddSeconds(-policy.MaxAgeSeconds);
            return clientDate >= lastModified;
        }

        return false;
    }

    private static void SetCacheHeaders(HttpContext context, CachePolicy policy)
    {
        var response = context.Response;

        // Cache-Control header
        var cacheControl = new List<string>();

        if (policy.IsPublic)
            cacheControl.Add("public");
        else
            cacheControl.Add("private");

        if (policy.MaxAgeSeconds > 0)
            cacheControl.Add($"max-age={policy.MaxAgeSeconds}");

        if (policy.StaleWhileRevalidateSeconds > 0)
            cacheControl.Add($"stale-while-revalidate={policy.StaleWhileRevalidateSeconds}");

        if (policy.MustRevalidate)
            cacheControl.Add("must-revalidate");

        if (policy.NoStore)
            cacheControl.Add("no-store");

        if (policy.NoCache)
            cacheControl.Add("no-cache");

        response.Headers.CacheControl = string.Join(", ", cacheControl);

        // Expires header
        if (policy.MaxAgeSeconds > 0)
        {
            response.Headers.Expires = DateTime.UtcNow.AddSeconds(policy.MaxAgeSeconds).ToString("R");
        }

        // Vary header
        if (policy.VaryHeaders.Count > 0)
        {
            response.Headers.Vary = string.Join(", ", policy.VaryHeaders);
        }
    }

    private static void SetETagHeader(HttpContext context, byte[] content)
    {
        var hash = ComputeETag(content);
        context.Response.Headers.ETag = $"\"{hash}\"";
    }

    private static void SetLastModifiedHeader(HttpContext context)
    {
        // Set to current time rounded down to the nearest minute for better caching
        var lastModified = DateTime.UtcNow.AddSeconds(-DateTime.UtcNow.Second).AddMilliseconds(-DateTime.UtcNow.Millisecond);
        context.Response.Headers.LastModified = lastModified.ToString("R");
    }

    private static string ComputeETag(byte[] content)
    {
        var hash = SHA256.HashData(content);
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}

public sealed class CachePolicy
{
    public int MaxAgeSeconds { get; set; }
    public int StaleWhileRevalidateSeconds { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool MustRevalidate { get; set; }
    public bool NoStore { get; set; }
    public bool NoCache { get; set; }
    public HashSet<string> VaryHeaders { get; set; } = new();
}

public sealed class ResponseCachingOptions
{
    public CachePolicy ShortTermCache { get; set; } = new()
    {
        MaxAgeSeconds = 300, // 5 minutes
        StaleWhileRevalidateSeconds = 60,
        IsPublic = true,
        VaryHeaders = { "Accept-Encoding", "Accept" }
    };

    public CachePolicy MediumTermCache { get; set; } = new()
    {
        MaxAgeSeconds = 1800, // 30 minutes
        StaleWhileRevalidateSeconds = 300,
        IsPublic = true,
        VaryHeaders = { "Accept-Encoding", "Accept" }
    };

    public CachePolicy LongTermCache { get; set; } = new()
    {
        MaxAgeSeconds = 3600, // 1 hour
        StaleWhileRevalidateSeconds = 600,
        IsPublic = true,
        VaryHeaders = { "Accept-Encoding", "Accept" }
    };

    public CachePolicy StaticContentCache { get; set; } = new()
    {
        MaxAgeSeconds = 86400, // 24 hours
        IsPublic = true,
        VaryHeaders = { "Accept-Encoding" }
    };

    public static ResponseCachingOptions Default => new();
}
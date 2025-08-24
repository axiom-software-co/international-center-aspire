using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace InternationalCenter.Shared.Infrastructure.Performance;

public sealed class ResponseCompressionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseCompressionMiddleware> _logger;
    private readonly ResponseCompressionOptions _options;

    public ResponseCompressionMiddleware(
        RequestDelegate next,
        ILogger<ResponseCompressionMiddleware> logger,
        ResponseCompressionOptions? options = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? ResponseCompressionOptions.Default;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldCompress(context))
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        var acceptEncoding = context.Request.Headers.AcceptEncoding.ToString();
        
        try
        {
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await _next(context);

            if (context.Response.HasStarted || memoryStream.Length == 0)
            {
                await CopyToOriginalStream(memoryStream, originalBodyStream);
                return;
            }

            var compressionType = GetCompressionType(acceptEncoding);
            if (compressionType == CompressionType.None || memoryStream.Length < _options.MinimumSizeBytes)
            {
                await CopyToOriginalStream(memoryStream, originalBodyStream);
                return;
            }

            var compressedData = await CompressAsync(memoryStream.ToArray(), compressionType);
            var compressionRatio = (double)compressedData.Length / memoryStream.Length;

            // Only use compression if it actually reduces size significantly
            if (compressionRatio > _options.MinimumCompressionRatio)
            {
                await CopyToOriginalStream(memoryStream, originalBodyStream);
                return;
            }

            context.Response.Headers.ContentEncoding = GetContentEncoding(compressionType);
            context.Response.Headers.ContentLength = compressedData.Length;
            context.Response.Headers.Vary = "Accept-Encoding";

            context.Response.Body = originalBodyStream;
            await context.Response.Body.WriteAsync(compressedData);

            _logger.LogDebug("Response compressed: {OriginalSize} -> {CompressedSize} bytes ({Ratio:P2})",
                memoryStream.Length, compressedData.Length, compressionRatio);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private bool ShouldCompress(HttpContext context)
    {
        if (context.Request.Headers.AcceptEncoding.Count == 0)
            return false;

        if (context.Response.Headers.ContainsKey("Content-Encoding"))
            return false;

        var contentType = context.Response.ContentType;
        if (string.IsNullOrEmpty(contentType))
            return false;

        return _options.CompressibleMediaTypes.Any(type => 
            contentType.StartsWith(type, StringComparison.OrdinalIgnoreCase));
    }

    private static CompressionType GetCompressionType(string acceptEncoding)
    {
        if (acceptEncoding.Contains("br", StringComparison.OrdinalIgnoreCase))
            return CompressionType.Brotli;
        
        if (acceptEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase))
            return CompressionType.Gzip;
        
        if (acceptEncoding.Contains("deflate", StringComparison.OrdinalIgnoreCase))
            return CompressionType.Deflate;

        return CompressionType.None;
    }

    private static string GetContentEncoding(CompressionType type)
    {
        return type switch
        {
            CompressionType.Brotli => "br",
            CompressionType.Gzip => "gzip",
            CompressionType.Deflate => "deflate",
            _ => throw new ArgumentException($"Unsupported compression type: {type}")
        };
    }

    private static async Task<byte[]> CompressAsync(byte[] data, CompressionType type)
    {
        using var output = new MemoryStream();
        
        Stream compressionStream = type switch
        {
            CompressionType.Brotli => new BrotliStream(output, CompressionLevel.Optimal),
            CompressionType.Gzip => new GZipStream(output, CompressionLevel.Optimal),
            CompressionType.Deflate => new DeflateStream(output, CompressionLevel.Optimal),
            _ => throw new ArgumentException($"Unsupported compression type: {type}")
        };

        using (compressionStream)
        {
            await compressionStream.WriteAsync(data);
        }

        return output.ToArray();
    }

    private static async Task CopyToOriginalStream(MemoryStream source, Stream destination)
    {
        source.Position = 0;
        await source.CopyToAsync(destination);
    }
}

public enum CompressionType
{
    None,
    Gzip,
    Deflate,
    Brotli
}

public sealed class ResponseCompressionOptions
{
    public int MinimumSizeBytes { get; set; } = 1024; // 1KB minimum
    public double MinimumCompressionRatio { get; set; } = 0.9; // Only compress if reduces size by at least 10%
    public HashSet<string> CompressibleMediaTypes { get; set; } = new()
    {
        "text/plain",
        "text/html",
        "text/css",
        "text/javascript",
        "application/javascript",
        "application/json",
        "application/xml",
        "text/xml"
    };

    public static ResponseCompressionOptions Default => new();
}
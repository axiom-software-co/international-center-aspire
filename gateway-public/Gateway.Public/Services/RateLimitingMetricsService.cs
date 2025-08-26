using System.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;

namespace Gateway.Public.Services;

public class RateLimitingMetricsService
{
    private readonly PublicGatewayMetricsService _metricsService;
    private readonly ILogger<RateLimitingMetricsService> _logger;
    
    public RateLimitingMetricsService(
        PublicGatewayMetricsService metricsService,
        ILogger<RateLimitingMetricsService> logger)
    {
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<RateLimitingResult> CheckRateLimitAsync(
        HttpContext context,
        Func<Task<RateLimitingResult>> rateLimitCheck)
    {
        var stopwatch = Stopwatch.StartNew();
        var clientIp = GetClientIpAddress(context);
        RateLimitingResult result;
        
        try
        {
            result = await rateLimitCheck();
            stopwatch.Stop();
            
            var allowed = result.IsAllowed;
            _metricsService.RecordRateLimitCheck(clientIp, allowed, stopwatch.Elapsed.TotalSeconds);
            
            if (!allowed)
            {
                _logger.LogDebug("Rate limit exceeded for client {ClientIp}", clientIp);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metricsService.RecordRateLimitCheck(clientIp, false, stopwatch.Elapsed.TotalSeconds);
            _logger.LogError(ex, "Rate limit check failed for client {ClientIp}", clientIp);
            throw;
        }
    }
    
    public void RecordRateLimitConfigurationMetrics()
    {
        // Record static configuration metrics
        _logger.LogInformation("Rate limiting configuration: IP-based, 1000 requests per minute");
    }
    
    private static string GetClientIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ??
               context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
               context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
               "unknown";
    }
}

// Custom rate limiting result for better metrics integration
public class RateLimitingResult
{
    public bool IsAllowed { get; set; }
    public TimeSpan? RetryAfter { get; set; }
    public string? Reason { get; set; }
    public int RemainingRequests { get; set; }
    
    public static RateLimitingResult Allowed(int remainingRequests = -1)
    {
        return new RateLimitingResult 
        { 
            IsAllowed = true, 
            RemainingRequests = remainingRequests 
        };
    }
    
    public static RateLimitingResult Rejected(TimeSpan? retryAfter = null, string? reason = null)
    {
        return new RateLimitingResult 
        { 
            IsAllowed = false, 
            RetryAfter = retryAfter,
            Reason = reason,
            RemainingRequests = 0
        };
    }
}
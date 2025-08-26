using Infrastructure.Metrics.Abstractions;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Gateway.Public.Services;

public sealed class PublicGatewayMetricsService : IDisposable
{
    private readonly ICustomMetricsRegistry _metricsRegistry;
    private readonly IPrometheusMetricsExporter _prometheusExporter;
    private readonly ILogger<PublicGatewayMetricsService> _logger;
    
    private readonly Meter _meter;
    
    // Traffic pattern metrics
    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _requestDuration;
    private readonly Counter<long> _responseCounter;
    private readonly Gauge<int> _concurrentRequests;
    
    // Rate limiting metrics
    private readonly Counter<long> _rateLimitCounter;
    private readonly Counter<long> _rateLimitViolations;
    private readonly Histogram<double> _rateLimitDecisionDuration;
    
    // Redis cache performance metrics
    private readonly Counter<long> _redisCacheOperations;
    private readonly Histogram<double> _redisCacheLatency;
    private readonly Counter<long> _redisCacheErrors;
    private readonly Gauge<double> _redisConnectionPoolUtilization;
    
    // Security metrics
    private readonly Counter<long> _securityViolations;
    private readonly Counter<long> _blockedRequests;
    private readonly Counter<long> _suspiciousActivityCounter;
    
    // Gateway-specific metrics
    private readonly Counter<long> _proxyRequestsCounter;
    private readonly Histogram<double> _proxyLatency;
    private readonly Counter<long> _proxyErrors;
    
    private int _currentConcurrentRequests = 0;
    
    public PublicGatewayMetricsService(
        ICustomMetricsRegistry metricsRegistry,
        IPrometheusMetricsExporter prometheusExporter,
        ILogger<PublicGatewayMetricsService> logger)
    {
        _metricsRegistry = metricsRegistry ?? throw new ArgumentNullException(nameof(metricsRegistry));
        _prometheusExporter = prometheusExporter ?? throw new ArgumentNullException(nameof(prometheusExporter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _meter = _metricsRegistry.CreateMeter("Gateway.Public", "1.0.0");
        
        // Initialize traffic pattern instruments
        _requestCounter = _meter.CreateCounter<long>(
            "gateway_public_requests_total",
            "count",
            "Total number of requests received by the Public Gateway");
            
        _requestDuration = _meter.CreateHistogram<double>(
            "gateway_public_request_duration_seconds",
            "seconds",
            "Duration of requests processed by the Public Gateway");
            
        _responseCounter = _meter.CreateCounter<long>(
            "gateway_public_responses_total", 
            "count",
            "Total number of responses sent by the Public Gateway");
            
        _concurrentRequests = _meter.CreateGauge<int>(
            "gateway_public_concurrent_requests",
            "count",
            "Current number of concurrent requests being processed");
            
        // Initialize rate limiting instruments
        _rateLimitCounter = _meter.CreateCounter<long>(
            "gateway_public_rate_limit_checks_total",
            "count", 
            "Total number of rate limit checks performed");
            
        _rateLimitViolations = _meter.CreateCounter<long>(
            "gateway_public_rate_limit_violations_total",
            "count",
            "Total number of rate limit violations");
            
        _rateLimitDecisionDuration = _meter.CreateHistogram<double>(
            "gateway_public_rate_limit_decision_duration_seconds",
            "seconds",
            "Time taken to make rate limiting decisions");
            
        // Initialize Redis cache performance instruments
        _redisCacheOperations = _meter.CreateCounter<long>(
            "gateway_public_redis_operations_total",
            "count",
            "Total number of Redis operations performed");
            
        _redisCacheLatency = _meter.CreateHistogram<double>(
            "gateway_public_redis_operation_duration_seconds", 
            "seconds",
            "Duration of Redis operations");
            
        _redisCacheErrors = _meter.CreateCounter<long>(
            "gateway_public_redis_errors_total",
            "count", 
            "Total number of Redis operation errors");
            
        _redisConnectionPoolUtilization = _meter.CreateGauge<double>(
            "gateway_public_redis_connection_pool_utilization_ratio",
            "ratio",
            "Redis connection pool utilization ratio");
            
        // Initialize security instruments
        _securityViolations = _meter.CreateCounter<long>(
            "gateway_public_security_violations_total",
            "count",
            "Total number of security policy violations");
            
        _blockedRequests = _meter.CreateCounter<long>(
            "gateway_public_blocked_requests_total", 
            "count",
            "Total number of blocked requests");
            
        _suspiciousActivityCounter = _meter.CreateCounter<long>(
            "gateway_public_suspicious_activity_total",
            "count",
            "Total number of suspicious activities detected");
            
        // Initialize proxy instruments
        _proxyRequestsCounter = _meter.CreateCounter<long>(
            "gateway_public_proxy_requests_total",
            "count", 
            "Total number of requests proxied to backend services");
            
        _proxyLatency = _meter.CreateHistogram<double>(
            "gateway_public_proxy_latency_seconds",
            "seconds",
            "Latency of proxied requests to backend services");
            
        _proxyErrors = _meter.CreateCounter<long>(
            "gateway_public_proxy_errors_total",
            "count",
            "Total number of proxy errors");
            
        _logger.LogInformation("PublicGatewayMetricsService initialized with meter: {MeterName}", _meter.Name);
    }
    
    public void RecordRequest(string method, string path, string clientIp, string userAgent = "")
    {
        var tags = new TagList
        {
            ["method"] = method,
            ["path"] = SanitizePath(path),
            ["gateway"] = "public",
            ["user_agent_category"] = CategorizeUserAgent(userAgent)
        };
        
        _requestCounter.Add(1, tags);
        
        Interlocked.Increment(ref _currentConcurrentRequests);
        _concurrentRequests.Record(_currentConcurrentRequests, tags);
        
        _logger.LogDebug("Recorded request: {Method} {Path} from {ClientIp}", method, path, clientIp);
    }
    
    public void RecordResponse(string method, string path, int statusCode, double durationSeconds, string clientIp)
    {
        var tags = new TagList
        {
            ["method"] = method,
            ["path"] = SanitizePath(path),
            ["status_code"] = statusCode.ToString(),
            ["status_class"] = GetStatusClass(statusCode),
            ["gateway"] = "public"
        };
        
        _responseCounter.Add(1, tags);
        _requestDuration.Record(durationSeconds, tags);
        
        Interlocked.Decrement(ref _currentConcurrentRequests);
        _concurrentRequests.Record(_currentConcurrentRequests, tags);
        
        _logger.LogDebug("Recorded response: {StatusCode} for {Method} {Path} in {Duration}ms", 
            statusCode, method, path, durationSeconds * 1000);
    }
    
    public void RecordRateLimitCheck(string clientIp, bool allowed, double decisionTimeSeconds, string limitType = "ip")
    {
        var tags = new TagList
        {
            ["limit_type"] = limitType,
            ["result"] = allowed ? "allowed" : "rejected",
            ["gateway"] = "public"
        };
        
        _rateLimitCounter.Add(1, tags);
        _rateLimitDecisionDuration.Record(decisionTimeSeconds, tags);
        
        if (!allowed)
        {
            _rateLimitViolations.Add(1, tags);
            _logger.LogDebug("Rate limit violation recorded for {ClientIp}", clientIp);
        }
    }
    
    public void RecordRedisOperation(string operation, bool success, double latencySeconds)
    {
        var tags = new TagList
        {
            ["operation"] = operation,
            ["success"] = success.ToString().ToLowerInvariant(),
            ["gateway"] = "public"
        };
        
        _redisCacheOperations.Add(1, tags);
        _redisCacheLatency.Record(latencySeconds, tags);
        
        if (!success)
        {
            _redisCacheErrors.Add(1, tags);
            _logger.LogDebug("Redis error recorded for operation: {Operation}", operation);
        }
    }
    
    public void RecordRedisConnectionPoolUtilization(double utilizationRatio)
    {
        var tags = new TagList { ["gateway"] = "public" };
        _redisConnectionPoolUtilization.Record(utilizationRatio, tags);
    }
    
    public void RecordSecurityViolation(string violationType, string clientIp, string details = "")
    {
        var tags = new TagList
        {
            ["violation_type"] = violationType.ToLowerInvariant().Replace(" ", "_"),
            ["gateway"] = "public"
        };
        
        _securityViolations.Add(1, tags);
        _logger.LogDebug("Security violation recorded: {ViolationType} from {ClientIp}", violationType, clientIp);
    }
    
    public void RecordBlockedRequest(string reason, string clientIp, string path = "")
    {
        var tags = new TagList
        {
            ["block_reason"] = reason.ToLowerInvariant().Replace(" ", "_"),
            ["path"] = SanitizePath(path),
            ["gateway"] = "public"
        };
        
        _blockedRequests.Add(1, tags);
        _logger.LogDebug("Blocked request recorded: {Reason} from {ClientIp} for {Path}", reason, clientIp, path);
    }
    
    public void RecordSuspiciousActivity(string activityType, string clientIp, string details = "")
    {
        var tags = new TagList
        {
            ["activity_type"] = activityType.ToLowerInvariant().Replace(" ", "_"),
            ["gateway"] = "public"
        };
        
        _suspiciousActivityCounter.Add(1, tags);
        _logger.LogDebug("Suspicious activity recorded: {ActivityType} from {ClientIp}", activityType, clientIp);
    }
    
    public void RecordProxyRequest(string targetService, string method, string path)
    {
        var tags = new TagList
        {
            ["target_service"] = targetService,
            ["method"] = method,
            ["path"] = SanitizePath(path),
            ["gateway"] = "public"
        };
        
        _proxyRequestsCounter.Add(1, tags);
        _logger.LogDebug("Proxy request recorded to {TargetService}: {Method} {Path}", targetService, method, path);
    }
    
    public void RecordProxyLatency(string targetService, double latencySeconds, bool success)
    {
        var tags = new TagList
        {
            ["target_service"] = targetService,
            ["success"] = success.ToString().ToLowerInvariant(),
            ["gateway"] = "public"
        };
        
        _proxyLatency.Record(latencySeconds, tags);
        
        if (!success)
        {
            _proxyErrors.Add(1, tags);
            _logger.LogDebug("Proxy error recorded for {TargetService}", targetService);
        }
    }
    
    public async Task<string> ExportMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _prometheusExporter.GetMetricsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Public Gateway metrics");
            throw;
        }
    }
    
    private static string SanitizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "/";
        
        // Replace dynamic segments with placeholders for better cardinality control
        var sanitized = path.ToLowerInvariant();
        
        // Common patterns to normalize
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"/api/v\d+", "/api/v*");
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"/\d+(/|$)", "/{id}$1");
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"/[a-f0-9-]{36}(/|$)", "/{guid}$1");
        
        // Limit path length for cardinality
        if (sanitized.Length > 100)
        {
            sanitized = sanitized[..97] + "...";
        }
        
        return sanitized;
    }
    
    private static string GetStatusClass(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "2xx",
            >= 300 and < 400 => "3xx", 
            >= 400 and < 500 => "4xx",
            >= 500 => "5xx",
            _ => "1xx"
        };
    }
    
    private static string CategorizeUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "unknown";
        
        var lowerAgent = userAgent.ToLowerInvariant();
        
        return lowerAgent switch
        {
            var ua when ua.Contains("chrome") => "chrome",
            var ua when ua.Contains("firefox") => "firefox",
            var ua when ua.Contains("safari") && !ua.Contains("chrome") => "safari",
            var ua when ua.Contains("edge") => "edge",
            var ua when ua.Contains("bot") || ua.Contains("crawl") || ua.Contains("spider") => "bot",
            var ua when ua.Contains("postman") || ua.Contains("insomnia") || ua.Contains("curl") => "api_client",
            _ => "other"
        };
    }
    
    public void Dispose()
    {
        _meter?.Dispose();
        _logger.LogInformation("PublicGatewayMetricsService disposed");
    }
}
using System.Diagnostics.Metrics;

namespace Shared.Infrastructure.Observability;

public sealed class ServiceMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<int> _requestCount;
    private readonly Histogram<double> _requestDuration;
    private readonly Counter<int> _errorCount;
    private readonly Gauge<int> _activeConnections;
    private readonly Counter<int> _cacheHits;
    private readonly Counter<int> _cacheMisses;
    private readonly Counter<long> _businessOperations;
    
    public ServiceMetrics(string serviceName)
    {
        _meter = new Meter($"InternationalCenter.{serviceName}", "1.0.0");
        
        // HTTP request metrics following OpenTelemetry semantic conventions
        _requestCount = _meter.CreateCounter<int>(
            "http_server_requests_total", 
            "requests", 
            "Total number of HTTP requests processed");
            
        _requestDuration = _meter.CreateHistogram<double>(
            "http_server_request_duration_seconds", 
            "seconds", 
            "Duration of HTTP requests in seconds");
            
        _errorCount = _meter.CreateCounter<int>(
            "application_errors_total", 
            "errors", 
            "Total number of application errors");
            
        // Database connection metrics
        _activeConnections = _meter.CreateGauge<int>(
            "database_connections_active", 
            "connections", 
            "Number of active database connections");
            
        // Cache metrics
        _cacheHits = _meter.CreateCounter<int>(
            "cache_hits_total", 
            "hits", 
            "Total number of cache hits");
            
        _cacheMisses = _meter.CreateCounter<int>(
            "cache_misses_total", 
            "misses", 
            "Total number of cache misses");
            
        // Business operations metric (configurable per service)
        _businessOperations = _meter.CreateCounter<long>(
            "business_operations_total",
            "operations",
            "Total number of business operations processed");
    }
    
    public void RecordRequest(string method, string endpoint, double duration, int statusCode)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("http.method", method),
            new("http.route", endpoint),
            new("http.status_code", statusCode),
            new("service.name", _meter.Name)
        };
        
        _requestCount.Add(1, tags);
        _requestDuration.Record(duration, tags);
        
        if (statusCode >= 400)
        {
            _errorCount.Add(1, tags);
        }
    }
    
    public void RecordDatabaseConnection(int activeConnections)
    {
        _activeConnections.Record(activeConnections, 
            new KeyValuePair<string, object?>("database.type", "postgresql"));
    }
    
    public void RecordCacheOperation(bool hit, string operation)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("cache.operation", operation),
            new("cache.type", "redis")
        };
        
        if (hit)
            _cacheHits.Add(1, tags);
        else
            _cacheMisses.Add(1, tags);
    }
    
    public void RecordBusinessOperation(string operation, long count = 1, Dictionary<string, object?>? additionalTags = null)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new("operation.type", operation),
            new("service.name", _meter.Name)
        };
        
        if (additionalTags != null)
        {
            tags.AddRange(additionalTags.Select(kv => new KeyValuePair<string, object?>(kv.Key, kv.Value)));
        }
        
        _businessOperations.Add(count, tags.ToArray());
    }
    
    public void IncrementCounter(string counterName, Dictionary<string, object?>? tags = null)
    {
        var tagArray = tags?.Select(kv => new KeyValuePair<string, object?>(kv.Key, kv.Value)).ToArray() 
                       ?? Array.Empty<KeyValuePair<string, object?>>();
        
        switch (counterName.ToLowerInvariant())
        {
            case "errors":
                _errorCount.Add(1, tagArray);
                break;
            case "requests":
                _requestCount.Add(1, tagArray);
                break;
            case "cache_hits":
                _cacheHits.Add(1, tagArray);
                break;
            case "cache_misses":
                _cacheMisses.Add(1, tagArray);
                break;
            default:
                // For unknown counters, use business operations counter
                _businessOperations.Add(1, tagArray);
                break;
        }
    }
    
    public void RecordValue(string metricName, double value, Dictionary<string, object?>? tags = null)
    {
        var tagArray = tags?.Select(kv => new KeyValuePair<string, object?>(kv.Key, kv.Value)).ToArray() 
                       ?? Array.Empty<KeyValuePair<string, object?>>();
        
        switch (metricName.ToLowerInvariant())
        {
            case "duration":
            case "request_duration":
                _requestDuration.Record(value, tagArray);
                break;
            case "connections":
            case "active_connections":
                _activeConnections.Record((int)value, tagArray);
                break;
            default:
                // For unknown metrics, record as request duration
                _requestDuration.Record(value, tagArray);
                break;
        }
    }
    
    public void Dispose() => _meter.Dispose();
}
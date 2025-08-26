using Infrastructure.Metrics.Abstractions;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Services.Public.Api.Infrastructure.Services;

public sealed class ServicesPublicApiMetricsService : IDisposable
{
    private readonly ICustomMetricsRegistry _metricsRegistry;
    private readonly IPrometheusMetricsExporter _prometheusExporter;
    private readonly ILogger<ServicesPublicApiMetricsService> _logger;
    
    private readonly Meter _meter;
    
    // Services catalog access metrics
    private readonly Counter<long> _servicesRequestsCounter;
    private readonly Counter<long> _categoriesRequestsCounter;
    private readonly Counter<long> _serviceBySlugRequestsCounter;
    private readonly Histogram<double> _serviceRequestDuration;
    private readonly Histogram<double> _categoryRequestDuration;
    
    // Dapper query performance metrics
    private readonly Counter<long> _dapperQueryCounter;
    private readonly Histogram<double> _dapperQueryDuration;
    private readonly Counter<long> _dapperConnectionCounter;
    private readonly Histogram<double> _dapperConnectionDuration;
    private readonly Gauge<int> _dapperActiveConnections;
    private readonly Counter<long> _dapperQueryErrorsCounter;
    
    // Service catalog business metrics
    private readonly Counter<long> _serviceViewsCounter;
    private readonly Counter<long> _categoryViewsCounter;
    private readonly Histogram<double> _serviceSearchDuration;
    private readonly Counter<long> _serviceNotFoundCounter;
    private readonly Gauge<long> _totalServicesCount;
    private readonly Gauge<long> _totalCategoriesCount;
    
    // Public access patterns
    private readonly Counter<long> _anonymousRequestsCounter;
    private readonly Histogram<double> _anonymousRequestDuration;
    private readonly Counter<long> _cacheHitsCounter;
    private readonly Counter<long> _cacheMissesCounter;
    private readonly Histogram<double> _cacheOperationDuration;
    
    // Performance distribution metrics
    private readonly Histogram<double> _responseTimesHistogram;
    private readonly Counter<long> _slowQueryCounter;
    private readonly Counter<long> _fastQueryCounter;
    private readonly Gauge<double> _averageResponseTime;
    
    private int _currentActiveConnections = 0;
    private long _totalServicesCache = 0;
    private long _totalCategoriesCache = 0;
    private double _runningAverageResponseTime = 0;
    private long _responseTimeCount = 0;
    
    public ServicesPublicApiMetricsService(
        ICustomMetricsRegistry metricsRegistry,
        IPrometheusMetricsExporter prometheusExporter,
        ILogger<ServicesPublicApiMetricsService> logger)
    {
        _metricsRegistry = metricsRegistry ?? throw new ArgumentNullException(nameof(metricsRegistry));
        _prometheusExporter = prometheusExporter ?? throw new ArgumentNullException(nameof(prometheusExporter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _meter = _metricsRegistry.CreateMeter("Services.Public.Api", "1.0.0");
        
        // Initialize service catalog access instruments
        _servicesRequestsCounter = _meter.CreateCounter<long>(
            "services_public_api_service_requests_total",
            "count",
            "Total number of service requests to the Services Public API");
            
        _categoriesRequestsCounter = _meter.CreateCounter<long>(
            "services_public_api_category_requests_total",
            "count",
            "Total number of category requests to the Services Public API");
            
        _serviceBySlugRequestsCounter = _meter.CreateCounter<long>(
            "services_public_api_service_by_slug_requests_total",
            "count",
            "Total number of service-by-slug requests to the Services Public API");
            
        _serviceRequestDuration = _meter.CreateHistogram<double>(
            "services_public_api_service_request_duration_seconds",
            "seconds",
            "Duration of service requests");
            
        _categoryRequestDuration = _meter.CreateHistogram<double>(
            "services_public_api_category_request_duration_seconds",
            "seconds",
            "Duration of category requests");
            
        // Initialize Dapper performance instruments
        _dapperQueryCounter = _meter.CreateCounter<long>(
            "services_public_api_dapper_queries_total",
            "count",
            "Total number of Dapper queries executed");
            
        _dapperQueryDuration = _meter.CreateHistogram<double>(
            "services_public_api_dapper_query_duration_seconds",
            "seconds",
            "Duration of Dapper queries");
            
        _dapperConnectionCounter = _meter.CreateCounter<long>(
            "services_public_api_dapper_connections_total",
            "count",
            "Total number of Dapper database connections opened");
            
        _dapperConnectionDuration = _meter.CreateHistogram<double>(
            "services_public_api_dapper_connection_duration_seconds",
            "seconds",
            "Duration of Dapper connection operations");
            
        _dapperActiveConnections = _meter.CreateGauge<int>(
            "services_public_api_dapper_active_connections",
            "count",
            "Current number of active Dapper database connections");
            
        _dapperQueryErrorsCounter = _meter.CreateCounter<long>(
            "services_public_api_dapper_query_errors_total",
            "count",
            "Total number of Dapper query errors");
            
        // Initialize business metrics instruments
        _serviceViewsCounter = _meter.CreateCounter<long>(
            "services_public_api_service_views_total",
            "count",
            "Total number of individual service views");
            
        _categoryViewsCounter = _meter.CreateCounter<long>(
            "services_public_api_category_views_total",
            "count",
            "Total number of category views");
            
        _serviceSearchDuration = _meter.CreateHistogram<double>(
            "services_public_api_service_search_duration_seconds",
            "seconds",
            "Duration of service search operations");
            
        _serviceNotFoundCounter = _meter.CreateCounter<long>(
            "services_public_api_service_not_found_total",
            "count",
            "Total number of service not found responses");
            
        _totalServicesCount = _meter.CreateGauge<long>(
            "services_public_api_total_services",
            "count",
            "Total number of services in the catalog");
            
        _totalCategoriesCount = _meter.CreateGauge<long>(
            "services_public_api_total_categories",
            "count",
            "Total number of categories in the catalog");
            
        // Initialize public access pattern instruments
        _anonymousRequestsCounter = _meter.CreateCounter<long>(
            "services_public_api_anonymous_requests_total",
            "count",
            "Total number of anonymous requests");
            
        _anonymousRequestDuration = _meter.CreateHistogram<double>(
            "services_public_api_anonymous_request_duration_seconds",
            "seconds",
            "Duration of anonymous requests");
            
        _cacheHitsCounter = _meter.CreateCounter<long>(
            "services_public_api_cache_hits_total",
            "count",
            "Total number of cache hits");
            
        _cacheMissesCounter = _meter.CreateCounter<long>(
            "services_public_api_cache_misses_total",
            "count",
            "Total number of cache misses");
            
        _cacheOperationDuration = _meter.CreateHistogram<double>(
            "services_public_api_cache_operation_duration_seconds",
            "seconds",
            "Duration of cache operations");
            
        // Initialize performance distribution instruments
        _responseTimesHistogram = _meter.CreateHistogram<double>(
            "services_public_api_response_times_seconds",
            "seconds",
            "Distribution of response times for public API calls");
            
        _slowQueryCounter = _meter.CreateCounter<long>(
            "services_public_api_slow_queries_total",
            "count",
            "Total number of slow queries (>1 second)");
            
        _fastQueryCounter = _meter.CreateCounter<long>(
            "services_public_api_fast_queries_total",
            "count",
            "Total number of fast queries (<0.1 second)");
            
        _averageResponseTime = _meter.CreateGauge<double>(
            "services_public_api_average_response_time_seconds",
            "seconds",
            "Current average response time across all operations");
            
        _logger.LogInformation("ServicesPublicApiMetricsService initialized with meter: {MeterName}", _meter.Name);
    }
    
    public void RecordServiceRequest(string operation, string serviceId, double durationSeconds, bool success, int resultCount = 0)
    {
        var tags = new TagList
        {
            ["operation"] = operation.ToLowerInvariant(),
            ["result"] = success ? "success" : "error",
            ["api"] = "services_public",
            ["access_type"] = "anonymous"
        };
        
        if (resultCount > 0)
        {
            tags["result_count"] = GetResultCountBucket(resultCount);
        }
        
        _servicesRequestsCounter.Add(1, tags);
        _serviceRequestDuration.Record(durationSeconds, tags);
        
        // Track performance buckets
        if (durationSeconds > 1.0)
        {
            _slowQueryCounter.Add(1, tags);
        }
        else if (durationSeconds < 0.1)
        {
            _fastQueryCounter.Add(1, tags);
        }
        
        UpdateAverageResponseTime(durationSeconds);
        
        _logger.LogDebug("Service request recorded: operation={Operation}, serviceId={ServiceId}, success={Success}, duration={Duration}ms",
            operation, serviceId, success, durationSeconds * 1000);
    }
    
    public void RecordCategoryRequest(string operation, double durationSeconds, bool success, int resultCount = 0)
    {
        var tags = new TagList
        {
            ["operation"] = operation.ToLowerInvariant(),
            ["result"] = success ? "success" : "error",
            ["api"] = "services_public",
            ["access_type"] = "anonymous"
        };
        
        if (resultCount > 0)
        {
            tags["result_count"] = GetResultCountBucket(resultCount);
        }
        
        _categoriesRequestsCounter.Add(1, tags);
        _categoryRequestDuration.Record(durationSeconds, tags);
        
        UpdateAverageResponseTime(durationSeconds);
        
        _logger.LogDebug("Category request recorded: operation={Operation}, success={Success}, duration={Duration}ms",
            operation, success, durationSeconds * 1000);
    }
    
    public void RecordServiceBySlugRequest(string slug, double durationSeconds, bool found)
    {
        var tags = new TagList
        {
            ["operation"] = "get_by_slug",
            ["result"] = found ? "found" : "not_found",
            ["api"] = "services_public",
            ["access_type"] = "anonymous"
        };
        
        _serviceBySlugRequestsCounter.Add(1, tags);
        _serviceRequestDuration.Record(durationSeconds, tags);
        
        if (found)
        {
            _serviceViewsCounter.Add(1, tags);
        }
        else
        {
            _serviceNotFoundCounter.Add(1, tags);
        }
        
        UpdateAverageResponseTime(durationSeconds);
        
        _logger.LogDebug("Service by slug request recorded: slug={Slug}, found={Found}, duration={Duration}ms",
            slug, found, durationSeconds * 1000);
    }
    
    public void RecordDapperQuery(string queryType, string operation, double durationSeconds, bool success, int affectedRows = 0)
    {
        var tags = new TagList
        {
            ["query_type"] = queryType.ToLowerInvariant(),
            ["operation"] = operation.ToLowerInvariant(),
            ["result"] = success ? "success" : "error",
            ["api"] = "services_public"
        };
        
        if (affectedRows > 0)
        {
            tags["affected_rows"] = GetRowCountBucket(affectedRows);
        }
        
        _dapperQueryCounter.Add(1, tags);
        _dapperQueryDuration.Record(durationSeconds, tags);
        
        if (!success)
        {
            _dapperQueryErrorsCounter.Add(1, tags);
        }
        
        _logger.LogDebug("Dapper query recorded: queryType={QueryType}, operation={Operation}, success={Success}, duration={Duration}ms, rows={Rows}",
            queryType, operation, success, durationSeconds * 1000, affectedRows);
    }
    
    public void RecordDapperConnection(double durationSeconds, bool success)
    {
        var tags = new TagList
        {
            ["result"] = success ? "success" : "error",
            ["api"] = "services_public"
        };
        
        _dapperConnectionCounter.Add(1, tags);
        _dapperConnectionDuration.Record(durationSeconds, tags);
        
        if (success)
        {
            Interlocked.Increment(ref _currentActiveConnections);
        }
        
        _dapperActiveConnections.Record(_currentActiveConnections, tags);
        
        _logger.LogDebug("Dapper connection recorded: success={Success}, duration={Duration}ms, active={ActiveConnections}",
            success, durationSeconds * 1000, _currentActiveConnections);
    }
    
    public void RecordDapperConnectionClosed()
    {
        Interlocked.Decrement(ref _currentActiveConnections);
        var tags = new TagList { ["api"] = "services_public" };
        _dapperActiveConnections.Record(_currentActiveConnections, tags);
    }
    
    public void RecordCacheOperation(string operation, bool hit, double durationSeconds)
    {
        var tags = new TagList
        {
            ["operation"] = operation.ToLowerInvariant(),
            ["api"] = "services_public",
            ["cache_type"] = "redis"
        };
        
        if (hit)
        {
            _cacheHitsCounter.Add(1, tags);
        }
        else
        {
            _cacheMissesCounter.Add(1, tags);
        }
        
        _cacheOperationDuration.Record(durationSeconds, tags);
        
        _logger.LogDebug("Cache operation recorded: operation={Operation}, hit={Hit}, duration={Duration}ms",
            operation, hit, durationSeconds * 1000);
    }
    
    public void RecordAnonymousRequest(string endpoint, double durationSeconds, int statusCode)
    {
        var tags = new TagList
        {
            ["endpoint"] = SanitizeEndpoint(endpoint),
            ["status_code"] = statusCode.ToString(),
            ["status_class"] = GetStatusClass(statusCode),
            ["api"] = "services_public",
            ["access_type"] = "anonymous"
        };
        
        _anonymousRequestsCounter.Add(1, tags);
        _anonymousRequestDuration.Record(durationSeconds, tags);
        _responseTimesHistogram.Record(durationSeconds, tags);
        
        UpdateAverageResponseTime(durationSeconds);
        
        _logger.LogDebug("Anonymous request recorded: endpoint={Endpoint}, statusCode={StatusCode}, duration={Duration}ms",
            endpoint, statusCode, durationSeconds * 1000);
    }
    
    public void RecordServiceSearch(string searchTerm, double durationSeconds, int resultCount)
    {
        var tags = new TagList
        {
            ["operation"] = "search",
            ["result_count"] = GetResultCountBucket(resultCount),
            ["api"] = "services_public",
            ["access_type"] = "anonymous"
        };
        
        _serviceSearchDuration.Record(durationSeconds, tags);
        
        _logger.LogDebug("Service search recorded: term={SearchTerm}, results={ResultCount}, duration={Duration}ms",
            searchTerm?.Length > 0 ? "***" : "empty", resultCount, durationSeconds * 1000);
    }
    
    public void UpdateServiceCatalogCounts(long servicesCount, long categoriesCount)
    {
        Interlocked.Exchange(ref _totalServicesCache, servicesCount);
        Interlocked.Exchange(ref _totalCategoriesCache, categoriesCount);
        
        var tags = new TagList { ["api"] = "services_public" };
        _totalServicesCount.Record(servicesCount, tags);
        _totalCategoriesCount.Record(categoriesCount, tags);
        
        _logger.LogDebug("Service catalog counts updated: services={ServicesCount}, categories={CategoriesCount}",
            servicesCount, categoriesCount);
    }
    
    private void UpdateAverageResponseTime(double durationSeconds)
    {
        var count = Interlocked.Increment(ref _responseTimeCount);
        var newAverage = (_runningAverageResponseTime * (count - 1) + durationSeconds) / count;
        Interlocked.Exchange(ref _runningAverageResponseTime, newAverage);
        
        var tags = new TagList { ["api"] = "services_public" };
        _averageResponseTime.Record(newAverage, tags);
    }
    
    public async Task<string> ExportMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _prometheusExporter.GetMetricsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Services Public API metrics");
            throw;
        }
    }
    
    private static string SanitizeEndpoint(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint)) return "/";
        
        // Replace dynamic segments with placeholders
        var sanitized = endpoint.ToLowerInvariant();
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"/services/[^/]+", "/services/{slug}");
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"/categories/[^/]+", "/categories/{id}");
        
        return sanitized.Length > 100 ? sanitized[..97] + "..." : sanitized;
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
    
    private static string GetResultCountBucket(int count)
    {
        return count switch
        {
            0 => "0",
            1 => "1",
            >= 2 and <= 10 => "2-10",
            >= 11 and <= 50 => "11-50",
            >= 51 and <= 100 => "51-100",
            _ => "100+"
        };
    }
    
    private static string GetRowCountBucket(int rows)
    {
        return rows switch
        {
            0 => "0",
            1 => "1",
            >= 2 and <= 10 => "2-10",
            >= 11 and <= 100 => "11-100",
            >= 101 and <= 1000 => "101-1000",
            _ => "1000+"
        };
    }
    
    public void Dispose()
    {
        _meter?.Dispose();
        _logger.LogInformation("ServicesPublicApiMetricsService disposed");
    }
}
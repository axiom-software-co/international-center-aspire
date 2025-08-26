namespace Infrastructure.Metrics.Configuration;

public sealed class MetricsOptions
{
    public const string SectionName = "Metrics";
    
    public bool Enabled { get; set; } = true;
    public string MetricsPath { get; set; } = "/metrics";
    public string ServiceName { get; set; } = "Unknown";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Development";
    public TimeSpan ExportInterval { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan ExportTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableGzip { get; set; } = true;
    public int MaxConcurrentExports { get; set; } = 10;
    
    // Security settings
    public SecurityOptions Security { get; set; } = new();
    
    // Prometheus settings
    public PrometheusOptions Prometheus { get; set; } = new();
    
    // Service discovery settings  
    public ServiceDiscoveryOptions ServiceDiscovery { get; set; } = new();
    
    // Custom metrics settings
    public CustomMetricsOptions CustomMetrics { get; set; } = new();
    
    // Performance settings
    public PerformanceOptions Performance { get; set; } = new();
}

public sealed class SecurityOptions
{
    public bool EnableSecurity { get; set; } = true;
    public string[]? AllowedIps { get; set; }
    public bool RequireAuthentication { get; set; } = false;
    public string? AuthenticationScheme { get; set; }
    public bool EnableRateLimiting { get; set; } = true;
    public int MaxRequestsPerMinute { get; set; } = 100;
    public bool LogSecurityEvents { get; set; } = true;
    public bool EnableSecurityHeaders { get; set; } = true;
    public TimeSpan IpBlockDuration { get; set; } = TimeSpan.FromMinutes(15);
}

public sealed class PrometheusOptions
{
    public string ExporterType { get; set; } = "AspNetCore";
    public bool EnableOpenMetrics { get; set; } = true;
    public string[]? AdditionalLabels { get; set; }
    public IDictionary<string, string> StaticLabels { get; set; } = new Dictionary<string, string>();
    public bool IncludeTargetInfo { get; set; } = true;
    public string? ExternalUrl { get; set; }
    public TimeSpan ScrapeConfigRefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
}

public sealed class ServiceDiscoveryOptions
{
    public bool EnableAutoDiscovery { get; set; } = true;
    public string DiscoveryMethod { get; set; } = "Aspire";
    public TimeSpan DiscoveryInterval { get; set; } = TimeSpan.FromMinutes(1);
    public string[]? StaticEndpoints { get; set; }
    public IDictionary<string, string> DefaultLabels { get; set; } = new Dictionary<string, string>();
    public bool GeneratePrometheusConfig { get; set; } = true;
    public string PrometheusConfigPath { get; set; } = "/config/prometheus.yml";
}

public sealed class CustomMetricsOptions
{
    public bool EnableCustomMetrics { get; set; } = true;
    public string MeterName { get; set; } = "Infrastructure.Metrics";
    public string MeterVersion { get; set; } = "1.0.0";
    public int MaxCustomMetrics { get; set; } = 1000;
    public bool ValidateMetricNames { get; set; } = true;
    public string[]? MetricPrefixes { get; set; }
    public TimeSpan MetricRetention { get; set; } = TimeSpan.FromHours(1);
}

public sealed class PerformanceOptions
{
    public bool EnablePerformanceMetrics { get; set; } = true;
    public int MetricsBufferSize { get; set; } = 10000;
    public TimeSpan MetricsFlushInterval { get; set; } = TimeSpan.FromSeconds(5);
    public bool EnableCompression { get; set; } = true;
    public int MaxResponseSize { get; set; } = 50 * 1024 * 1024; // 50MB
    public bool EnableCaching { get; set; } = true;
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromSeconds(10);
}
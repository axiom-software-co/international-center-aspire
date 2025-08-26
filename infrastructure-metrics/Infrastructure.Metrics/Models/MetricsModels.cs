namespace Infrastructure.Metrics.Models;

public sealed class MetricsSnapshot
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string ServiceName { get; init; } = string.Empty;
    public string ServiceVersion { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public IDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<MetricData> Metrics { get; init; } = Array.Empty<MetricData>();
    public TimeSpan CollectionDuration { get; init; }
    public long TotalMetrics { get; init; }
    public string Format { get; init; } = "prometheus";
}

public sealed class MetricData
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Help { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public object Value { get; init; } = 0;
    public IDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>();
    public DateTimeOffset Timestamp { get; init; }
}

public sealed class PrometheusExporterStatus
{
    public bool IsHealthy { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset LastExport { get; init; }
    public TimeSpan LastExportDuration { get; init; }
    public long TotalExports { get; init; }
    public long FailedExports { get; init; }
    public double SuccessRate { get; init; }
    public string? LastError { get; init; }
    public IDictionary<string, object> Diagnostics { get; init; } = new Dictionary<string, object>();
}

public sealed class ServiceEndpoint
{
    public string ServiceName { get; init; } = string.Empty;
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public string Scheme { get; init; } = "http";
    public string MetricsPath { get; init; } = "/metrics";
    public TimeSpan ScrapeInterval { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan ScrapeTimeout { get; init; } = TimeSpan.FromSeconds(10);
    public IDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>();
    public IDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
    public bool Enabled { get; init; } = true;
}

public sealed class ServiceDiscoveryConfiguration
{
    public string ConfigurationName { get; init; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
    public string Environment { get; init; } = string.Empty;
    public IDictionary<string, string> GlobalLabels { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<ServiceEndpoint> Services { get; init; } = Array.Empty<ServiceEndpoint>();
    public PrometheusConfiguration PrometheusConfig { get; init; } = new();
}

public sealed class PrometheusConfiguration
{
    public TimeSpan GlobalScrapeInterval { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan GlobalScrapeTimeout { get; init; } = TimeSpan.FromSeconds(10);
    public string GlobalExternalLabels { get; init; } = string.Empty;
    public TimeSpan EvaluationInterval { get; init; } = TimeSpan.FromSeconds(15);
    public IReadOnlyList<string> RuleFiles { get; init; } = Array.Empty<string>();
    public AlertManagerConfiguration AlertManager { get; init; } = new();
}

public sealed class AlertManagerConfiguration
{
    public IReadOnlyList<string> StaticConfigs { get; init; } = Array.Empty<string>();
    public TimeSpan GroupWait { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan GroupInterval { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan RepeatInterval { get; init; } = TimeSpan.FromHours(1);
}
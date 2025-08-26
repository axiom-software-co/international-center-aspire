using OpenTelemetry.Exporter;
using System.Collections.Concurrent;
using System.Text;

namespace Infrastructure.Metrics.Services;

public sealed class PrometheusMetricsExporter : IPrometheusMetricsExporter, IDisposable
{
    private readonly ICustomMetricsRegistry _metricsRegistry;
    private readonly ILogger<PrometheusMetricsExporter> _logger;
    private readonly MetricsOptions _options;
    private readonly ConcurrentDictionary<string, object> _customMetrics = new();
    private readonly SemaphoreSlim _exportSemaphore;
    private readonly Timer? _performanceTimer;

    private long _totalExports;
    private long _failedExports;
    private DateTimeOffset _lastExport = DateTimeOffset.MinValue;
    private TimeSpan _lastExportDuration;
    private string? _lastError;

    public PrometheusMetricsExporter(
        ICustomMetricsRegistry metricsRegistry,
        ILogger<PrometheusMetricsExporter> logger,
        IOptions<MetricsOptions> options)
    {
        _metricsRegistry = metricsRegistry ?? throw new ArgumentNullException(nameof(metricsRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        _exportSemaphore = new SemaphoreSlim(_options.MaxConcurrentExports, _options.MaxConcurrentExports);
        
        if (_options.Performance.EnablePerformanceMetrics)
        {
            _performanceTimer = new Timer(CollectPerformanceMetrics, null, 
                _options.Performance.MetricsFlushInterval, 
                _options.Performance.MetricsFlushInterval);
        }
    }

    public async Task<string> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return string.Empty;
        }

        await _exportSemaphore.WaitAsync(cancellationToken);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var snapshot = await GetMetricsSnapshotAsync(cancellationToken);
            var prometheusFormat = ConvertToPrometheusFormat(snapshot);

            stopwatch.Stop();
            RecordExportSuccess(stopwatch.Elapsed);

            _logger.LogDebug("Exported metrics in {Duration}ms, {Size} bytes", 
                stopwatch.Elapsed.TotalMilliseconds, prometheusFormat.Length);

            return prometheusFormat;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordExportFailure(ex.Message, stopwatch.Elapsed);

            _logger.LogError(ex, "Failed to export metrics after {Duration}ms", 
                stopwatch.Elapsed.TotalMilliseconds);

            throw;
        }
        finally
        {
            _exportSemaphore.Release();
        }
    }

    public async Task<MetricsSnapshot> GetMetricsSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var metrics = new List<MetricData>();

        try
        {
            // Collect built-in metrics
            await CollectBuiltInMetricsAsync(metrics, cancellationToken);

            // Collect custom metrics
            CollectCustomMetrics(metrics);

            // Collect registry metrics
            var registryMetrics = await _metricsRegistry.GetMetricsDefinitionsAsync(cancellationToken);
            CollectRegistryMetrics(metrics, registryMetrics);

            stopwatch.Stop();

            var snapshot = new MetricsSnapshot
            {
                ServiceName = _options.ServiceName,
                ServiceVersion = _options.ServiceVersion,
                Environment = _options.Environment,
                Labels = CreateServiceLabels(),
                Metrics = metrics.AsReadOnly(),
                CollectionDuration = stopwatch.Elapsed,
                TotalMetrics = metrics.Count,
                Format = "prometheus"
            };

            _logger.LogDebug("Created metrics snapshot with {Count} metrics in {Duration}ms", 
                metrics.Count, stopwatch.Elapsed.TotalMilliseconds);

            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create metrics snapshot");
            throw;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await GetStatusAsync(cancellationToken);
            return status.IsHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for metrics exporter");
            return false;
        }
    }

    public async Task<PrometheusExporterStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var totalExports = Interlocked.Read(ref _totalExports);
        var failedExports = Interlocked.Read(ref _failedExports);
        var successRate = totalExports > 0 ? (double)(totalExports - failedExports) / totalExports : 1.0;

        var diagnostics = new Dictionary<string, object>
        {
            ["registered_meters"] = _metricsRegistry.GetRegisteredMeters().Count,
            ["custom_metrics_count"] = _customMetrics.Count,
            ["export_semaphore_available"] = _exportSemaphore.CurrentCount,
            ["export_semaphore_max"] = _options.MaxConcurrentExports,
            ["last_export_size"] = GetLastExportSize(),
            ["memory_usage_mb"] = GC.GetTotalMemory(false) / (1024.0 * 1024.0)
        };

        await Task.CompletedTask;

        return new PrometheusExporterStatus
        {
            IsHealthy = successRate > 0.95 && (DateTimeOffset.UtcNow - _lastExport) < _options.ExportInterval * 2,
            Status = DetermineStatus(successRate),
            LastExport = _lastExport,
            LastExportDuration = _lastExportDuration,
            TotalExports = totalExports,
            FailedExports = failedExports,
            SuccessRate = successRate,
            LastError = _lastError,
            Diagnostics = diagnostics
        };
    }

    public void RecordCustomMetric(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        if (!_options.CustomMetrics.EnableCustomMetrics)
        {
            return;
        }

        ValidateMetricName(name);

        var metricKey = CreateMetricKey(name, tags);
        _customMetrics.AddOrUpdate(metricKey, value, (key, oldValue) => value);

        _logger.LogDebug("Recorded custom metric {Name} = {Value}", name, value);
    }

    public void IncrementCounter(string name, params KeyValuePair<string, object?>[] tags)
    {
        RecordCustomMetric($"{name}_total", 1, tags);
    }

    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        // In a full implementation, this would record histogram buckets
        RecordCustomMetric($"{name}_sum", value, tags);
        RecordCustomMetric($"{name}_count", 1, tags);
    }

    public void SetGauge(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        RecordCustomMetric(name, value, tags);
    }

    private async Task CollectBuiltInMetricsAsync(List<MetricData> metrics, CancellationToken cancellationToken)
    {
        // System metrics
        var gcMemory = GC.GetTotalMemory(false);
        metrics.Add(CreateMetricData("system_gc_memory_bytes", "gauge", gcMemory, "Total GC memory"));

        var gcCollections0 = GC.CollectionCount(0);
        var gcCollections1 = GC.CollectionCount(1);
        var gcCollections2 = GC.CollectionCount(2);

        metrics.Add(CreateMetricData("system_gc_collections_total", "counter", gcCollections0, "GC Gen 0", ("generation", "0")));
        metrics.Add(CreateMetricData("system_gc_collections_total", "counter", gcCollections1, "GC Gen 1", ("generation", "1")));
        metrics.Add(CreateMetricData("system_gc_collections_total", "counter", gcCollections2, "GC Gen 2", ("generation", "2")));

        // Process metrics
        using var process = Process.GetCurrentProcess();
        metrics.Add(CreateMetricData("process_working_set_bytes", "gauge", process.WorkingSet64, "Process working set"));
        metrics.Add(CreateMetricData("process_cpu_seconds_total", "counter", process.TotalProcessorTime.TotalSeconds, "Process CPU time"));

        // Exporter metrics
        metrics.Add(CreateMetricData("prometheus_exports_total", "counter", _totalExports, "Total exports"));
        metrics.Add(CreateMetricData("prometheus_export_failures_total", "counter", _failedExports, "Export failures"));
        metrics.Add(CreateMetricData("prometheus_last_export_duration_seconds", "gauge", _lastExportDuration.TotalSeconds, "Last export duration"));

        await Task.CompletedTask;
    }

    private void CollectCustomMetrics(List<MetricData> metrics)
    {
        foreach (var kvp in _customMetrics)
        {
            var (name, tags) = ParseMetricKey(kvp.Key);
            var labels = tags.ToDictionary(t => t.Key, t => t.Value?.ToString() ?? "");
            
            metrics.Add(new MetricData
            {
                Name = name,
                Type = "gauge", // Simplified - in reality would track type
                Value = kvp.Value,
                Labels = labels,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }

    private void CollectRegistryMetrics(List<MetricData> metrics, IDictionary<string, object> registryMetrics)
    {
        foreach (var kvp in registryMetrics)
        {
            metrics.Add(new MetricData
            {
                Name = $"registry_{kvp.Key}",
                Type = "info",
                Value = 1,
                Labels = new Dictionary<string, string> { ["info"] = kvp.Value.ToString() ?? "" },
                Timestamp = DateTimeOffset.UtcNow,
                Help = $"Registry metric: {kvp.Key}"
            });
        }
    }

    private static MetricData CreateMetricData(string name, string type, object value, string help, params (string key, string value)[] labels)
    {
        return new MetricData
        {
            Name = name,
            Type = type,
            Value = value,
            Help = help,
            Labels = labels.ToDictionary(l => l.key, l => l.value),
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private Dictionary<string, string> CreateServiceLabels()
    {
        var labels = new Dictionary<string, string>
        {
            ["service"] = _options.ServiceName,
            ["version"] = _options.ServiceVersion,
            ["environment"] = _options.Environment
        };

        // Add static labels from configuration
        foreach (var kvp in _options.Prometheus.StaticLabels)
        {
            labels[kvp.Key] = kvp.Value;
        }

        return labels;
    }

    private string ConvertToPrometheusFormat(MetricsSnapshot snapshot)
    {
        var builder = new StringBuilder();

        // Group metrics by name for proper formatting
        var groupedMetrics = snapshot.Metrics.GroupBy(m => m.Name);

        foreach (var group in groupedMetrics)
        {
            var metric = group.First();
            
            // Write HELP
            if (!string.IsNullOrEmpty(metric.Help))
            {
                builder.AppendLine($"# HELP {metric.Name} {metric.Help}");
            }

            // Write TYPE
            builder.AppendLine($"# TYPE {metric.Name} {metric.Type}");

            // Write all samples for this metric
            foreach (var sample in group)
            {
                var labelString = FormatLabels(sample.Labels);
                builder.AppendLine($"{sample.Name}{labelString} {FormatValue(sample.Value)} {((DateTimeOffset)sample.Timestamp).ToUnixTimeMilliseconds()}");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string FormatLabels(IDictionary<string, string> labels)
    {
        if (labels.Count == 0) return "";

        var labelPairs = labels.Select(kvp => $"{kvp.Key}=\"{EscapeLabelValue(kvp.Value)}\"");
        return "{" + string.Join(",", labelPairs) + "}";
    }

    private static string EscapeLabelValue(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }

    private static string FormatValue(object value)
    {
        return value switch
        {
            double d => d.ToString("G17", System.Globalization.CultureInfo.InvariantCulture),
            float f => f.ToString("G9", System.Globalization.CultureInfo.InvariantCulture),
            long l => l.ToString(),
            int i => i.ToString(),
            _ => value.ToString() ?? "0"
        };
    }

    private void ValidateMetricName(string name)
    {
        if (!_options.CustomMetrics.ValidateMetricNames) return;

        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Metric name cannot be null or empty");

        if (!char.IsLetter(name[0]) && name[0] != '_' && name[0] != ':')
            throw new ArgumentException($"Metric name '{name}' must start with a letter, underscore, or colon");

        if (!name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == ':'))
            throw new ArgumentException($"Metric name '{name}' contains invalid characters");
    }

    private string CreateMetricKey(string name, KeyValuePair<string, object?>[] tags)
    {
        if (tags.Length == 0) return name;

        var tagString = string.Join(",", tags.Select(t => $"{t.Key}={t.Value}"));
        return $"{name}[{tagString}]";
    }

    private static (string name, KeyValuePair<string, object?>[] tags) ParseMetricKey(string key)
    {
        var bracketIndex = key.IndexOf('[');
        if (bracketIndex == -1)
        {
            return (key, Array.Empty<KeyValuePair<string, object?>>());
        }

        var name = key[..bracketIndex];
        var tagString = key[(bracketIndex + 1)..^1];
        
        var tags = tagString.Split(',')
            .Select(t => t.Split('='))
            .Where(parts => parts.Length == 2)
            .Select(parts => new KeyValuePair<string, object?>(parts[0], parts[1]))
            .ToArray();

        return (name, tags);
    }

    private void RecordExportSuccess(TimeSpan duration)
    {
        Interlocked.Increment(ref _totalExports);
        _lastExport = DateTimeOffset.UtcNow;
        _lastExportDuration = duration;
        _lastError = null;
    }

    private void RecordExportFailure(string error, TimeSpan duration)
    {
        Interlocked.Increment(ref _totalExports);
        Interlocked.Increment(ref _failedExports);
        _lastExport = DateTimeOffset.UtcNow;
        _lastExportDuration = duration;
        _lastError = error;
    }

    private static string DetermineStatus(double successRate)
    {
        return successRate switch
        {
            >= 0.95 => "Healthy",
            >= 0.85 => "Degraded",
            _ => "Unhealthy"
        };
    }

    private int GetLastExportSize()
    {
        // This would be tracked in a real implementation
        return 0;
    }

    private void CollectPerformanceMetrics(object? state)
    {
        try
        {
            var exporterMetrics = new Dictionary<string, object>
            {
                ["total_exports"] = _totalExports,
                ["failed_exports"] = _failedExports,
                ["success_rate"] = _totalExports > 0 ? (double)(_totalExports - _failedExports) / _totalExports : 1.0,
                ["last_export_duration_ms"] = _lastExportDuration.TotalMilliseconds,
                ["custom_metrics_count"] = _customMetrics.Count,
                ["available_export_slots"] = _exportSemaphore.CurrentCount
            };

            _logger.LogDebug("Performance metrics: {Metrics}", JsonSerializer.Serialize(exporterMetrics));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect performance metrics");
        }
    }

    public void Dispose()
    {
        _performanceTimer?.Dispose();
        _exportSemaphore?.Dispose();
    }
}
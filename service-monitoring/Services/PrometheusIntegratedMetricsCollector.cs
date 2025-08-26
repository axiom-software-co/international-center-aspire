using Infrastructure.Metrics.Abstractions;
using System.Runtime;

namespace Service.Monitoring.Services;

public sealed class PrometheusIntegratedMetricsCollector : IMetricsCollector, IDisposable
{
    private readonly IDatabaseHealthCheck _databaseHealthCheck;
    private readonly IRedisHealthCheck _redisHealthCheck;
    private readonly ICustomMetricsRegistry _metricsRegistry;
    private readonly IPrometheusMetricsExporter _prometheusExporter;
    private readonly ILogger<PrometheusIntegratedMetricsCollector> _logger;
    private readonly MonitoringOptions _options;
    private readonly Meter _meter;
    
    // OpenTelemetry instruments registered with Infrastructure-Metrics
    private readonly Counter<long> _healthCheckCounter;
    private readonly Histogram<double> _healthCheckDuration;
    private readonly Counter<long> _healthCheckResultCounter;
    private readonly Gauge<long> _systemMemoryGauge;
    private readonly Gauge<double> _databaseLatencyGauge;
    private readonly Gauge<double> _redisLatencyGauge;
    private readonly Counter<long> _databaseConnectionCounter;
    private readonly Counter<long> _redisConnectionCounter;

    public PrometheusIntegratedMetricsCollector(
        IDatabaseHealthCheck databaseHealthCheck,
        IRedisHealthCheck redisHealthCheck,
        ICustomMetricsRegistry metricsRegistry,
        IPrometheusMetricsExporter prometheusExporter,
        ILogger<PrometheusIntegratedMetricsCollector> logger,
        IOptions<MonitoringOptions> options)
    {
        _databaseHealthCheck = databaseHealthCheck ?? throw new ArgumentNullException(nameof(databaseHealthCheck));
        _redisHealthCheck = redisHealthCheck ?? throw new ArgumentNullException(nameof(redisHealthCheck));
        _metricsRegistry = metricsRegistry ?? throw new ArgumentNullException(nameof(metricsRegistry));
        _prometheusExporter = prometheusExporter ?? throw new ArgumentNullException(nameof(prometheusExporter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        var meterName = _options.Metrics?.MeterName ?? "Service.Monitoring";
        _meter = new Meter(meterName, "1.0.0");
        
        // Register the meter with Infrastructure-Metrics registry
        _metricsRegistry.RegisterMeter(_meter);
        
        // Create instruments using Infrastructure-Metrics registry
        _healthCheckCounter = _metricsRegistry.CreateCounter<long>(
            "monitoring_health_checks_total",
            "count",
            "Total number of health checks performed");
            
        _healthCheckDuration = _metricsRegistry.CreateHistogram<double>(
            "monitoring_health_check_duration_seconds",
            "seconds", 
            "Duration of health checks");
            
        _healthCheckResultCounter = _metricsRegistry.CreateCounter<long>(
            "monitoring_health_check_results_total",
            "count",
            "Total number of health check results by status");

        // System metrics instruments
        _systemMemoryGauge = _metricsRegistry.CreateGauge<long>(
            "monitoring_system_memory_bytes",
            "bytes",
            "System memory usage in bytes");

        // Database metrics instruments
        _databaseLatencyGauge = _metricsRegistry.CreateGauge<double>(
            "monitoring_database_latency_seconds",
            "seconds",
            "Database connection latency in seconds");
            
        _databaseConnectionCounter = _metricsRegistry.CreateCounter<long>(
            "monitoring_database_connections_total",
            "count",
            "Total database connection attempts");

        // Redis metrics instruments
        _redisLatencyGauge = _metricsRegistry.CreateGauge<double>(
            "monitoring_redis_latency_seconds",
            "seconds",
            "Redis connection latency in seconds");
            
        _redisConnectionCounter = _metricsRegistry.CreateCounter<long>(
            "monitoring_redis_connections_total",
            "count",
            "Total Redis connection attempts");

        _logger.LogInformation("Initialized Prometheus-integrated metrics collector with meter {MeterName}", meterName);
    }

    public async Task<IDictionary<string, object>> CollectAllMetricsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var metrics = new Dictionary<string, object>();

        if (_options.Metrics?.CollectSystemMetrics == true)
        {
            var systemMetrics = await CollectSystemMetricsAsync(cancellationToken);
            foreach (var metric in systemMetrics)
            {
                metrics[$"system_{metric.Key}"] = metric.Value;
                
                // Export key system metrics to Prometheus
                if (metric.Key == "gc_total_memory_bytes" && metric.Value is long memoryValue)
                {
                    _systemMemoryGauge.Record(memoryValue);
                }
            }
        }

        if (_options.Metrics?.CollectDatabaseMetrics == true)
        {
            var databaseMetrics = await CollectDatabaseMetricsAsync(cancellationToken);
            foreach (var metric in databaseMetrics)
            {
                metrics[$"database_{metric.Key}"] = metric.Value;
                
                // Export key database metrics to Prometheus
                if (metric.Key == "latency_ms" && metric.Value is double latencyMs)
                {
                    _databaseLatencyGauge.Record(latencyMs / 1000.0); // Convert to seconds
                }
            }
        }

        if (_options.Metrics?.CollectRedisMetrics == true)
        {
            var redisMetrics = await CollectRedisMetricsAsync(cancellationToken);
            foreach (var metric in redisMetrics)
            {
                metrics[$"redis_{metric.Key}"] = metric.Value;
                
                // Export key Redis metrics to Prometheus
                if (metric.Key == "latency_ms" && metric.Value is double latencyMs)
                {
                    _redisLatencyGauge.Record(latencyMs / 1000.0); // Convert to seconds
                }
            }
        }

        stopwatch.Stop();
        
        // Add collection metadata
        metrics["collection_timestamp"] = DateTimeOffset.UtcNow.ToString("O");
        metrics["collection_duration_ms"] = stopwatch.Elapsed.TotalMilliseconds;
        metrics["prometheus_integration"] = true;
        metrics["total_metrics"] = metrics.Count;

        // Record collection performance metrics
        _prometheusExporter.RecordHistogram("monitoring_collection_duration_seconds", 
            stopwatch.Elapsed.TotalSeconds,
            new KeyValuePair<string, object?>("collector", "integrated"));

        _logger.LogDebug("Collected {Count} metrics in {Duration}ms with Prometheus integration", 
            metrics.Count, stopwatch.Elapsed.TotalMilliseconds);

        return metrics;
    }

    public async Task<IDictionary<string, object>> CollectSystemMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            // Memory metrics
            var gcInfo = GC.GetGCMemoryInfo();
            var totalMemory = GC.GetTotalMemory(forceFullCollection: false);
            
            metrics["gc_total_memory_bytes"] = totalMemory;
            metrics["gc_heap_size_bytes"] = gcInfo.HeapSizeBytes;
            metrics["gc_committed_memory_load"] = gcInfo.MemoryLoadBytes;
            metrics["gc_fragmented_bytes"] = gcInfo.FragmentedBytes;
            
            // Export to Prometheus
            _prometheusExporter.SetGauge("system_gc_memory_bytes", totalMemory);
            _prometheusExporter.SetGauge("system_gc_heap_size_bytes", gcInfo.HeapSizeBytes);
            
            // GC metrics with generation labels
            var gc0 = GC.CollectionCount(0);
            var gc1 = GC.CollectionCount(1);
            var gc2 = GC.CollectionCount(2);
            
            metrics["gc_collection_count_gen0"] = gc0;
            metrics["gc_collection_count_gen1"] = gc1;
            metrics["gc_collection_count_gen2"] = gc2;
            
            _prometheusExporter.SetGauge("system_gc_collections_total", gc0, 
                new KeyValuePair<string, object?>("generation", "0"));
            _prometheusExporter.SetGauge("system_gc_collections_total", gc1, 
                new KeyValuePair<string, object?>("generation", "1"));
            _prometheusExporter.SetGauge("system_gc_collections_total", gc2, 
                new KeyValuePair<string, object?>("generation", "2"));

            // Process metrics
            using var process = Process.GetCurrentProcess();
            var processMetrics = new Dictionary<string, long>
            {
                ["process_working_set_bytes"] = process.WorkingSet64,
                ["process_virtual_memory_bytes"] = process.VirtualMemorySize64,
                ["process_private_memory_bytes"] = process.PrivateMemorySize64,
                ["process_threads_count"] = process.Threads.Count,
                ["process_handles_count"] = process.HandleCount
            };

            foreach (var pm in processMetrics)
            {
                metrics[pm.Key] = pm.Value;
                _prometheusExporter.SetGauge($"system_{pm.Key}", pm.Value);
            }

            metrics["process_cpu_time_seconds"] = process.TotalProcessorTime.TotalSeconds;
            _prometheusExporter.SetGauge("system_process_cpu_seconds_total", process.TotalProcessorTime.TotalSeconds);

            // Thread pool metrics
            ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
            
            var threadPoolMetrics = new Dictionary<string, int>
            {
                ["threadpool_available_worker_threads"] = availableWorkerThreads,
                ["threadpool_available_completion_port_threads"] = availableCompletionPortThreads,
                ["threadpool_max_worker_threads"] = maxWorkerThreads,
                ["threadpool_max_completion_port_threads"] = maxCompletionPortThreads,
                ["threadpool_busy_worker_threads"] = maxWorkerThreads - availableWorkerThreads,
                ["threadpool_busy_completion_port_threads"] = maxCompletionPortThreads - availableCompletionPortThreads
            };

            foreach (var tpm in threadPoolMetrics)
            {
                metrics[tpm.Key] = tpm.Value;
                _prometheusExporter.SetGauge($"system_{tpm.Key}", tpm.Value);
            }

            _logger.LogDebug("Collected {Count} system metrics with Prometheus export", metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect some system metrics");
            _prometheusExporter.IncrementCounter("monitoring_system_metrics_errors_total");
        }

        await Task.CompletedTask;
        return metrics;
    }

    public async Task<IDictionary<string, object>> CollectDatabaseMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new Dictionary<string, object>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _databaseConnectionCounter.Add(1, 
                new KeyValuePair<string, object?>("operation", "health_check"));

            var canConnect = await _databaseHealthCheck.CanConnectAsync(cancellationToken);
            metrics["connection_available"] = canConnect;
            
            _prometheusExporter.SetGauge("database_connection_available", canConnect ? 1 : 0);

            if (canConnect)
            {
                var latency = await _databaseHealthCheck.MeasureLatencyAsync(cancellationToken);
                var latencyMs = latency.TotalMilliseconds;
                var isHealthy = latency < TimeSpan.FromSeconds(2);
                
                metrics["latency_ms"] = latencyMs;
                metrics["latency_healthy"] = isHealthy;
                
                _prometheusExporter.RecordHistogram("database_latency_seconds", latency.TotalSeconds);
                _prometheusExporter.SetGauge("database_latency_healthy", isHealthy ? 1 : 0);

                var migrationsUpToDate = await _databaseHealthCheck.AreMigrationsCurrentAsync(cancellationToken);
                metrics["migrations_current"] = migrationsUpToDate;
                
                _prometheusExporter.SetGauge("database_migrations_current", migrationsUpToDate ? 1 : 0);
                
                _prometheusExporter.IncrementCounter("database_health_checks_total", 
                    new KeyValuePair<string, object?>("result", "success"));
            }
            else
            {
                metrics["latency_ms"] = double.MaxValue;
                metrics["latency_healthy"] = false;
                metrics["migrations_current"] = false;
                
                _prometheusExporter.SetGauge("database_latency_healthy", 0);
                _prometheusExporter.SetGauge("database_migrations_current", 0);
                
                _prometheusExporter.IncrementCounter("database_health_checks_total", 
                    new KeyValuePair<string, object?>("result", "connection_failed"));
            }

            stopwatch.Stop();
            _prometheusExporter.RecordHistogram("database_health_check_duration_seconds", 
                stopwatch.Elapsed.TotalSeconds);

            _logger.LogDebug("Collected {Count} database metrics in {Duration}ms", 
                metrics.Count, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect database metrics");
            
            metrics["connection_available"] = false;
            metrics["latency_ms"] = double.MaxValue;
            metrics["latency_healthy"] = false;
            metrics["migrations_current"] = false;
            
            _prometheusExporter.IncrementCounter("database_health_checks_total", 
                new KeyValuePair<string, object?>("result", "error"));
            _prometheusExporter.IncrementCounter("monitoring_database_metrics_errors_total");
        }

        return metrics;
    }

    public async Task<IDictionary<string, object>> CollectRedisMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new Dictionary<string, object>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _redisConnectionCounter.Add(1, 
                new KeyValuePair<string, object?>("operation", "health_check"));

            var canConnect = await _redisHealthCheck.CanConnectAsync(cancellationToken);
            metrics["connection_available"] = canConnect;
            
            _prometheusExporter.SetGauge("redis_connection_available", canConnect ? 1 : 0);

            if (canConnect)
            {
                var canReadWrite = await _redisHealthCheck.CanReadWriteAsync(cancellationToken);
                metrics["read_write_available"] = canReadWrite;
                
                _prometheusExporter.SetGauge("redis_read_write_available", canReadWrite ? 1 : 0);

                var latency = await _redisHealthCheck.MeasureLatencyAsync(cancellationToken);
                var latencyMs = latency.TotalMilliseconds;
                var isHealthy = latency < TimeSpan.FromSeconds(1);
                
                metrics["latency_ms"] = latencyMs;
                metrics["latency_healthy"] = isHealthy;
                
                _prometheusExporter.RecordHistogram("redis_latency_seconds", latency.TotalSeconds);
                _prometheusExporter.SetGauge("redis_latency_healthy", isHealthy ? 1 : 0);

                var memoryUsage = await _redisHealthCheck.GetMemoryUsageAsync(cancellationToken);
                if (memoryUsage > 0)
                {
                    metrics["memory_usage_bytes"] = memoryUsage;
                    metrics["memory_usage_mb"] = memoryUsage / (1024.0 * 1024.0);
                    
                    _prometheusExporter.SetGauge("redis_memory_usage_bytes", memoryUsage);
                }
                
                _prometheusExporter.IncrementCounter("redis_health_checks_total", 
                    new KeyValuePair<string, object?>("result", "success"));
            }
            else
            {
                metrics["read_write_available"] = false;
                metrics["latency_ms"] = double.MaxValue;
                metrics["latency_healthy"] = false;
                metrics["memory_usage_bytes"] = 0;
                
                _prometheusExporter.SetGauge("redis_read_write_available", 0);
                _prometheusExporter.SetGauge("redis_latency_healthy", 0);
                
                _prometheusExporter.IncrementCounter("redis_health_checks_total", 
                    new KeyValuePair<string, object?>("result", "connection_failed"));
            }

            stopwatch.Stop();
            _prometheusExporter.RecordHistogram("redis_health_check_duration_seconds", 
                stopwatch.Elapsed.TotalSeconds);

            _logger.LogDebug("Collected {Count} Redis metrics in {Duration}ms", 
                metrics.Count, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect Redis metrics");
            
            metrics["connection_available"] = false;
            metrics["read_write_available"] = false;
            metrics["latency_ms"] = double.MaxValue;
            metrics["latency_healthy"] = false;
            metrics["memory_usage_bytes"] = 0;
            
            _prometheusExporter.IncrementCounter("redis_health_checks_total", 
                new KeyValuePair<string, object?>("result", "error"));
            _prometheusExporter.IncrementCounter("monitoring_redis_metrics_errors_total");
        }

        return metrics;
    }

    public void RecordHealthCheckDuration(string checkName, TimeSpan duration)
    {
        _healthCheckDuration.Record(duration.TotalSeconds, 
            new KeyValuePair<string, object?>("check_name", checkName));
            
        // Also export to Prometheus
        _prometheusExporter.RecordHistogram("health_check_duration_seconds", duration.TotalSeconds,
            new KeyValuePair<string, object?>("check_name", checkName));
            
        _logger.LogDebug("Recorded health check duration for {CheckName}: {Duration}ms", 
            checkName, duration.TotalMilliseconds);
    }

    public void RecordHealthCheckResult(string checkName, HealthStatus status)
    {
        _healthCheckResultCounter.Add(1, 
            new KeyValuePair<string, object?>("check_name", checkName),
            new KeyValuePair<string, object?>("status", status.ToString().ToLowerInvariant()));
            
        // Also export to Prometheus
        _prometheusExporter.IncrementCounter("health_check_results_total",
            new KeyValuePair<string, object?>("check_name", checkName),
            new KeyValuePair<string, object?>("status", status.ToString().ToLowerInvariant()));
            
        _logger.LogDebug("Recorded health check result for {CheckName}: {Status}", checkName, status);
    }

    public void IncrementHealthCheckCount(string checkName)
    {
        _healthCheckCounter.Add(1, 
            new KeyValuePair<string, object?>("check_name", checkName));
            
        // Also export to Prometheus
        _prometheusExporter.IncrementCounter("health_checks_total",
            new KeyValuePair<string, object?>("check_name", checkName));
            
        _logger.LogDebug("Incremented health check count for {CheckName}", checkName);
    }

    public void Dispose()
    {
        if (_meter != null)
        {
            _metricsRegistry.UnregisterMeter(_meter);
            _meter.Dispose();
        }
    }
}
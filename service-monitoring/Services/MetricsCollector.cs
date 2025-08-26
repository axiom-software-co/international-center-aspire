using System.Runtime;

namespace Service.Monitoring.Services;

public sealed class MetricsCollector : IMetricsCollector, IDisposable
{
    private readonly IDatabaseHealthCheck _databaseHealthCheck;
    private readonly IRedisHealthCheck _redisHealthCheck;
    private readonly ILogger<MetricsCollector> _logger;
    private readonly MetricsOptions _options;
    private readonly Meter _meter;
    
    private readonly Counter<long> _healthCheckCounter;
    private readonly Histogram<double> _healthCheckDuration;
    private readonly Counter<long> _healthCheckResultCounter;

    public MetricsCollector(
        IDatabaseHealthCheck databaseHealthCheck,
        IRedisHealthCheck redisHealthCheck,
        ILogger<MetricsCollector> logger,
        IOptions<MonitoringOptions> options)
    {
        _databaseHealthCheck = databaseHealthCheck ?? throw new ArgumentNullException(nameof(databaseHealthCheck));
        _redisHealthCheck = redisHealthCheck ?? throw new ArgumentNullException(nameof(redisHealthCheck));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value?.Metrics ?? throw new ArgumentNullException(nameof(options));
        
        _meter = new Meter(_options.MeterName, "1.0.0");
        
        _healthCheckCounter = _meter.CreateCounter<long>(
            "monitoring_health_checks_total",
            "count",
            "Total number of health checks performed");
            
        _healthCheckDuration = _meter.CreateHistogram<double>(
            "monitoring_health_check_duration_seconds",
            "seconds", 
            "Duration of health checks");
            
        _healthCheckResultCounter = _meter.CreateCounter<long>(
            "monitoring_health_check_results_total",
            "count",
            "Total number of health check results by status");
    }

    public async Task<IDictionary<string, object>> CollectAllMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new Dictionary<string, object>();

        if (_options.CollectSystemMetrics)
        {
            var systemMetrics = await CollectSystemMetricsAsync(cancellationToken);
            foreach (var metric in systemMetrics)
            {
                metrics[$"system_{metric.Key}"] = metric.Value;
            }
        }

        if (_options.CollectDatabaseMetrics)
        {
            var databaseMetrics = await CollectDatabaseMetricsAsync(cancellationToken);
            foreach (var metric in databaseMetrics)
            {
                metrics[$"database_{metric.Key}"] = metric.Value;
            }
        }

        if (_options.CollectRedisMetrics)
        {
            var redisMetrics = await CollectRedisMetricsAsync(cancellationToken);
            foreach (var metric in redisMetrics)
            {
                metrics[$"redis_{metric.Key}"] = metric.Value;
            }
        }

        metrics["collection_timestamp"] = DateTimeOffset.UtcNow.ToString("O");
        metrics["collection_duration_ms"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        _logger.LogDebug("Collected {Count} metrics", metrics.Count);

        return metrics;
    }

    public async Task<IDictionary<string, object>> CollectSystemMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            // Memory metrics
            var gcInfo = GC.GetGCMemoryInfo();
            metrics["gc_total_memory_bytes"] = GC.GetTotalMemory(forceFullCollection: false);
            metrics["gc_heap_size_bytes"] = gcInfo.HeapSizeBytes;
            metrics["gc_committed_memory_load"] = gcInfo.MemoryLoadBytes;
            
            // GC metrics
            metrics["gc_collection_count_gen0"] = GC.CollectionCount(0);
            metrics["gc_collection_count_gen1"] = GC.CollectionCount(1);
            metrics["gc_collection_count_gen2"] = GC.CollectionCount(2);

            // Process metrics
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            metrics["process_working_set_bytes"] = process.WorkingSet64;
            metrics["process_virtual_memory_bytes"] = process.VirtualMemorySize64;
            metrics["process_private_memory_bytes"] = process.PrivateMemorySize64;
            metrics["process_cpu_time_seconds"] = process.TotalProcessorTime.TotalSeconds;
            metrics["process_threads_count"] = process.Threads.Count;
            metrics["process_handles_count"] = process.HandleCount;

            // Thread pool metrics
            ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
            
            metrics["threadpool_available_worker_threads"] = availableWorkerThreads;
            metrics["threadpool_available_completion_port_threads"] = availableCompletionPortThreads;
            metrics["threadpool_max_worker_threads"] = maxWorkerThreads;
            metrics["threadpool_max_completion_port_threads"] = maxCompletionPortThreads;
            metrics["threadpool_busy_worker_threads"] = maxWorkerThreads - availableWorkerThreads;
            metrics["threadpool_busy_completion_port_threads"] = maxCompletionPortThreads - availableCompletionPortThreads;

            _logger.LogDebug("Collected {Count} system metrics", metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect some system metrics");
        }

        await Task.CompletedTask;
        return metrics;
    }

    public async Task<IDictionary<string, object>> CollectDatabaseMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            var canConnect = await _databaseHealthCheck.CanConnectAsync(cancellationToken);
            metrics["connection_available"] = canConnect;

            if (canConnect)
            {
                var latency = await _databaseHealthCheck.MeasureLatencyAsync(cancellationToken);
                metrics["latency_ms"] = latency.TotalMilliseconds;
                metrics["latency_healthy"] = latency < TimeSpan.FromSeconds(2);

                var migrationsUpToDate = await _databaseHealthCheck.AreMigrationsCurrentAsync(cancellationToken);
                metrics["migrations_current"] = migrationsUpToDate;
            }
            else
            {
                metrics["latency_ms"] = double.MaxValue;
                metrics["latency_healthy"] = false;
                metrics["migrations_current"] = false;
            }

            _logger.LogDebug("Collected {Count} database metrics", metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect database metrics");
            metrics["connection_available"] = false;
            metrics["latency_ms"] = double.MaxValue;
            metrics["latency_healthy"] = false;
            metrics["migrations_current"] = false;
        }

        return metrics;
    }

    public async Task<IDictionary<string, object>> CollectRedisMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            var canConnect = await _redisHealthCheck.CanConnectAsync(cancellationToken);
            metrics["connection_available"] = canConnect;

            if (canConnect)
            {
                var canReadWrite = await _redisHealthCheck.CanReadWriteAsync(cancellationToken);
                metrics["read_write_available"] = canReadWrite;

                var latency = await _redisHealthCheck.MeasureLatencyAsync(cancellationToken);
                metrics["latency_ms"] = latency.TotalMilliseconds;
                metrics["latency_healthy"] = latency < TimeSpan.FromSeconds(1);

                var memoryUsage = await _redisHealthCheck.GetMemoryUsageAsync(cancellationToken);
                if (memoryUsage > 0)
                {
                    metrics["memory_usage_bytes"] = memoryUsage;
                    metrics["memory_usage_mb"] = memoryUsage / (1024.0 * 1024.0);
                }
            }
            else
            {
                metrics["read_write_available"] = false;
                metrics["latency_ms"] = double.MaxValue;
                metrics["latency_healthy"] = false;
                metrics["memory_usage_bytes"] = 0;
            }

            _logger.LogDebug("Collected {Count} Redis metrics", metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect Redis metrics");
            metrics["connection_available"] = false;
            metrics["read_write_available"] = false;
            metrics["latency_ms"] = double.MaxValue;
            metrics["latency_healthy"] = false;
            metrics["memory_usage_bytes"] = 0;
        }

        return metrics;
    }

    public void RecordHealthCheckDuration(string checkName, TimeSpan duration)
    {
        _healthCheckDuration.Record(duration.TotalSeconds, 
            new KeyValuePair<string, object?>("check_name", checkName));
            
        _logger.LogDebug("Recorded health check duration for {CheckName}: {Duration}ms", 
            checkName, duration.TotalMilliseconds);
    }

    public void RecordHealthCheckResult(string checkName, HealthStatus status)
    {
        _healthCheckResultCounter.Add(1, 
            new KeyValuePair<string, object?>("check_name", checkName),
            new KeyValuePair<string, object?>("status", status.ToString().ToLowerInvariant()));
            
        _logger.LogDebug("Recorded health check result for {CheckName}: {Status}", checkName, status);
    }

    public void IncrementHealthCheckCount(string checkName)
    {
        _healthCheckCounter.Add(1, 
            new KeyValuePair<string, object?>("check_name", checkName));
            
        _logger.LogDebug("Incremented health check count for {CheckName}", checkName);
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }
}
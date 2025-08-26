namespace Service.Monitoring.Services;

public sealed class MonitoringService : IMonitoringService
{
    private readonly IDatabaseHealthCheck _databaseHealthCheck;
    private readonly IRedisHealthCheck _redisHealthCheck;
    private readonly IMetricsCollector _metricsCollector;
    private readonly ILogger<MonitoringService> _logger;
    private readonly MonitoringOptions _options;
    
    private readonly SemaphoreSlim _healthCheckSemaphore = new(1, 1);
    private HealthCheckReport? _cachedReport;
    private DateTimeOffset _lastHealthCheckTime;

    public MonitoringService(
        IDatabaseHealthCheck databaseHealthCheck,
        IRedisHealthCheck redisHealthCheck,
        IMetricsCollector metricsCollector,
        ILogger<MonitoringService> logger,
        IOptions<MonitoringOptions> options)
    {
        _databaseHealthCheck = databaseHealthCheck ?? throw new ArgumentNullException(nameof(databaseHealthCheck));
        _redisHealthCheck = redisHealthCheck ?? throw new ArgumentNullException(nameof(redisHealthCheck));
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<HealthCheckReport> CheckHealthAsync(HealthCheckType checkType = HealthCheckType.Full, 
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new HealthCheckReport
            {
                Status = HealthStatus.Healthy,
                TotalDuration = TimeSpan.Zero,
                Results = new Dictionary<string, HealthCheckResult>()
            };
        }

        // Use cached result if available and still valid
        if (_options.CacheResults && _cachedReport != null && 
            DateTimeOffset.UtcNow - _lastHealthCheckTime < _options.CacheDuration)
        {
            _logger.LogDebug("Returning cached health check report");
            return _cachedReport;
        }

        await _healthCheckSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check pattern for cache
            if (_options.CacheResults && _cachedReport != null && 
                DateTimeOffset.UtcNow - _lastHealthCheckTime < _options.CacheDuration)
            {
                _logger.LogDebug("Returning cached health check report (double-check)");
                return _cachedReport;
            }

            var overallStopwatch = Stopwatch.StartNew();
            var results = new Dictionary<string, HealthCheckResult>();
            var tasks = new List<Task<HealthCheckResult>>();

            // Determine which checks to run based on check type
            var checksToRun = GetChecksToRun(checkType);

            foreach (var checkName in checksToRun)
            {
                tasks.Add(RunHealthCheck(checkName, cancellationToken));
            }

            // Execute all health checks concurrently with timeout
            using var timeoutCts = new CancellationTokenSource(_options.HealthCheckTimeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                var completedResults = await Task.WhenAll(tasks.ToArray()).WaitAsync(combinedCts.Token);
                
                foreach (var result in completedResults)
                {
                    results[result.Name] = result;
                    
                    // Record metrics
                    _metricsCollector.IncrementHealthCheckCount(result.Name);
                    _metricsCollector.RecordHealthCheckDuration(result.Name, result.Duration);
                    _metricsCollector.RecordHealthCheckResult(result.Name, result.Status);
                }
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                _logger.LogWarning("Health check timed out after {Timeout}", _options.HealthCheckTimeout);
                
                // Add timeout results for any incomplete checks
                foreach (var checkName in checksToRun)
                {
                    if (!results.ContainsKey(checkName))
                    {
                        results[checkName] = new HealthCheckResult
                        {
                            Name = checkName,
                            Status = HealthStatus.Unhealthy,
                            Description = "Health check timed out",
                            Duration = _options.HealthCheckTimeout,
                            Exception = "Operation timed out"
                        };
                    }
                }
            }

            overallStopwatch.Stop();

            // Determine overall status
            var overallStatus = DetermineOverallStatus(results.Values);

            var report = new HealthCheckReport
            {
                Status = overallStatus,
                TotalDuration = overallStopwatch.Elapsed,
                Results = results
            };

            // Cache the result if caching is enabled
            if (_options.CacheResults)
            {
                _cachedReport = report;
                _lastHealthCheckTime = DateTimeOffset.UtcNow;
            }

            _logger.LogInformation("Health check completed: {Status} in {Duration}ms with {CheckCount} checks", 
                overallStatus, overallStopwatch.Elapsed.TotalMilliseconds, results.Count);

            return report;
        }
        finally
        {
            _healthCheckSemaphore.Release();
        }
    }

    public async Task<HealthStatus> CheckLivenessAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await CheckHealthAsync(HealthCheckType.Liveness, cancellationToken);
            return report.Status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Liveness check failed");
            return HealthStatus.Unhealthy;
        }
    }

    public async Task<HealthStatus> CheckReadinessAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await CheckHealthAsync(HealthCheckType.Readiness, cancellationToken);
            return report.Status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return HealthStatus.Unhealthy;
        }
    }

    public async Task<IDictionary<string, object>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _metricsCollector.CollectAllMetricsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect metrics");
            return new Dictionary<string, object>
            {
                ["error"] = "Failed to collect metrics",
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await CheckLivenessAsync(cancellationToken);
            return status == HealthStatus.Healthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if system is healthy");
            return false;
        }
    }

    private async Task<HealthCheckResult> RunHealthCheck(string checkName, CancellationToken cancellationToken)
    {
        try
        {
            return checkName switch
            {
                "PostgreSQL Database" => await _databaseHealthCheck.CheckHealthAsync(cancellationToken),
                "Redis Cache" => await _redisHealthCheck.CheckHealthAsync(cancellationToken),
                "System" => CreateSystemHealthCheck(),
                _ => new HealthCheckResult
                {
                    Name = checkName,
                    Status = HealthStatus.Unhealthy,
                    Description = $"Unknown health check: {checkName}",
                    Duration = TimeSpan.Zero
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check {CheckName} failed with exception", checkName);
            
            return new HealthCheckResult
            {
                Name = checkName,
                Status = HealthStatus.Unhealthy,
                Description = $"Health check failed: {ex.Message}",
                Duration = TimeSpan.Zero,
                Exception = ex.Message
            };
        }
    }

    private static HealthCheckResult CreateSystemHealthCheck()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Basic system availability check
            var gcMemory = GC.GetTotalMemory(forceFullCollection: false);
            var data = new Dictionary<string, object>
            {
                ["gc_memory_bytes"] = gcMemory,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };

            return new HealthCheckResult
            {
                Name = "System",
                Status = HealthStatus.Healthy,
                Description = "System is operational",
                Duration = stopwatch.Elapsed,
                Data = data
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckResult
            {
                Name = "System",
                Status = HealthStatus.Unhealthy,
                Description = "System check failed",
                Duration = TimeSpan.Zero,
                Exception = ex.Message
            };
        }
    }

    private List<string> GetChecksToRun(HealthCheckType checkType)
    {
        return checkType switch
        {
            HealthCheckType.Liveness => new List<string> { "System" },
            HealthCheckType.Readiness => new List<string> { "System", "PostgreSQL Database", "Redis Cache" },
            HealthCheckType.Full => new List<string> { "System", "PostgreSQL Database", "Redis Cache" },
            _ => new List<string> { "System" }
        };
    }

    private static HealthStatus DetermineOverallStatus(IEnumerable<HealthCheckResult> results)
    {
        var statuses = results.Select(r => r.Status).ToList();
        
        if (statuses.Any(s => s == HealthStatus.Unhealthy))
        {
            return HealthStatus.Unhealthy;
        }
        
        if (statuses.Any(s => s == HealthStatus.Degraded))
        {
            return HealthStatus.Degraded;
        }
        
        return HealthStatus.Healthy;
    }
}
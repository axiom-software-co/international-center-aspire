using System.Collections.Concurrent;

namespace Infrastructure.Metrics.Services;

public sealed class CustomMetricsRegistry : ICustomMetricsRegistry, IDisposable
{
    private readonly ILogger<CustomMetricsRegistry> _logger;
    private readonly MetricsOptions _options;
    private readonly ConcurrentDictionary<string, Meter> _registeredMeters = new();
    private readonly ConcurrentDictionary<string, object> _registeredInstruments = new();
    private readonly SemaphoreSlim _registrationSemaphore = new(1, 1);
    private readonly Timer? _cleanupTimer;

    public CustomMetricsRegistry(
        ILogger<CustomMetricsRegistry> logger,
        IOptions<MetricsOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (_options.CustomMetrics.MetricRetention > TimeSpan.Zero)
        {
            _cleanupTimer = new Timer(CleanupExpiredMetrics, null, 
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }
    }

    public Counter<T> CreateCounter<T>(string name, string? unit = null, string? description = null) where T : struct
    {
        if (!_options.CustomMetrics.EnableCustomMetrics)
        {
            throw new InvalidOperationException("Custom metrics are disabled");
        }

        ValidateMetricName(name);
        ThrowIfTooManyMetrics();

        var meterName = _options.CustomMetrics.MeterName;
        var meter = GetOrCreateMeter(meterName, _options.CustomMetrics.MeterVersion);

        var instrumentKey = CreateInstrumentKey(meterName, name, "Counter");
        
        if (_registeredInstruments.TryGetValue(instrumentKey, out var existingInstrument))
        {
            if (existingInstrument is Counter<T> existingCounter)
            {
                _logger.LogDebug("Returning existing counter {Name}", name);
                return existingCounter;
            }
            
            throw new InvalidOperationException($"Instrument {name} already exists with different type");
        }

        var counter = meter.CreateCounter<T>(name, unit, description);
        _registeredInstruments.TryAdd(instrumentKey, counter);

        _logger.LogDebug("Created counter {Name} with unit {Unit}", name, unit ?? "none");
        return counter;
    }

    public Histogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null) where T : struct
    {
        if (!_options.CustomMetrics.EnableCustomMetrics)
        {
            throw new InvalidOperationException("Custom metrics are disabled");
        }

        ValidateMetricName(name);
        ThrowIfTooManyMetrics();

        var meterName = _options.CustomMetrics.MeterName;
        var meter = GetOrCreateMeter(meterName, _options.CustomMetrics.MeterVersion);

        var instrumentKey = CreateInstrumentKey(meterName, name, "Histogram");
        
        if (_registeredInstruments.TryGetValue(instrumentKey, out var existingInstrument))
        {
            if (existingInstrument is Histogram<T> existingHistogram)
            {
                _logger.LogDebug("Returning existing histogram {Name}", name);
                return existingHistogram;
            }
            
            throw new InvalidOperationException($"Instrument {name} already exists with different type");
        }

        var histogram = meter.CreateHistogram<T>(name, unit, description);
        _registeredInstruments.TryAdd(instrumentKey, histogram);

        _logger.LogDebug("Created histogram {Name} with unit {Unit}", name, unit ?? "none");
        return histogram;
    }

    public Gauge<T> CreateGauge<T>(string name, string? unit = null, string? description = null) where T : struct
    {
        if (!_options.CustomMetrics.EnableCustomMetrics)
        {
            throw new InvalidOperationException("Custom metrics are disabled");
        }

        ValidateMetricName(name);
        ThrowIfTooManyMetrics();

        var meterName = _options.CustomMetrics.MeterName;
        var meter = GetOrCreateMeter(meterName, _options.CustomMetrics.MeterVersion);

        var instrumentKey = CreateInstrumentKey(meterName, name, "Gauge");
        
        if (_registeredInstruments.TryGetValue(instrumentKey, out var existingInstrument))
        {
            if (existingInstrument is Gauge<T> existingGauge)
            {
                _logger.LogDebug("Returning existing gauge {Name}", name);
                return existingGauge;
            }
            
            throw new InvalidOperationException($"Instrument {name} already exists with different type");
        }

        var gauge = meter.CreateGauge<T>(name, unit, description);
        _registeredInstruments.TryAdd(instrumentKey, gauge);

        _logger.LogDebug("Created gauge {Name} with unit {Unit}", name, unit ?? "none");
        return gauge;
    }

    public UpDownCounter<T> CreateUpDownCounter<T>(string name, string? unit = null, string? description = null) where T : struct
    {
        if (!_options.CustomMetrics.EnableCustomMetrics)
        {
            throw new InvalidOperationException("Custom metrics are disabled");
        }

        ValidateMetricName(name);
        ThrowIfTooManyMetrics();

        var meterName = _options.CustomMetrics.MeterName;
        var meter = GetOrCreateMeter(meterName, _options.CustomMetrics.MeterVersion);

        var instrumentKey = CreateInstrumentKey(meterName, name, "UpDownCounter");
        
        if (_registeredInstruments.TryGetValue(instrumentKey, out var existingInstrument))
        {
            if (existingInstrument is UpDownCounter<T> existingUpDownCounter)
            {
                _logger.LogDebug("Returning existing up-down counter {Name}", name);
                return existingUpDownCounter;
            }
            
            throw new InvalidOperationException($"Instrument {name} already exists with different type");
        }

        var upDownCounter = meter.CreateUpDownCounter<T>(name, unit, description);
        _registeredInstruments.TryAdd(instrumentKey, upDownCounter);

        _logger.LogDebug("Created up-down counter {Name} with unit {Unit}", name, unit ?? "none");
        return upDownCounter;
    }

    public void RegisterMeter(Meter meter)
    {
        if (meter == null)
        {
            throw new ArgumentNullException(nameof(meter));
        }

        var key = CreateMeterKey(meter.Name, meter.Version);
        
        if (_registeredMeters.TryAdd(key, meter))
        {
            _logger.LogInformation("Registered meter {Name} version {Version}", meter.Name, meter.Version);
        }
        else
        {
            _logger.LogDebug("Meter {Name} version {Version} already registered", meter.Name, meter.Version);
        }
    }

    public void UnregisterMeter(Meter meter)
    {
        if (meter == null)
        {
            throw new ArgumentNullException(nameof(meter));
        }

        var key = CreateMeterKey(meter.Name, meter.Version);
        
        if (_registeredMeters.TryRemove(key, out var removedMeter))
        {
            // Remove all instruments from this meter
            var instrumentsToRemove = _registeredInstruments.Keys
                .Where(k => k.StartsWith($"{meter.Name}|"))
                .ToList();

            foreach (var instrumentKey in instrumentsToRemove)
            {
                _registeredInstruments.TryRemove(instrumentKey, out _);
            }

            removedMeter.Dispose();
            _logger.LogInformation("Unregistered meter {Name} version {Version} and {Count} instruments", 
                meter.Name, meter.Version, instrumentsToRemove.Count);
        }
        else
        {
            _logger.LogWarning("Attempted to unregister meter {Name} version {Version} that was not registered", 
                meter.Name, meter.Version);
        }
    }

    public IReadOnlyList<Meter> GetRegisteredMeters()
    {
        return _registeredMeters.Values.ToList().AsReadOnly();
    }

    public async Task<IDictionary<string, object>> GetMetricsDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var definitions = new Dictionary<string, object>();

        await _registrationSemaphore.WaitAsync(cancellationToken);
        try
        {
            definitions["total_meters"] = _registeredMeters.Count;
            definitions["total_instruments"] = _registeredInstruments.Count;
            definitions["custom_metrics_enabled"] = _options.CustomMetrics.EnableCustomMetrics;
            definitions["max_custom_metrics"] = _options.CustomMetrics.MaxCustomMetrics;
            definitions["meter_name"] = _options.CustomMetrics.MeterName;
            definitions["meter_version"] = _options.CustomMetrics.MeterVersion;

            var meterStats = new Dictionary<string, object>();
            foreach (var kvp in _registeredMeters)
            {
                var instrumentCount = _registeredInstruments.Keys.Count(k => k.StartsWith($"{kvp.Value.Name}|"));
                meterStats[kvp.Key] = new { InstrumentCount = instrumentCount, Version = kvp.Value.Version };
            }
            definitions["meter_statistics"] = meterStats;

            var instrumentTypes = _registeredInstruments.Values
                .GroupBy(i => i.GetType().Name)
                .ToDictionary(g => g.Key, g => g.Count());
            definitions["instrument_types"] = instrumentTypes;

            _logger.LogDebug("Generated metrics definitions with {MeterCount} meters and {InstrumentCount} instruments",
                _registeredMeters.Count, _registeredInstruments.Count);
        }
        finally
        {
            _registrationSemaphore.Release();
        }

        return definitions;
    }

    public bool IsMetricRegistered(string meterName, string instrumentName)
    {
        if (string.IsNullOrEmpty(meterName) || string.IsNullOrEmpty(instrumentName))
        {
            return false;
        }

        // Check for any instrument type with this name
        var prefixes = new[] { "Counter", "Histogram", "Gauge", "UpDownCounter" };
        return prefixes.Any(prefix => _registeredInstruments.ContainsKey(CreateInstrumentKey(meterName, instrumentName, prefix)));
    }

    public void ValidateMetricName(string name)
    {
        if (!_options.CustomMetrics.ValidateMetricNames)
        {
            return;
        }

        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Metric name cannot be null or empty", nameof(name));
        }

        if (name.Length > 255)
        {
            throw new ArgumentException("Metric name cannot exceed 255 characters", nameof(name));
        }

        // Prometheus metric name validation
        if (!char.IsLetter(name[0]) && name[0] != '_' && name[0] != ':')
        {
            throw new ArgumentException($"Metric name '{name}' must start with a letter, underscore, or colon", nameof(name));
        }

        if (!name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == ':'))
        {
            throw new ArgumentException($"Metric name '{name}' contains invalid characters. Only letters, numbers, underscores, and colons are allowed", nameof(name));
        }

        // Check prefixes if configured
        if (_options.CustomMetrics.MetricPrefixes?.Length > 0)
        {
            var hasValidPrefix = _options.CustomMetrics.MetricPrefixes.Any(prefix => name.StartsWith(prefix));
            if (!hasValidPrefix)
            {
                var allowedPrefixes = string.Join(", ", _options.CustomMetrics.MetricPrefixes);
                throw new ArgumentException($"Metric name '{name}' must start with one of the allowed prefixes: {allowedPrefixes}", nameof(name));
            }
        }

        // Check for reserved names
        var reservedPrefixes = new[] { "system_", "process_", "prometheus_", "opentelemetry_" };
        if (reservedPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"Metric name '{name}' uses a reserved prefix", nameof(name));
        }
    }

    private Meter GetOrCreateMeter(string name, string? version)
    {
        var key = CreateMeterKey(name, version);
        
        return _registeredMeters.GetOrAdd(key, _ =>
        {
            var meter = new Meter(name, version);
            _logger.LogDebug("Created new meter {Name} version {Version}", name, version);
            return meter;
        });
    }

    private void ThrowIfTooManyMetrics()
    {
        if (_registeredInstruments.Count >= _options.CustomMetrics.MaxCustomMetrics)
        {
            throw new InvalidOperationException($"Maximum number of custom metrics ({_options.CustomMetrics.MaxCustomMetrics}) exceeded");
        }
    }

    private static string CreateMeterKey(string? name, string? version)
    {
        return $"{name ?? "unknown"}|{version ?? "unknown"}";
    }

    private static string CreateInstrumentKey(string meterName, string instrumentName, string instrumentType)
    {
        return $"{meterName}|{instrumentName}|{instrumentType}";
    }

    private void CleanupExpiredMetrics(object? state)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var retentionPeriod = _options.CustomMetrics.MetricRetention;

            // In a full implementation, we would track creation times and clean up expired metrics
            // For now, this is a placeholder that logs cleanup activity

            var metersBeforeCleanup = _registeredMeters.Count;
            var instrumentsBeforeCleanup = _registeredInstruments.Count;

            // Cleanup logic would go here - checking creation timestamps and removing old metrics

            _logger.LogDebug("Cleanup check completed. Meters: {Meters}, Instruments: {Instruments}", 
                _registeredMeters.Count, _registeredInstruments.Count);

            if (metersBeforeCleanup != _registeredMeters.Count || instrumentsBeforeCleanup != _registeredInstruments.Count)
            {
                _logger.LogInformation("Cleaned up expired metrics. Meters: {Before} -> {After}, Instruments: {InstrumentsBefore} -> {InstrumentsAfter}",
                    metersBeforeCleanup, _registeredMeters.Count, instrumentsBeforeCleanup, _registeredInstruments.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during metrics cleanup");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();

        foreach (var meter in _registeredMeters.Values)
        {
            meter.Dispose();
        }

        _registeredMeters.Clear();
        _registeredInstruments.Clear();
        _registrationSemaphore.Dispose();

        _logger.LogInformation("Disposed metrics registry with {MeterCount} meters", _registeredMeters.Count);
    }
}
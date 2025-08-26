namespace Infrastructure.Metrics.Abstractions;

public interface ICustomMetricsRegistry
{
    Counter<T> CreateCounter<T>(string name, string? unit = null, string? description = null) where T : struct;
    
    Histogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null) where T : struct;
    
    Gauge<T> CreateGauge<T>(string name, string? unit = null, string? description = null) where T : struct;
    
    UpDownCounter<T> CreateUpDownCounter<T>(string name, string? unit = null, string? description = null) where T : struct;
    
    void RegisterMeter(Meter meter);
    
    void UnregisterMeter(Meter meter);
    
    IReadOnlyList<Meter> GetRegisteredMeters();
    
    Task<IDictionary<string, object>> GetMetricsDefinitionsAsync(CancellationToken cancellationToken = default);
    
    bool IsMetricRegistered(string meterName, string instrumentName);
    
    void ValidateMetricName(string name);
}
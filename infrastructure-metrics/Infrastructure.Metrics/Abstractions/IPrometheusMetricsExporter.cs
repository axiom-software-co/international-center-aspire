namespace Infrastructure.Metrics.Abstractions;

public interface IPrometheusMetricsExporter
{
    Task<string> GetMetricsAsync(CancellationToken cancellationToken = default);
    
    Task<MetricsSnapshot> GetMetricsSnapshotAsync(CancellationToken cancellationToken = default);
    
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    
    Task<PrometheusExporterStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    
    void RecordCustomMetric(string name, double value, params KeyValuePair<string, object?>[] tags);
    
    void IncrementCounter(string name, params KeyValuePair<string, object?>[] tags);
    
    void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags);
    
    void SetGauge(string name, double value, params KeyValuePair<string, object?>[] tags);
}
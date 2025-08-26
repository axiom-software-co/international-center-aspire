namespace Infrastructure.Metrics.Abstractions;

public interface IServiceDiscoveryLabels
{
    Task<ServiceDiscoveryConfiguration> GenerateConfigurationAsync(
        IEnumerable<ServiceEndpoint> endpoints, CancellationToken cancellationToken = default);
        
    Task<IDictionary<string, string>> GetServiceLabelsAsync(string serviceName, 
        CancellationToken cancellationToken = default);
        
    Task<IDictionary<string, string>> GetGlobalLabelsAsync(CancellationToken cancellationToken = default);
    
    Task<string> GeneratePrometheusConfigAsync(ServiceDiscoveryConfiguration configuration,
        CancellationToken cancellationToken = default);
        
    void AddServiceLabel(string serviceName, string key, string value);
    
    void RemoveServiceLabel(string serviceName, string key);
    
    void SetGlobalLabel(string key, string value);
    
    void RemoveGlobalLabel(string key);
    
    bool ValidateLabelName(string labelName);
    
    string SanitizeLabelValue(string labelValue);
}
namespace Infrastructure.Metrics.Abstractions;

public interface IMetricsEndpointSecurity
{
    Task<bool> IsAuthorizedAsync(HttpContext context, CancellationToken cancellationToken = default);
    
    Task<SecurityValidationResult> ValidateRequestAsync(HttpRequest request, 
        CancellationToken cancellationToken = default);
        
    Task<bool> IsAllowedIpAsync(string ipAddress, CancellationToken cancellationToken = default);
    
    Task<bool> HasValidAuthenticationAsync(HttpRequest request, CancellationToken cancellationToken = default);
    
    Task LogSecurityEventAsync(SecurityEventType eventType, string clientIp, string? userAgent = null,
        string? details = null, CancellationToken cancellationToken = default);
        
    string GenerateSecurityHeaders(HttpResponse response);
    
    bool ShouldRateLimitRequest(string clientIp, string endpoint);
    
    Task<MetricsAccessAttempt> RecordAccessAttemptAsync(HttpRequest request, bool authorized,
        CancellationToken cancellationToken = default);
}
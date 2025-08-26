using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shared.Configuration;
using Microsoft.Extensions.Logging;

namespace Shared.HealthChecks;

/// <summary>
/// Aspire-native health check for validating service discovery and endpoint availability
/// Integrates seamlessly with Aspire dashboard and service mesh monitoring
/// </summary>
public sealed class AspireServiceHealthCheck : IHealthCheck
{
    private readonly ServiceDiscoveryConfiguration _serviceDiscovery;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AspireServiceHealthCheck> _logger;

    public AspireServiceHealthCheck(
        ServiceDiscoveryConfiguration serviceDiscovery,
        HttpClient httpClient,
        ILogger<AspireServiceHealthCheck> logger)
    {
        _serviceDiscovery = serviceDiscovery;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🔍 Starting Aspire service discovery health check");

            var serviceEndpoints = _serviceDiscovery.GetAllServiceEndpoints();
            var healthData = new Dictionary<string, object>();
            var healthyServices = 0;
            var totalServices = serviceEndpoints.Count;

            foreach (var (serviceName, endpoint) in serviceEndpoints)
            {
                try
                {
                    // Validate endpoint format first
                    if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
                    {
                        healthData[serviceName] = $"Invalid URL format: {endpoint}";
                        _logger.LogWarning("❌ Invalid service URL: {ServiceName} -> {Endpoint}", serviceName, endpoint);
                        continue;
                    }

                    // Check if service is reachable (with timeout)
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(5)); // 5 second timeout per service

                    var healthEndpoint = new Uri(uri, "/health");
                    var response = await _httpClient.GetAsync(healthEndpoint, cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        healthyServices++;
                        healthData[serviceName] = $"✅ Healthy ({response.StatusCode})";
                        _logger.LogDebug("✅ Service health check passed: {ServiceName}", serviceName);
                    }
                    else
                    {
                        healthData[serviceName] = $"❌ Unhealthy ({response.StatusCode})";
                        _logger.LogWarning("⚠️ Service health check failed: {ServiceName} returned {StatusCode}", 
                            serviceName, response.StatusCode);
                    }
                }
                catch (OperationCanceledException)
                {
                    healthData[serviceName] = "❌ Timeout (5s)";
                    _logger.LogWarning("⏱️ Service health check timeout: {ServiceName}", serviceName);
                }
                catch (Exception ex)
                {
                    healthData[serviceName] = $"❌ Error: {ex.Message}";
                    _logger.LogWarning(ex, "💥 Service health check error: {ServiceName}", serviceName);
                }
            }

            // Calculate overall health status
            var healthPercentage = totalServices > 0 ? (double)healthyServices / totalServices * 100 : 0;
            healthData["HealthySummary"] = $"{healthyServices}/{totalServices} services ({healthPercentage:F1}%)";
            healthData["AspireEnvironment"] = Environment.GetEnvironmentVariable("ASPIRE_ENVIRONMENT") ?? "Unknown";

            // Determine result based on healthy service percentage
            if (healthyServices == totalServices)
            {
                _logger.LogInformation("🎉 All {Count} Aspire services are healthy", totalServices);
                return new HealthCheckResult(HealthStatus.Healthy, $"All {totalServices} services healthy", null, healthData);
            }
            else if (healthyServices >= totalServices * 0.7) // 70% threshold
            {
                _logger.LogWarning("⚠️ Partial service health: {Healthy}/{Total} services", healthyServices, totalServices);
                return new HealthCheckResult(HealthStatus.Degraded, $"{healthyServices}/{totalServices} services healthy", null, healthData);
            }
            else
            {
                _logger.LogError("💥 Critical service health issue: {Healthy}/{Total} services", healthyServices, totalServices);
                return new HealthCheckResult(HealthStatus.Unhealthy, $"Only {healthyServices}/{totalServices} services healthy", null, healthData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Aspire service health check failed completely");
            return HealthCheckResult.Unhealthy($"Health check failed: {ex.Message}");
        }
    }
}
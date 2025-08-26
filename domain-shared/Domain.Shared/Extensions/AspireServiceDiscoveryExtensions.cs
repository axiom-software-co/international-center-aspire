using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Configuration;
using Shared.HealthChecks;

namespace Shared.Extensions;

/// <summary>
/// Aspire-native service discovery extensions for enhanced configuration and health monitoring
/// Provides seamless integration with Aspire orchestration and service mesh patterns
/// </summary>
public static class AspireServiceDiscoveryExtensions
{
    /// <summary>
    /// Adds Aspire-native service discovery with enhanced configuration management
    /// </summary>
    public static IServiceCollection AddAspireServiceDiscovery(this IServiceCollection services, IConfiguration configuration)
    {
        // Register service discovery configuration as singleton
        services.AddSingleton<ServiceDiscoveryConfiguration>();
        
        // Add HttpClient for health checks with proper configuration
        services.AddHttpClient<AspireServiceHealthCheck>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "AspireServiceHealthCheck/1.0");
        });

        // Add health checks with Aspire integration
        services.AddHealthChecks()
            .AddCheck<AspireServiceHealthCheck>(
                "aspire-services",
                tags: new[] { "aspire", "services", "discovery" });

        return services;
    }

    /// <summary>
    /// Validates and logs service discovery configuration during startup
    /// Useful for debugging and ensuring proper Aspire configuration
    /// </summary>
    public static IServiceCollection ValidateAspireServiceDiscovery(
        this IServiceCollection services, 
        IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetService<ILogger<ServiceDiscoveryConfiguration>>();
        var serviceDiscovery = serviceProvider.GetService<ServiceDiscoveryConfiguration>();

        if (serviceDiscovery != null && logger != null)
        {
            logger.LogInformation("🔍 Validating Aspire service discovery configuration...");
            
            var isValid = serviceDiscovery.ValidateServiceDiscovery();
            if (isValid)
            {
                logger.LogInformation("✅ All Aspire services successfully discovered");
            }
            else
            {
                logger.LogWarning("⚠️ Some Aspire services could not be discovered - check AppHost configuration");
            }
        }

        return services;
    }


    /// <summary>
    /// Logs comprehensive Aspire environment information for debugging
    /// </summary>
    public static void LogAspireEnvironmentInfo(this ILogger logger)
    {
        logger.LogInformation("🚀 Aspire Environment Information:");
        logger.LogInformation("  Environment: {Environment}", Environment.GetEnvironmentVariable("ASPIRE_ENVIRONMENT") ?? "Unknown");
        logger.LogInformation("  Allow Unsecured Transport: {AllowUnsecured}", Environment.GetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT") ?? "false");
        logger.LogInformation("  OpenTelemetry Service: {OTelService}", Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "Unknown");
        
        // Log discovered service endpoints (for debugging)
        var serviceKeys = new[]
        {
            ServiceDiscoveryConfiguration.SERVICES_API_KEY,
            ServiceDiscoveryConfiguration.SERVICES_ADMIN_API_KEY,
            ServiceDiscoveryConfiguration.NEWS_API_KEY,
            ServiceDiscoveryConfiguration.CONTACTS_API_KEY,
            ServiceDiscoveryConfiguration.RESEARCH_API_KEY,
            ServiceDiscoveryConfiguration.SEARCH_API_KEY,
            ServiceDiscoveryConfiguration.EVENTS_API_KEY,
            ServiceDiscoveryConfiguration.NEWSLETTER_API_KEY
        };

        logger.LogInformation("🔍 Service Discovery Status:");
        foreach (var key in serviceKeys)
        {
            var value = Environment.GetEnvironmentVariable(key.Replace(":", "__"));
            if (!string.IsNullOrWhiteSpace(value))
            {
                logger.LogInformation("  ✅ {Key} -> {Value}", key, value);
            }
            else
            {
                logger.LogWarning("  ❌ {Key} -> Not configured", key);
            }
        }
    }
}
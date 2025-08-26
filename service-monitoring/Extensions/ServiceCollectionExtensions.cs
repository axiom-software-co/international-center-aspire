using Infrastructure.Cache.Extensions;
using Infrastructure.Database.Extensions;
using Infrastructure.Metrics.Extensions;

namespace Service.Monitoring.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMonitoringServices(this IServiceCollection services, 
        IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        // Configure options
        services.Configure<MonitoringOptions>(configuration.GetSection(MonitoringOptions.SectionName));
        services.AddSingleton<IValidator<MonitoringOptions>, MonitoringOptionsValidator>();

        // Add infrastructure dependencies
        services.AddDatabaseInfrastructure(configuration);
        services.AddCacheInfrastructure(configuration);
        services.AddMetricsInfrastructure(configuration);

        // Register health check services
        services.AddScoped<IDatabaseHealthCheck, PostgreSqlHealthCheck>();
        services.AddScoped<IRedisHealthCheck, RedisHealthCheck>();
        
        // Register Prometheus-integrated metrics collector (singleton for meter lifecycle)
        services.AddSingleton<IMetricsCollector, PrometheusIntegratedMetricsCollector>();
        
        // Register main monitoring service
        services.AddScoped<IMonitoringService, Services.MonitoringService>();

        // Add built-in health checks
        var monitoringOptions = configuration.GetSection(MonitoringOptions.SectionName).Get<MonitoringOptions>();
        
        if (monitoringOptions?.Enabled == true)
        {
            var healthChecksBuilder = services.AddHealthChecks();

            if (monitoringOptions.Database?.Enabled == true)
            {
                var connectionString = monitoringOptions.Database.ConnectionString ?? 
                                     configuration.GetConnectionString("DefaultConnection");
                                     
                if (!string.IsNullOrEmpty(connectionString))
                {
                    healthChecksBuilder.AddNpgSql(
                        connectionString,
                        name: "postgresql",
                        timeout: monitoringOptions.Database.Timeout,
                        tags: new[] { "database", "postgresql", "readiness" });
                }
            }

            if (monitoringOptions.Redis?.Enabled == true)
            {
                var connectionString = monitoringOptions.Redis.ConnectionString ?? 
                                     configuration.GetConnectionString("Redis");
                                     
                if (!string.IsNullOrEmpty(connectionString))
                {
                    healthChecksBuilder.AddRedis(
                        connectionString,
                        name: "redis",
                        timeout: monitoringOptions.Redis.Timeout,
                        tags: new[] { "cache", "redis", "readiness" });
                }
            }
        }

        return services;
    }

    public static IServiceCollection AddMonitoringServices(this IServiceCollection services, 
        IConfiguration configuration, Action<MonitoringOptions> configureOptions)
    {
        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        services.AddMonitoringServices(configuration);
        services.Configure(configureOptions);

        return services;
    }
}
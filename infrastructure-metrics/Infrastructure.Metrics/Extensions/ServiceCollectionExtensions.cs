using Microsoft.AspNetCore.Builder;
using OpenTelemetry.Exporter;

namespace Infrastructure.Metrics.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMetricsInfrastructure(this IServiceCollection services, 
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
        services.Configure<MetricsOptions>(configuration.GetSection(MetricsOptions.SectionName));
        services.AddSingleton<IValidator<MetricsOptions>, MetricsOptionsValidator>();

        // Register core metrics services
        services.AddSingleton<ICustomMetricsRegistry, CustomMetricsRegistry>();
        services.AddScoped<IPrometheusMetricsExporter, PrometheusMetricsExporter>();
        services.AddSingleton<IMetricsEndpointSecurity, MetricsEndpointSecurity>();

        // Configure OpenTelemetry
        var metricsOptions = configuration.GetSection(MetricsOptions.SectionName).Get<MetricsOptions>();
        if (metricsOptions?.Enabled == true)
        {
            ConfigureOpenTelemetry(services, metricsOptions);
        }

        return services;
    }

    public static IServiceCollection AddMetricsInfrastructure(this IServiceCollection services, 
        IConfiguration configuration, Action<MetricsOptions> configureOptions)
    {
        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        services.AddMetricsInfrastructure(configuration);
        services.Configure(configureOptions);

        return services;
    }

    public static IApplicationBuilder UseMetricsInfrastructure(this IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        var options = app.ApplicationServices.GetRequiredService<IOptions<MetricsOptions>>().Value;
        
        if (options.Enabled)
        {
            app.UseMetricsEndpoint();
        }

        return app;
    }

    public static IApplicationBuilder UseMetricsInfrastructure(this IApplicationBuilder app, 
        Action<MetricsOptions> configureOptions)
    {
        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        app.ApplicationServices.GetRequiredService<IOptionsMonitor<MetricsOptions>>()
            .OnChange(configureOptions);

        return app.UseMetricsInfrastructure();
    }

    private static void ConfigureOpenTelemetry(IServiceCollection services, MetricsOptions metricsOptions)
    {
        services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                // Configure resource
                builder.ConfigureResource(resource =>
                {
                    resource.AddService(
                        serviceName: metricsOptions.ServiceName,
                        serviceVersion: metricsOptions.ServiceVersion,
                        serviceInstanceId: Environment.MachineName);

                    resource.AddAttributes(new Dictionary<string, object>
                    {
                        ["environment"] = metricsOptions.Environment,
                        ["host.name"] = Environment.MachineName,
                        ["process.pid"] = Environment.ProcessId
                    });

                    // Add static labels from configuration
                    foreach (var label in metricsOptions.Prometheus.StaticLabels)
                    {
                        resource.AddAttributes(new[] { new KeyValuePair<string, object>(label.Key, label.Value) });
                    }
                });

                // Add instrumentation
                builder.AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                });

                builder.AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                });

                builder.AddRuntimeInstrumentation();
                builder.AddProcessInstrumentation();

                // Add Entity Framework instrumentation if available
                try
                {
                    builder.AddEntityFrameworkCoreInstrumentation();
                }
                catch (Exception)
                {
                    // EF Core instrumentation is optional
                }

                // Add custom meters
                if (metricsOptions.CustomMetrics.EnableCustomMetrics)
                {
                    builder.AddMeter(metricsOptions.CustomMetrics.MeterName);
                }

                // Configure Prometheus exporter
                if (metricsOptions.Prometheus.ExporterType == "AspNetCore")
                {
                    builder.AddPrometheusExporter(options =>
                    {
                        options.ScrapeEndpointPath = metricsOptions.MetricsPath;
                        options.DisableTotalNameSuffixForCounters = false;
                    });
                }
                else
                {
                    builder.AddPrometheusHttpListener(options =>
                    {
                        options.UriPrefixes = new[] { $"http://localhost:9090{metricsOptions.MetricsPath}" };
                        options.ScrapeEndpointPath = metricsOptions.MetricsPath;
                    });
                }

                // Add OTLP exporter if external URL is configured
                if (!string.IsNullOrEmpty(metricsOptions.Prometheus.ExternalUrl))
                {
                    builder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(metricsOptions.Prometheus.ExternalUrl);
                        options.ExportProcessorType = ExportProcessorType.Batch;
                        options.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
                        {
                            MaxExportBatchSize = 512,
                            ScheduledDelayMilliseconds = (int)metricsOptions.ExportInterval.TotalMilliseconds,
                            ExporterTimeoutMilliseconds = (int)metricsOptions.ExportTimeout.TotalMilliseconds,
                            MaxQueueSize = 2048
                        };
                    });
                }
            });

        // Register metrics provider as singleton
        services.AddSingleton(provider =>
        {
            return provider.GetRequiredService<IMeterProvider>();
        });
    }

    public static IServiceCollection AddCustomMetrics(this IServiceCollection services, 
        Action<ICustomMetricsRegistry> configureMetrics)
    {
        if (configureMetrics == null)
        {
            throw new ArgumentNullException(nameof(configureMetrics));
        }

        services.AddSingleton<IHostedService>(provider =>
        {
            var registry = provider.GetRequiredService<ICustomMetricsRegistry>();
            var logger = provider.GetRequiredService<ILogger<CustomMetricsSetupService>>();
            
            return new CustomMetricsSetupService(registry, configureMetrics, logger);
        });

        return services;
    }

    private sealed class CustomMetricsSetupService : IHostedService
    {
        private readonly ICustomMetricsRegistry _registry;
        private readonly Action<ICustomMetricsRegistry> _configureMetrics;
        private readonly ILogger<CustomMetricsSetupService> _logger;

        public CustomMetricsSetupService(
            ICustomMetricsRegistry registry,
            Action<ICustomMetricsRegistry> configureMetrics,
            ILogger<CustomMetricsSetupService> logger)
        {
            _registry = registry;
            _configureMetrics = configureMetrics;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _configureMetrics(_registry);
                _logger.LogInformation("Custom metrics configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure custom metrics");
                throw;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
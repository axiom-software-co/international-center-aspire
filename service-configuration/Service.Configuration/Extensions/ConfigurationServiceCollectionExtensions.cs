using Service.Configuration.Abstractions;
using Service.Configuration.Options;
using Service.Configuration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Service.Configuration.Extensions;

/// <summary>
/// Extension methods for registering configuration services in dependency injection.
/// DEPENDENCY INVERSION: Provides clean registration API for variable concerns
/// OPTIONS PATTERN: Eliminates .Value calls across Services APIs
/// </summary>
public static class ConfigurationServiceCollectionExtensions
{
    /// <summary>
    /// Adds configuration services with Options pattern support to Services APIs.
    /// 
    /// REGISTRATION: IConfigurationService registered as singleton (configuration doesn't change)
    /// MEDICAL COMPLIANCE: Validates all configuration during startup
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configuration">Configuration root for binding</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddServiceConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ServiceConfigurationOptions>? configureOptions = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Configure options
        var options = new ServiceConfigurationOptions();
        configureOptions?.Invoke(options);

        // Register configuration service as singleton
        // SINGLETON DEPENDENCY: Configuration is stable and doesn't change during application lifetime
        services.AddSingleton<IConfigurationService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ConfigurationService>>();
            return new ConfigurationService(configuration, services, logger);
        });

        // Auto-register core configuration options if enabled
        if (options.AutoRegisterCoreOptions)
        {
            RegisterCoreConfigurationOptions(services, configuration);
        }

        // Add hosted service for startup validation if enabled
        if (options.ValidateOnStartup)
        {
            services.AddHostedService<ConfigurationValidationHostedService>();
        }

        return services;
    }

    /// <summary>
    /// Registers Services APIs configuration options with Options pattern.
    /// 
    /// SERVICES APIS SCOPE: Only registers options needed for current Services Public/Admin APIs
    /// MEDICAL COMPLIANCE: All options include validation for medical-grade requirements
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configuration">Configuration root for binding</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddServicesApiConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Get configuration service
        var serviceProvider = services.BuildServiceProvider();
        var configService = serviceProvider.GetRequiredService<IConfigurationService>();

        // Register Services APIs configuration options with automatic .Value binding
        // ELIMINATES .Value CALLS: Direct injection without Options<T>.Value
        configService.RegisterOptions<DatabaseOptions>(DatabaseOptions.SectionName, validateOnStart: true);
        configService.RegisterOptions<RedisOptions>(RedisOptions.SectionName, validateOnStart: true);
        configService.RegisterOptions<LoggingOptions>(LoggingOptions.SectionName, validateOnStart: true);
        configService.RegisterOptions<SecurityOptions>(SecurityOptions.SectionName, validateOnStart: true);

        return services;
    }

    /// <summary>
    /// Registers Public Gateway specific configuration options.
    /// 
    /// PUBLIC GATEWAY SCOPE: Only registers options needed for Public Gateway (1000 req/min)
    /// WEBSITE INTEGRATION: Supports public website access through gateway
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configuration">Configuration root for binding</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddPublicGatewayConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var serviceProvider = services.BuildServiceProvider();
        var configService = serviceProvider.GetRequiredService<IConfigurationService>();

        // Public Gateway specific options
        configService.RegisterOptions<RedisOptions>(RedisOptions.SectionName, validateOnStart: true);
        configService.RegisterOptions<SecurityOptions>(SecurityOptions.SectionName, validateOnStart: true);
        configService.RegisterOptions<LoggingOptions>(LoggingOptions.SectionName, validateOnStart: true);

        return services;
    }

    /// <summary>
    /// Registers Admin Gateway specific configuration options.
    /// 
    /// ADMIN GATEWAY SCOPE: Registers options needed for Admin Gateway (100 req/min, RBAC)
    /// FUTURE ADMIN PORTAL: Configuration ready for when admin portal is implemented
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configuration">Configuration root for binding</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAdminGatewayConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var serviceProvider = services.BuildServiceProvider();
        var configService = serviceProvider.GetRequiredService<IConfigurationService>();

        // Admin Gateway specific options (includes all security features)
        configService.RegisterOptions<SecurityOptions>(SecurityOptions.SectionName, validateOnStart: true);
        configService.RegisterOptions<LoggingOptions>(LoggingOptions.SectionName, validateOnStart: true);

        return services;
    }

    /// <summary>
    /// Registers core configuration options that all projects need.
    /// </summary>
    private static void RegisterCoreConfigurationOptions(IServiceCollection services, IConfiguration configuration)
    {
        // Core options that every service needs
        services.Configure<LoggingOptions>(configuration.GetSection(LoggingOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
    }
}

/// <summary>
/// Configuration options for Service.Configuration setup.
/// </summary>
public sealed class ServiceConfigurationOptions
{
    /// <summary>
    /// Automatically register core configuration options during setup.
    /// DEFAULT: true for convenience
    /// </summary>
    public bool AutoRegisterCoreOptions { get; set; } = true;

    /// <summary>
    /// Validate all configuration during application startup.
    /// MEDICAL COMPLIANCE: Should be true to catch configuration issues early
    /// </summary>
    public bool ValidateOnStartup { get; set; } = true;

    /// <summary>
    /// Fail application startup if configuration validation fails.
    /// MEDICAL COMPLIANCE: Should be true to prevent misconfigured deployments
    /// </summary>
    public bool FailOnValidationErrors { get; set; } = true;
}

/// <summary>
/// Hosted service that validates configuration during application startup.
/// MEDICAL COMPLIANCE: Ensures configuration is valid before accepting requests
/// </summary>
internal sealed class ConfigurationValidationHostedService : IHostedService
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ConfigurationValidationHostedService> _logger;
    private readonly ServiceConfigurationOptions _options;

    public ConfigurationValidationHostedService(
        IConfigurationService configurationService,
        ILogger<ConfigurationValidationHostedService> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = new ServiceConfigurationOptions(); // TODO: Inject configured options
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting configuration validation during application startup");

        try
        {
            var validationResult = _configurationService.ValidateAllOptions();
            
            if (!validationResult.IsValid)
            {
                _logger.LogError("Configuration validation failed with {ErrorCount} errors: {Errors}",
                    validationResult.Errors.Count, string.Join("; ", validationResult.Errors));

                if (_options.FailOnValidationErrors)
                {
                    throw new InvalidOperationException(
                        $"Application startup failed due to configuration validation errors: {string.Join("; ", validationResult.Errors)}");
                }
            }
            else
            {
                _logger.LogInformation("Configuration validation completed successfully");
            }

            // Log environment context for audit purposes
            var environmentContext = _configurationService.GetEnvironmentContext();
            _logger.LogInformation("Application starting in {Environment} environment with medical compliance: {MedicalCompliance}",
                environmentContext.Environment, environmentContext.MedicalComplianceEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error during configuration validation");
            
            if (_options.FailOnValidationErrors)
            {
                throw;
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
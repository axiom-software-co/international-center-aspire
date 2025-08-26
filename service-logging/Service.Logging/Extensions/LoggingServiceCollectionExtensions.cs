using CorrelationId;
using Service.Configuration.Abstractions;
using Service.Logging.Abstractions;
using Service.Logging.Options;
using Service.Logging.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers.CorrelationId;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Settings.Configuration;

namespace Service.Logging.Extensions;

/// <summary>
/// Extension methods for registering structured logging services in dependency injection.
/// DEPENDENCY INVERSION: Provides clean registration API for structured logging concerns
/// MEDICAL COMPLIANCE: Configures medical-grade logging with correlation IDs and audit trails
/// SERVICES APIS SCOPE: Logging patterns for Services Public/Admin APIs
/// </summary>
public static class LoggingServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure structured logging with correlation ID propagation to Services APIs.
    /// 
    /// REGISTRATION: ILoggingService registered as singleton with Serilog integration
    /// MEDICAL COMPLIANCE: Validates all logging configuration during startup
    /// CORRELATION: Automatic correlation ID propagation across request boundaries
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configuration">Configuration root for binding</param>
    /// <param name="configureOptions">Optional structured logging configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddInfrastructureLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<InfrastructureLoggingOptions>? configureOptions = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Configure options
        var options = new InfrastructureLoggingOptions();
        configureOptions?.Invoke(options);

        // Register correlation ID services first
        services.AddCorrelationId(correlationOptions =>
        {
            correlationOptions.RequestHeader = options.CorrelationIdHeaderName;
            correlationOptions.ResponseHeader = options.CorrelationIdHeaderName;
            correlationOptions.IncludeInResponse = options.IncludeCorrelationIdInResponse;
        });

        // Configure structured logging options with validation
        var configService = services.BuildServiceProvider().GetService<IConfigurationService>();
        if (configService != null && options.AutoRegisterStructuredOptions)
        {
            configService.RegisterOptions<StructuredLoggingOptions>(
                StructuredLoggingOptions.SectionName, 
                validateOnStart: options.ValidateOnStartup);
        }
        else
        {
            // Fallback registration without configuration service
            services.Configure<StructuredLoggingOptions>(
                configuration.GetSection(StructuredLoggingOptions.SectionName));
        }

        // Configure Serilog for structured logging
        if (options.ConfigureSerilog)
        {
            ConfigureSerilogLogging(services, configuration, options);
        }

        // Register logging service as singleton
        // SINGLETON DEPENDENCY: Logging service is stable and thread-safe
        services.AddSingleton<ILoggingService, LoggingService>();

        // Add hosted service for startup validation if enabled
        if (options.ValidateOnStartup)
        {
            services.AddHostedService<LoggingValidationHostedService>();
        }

        // Add logging enrichment services
        if (options.EnableLoggingEnrichment)
        {
            services.AddSingleton<ILoggingEnrichmentService, LoggingEnrichmentService>();
        }

        return services;
    }

    /// <summary>
    /// Adds Services APIs specific structured logging configuration.
    /// 
    /// SERVICES APIS SCOPE: Logging patterns optimized for Services Public/Admin APIs
    /// MEDICAL COMPLIANCE: Request/response logging with correlation and audit trails
    /// PERFORMANCE: Performance logging for API request timing and metrics
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configuration">Configuration root for binding</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddServicesApiLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Add base infrastructure logging
        services.AddInfrastructureLogging(configuration, options =>
        {
            options.EnableRequestResponseLogging = true;
            options.EnablePerformanceLogging = true;
            options.EnableMedicalComplianceLogging = true;
            options.ValidateOnStartup = true;
        });

        // Add Services APIs specific logging middleware
        services.AddTransient<ServicesApiLoggingMiddleware>();
        
        // Add performance tracking for Services APIs
        services.AddSingleton<IApiPerformanceTracker, ApiPerformanceTracker>();

        return services;
    }

    /// <summary>
    /// Adds Public Gateway specific structured logging configuration.
    /// 
    /// PUBLIC GATEWAY SCOPE: Logging patterns for Public Gateway (1000 req/min)
    /// RATE LIMITING: Request rate limiting logging with correlation
    /// ANONYMOUS ACCESS: Public access logging without sensitive user data
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configuration">Configuration root for binding</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddPublicGatewayLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Add base infrastructure logging with public gateway settings
        services.AddInfrastructureLogging(configuration, options =>
        {
            options.EnableRequestResponseLogging = true;
            options.EnableRateLimitingLogging = true;
            options.EnableAnonymousRequestLogging = true;
            options.SensitiveDataRedactionStrict = false; // Public data, less strict
        });

        // Add rate limiting logging
        services.AddSingleton<IRateLimitingLogger, RateLimitingLogger>();

        return services;
    }

    /// <summary>
    /// Adds Admin Gateway specific structured logging configuration.
    /// 
    /// ADMIN GATEWAY SCOPE: Logging patterns for Admin Gateway (100 req/min, RBAC)
    /// AUTHENTICATION: Authentication/authorization logging with correlation
    /// MEDICAL AUDIT: Complete audit trail for admin actions with medical compliance
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configuration">Configuration root for binding</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAdminGatewayLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Add base infrastructure logging with admin gateway settings
        services.AddInfrastructureLogging(configuration, options =>
        {
            options.EnableRequestResponseLogging = true;
            options.EnableAuthenticationLogging = true;
            options.EnableAuthorizationLogging = true;
            options.EnableMedicalComplianceLogging = true;
            options.SensitiveDataRedactionStrict = true; // Strict redaction for admin
            options.ValidateOnStartup = true;
        });

        // Add authentication/authorization logging
        services.AddSingleton<IAuthenticationLogger, AuthenticationLogger>();
        services.AddSingleton<IAuthorizationLogger, AuthorizationLogger>();

        return services;
    }

    /// <summary>
    /// Configures Serilog for structured logging with medical-grade compliance.
    /// </summary>
    private static void ConfigureSerilogLogging(
        IServiceCollection services,
        IConfiguration configuration,
        InfrastructureLoggingOptions options)
    {
        // Configure Serilog with structured logging
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithCorrelationId()
            .WriteTo.Console(new CompactJsonFormatter())
            .WriteTo.File(
                path: "logs/infrastructure-.log",
                formatter: new CompactJsonFormatter(),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: options.LogFileRetentionDays,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

        // Replace default logging with Serilog
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(dispose: true);
        });
    }
}

/// <summary>
/// Configuration options for Service.Logging setup.
/// MEDICAL COMPLIANCE: Medical-grade logging configuration options
/// </summary>
public sealed class InfrastructureLoggingOptions
{
    /// <summary>
    /// Automatically register structured logging options during setup.
    /// DEFAULT: true for convenience
    /// </summary>
    public bool AutoRegisterStructuredOptions { get; set; } = true;

    /// <summary>
    /// Validate all logging configuration during application startup.
    /// MEDICAL COMPLIANCE: Should be true to catch logging configuration issues early
    /// </summary>
    public bool ValidateOnStartup { get; set; } = true;

    /// <summary>
    /// Configure Serilog automatically during setup.
    /// DEFAULT: true for structured logging
    /// </summary>
    public bool ConfigureSerilog { get; set; } = true;

    /// <summary>
    /// Correlation ID HTTP header name.
    /// SERVICES APIS: Header for correlation across Services APIs
    /// </summary>
    public string CorrelationIdHeaderName { get; set; } = "X-Correlation-ID";

    /// <summary>
    /// Include correlation ID in HTTP response headers.
    /// DEBUGGING: Useful for request tracing
    /// </summary>
    public bool IncludeCorrelationIdInResponse { get; set; } = true;

    /// <summary>
    /// Enable request/response logging for Services APIs.
    /// SERVICES APIS: HTTP request/response logging patterns
    /// </summary>
    public bool EnableRequestResponseLogging { get; set; } = true;

    /// <summary>
    /// Enable performance logging for API requests.
    /// MONITORING: Request timing and performance metrics
    /// </summary>
    public bool EnablePerformanceLogging { get; set; } = true;

    /// <summary>
    /// Enable authentication logging for Admin APIs.
    /// ADMIN GATEWAY: Authentication event logging
    /// </summary>
    public bool EnableAuthenticationLogging { get; set; } = false;

    /// <summary>
    /// Enable authorization logging for Admin APIs.
    /// ADMIN GATEWAY: Authorization decision logging
    /// </summary>
    public bool EnableAuthorizationLogging { get; set; } = false;

    /// <summary>
    /// Enable rate limiting logging for Public Gateway.
    /// PUBLIC GATEWAY: Rate limiting event logging
    /// </summary>
    public bool EnableRateLimitingLogging { get; set; } = false;

    /// <summary>
    /// Enable medical compliance logging features.
    /// MEDICAL COMPLIANCE: Medical-grade audit logging
    /// </summary>
    public bool EnableMedicalComplianceLogging { get; set; } = true;

    /// <summary>
    /// Enable anonymous request logging for Public Gateway.
    /// PUBLIC GATEWAY: Anonymous request patterns
    /// </summary>
    public bool EnableAnonymousRequestLogging { get; set; } = false;

    /// <summary>
    /// Enable logging enrichment services.
    /// ENRICHMENT: Additional structured data in logs
    /// </summary>
    public bool EnableLoggingEnrichment { get; set; } = true;

    /// <summary>
    /// Strict sensitive data redaction mode.
    /// MEDICAL COMPLIANCE: Enhanced PII/PHI protection
    /// </summary>
    public bool SensitiveDataRedactionStrict { get; set; } = true;

    /// <summary>
    /// Log file retention period in days.
    /// MEDICAL COMPLIANCE: Long-term log retention for audit
    /// </summary>
    public int LogFileRetentionDays { get; set; } = 2555; // ~7 years
}

/// <summary>
/// Hosted service that validates logging configuration during application startup.
/// MEDICAL COMPLIANCE: Ensures logging is properly configured before accepting requests
/// </summary>
internal sealed class LoggingValidationHostedService : IHostedService
{
    private readonly ILoggingService _loggingService;
    private readonly ILogger<LoggingValidationHostedService> _logger;
    private readonly InfrastructureLoggingOptions _options;

    public LoggingValidationHostedService(
        ILoggingService loggingService,
        ILogger<LoggingValidationHostedService> logger)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = new InfrastructureLoggingOptions(); // TODO: Inject configured options
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting structured logging validation during application startup");

        try
        {
            // Configure structured logging first
            _loggingService.ConfigureStructuredLogging();

            // Validate logging configuration
            var validationResult = _loggingService.ValidateLoggingConfiguration();
            
            if (!validationResult.IsValid)
            {
                _logger.LogError("Logging configuration validation failed with {ErrorCount} errors: {Errors}",
                    validationResult.Errors.Count, string.Join("; ", validationResult.Errors));

                if (_options.ValidateOnStartup)
                {
                    throw new InvalidOperationException(
                        $"Application startup failed due to logging validation errors: {string.Join("; ", validationResult.Errors)}");
                }
            }
            else
            {
                _logger.LogInformation("Logging configuration validation completed successfully with {FeatureCount} validated features: {Features}",
                    validationResult.ValidatedFeatures.Count, string.Join(", ", validationResult.ValidatedFeatures));
            }

            // Log environment context for audit purposes
            var environmentContext = _loggingService.GetLoggingEnvironmentContext();
            _logger.LogInformation("Structured logging started in {Environment} environment with medical compliance: {MedicalCompliance}, correlation IDs: {CorrelationIds}, retention: {RetentionDays} days",
                environmentContext.Environment, 
                environmentContext.MedicalComplianceLoggingEnabled, 
                environmentContext.CorrelationIdEnabled,
                environmentContext.LogRetentionDays);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error during structured logging validation");
            
            if (_options.ValidateOnStartup)
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

// Placeholder service interfaces for specialized logging components
// These would be implemented based on specific needs

/// <summary>Logging enrichment service for additional structured data</summary>
public interface ILoggingEnrichmentService
{
    void EnrichLogContext(IDictionary<string, object> properties);
}

/// <summary>API performance tracking service</summary>
public interface IApiPerformanceTracker
{
    void TrackRequestPerformance(string requestPath, TimeSpan duration, int statusCode);
}

/// <summary>Rate limiting logging service for Public Gateway</summary>
public interface IRateLimitingLogger
{
    void LogRateLimitResult(string clientId, bool allowed, int currentCount, int limit);
}

/// <summary>Authentication logging service for Admin Gateway</summary>
public interface IAuthenticationLogger
{
    void LogAuthenticationAttempt(string userId, string method, bool success);
}

/// <summary>Authorization logging service for Admin Gateway</summary>
public interface IAuthorizationLogger
{
    void LogAuthorizationDecision(string userId, string resource, string policy, bool authorized);
}

// Placeholder middleware class
internal sealed class ServicesApiLoggingMiddleware
{
    // Implementation would handle request/response logging for Services APIs
}

// Placeholder implementations (these would be fully implemented based on requirements)
internal sealed class LoggingEnrichmentService : ILoggingEnrichmentService
{
    public void EnrichLogContext(IDictionary<string, object> properties) { }
}

internal sealed class ApiPerformanceTracker : IApiPerformanceTracker
{
    public void TrackRequestPerformance(string requestPath, TimeSpan duration, int statusCode) { }
}

internal sealed class RateLimitingLogger : IRateLimitingLogger
{
    public void LogRateLimitResult(string clientId, bool allowed, int currentCount, int limit) { }
}

internal sealed class AuthenticationLogger : IAuthenticationLogger
{
    public void LogAuthenticationAttempt(string userId, string method, bool success) { }
}

internal sealed class AuthorizationLogger : IAuthorizationLogger
{
    public void LogAuthorizationDecision(string userId, string resource, string policy, bool authorized) { }
}
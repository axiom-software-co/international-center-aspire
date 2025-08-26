using FluentValidation;
using Infrastructure.Cache.Abstractions;
using Infrastructure.Cache.Base;
using Infrastructure.Cache.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Cache.Extensions;

/// <summary>
/// Generic dependency injection extensions for Redis caching and rate limiting infrastructure.
/// INFRASTRUCTURE: Generic DI patterns reusable by any domain
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add generic Redis caching infrastructure services.
    /// INFRASTRUCTURE: Generic Redis infrastructure for any domain
    /// </summary>
    public static IServiceCollection AddRedisInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register Redis connection options with validation
        services.ConfigureWithValidation<RedisConnectionOptions, RedisConnectionOptionsValidator>(
            configuration, RedisConnectionOptions.SectionName);

        // Register core Redis infrastructure services
        services.AddSingleton<IRedisConnectionFactory, DefaultRedisConnectionFactory>();
        services.AddSingleton<IDistributedCacheService, DefaultDistributedCacheService>();
        services.AddSingleton<IRateLimitingService, DefaultRateLimitingService>();

        // Register health checks if enabled
        services.AddSingleton<IHostedService, RedisHealthCheckService>();

        return services;
    }

    /// <summary>
    /// Add generic distributed caching services only.
    /// INFRASTRUCTURE: Distributed caching for any domain
    /// </summary>
    public static IServiceCollection AddDistributedCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register Redis connection options with validation
        services.ConfigureWithValidation<RedisConnectionOptions, RedisConnectionOptionsValidator>(
            configuration, RedisConnectionOptions.SectionName);

        // Register distributed caching services only
        services.AddSingleton<IRedisConnectionFactory, DefaultRedisConnectionFactory>();
        services.AddSingleton<IDistributedCacheService, DefaultDistributedCacheService>();

        return services;
    }

    /// <summary>
    /// Add generic rate limiting services only.
    /// INFRASTRUCTURE: Rate limiting for any domain
    /// </summary>
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register Redis connection options with validation
        services.ConfigureWithValidation<RedisConnectionOptions, RedisConnectionOptionsValidator>(
            configuration, RedisConnectionOptions.SectionName);

        // Register rate limiting services only
        services.AddSingleton<IRedisConnectionFactory, DefaultRedisConnectionFactory>();
        services.AddSingleton<IRateLimitingService, DefaultRateLimitingService>();

        return services;
    }

    /// <summary>
    /// Configure options with automatic validation using FluentValidation.
    /// INFRASTRUCTURE: Generic Options pattern with validation
    /// </summary>
    private static IServiceCollection ConfigureWithValidation<TOptions, TValidator>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
        where TValidator : class, IValidator<TOptions>
    {
        // Register the validator
        services.AddSingleton<IValidator<TOptions>, TValidator>();

        // Configure options from configuration section
        services.Configure<TOptions>(configuration.GetSection(sectionName));

        // Add validation on Options creation
        services.AddSingleton<IValidateOptions<TOptions>>(provider =>
        {
            var validator = provider.GetRequiredService<IValidator<TOptions>>();
            return new FluentValidationOptionsValidator<TOptions>(validator);
        });

        return services;
    }
}

/// <summary>
/// Default Redis connection factory implementation.
/// INFRASTRUCTURE: Generic Redis connection management
/// </summary>
internal sealed class DefaultRedisConnectionFactory : BaseRedisConnectionFactory
{
    public DefaultRedisConnectionFactory(
        IOptions<RedisConnectionOptions> options,
        ILogger<DefaultRedisConnectionFactory> logger)
        : base(options, logger)
    {
    }
}

/// <summary>
/// Default distributed cache service implementation.
/// INFRASTRUCTURE: Generic distributed caching
/// </summary>
internal sealed class DefaultDistributedCacheService : BaseDistributedCacheService
{
    public DefaultDistributedCacheService(
        IRedisConnectionFactory connectionFactory,
        IOptions<RedisConnectionOptions> options,
        ILogger<DefaultDistributedCacheService> logger)
        : base(connectionFactory, options, logger)
    {
    }
}

/// <summary>
/// Default rate limiting service implementation.
/// INFRASTRUCTURE: Generic rate limiting
/// </summary>
internal sealed class DefaultRateLimitingService : BaseRateLimitingService
{
    public DefaultRateLimitingService(
        IRedisConnectionFactory connectionFactory,
        IOptions<RedisConnectionOptions> options,
        ILogger<DefaultRateLimitingService> logger)
        : base(connectionFactory, options, logger)
    {
    }
}

/// <summary>
/// FluentValidation integration for Options pattern.
/// INFRASTRUCTURE: Generic validation integration
/// </summary>
internal sealed class FluentValidationOptionsValidator<TOptions> : IValidateOptions<TOptions>
    where TOptions : class
{
    private readonly IValidator<TOptions> _validator;

    public FluentValidationOptionsValidator(IValidator<TOptions> validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var validationResult = _validator.Validate(options);

        if (validationResult.IsValid)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = validationResult.Errors
            .Select(error => $"{error.PropertyName}: {error.ErrorMessage}")
            .ToArray();

        return ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>
/// Redis health check background service.
/// INFRASTRUCTURE: Generic health monitoring
/// </summary>
internal sealed class RedisHealthCheckService : BackgroundService
{
    private readonly IRedisConnectionFactory _connectionFactory;
    private readonly RedisHealthCheckOptions _options;
    private readonly ILogger<RedisHealthCheckService> _logger;

    public RedisHealthCheckService(
        IRedisConnectionFactory connectionFactory,
        IOptions<RedisConnectionOptions> options,
        ILogger<RedisHealthCheckService> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _options = options.Value?.HealthCheck ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableHealthChecks)
        {
            _logger.LogInformation("Redis health checks are disabled");
            return;
        }

        var interval = TimeSpan.FromSeconds(_options.IntervalSeconds);
        var failureCount = 0;

        _logger.LogInformation("Starting Redis health check service with {IntervalSeconds}s interval", _options.IntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var isHealthy = await _connectionFactory.TestConnectionAsync(stoppingToken);
                
                if (isHealthy)
                {
                    if (failureCount > 0)
                    {
                        _logger.LogInformation("Redis health check recovered after {FailureCount} failures", failureCount);
                        failureCount = 0;
                    }
                    else
                    {
                        _logger.LogDebug("Redis health check passed");
                    }
                }
                else
                {
                    failureCount++;
                    _logger.LogWarning("Redis health check failed (failure {FailureCount}/{Threshold})",
                        failureCount, _options.FailureThreshold);

                    if (failureCount >= _options.FailureThreshold)
                    {
                        _logger.LogError("Redis health check failure threshold exceeded. Service may be unhealthy.");
                    }
                }

                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex, "Redis health check encountered an error (failure {FailureCount}/{Threshold})",
                    failureCount, _options.FailureThreshold);

                await Task.Delay(interval, stoppingToken);
            }
        }

        _logger.LogInformation("Redis health check service stopped");
    }
}
using FluentValidation;

namespace Infrastructure.Cache.Options;

/// <summary>
/// Generic Redis connection configuration options for caching and rate limiting infrastructure.
/// INFRASTRUCTURE: Generic Redis connection patterns reusable by any domain
/// DEPENDENCY INVERSION: No knowledge of specific domains (Services, News, Events, etc.)
/// </summary>
public sealed class RedisConnectionOptions
{
    public const string SectionName = "RedisConnection";

    /// <summary>
    /// Primary Redis connection string
    /// INFRASTRUCTURE: Generic Redis connection for any domain
    /// </summary>
    public required string ConnectionString { get; init; }

    /// <summary>
    /// Redis database number for primary operations
    /// INFRASTRUCTURE: Generic database selection
    /// </summary>
    public int Database { get; init; } = 0;

    /// <summary>
    /// Application name for Redis connections
    /// MONITORING: Application identifier in Redis connections
    /// </summary>
    public string ApplicationName { get; init; } = "GenericApplication";

    /// <summary>
    /// Command timeout in seconds
    /// PERFORMANCE: Redis command execution timeout
    /// </summary>
    public int CommandTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Connection timeout in seconds
    /// RESILIENCE: Redis connection establishment timeout
    /// </summary>
    public int ConnectTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Connection pool configuration
    /// CONNECTION POOLING: Generic pooling settings
    /// </summary>
    public RedisPoolOptions ConnectionPool { get; init; } = new();

    /// <summary>
    /// Retry policy configuration
    /// RESILIENCE: Generic retry patterns for transient failures
    /// </summary>
    public RedisRetryOptions Retry { get; init; } = new();

    /// <summary>
    /// Health check configuration
    /// MONITORING: Generic health check settings
    /// </summary>
    public RedisHealthCheckOptions HealthCheck { get; init; } = new();

    /// <summary>
    /// Distributed cache configuration
    /// CACHING: Distributed cache patterns
    /// </summary>
    public DistributedCacheOptions DistributedCache { get; init; } = new();

    /// <summary>
    /// Rate limiting configuration
    /// RATE LIMITING: Generic rate limiting patterns
    /// </summary>
    public RateLimitingCacheOptions RateLimiting { get; init; } = new();
}

/// <summary>
/// Generic Redis connection pool configuration.
/// INFRASTRUCTURE: Connection pooling patterns for Redis
/// </summary>
public sealed class RedisPoolOptions
{
    /// <summary>
    /// Maximum pool size
    /// CONNECTION POOLING: Maximum number of connections
    /// </summary>
    public int MaxPoolSize { get; init; } = 100;

    /// <summary>
    /// Connection idle timeout in seconds
    /// CONNECTION POOLING: Idle connection timeout
    /// </summary>
    public int IdleTimeoutSeconds { get; init; } = 300; // 5 minutes

    /// <summary>
    /// Enable connection pooling
    /// PERFORMANCE: Connection pooling for high-performance operations
    /// </summary>
    public bool EnablePooling { get; init; } = true;

    /// <summary>
    /// Allow admin operations
    /// SECURITY: Enable administrative Redis commands
    /// </summary>
    public bool AllowAdmin { get; init; } = false;

    /// <summary>
    /// Abort connection on connect fail
    /// RESILIENCE: Connection failure handling
    /// </summary>
    public bool AbortOnConnectFail { get; init; } = false;
}

/// <summary>
/// Generic Redis retry policy configuration.
/// INFRASTRUCTURE: Resilience patterns for transient failures
/// </summary>
public sealed class RedisRetryOptions
{
    /// <summary>
    /// Maximum retry attempts
    /// RESILIENCE: Retry policy for transient failures
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Retry delay in seconds
    /// RESILIENCE: Delay between retry attempts
    /// </summary>
    public int RetryDelaySeconds { get; init; } = 2;

    /// <summary>
    /// Enable exponential backoff
    /// RESILIENCE: Exponential backoff for retry attempts
    /// </summary>
    public bool EnableExponentialBackoff { get; init; } = true;

    /// <summary>
    /// Jitter for retry delays
    /// RESILIENCE: Random jitter to prevent thundering herd
    /// </summary>
    public bool EnableJitter { get; init; } = true;
}

/// <summary>
/// Generic Redis health check configuration.
/// INFRASTRUCTURE: Health check patterns for Redis
/// </summary>
public sealed class RedisHealthCheckOptions
{
    /// <summary>
    /// Enable Redis health checks
    /// MONITORING: Health check configuration
    /// </summary>
    public bool EnableHealthChecks { get; init; } = true;

    /// <summary>
    /// Health check timeout in seconds
    /// MONITORING: Timeout for Redis health checks
    /// </summary>
    public int TimeoutSeconds { get; init; } = 10;

    /// <summary>
    /// Health check interval in seconds
    /// MONITORING: How often to perform health checks
    /// </summary>
    public int IntervalSeconds { get; init; } = 30;

    /// <summary>
    /// Health check failure threshold
    /// MONITORING: Number of failures before marking unhealthy
    /// </summary>
    public int FailureThreshold { get; init; } = 3;
}

/// <summary>
/// Generic distributed cache configuration.
/// INFRASTRUCTURE: Distributed caching patterns for any domain
/// </summary>
public sealed class DistributedCacheOptions
{
    /// <summary>
    /// Default cache expiration in minutes
    /// CACHING: Default TTL for cached items
    /// </summary>
    public int DefaultExpirationMinutes { get; init; } = 60;

    /// <summary>
    /// Enable sliding expiration
    /// CACHING: Reset expiration on access
    /// </summary>
    public bool EnableSlidingExpiration { get; init; } = true;

    /// <summary>
    /// Maximum cache key length
    /// CACHING: Cache key validation
    /// </summary>
    public int MaxKeyLength { get; init; } = 250;

    /// <summary>
    /// Cache key prefix for namespacing
    /// CACHING: Key namespacing for multi-tenant scenarios
    /// </summary>
    public string KeyPrefix { get; init; } = "";

    /// <summary>
    /// Enable cache compression
    /// PERFORMANCE: Compress cached data
    /// </summary>
    public bool EnableCompression { get; init; } = false;

    /// <summary>
    /// Compression threshold in bytes
    /// PERFORMANCE: Compress data above this size
    /// </summary>
    public int CompressionThresholdBytes { get; init; } = 1024;
}

/// <summary>
/// Generic rate limiting cache configuration.
/// INFRASTRUCTURE: Rate limiting patterns for any domain
/// </summary>
public sealed class RateLimitingCacheOptions
{
    /// <summary>
    /// Rate limiting database number (separate from general cache)
    /// RATE LIMITING: Dedicated database for rate limiting
    /// </summary>
    public int Database { get; init; } = 1;

    /// <summary>
    /// Default rate limiting window in minutes
    /// RATE LIMITING: Time window for rate calculations
    /// </summary>
    public int DefaultWindowMinutes { get; init; } = 1;

    /// <summary>
    /// Rate limit key prefix
    /// RATE LIMITING: Key namespacing for rate limit entries
    /// </summary>
    public string KeyPrefix { get; init; } = "rate_limit:";

    /// <summary>
    /// Enable distributed rate limiting
    /// SCALABILITY: Rate limiting across multiple instances
    /// </summary>
    public bool EnableDistributedRateLimiting { get; init; } = true;

    /// <summary>
    /// Rate limit precision in seconds
    /// RATE LIMITING: Precision for rate limit calculations
    /// </summary>
    public int PrecisionSeconds { get; init; } = 10;

    /// <summary>
    /// Enable rate limit metrics
    /// MONITORING: Rate limiting metrics collection
    /// </summary>
    public bool EnableMetrics { get; init; } = true;
}

/// <summary>
/// FluentValidation validator for RedisConnectionOptions.
/// INFRASTRUCTURE: Ensures Redis configuration is valid
/// </summary>
public sealed class RedisConnectionOptionsValidator : AbstractValidator<RedisConnectionOptions>
{
    public RedisConnectionOptionsValidator()
    {
        RuleFor(x => x.ConnectionString)
            .NotEmpty()
            .WithMessage("Redis connection string is required");

        RuleFor(x => x.Database)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(15)
            .WithMessage("Redis database must be between 0 and 15");

        RuleFor(x => x.CommandTimeoutSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(300) // 5 minutes max
            .WithMessage("Command timeout must be between 1 and 300 seconds");

        RuleFor(x => x.ConnectTimeoutSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(120) // 2 minutes max
            .WithMessage("Connect timeout must be between 1 and 120 seconds");

        RuleFor(x => x.ConnectionPool.MaxPoolSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
            .WithMessage("Max pool size must be between 1 and 1000");

        RuleFor(x => x.Retry.MaxRetryAttempts)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(10)
            .WithMessage("Max retry attempts must be between 0 and 10");

        RuleFor(x => x.Retry.RetryDelaySeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(60)
            .WithMessage("Retry delay must be between 1 and 60 seconds");

        RuleFor(x => x.HealthCheck.TimeoutSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(60)
            .WithMessage("Health check timeout must be between 1 and 60 seconds");

        RuleFor(x => x.DistributedCache.DefaultExpirationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(1440) // 24 hours max
            .WithMessage("Default expiration must be between 1 and 1440 minutes");

        RuleFor(x => x.DistributedCache.MaxKeyLength)
            .GreaterThan(0)
            .LessThanOrEqualTo(512)
            .WithMessage("Max key length must be between 1 and 512 characters");

        RuleFor(x => x.RateLimiting.Database)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(15)
            .WithMessage("Rate limiting database must be between 0 and 15");

        RuleFor(x => x.RateLimiting.DefaultWindowMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(60)
            .WithMessage("Rate limiting window must be between 1 and 60 minutes");
    }
}
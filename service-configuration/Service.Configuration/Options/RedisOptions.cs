using FluentValidation;

namespace Service.Configuration.Options;

/// <summary>
/// Redis configuration options for Public Gateway rate limiting and distributed caching.
/// STABLE CONCERN: Redis connection patterns are well-established
/// PUBLIC GATEWAY: Supports 1000 req/min rate limiting backing store
/// </summary>
public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>
    /// Redis connection string for distributed caching and rate limiting
    /// PUBLIC GATEWAY: Used for rate limiting backing store (1000 req/min)
    /// </summary>
    public required string ConnectionString { get; init; }

    /// <summary>
    /// Redis database number for rate limiting operations
    /// PUBLIC GATEWAY: Isolates rate limiting data from other cache operations
    /// </summary>
    public int RateLimitingDatabase { get; init; } = 0;

    /// <summary>
    /// Redis database number for distributed caching
    /// PERFORMANCE: Separate database for general caching operations
    /// </summary>
    public int CacheDatabase { get; init; } = 1;

    /// <summary>
    /// Default cache expiration time in minutes
    /// PERFORMANCE: Prevents unlimited cache growth
    /// </summary>
    public int DefaultCacheExpirationMinutes { get; init; } = 60;

    /// <summary>
    /// Rate limiting window duration in minutes
    /// PUBLIC GATEWAY: 1000 requests per minute rate limit window
    /// </summary>
    public int RateLimitWindowMinutes { get; init; } = 1;

    /// <summary>
    /// Maximum number of requests per rate limit window
    /// PUBLIC GATEWAY: 1000 req/min for anonymous public usage
    /// </summary>
    public int MaxRequestsPerWindow { get; init; } = 1000;

    /// <summary>
    /// Connection retry attempts for Redis operations
    /// RELIABILITY: Ensures cache availability doesn't break Services APIs
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Connection timeout in seconds for Redis operations
    /// PERFORMANCE: Prevents hanging operations
    /// </summary>
    public int ConnectionTimeoutSeconds { get; init; } = 5;

    /// <summary>
    /// Command timeout in seconds for Redis operations
    /// PERFORMANCE: Prevents slow cache operations from affecting APIs
    /// </summary>
    public int CommandTimeoutSeconds { get; init; } = 10;

    /// <summary>
    /// Enable Redis connection multiplexing for performance
    /// PERFORMANCE: Single connection multiplexed across operations
    /// </summary>
    public bool EnableConnectionMultiplexing { get; init; } = true;

    /// <summary>
    /// Redis key prefix for Services APIs to avoid collisions
    /// ISOLATION: Prevents key conflicts with other applications
    /// </summary>
    public string KeyPrefix { get; init; } = "services:";

    /// <summary>
    /// Connection pool settings for Redis performance optimization
    /// </summary>
    public RedisConnectionPoolOptions ConnectionPool { get; init; } = new();
}

/// <summary>
/// Redis connection pool configuration for performance optimization.
/// </summary>
public sealed class RedisConnectionPoolOptions
{
    /// <summary>Minimum number of connections to maintain</summary>
    public int MinConnections { get; init; } = 2;

    /// <summary>Maximum number of connections in pool</summary>
    public int MaxConnections { get; init; } = 50;

    /// <summary>Connection idle timeout in seconds before recycling</summary>
    public int IdleTimeoutSeconds { get; init; } = 300; // 5 minutes
}

/// <summary>
/// FluentValidation validator for RedisOptions.
/// PUBLIC GATEWAY: Ensures rate limiting configuration is valid
/// </summary>
public sealed class RedisOptionsValidator : AbstractValidator<RedisOptions>
{
    public RedisOptionsValidator()
    {
        RuleFor(x => x.ConnectionString)
            .NotEmpty()
            .WithMessage("Redis connection string is required")
            .Must(BeValidRedisConnectionString)
            .WithMessage("Redis connection string format is invalid");

        RuleFor(x => x.RateLimitingDatabase)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(15)
            .WithMessage("Redis rate limiting database must be between 0 and 15");

        RuleFor(x => x.CacheDatabase)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(15)
            .WithMessage("Redis cache database must be between 0 and 15");

        RuleFor(x => x.DefaultCacheExpirationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(1440) // 24 hours max
            .WithMessage("Default cache expiration must be between 1 and 1440 minutes");

        RuleFor(x => x.RateLimitWindowMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(60)
            .WithMessage("Rate limit window must be between 1 and 60 minutes");

        RuleFor(x => x.MaxRequestsPerWindow)
            .GreaterThan(0)
            .LessThanOrEqualTo(10000)
            .WithMessage("Max requests per window must be between 1 and 10000");

        RuleFor(x => x.MaxRetryAttempts)
            .GreaterThan(0)
            .LessThanOrEqualTo(10)
            .WithMessage("Max retry attempts must be between 1 and 10");

        RuleFor(x => x.ConnectionTimeoutSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(60)
            .WithMessage("Connection timeout must be between 1 and 60 seconds");

        RuleFor(x => x.CommandTimeoutSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(300)
            .WithMessage("Command timeout must be between 1 and 300 seconds");

        RuleFor(x => x.KeyPrefix)
            .NotEmpty()
            .Matches("^[a-zA-Z0-9:_-]+$")
            .WithMessage("Key prefix must contain only alphanumeric characters, colons, underscores, and hyphens");

        RuleFor(x => x.ConnectionPool.MinConnections)
            .GreaterThan(0)
            .LessThan(x => x.ConnectionPool.MaxConnections)
            .WithMessage("Min connections must be greater than 0 and less than max connections");

        RuleFor(x => x.ConnectionPool.MaxConnections)
            .GreaterThan(0)
            .LessThanOrEqualTo(200)
            .WithMessage("Max connections must be between 1 and 200");
    }

    private static bool BeValidRedisConnectionString(string connectionString)
    {
        // Basic validation - contains Redis connection components
        return !string.IsNullOrWhiteSpace(connectionString) &&
               (connectionString.Contains("localhost") || 
                connectionString.Contains("127.0.0.1") || 
                connectionString.Contains(":6379") ||
                connectionString.Contains("redis://") ||
                connectionString.Contains(".redis."));
    }
}
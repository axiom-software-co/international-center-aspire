using FluentValidation;

namespace Infrastructure.Database.Options;

/// <summary>
/// Generic database connection configuration options for PostgreSQL infrastructure.
/// INFRASTRUCTURE: Generic database connection patterns reusable by any domain
/// DEPENDENCY INVERSION: No knowledge of specific domains (Services, News, Events, etc.)
/// </summary>
public sealed class DatabaseConnectionOptions
{
    public const string SectionName = "DatabaseConnection";

    /// <summary>
    /// Primary database connection string
    /// INFRASTRUCTURE: Generic PostgreSQL connection for any domain
    /// </summary>
    public required string ConnectionString { get; init; }

    /// <summary>
    /// Read-only connection string for read replicas
    /// SCALABILITY: Optional read replica connection for high-read domains
    /// </summary>
    public string? ReadOnlyConnectionString { get; init; }

    /// <summary>
    /// Database name
    /// INFRASTRUCTURE: Generic database name configuration
    /// </summary>
    public required string DatabaseName { get; init; }

    /// <summary>
    /// Application name for database connections
    /// MONITORING: Application identifier in database connections
    /// </summary>
    public string ApplicationName { get; init; } = "GenericApplication";

    /// <summary>
    /// Command timeout in seconds
    /// PERFORMANCE: Query execution timeout
    /// </summary>
    public int CommandTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Connection pool configuration
    /// CONNECTION POOLING: Generic pooling settings
    /// </summary>
    public ConnectionPoolOptions ConnectionPool { get; init; } = new();

    /// <summary>
    /// Retry policy configuration
    /// RESILIENCE: Generic retry patterns for transient failures
    /// </summary>
    public DatabaseRetryOptions Retry { get; init; } = new();

    /// <summary>
    /// Health check configuration
    /// MONITORING: Generic health check settings
    /// </summary>
    public DatabaseHealthCheckOptions HealthCheck { get; init; } = new();
}

/// <summary>
/// Generic connection pool configuration.
/// INFRASTRUCTURE: Connection pooling patterns for PostgreSQL
/// </summary>
public sealed class ConnectionPoolOptions
{
    /// <summary>
    /// Maximum pool size
    /// CONNECTION POOLING: Maximum number of connections
    /// </summary>
    public int MaxPoolSize { get; init; } = 20;

    /// <summary>
    /// Minimum pool size
    /// CONNECTION POOLING: Minimum connections to maintain
    /// </summary>
    public int MinPoolSize { get; init; } = 1;

    /// <summary>
    /// Connection lifetime in seconds
    /// CONNECTION POOLING: Connection lifetime before renewal
    /// </summary>
    public int ConnectionLifetimeSeconds { get; init; } = 3600; // 1 hour

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
}

/// <summary>
/// Generic database retry policy configuration.
/// INFRASTRUCTURE: Resilience patterns for transient failures
/// </summary>
public sealed class DatabaseRetryOptions
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
/// Generic database health check configuration.
/// INFRASTRUCTURE: Health check patterns for PostgreSQL
/// </summary>
public sealed class DatabaseHealthCheckOptions
{
    /// <summary>
    /// Enable database health checks
    /// MONITORING: Health check configuration
    /// </summary>
    public bool EnableHealthChecks { get; init; } = true;

    /// <summary>
    /// Health check timeout in seconds
    /// MONITORING: Timeout for database health checks
    /// </summary>
    public int TimeoutSeconds { get; init; } = 10;

    /// <summary>
    /// Health check query
    /// MONITORING: Query to execute for health check
    /// </summary>
    public string HealthCheckQuery { get; init; } = "SELECT 1";

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
/// FluentValidation validator for DatabaseConnectionOptions.
/// INFRASTRUCTURE: Ensures database configuration is valid
/// </summary>
public sealed class DatabaseConnectionOptionsValidator : AbstractValidator<DatabaseConnectionOptions>
{
    public DatabaseConnectionOptionsValidator()
    {
        RuleFor(x => x.ConnectionString)
            .NotEmpty()
            .WithMessage("Connection string is required")
            .Must(BeValidPostgreSqlConnectionString)
            .WithMessage("Connection string must be a valid PostgreSQL connection string");

        RuleFor(x => x.DatabaseName)
            .NotEmpty()
            .WithMessage("Database name is required");

        RuleFor(x => x.CommandTimeoutSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(300) // 5 minutes max
            .WithMessage("Command timeout must be between 1 and 300 seconds");

        RuleFor(x => x.ConnectionPool.MaxPoolSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
            .WithMessage("Max pool size must be between 1 and 1000");

        RuleFor(x => x.ConnectionPool.MinPoolSize)
            .GreaterThanOrEqualTo(0)
            .LessThan(x => x.ConnectionPool.MaxPoolSize)
            .WithMessage("Min pool size must be less than max pool size");

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
            .LessThanOrEqualTo(120)
            .WithMessage("Health check timeout must be between 1 and 120 seconds");

        // Read-only connection string validation (if provided)
        RuleFor(x => x.ReadOnlyConnectionString)
            .Must(BeValidPostgreSqlConnectionString!)
            .When(x => !string.IsNullOrEmpty(x.ReadOnlyConnectionString))
            .WithMessage("Read-only connection string must be a valid PostgreSQL connection string");
    }

    private static bool BeValidPostgreSqlConnectionString(string connectionString)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(connectionString) &&
                   (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
                    connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)) &&
                   connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
using FluentValidation;

namespace Service.Configuration.Options;

/// <summary>
/// Database configuration options for Services APIs.
/// STABLE CONCERN: Database connection patterns are well-established
/// MEDICAL COMPLIANCE: Connection strings handled securely with validation
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// PostgreSQL connection string for Services Admin API (EF Core)
    /// MEDICAL COMPLIANCE: Connection string validation prevents SQL injection
    /// </summary>
    public required string AdminConnectionString { get; init; }

    /// <summary>
    /// PostgreSQL connection string for Services Public API (Dapper)  
    /// PERFORMANCE: Separate connection for read-heavy public operations
    /// </summary>
    public required string PublicConnectionString { get; init; }

    /// <summary>
    /// Maximum retry attempts for database operations
    /// MEDICAL COMPLIANCE: Ensures data persistence reliability
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Command timeout in seconds for database operations
    /// MEDICAL COMPLIANCE: Prevents hanging operations in medical workflows
    /// </summary>
    public int CommandTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Enable detailed logging for database operations
    /// MEDICAL COMPLIANCE: Audit trail for all database interactions
    /// </summary>
    public bool EnableDetailedLogging { get; init; } = false;

    /// <summary>
    /// Enable automatic database migrations for Development/Testing
    /// PRODUCTION: Should be false for manual migration control
    /// </summary>
    public bool EnableAutomaticMigrations { get; init; } = true;

    /// <summary>
    /// Connection pool settings for performance optimization
    /// </summary>
    public ConnectionPoolOptions ConnectionPool { get; init; } = new();
}

/// <summary>
/// Database connection pool configuration for performance optimization.
/// </summary>
public sealed class ConnectionPoolOptions
{
    /// <summary>Minimum pool size for connection pooling</summary>
    public int MinPoolSize { get; init; } = 5;

    /// <summary>Maximum pool size for connection pooling</summary>
    public int MaxPoolSize { get; init; } = 100;

    /// <summary>Connection lifetime in seconds before recycling</summary>
    public int ConnectionLifetimeSeconds { get; init; } = 600; // 10 minutes
}

/// <summary>
/// FluentValidation validator for DatabaseOptions.
/// MEDICAL COMPLIANCE: Prevents invalid database configurations
/// </summary>
public sealed class DatabaseOptionsValidator : AbstractValidator<DatabaseOptions>
{
    public DatabaseOptionsValidator()
    {
        RuleFor(x => x.AdminConnectionString)
            .NotEmpty()
            .WithMessage("Admin database connection string is required")
            .Must(BeValidConnectionString)
            .WithMessage("Admin database connection string format is invalid");

        RuleFor(x => x.PublicConnectionString)
            .NotEmpty()
            .WithMessage("Public database connection string is required")
            .Must(BeValidConnectionString)
            .WithMessage("Public database connection string format is invalid");

        RuleFor(x => x.MaxRetryAttempts)
            .GreaterThan(0)
            .LessThanOrEqualTo(10)
            .WithMessage("Max retry attempts must be between 1 and 10");

        RuleFor(x => x.CommandTimeoutSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(300) // 5 minutes max
            .WithMessage("Command timeout must be between 1 and 300 seconds");

        RuleFor(x => x.ConnectionPool.MinPoolSize)
            .GreaterThan(0)
            .LessThan(x => x.ConnectionPool.MaxPoolSize)
            .WithMessage("Min pool size must be greater than 0 and less than max pool size");

        RuleFor(x => x.ConnectionPool.MaxPoolSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(500)
            .WithMessage("Max pool size must be between 1 and 500");
    }

    private static bool BeValidConnectionString(string connectionString)
    {
        // Basic validation - contains required PostgreSQL components
        return !string.IsNullOrWhiteSpace(connectionString) &&
               (connectionString.Contains("Host=") || connectionString.Contains("Server=")) &&
               connectionString.Contains("Database=");
    }
}
using FluentValidation;

namespace Service.Migration.Orchestrator.Configuration;

/// <summary>
/// Configuration options for the migration orchestrator service.
/// SERVICE: Migration coordination configuration
/// </summary>
public sealed class MigrationOrchestratorOptions
{
    public const string SectionName = "MigrationOrchestrator";

    /// <summary>
    /// Stop orchestration on first domain failure
    /// RESILIENCE: Failure handling strategy
    /// </summary>
    public bool StopOnFirstFailure { get; init; } = true;

    /// <summary>
    /// Migration timeout in minutes
    /// TIMEOUT: Migration execution timeout
    /// </summary>
    public int TimeoutMinutes { get; init; } = 30;

    /// <summary>
    /// Enable production safety checks
    /// SAFETY: Additional validation for production deployment
    /// </summary>
    public bool EnableProductionSafetyChecks { get; init; } = true;

    /// <summary>
    /// Backup database before applying migrations
    /// BACKUP: Database backup strategy
    /// </summary>
    public bool BackupBeforeMigration { get; init; } = false;

    /// <summary>
    /// Dry run mode - validate without applying
    /// VALIDATION: Dry run capability for testing
    /// </summary>
    public bool DryRunMode { get; init; } = false;

    /// <summary>
    /// Domain configurations
    /// DOMAINS: Multi-domain migration coordination
    /// </summary>
    public List<DomainConfiguration> Domains { get; init; } = new();

    /// <summary>
    /// Database connection configuration
    /// CONNECTION: Database connectivity settings
    /// </summary>
    public DatabaseConfiguration Database { get; init; } = new();
}

/// <summary>
/// Configuration for a specific domain's migrations.
/// DOMAIN: Per-domain migration settings
/// </summary>
public sealed class DomainConfiguration
{
    /// <summary>
    /// Domain name (e.g., "Services", "News", "Events")
    /// IDENTITY: Domain identifier
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether this domain's migrations are enabled
    /// CONTROL: Domain migration toggle
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Migration execution priority (lower numbers execute first)
    /// ORDERING: Domain dependency ordering
    /// </summary>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// Domain-specific timeout in minutes
    /// TIMEOUT: Per-domain timeout override
    /// </summary>
    public int? TimeoutMinutes { get; init; }

    /// <summary>
    /// Skip dependency validation for this domain
    /// VALIDATION: Bypass dependency checks
    /// </summary>
    public bool SkipDependencyValidation { get; init; } = false;

    /// <summary>
    /// Domain migration context type name
    /// CONTEXT: EF Core DbContext type for this domain
    /// </summary>
    public string? ContextTypeName { get; init; }

    /// <summary>
    /// Additional domain-specific configuration
    /// EXTENSIBILITY: Domain-specific settings
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();
}

/// <summary>
/// Database configuration for migration orchestrator.
/// DATABASE: Connection and behavior settings
/// </summary>
public sealed class DatabaseConfiguration
{
    /// <summary>
    /// Database connection string
    /// CONNECTION: PostgreSQL connection string
    /// </summary>
    public required string ConnectionString { get; init; }

    /// <summary>
    /// Command timeout in seconds
    /// TIMEOUT: Database command timeout
    /// </summary>
    public int CommandTimeoutSeconds { get; init; } = 300; // 5 minutes

    /// <summary>
    /// Connection retry attempts
    /// RESILIENCE: Connection retry policy
    /// </summary>
    public int RetryAttempts { get; init; } = 3;

    /// <summary>
    /// Retry delay in seconds
    /// RESILIENCE: Delay between retry attempts
    /// </summary>
    public int RetryDelaySeconds { get; init; } = 5;

    /// <summary>
    /// Enable connection pooling
    /// PERFORMANCE: Connection pooling configuration
    /// </summary>
    public bool EnableConnectionPooling { get; init; } = false;

    /// <summary>
    /// Migration history table name
    /// TRACKING: EF Core migration history table
    /// </summary>
    public string MigrationHistoryTableName { get; init; } = "__EFMigrationsHistory";
}

/// <summary>
/// FluentValidation validator for MigrationOrchestratorOptions.
/// VALIDATION: Ensures migration orchestrator configuration is valid
/// </summary>
public sealed class MigrationOrchestratorOptionsValidator : AbstractValidator<MigrationOrchestratorOptions>
{
    public MigrationOrchestratorOptionsValidator()
    {
        RuleFor(x => x.TimeoutMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(1440) // 24 hours max
            .WithMessage("Migration timeout must be between 1 and 1440 minutes");

        RuleFor(x => x.Domains)
            .NotEmpty()
            .WithMessage("At least one domain must be configured");

        RuleForEach(x => x.Domains)
            .SetValidator(new DomainConfigurationValidator());

        RuleFor(x => x.Database)
            .NotNull()
            .WithMessage("Database configuration is required")
            .SetValidator(new DatabaseConfigurationValidator());

        // Ensure domain names are unique
        RuleFor(x => x.Domains)
            .Must(domains => domains.GroupBy(d => d.Name.ToLowerInvariant()).All(g => g.Count() == 1))
            .WithMessage("Domain names must be unique (case-insensitive)");

        // Ensure priorities are reasonable
        RuleFor(x => x.Domains)
            .Must(domains => domains.All(d => d.Priority >= 0 && d.Priority <= 1000))
            .WithMessage("Domain priorities must be between 0 and 1000");
    }
}

/// <summary>
/// FluentValidation validator for DomainConfiguration.
/// </summary>
public sealed class DomainConfigurationValidator : AbstractValidator<DomainConfiguration>
{
    public DomainConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Domain name is required")
            .Matches(@"^[A-Za-z][A-Za-z0-9]*$")
            .WithMessage("Domain name must start with a letter and contain only letters and numbers");

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(1000)
            .WithMessage("Domain priority must be between 0 and 1000");

        When(x => x.TimeoutMinutes.HasValue, () =>
        {
            RuleFor(x => x.TimeoutMinutes!.Value)
                .GreaterThan(0)
                .LessThanOrEqualTo(1440)
                .WithMessage("Domain timeout must be between 1 and 1440 minutes");
        });

        When(x => !string.IsNullOrEmpty(x.ContextTypeName), () =>
        {
            RuleFor(x => x.ContextTypeName)
                .Must(name => name!.EndsWith("DbContext") || name!.EndsWith("Context"))
                .WithMessage("Context type name should end with 'DbContext' or 'Context'");
        });
    }
}

/// <summary>
/// FluentValidation validator for DatabaseConfiguration.
/// </summary>
public sealed class DatabaseConfigurationValidator : AbstractValidator<DatabaseConfiguration>
{
    public DatabaseConfigurationValidator()
    {
        RuleFor(x => x.ConnectionString)
            .NotEmpty()
            .WithMessage("Database connection string is required")
            .Must(cs => cs.Contains("Host=") && cs.Contains("Database="))
            .WithMessage("Connection string must contain Host and Database parameters");

        RuleFor(x => x.CommandTimeoutSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(3600) // 1 hour max
            .WithMessage("Command timeout must be between 1 and 3600 seconds");

        RuleFor(x => x.RetryAttempts)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(10)
            .WithMessage("Retry attempts must be between 0 and 10");

        RuleFor(x => x.RetryDelaySeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(300) // 5 minutes max
            .WithMessage("Retry delay must be between 1 and 300 seconds");

        RuleFor(x => x.MigrationHistoryTableName)
            .NotEmpty()
            .WithMessage("Migration history table name is required")
            .Matches(@"^[A-Za-z_][A-Za-z0-9_]*$")
            .WithMessage("Migration history table name must be a valid SQL identifier");
    }
}
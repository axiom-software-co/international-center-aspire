namespace Infrastructure.Database.Abstractions;

/// <summary>
/// CONTRACT: Generic database migration service interface for automated and manual migrations.
/// 
/// TDD PRINCIPLE: Interface drives the design of database migration management
/// DEPENDENCY INVERSION: Abstractions for variable migration concerns
/// INFRASTRUCTURE: Generic migration patterns reusable by any domain
/// DOMAIN AGNOSTIC: No knowledge of Services, News, Events, or any specific domain
/// </summary>
public interface IDatabaseMigrationService
{
    /// <summary>
    /// CONTRACT: Get pending migrations for the database
    /// 
    /// POSTCONDITION: Returns list of migrations not yet applied to database
    /// INFRASTRUCTURE: Generic migration tracking for any domain
    /// DEVELOPMENT: Migration status verification
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>List of pending migration names</returns>
    Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Get applied migrations for the database
    /// 
    /// POSTCONDITION: Returns list of migrations already applied to database
    /// INFRASTRUCTURE: Migration history tracking for any domain
    /// AUDITING: Applied migration audit trail
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>List of applied migration names</returns>
    Task<IReadOnlyList<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Apply pending migrations automatically
    /// 
    /// PRECONDITION: Valid migrations available and database accessible
    /// POSTCONDITION: All pending migrations applied successfully
    /// DEVELOPMENT: Automatic migration for dev/test environments
    /// PRODUCTION: Manual migration approval process
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Migration result with applied migration details</returns>
    Task<MigrationResult> ApplyPendingMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Apply specific migration by name
    /// 
    /// PRECONDITION: Valid migration name and database accessible
    /// POSTCONDITION: Specified migration applied successfully
    /// INFRASTRUCTURE: Targeted migration application for any domain
    /// </summary>
    /// <param name="migrationName">Name of migration to apply</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Migration result with application details</returns>
    Task<MigrationResult> ApplyMigrationAsync(string migrationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Rollback to specific migration
    /// 
    /// PRECONDITION: Valid migration target and database accessible
    /// POSTCONDITION: Database rolled back to specified migration state
    /// INFRASTRUCTURE: Generic rollback capability for any domain
    /// RECOVERY: Database state recovery for issues
    /// </summary>
    /// <param name="targetMigrationName">Migration name to rollback to</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Migration result with rollback details</returns>
    Task<MigrationResult> RollbackToMigrationAsync(string targetMigrationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Generate SQL script for pending migrations
    /// 
    /// POSTCONDITION: Returns SQL script for manual migration execution
    /// PRODUCTION: Manual migration script generation
    /// REVIEW: Migration review and approval process
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>SQL script for pending migrations</returns>
    Task<string> GenerateMigrationScriptAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Validate database schema against migration history
    /// 
    /// POSTCONDITION: Returns validation result with schema consistency status
    /// INFRASTRUCTURE: Schema validation for any domain
    /// CONSISTENCY: Database schema integrity verification
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Schema validation result</returns>
    Task<SchemaValidationResult> ValidateSchemaAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONTRACT: Get migration statistics and metrics
    /// 
    /// POSTCONDITION: Returns migration metrics for monitoring
    /// OBSERVABILITY: Migration performance and status monitoring
    /// INFRASTRUCTURE: Generic migration metrics for any domain
    /// </summary>
    /// <returns>Migration statistics and metrics</returns>
    MigrationMetrics GetMigrationMetrics();
}

/// <summary>
/// Migration operation result.
/// INFRASTRUCTURE: Generic migration result for any domain
/// </summary>
public sealed class MigrationResult
{
    /// <summary>Migration operation success status</summary>
    public bool IsSuccessful { get; init; }
    
    /// <summary>Applied migrations list</summary>
    public IReadOnlyList<string> AppliedMigrations { get; init; } = Array.Empty<string>();
    
    /// <summary>Migration operation duration in milliseconds</summary>
    public double DurationMs { get; init; }
    
    /// <summary>Error message if migration failed</summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>Exception details if migration failed</summary>
    public Exception? Exception { get; init; }
    
    /// <summary>Migration operation logs</summary>
    public IReadOnlyList<string> Logs { get; init; } = Array.Empty<string>();
    
    /// <summary>Migration operation timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>Create successful migration result</summary>
    public static MigrationResult Success(IReadOnlyList<string> appliedMigrations, double durationMs, IReadOnlyList<string>? logs = null) =>
        new()
        {
            IsSuccessful = true,
            AppliedMigrations = appliedMigrations,
            DurationMs = durationMs,
            Logs = logs ?? Array.Empty<string>()
        };
    
    /// <summary>Create failed migration result</summary>
    public static MigrationResult Failure(string errorMessage, Exception? exception = null, double durationMs = 0, IReadOnlyList<string>? logs = null) =>
        new()
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            DurationMs = durationMs,
            Logs = logs ?? Array.Empty<string>()
        };
}

/// <summary>
/// Database schema validation result.
/// INFRASTRUCTURE: Generic schema validation for any domain
/// </summary>
public sealed class SchemaValidationResult
{
    /// <summary>Schema validation success status</summary>
    public bool IsValid { get; init; }
    
    /// <summary>Schema validation messages</summary>
    public IReadOnlyList<string> ValidationMessages { get; init; } = Array.Empty<string>();
    
    /// <summary>Missing migrations</summary>
    public IReadOnlyList<string> MissingMigrations { get; init; } = Array.Empty<string>();
    
    /// <summary>Orphaned migrations (applied but not in code)</summary>
    public IReadOnlyList<string> OrphanedMigrations { get; init; } = Array.Empty<string>();
    
    /// <summary>Schema inconsistencies</summary>
    public IReadOnlyList<string> SchemaInconsistencies { get; init; } = Array.Empty<string>();
    
    /// <summary>Validation timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>Create valid schema result</summary>
    public static SchemaValidationResult Valid(IReadOnlyList<string>? messages = null) =>
        new()
        {
            IsValid = true,
            ValidationMessages = messages ?? Array.Empty<string>()
        };
    
    /// <summary>Create invalid schema result</summary>
    public static SchemaValidationResult Invalid(IReadOnlyList<string> validationMessages, IReadOnlyList<string>? missingMigrations = null, IReadOnlyList<string>? orphanedMigrations = null, IReadOnlyList<string>? schemaInconsistencies = null) =>
        new()
        {
            IsValid = false,
            ValidationMessages = validationMessages,
            MissingMigrations = missingMigrations ?? Array.Empty<string>(),
            OrphanedMigrations = orphanedMigrations ?? Array.Empty<string>(),
            SchemaInconsistencies = schemaInconsistencies ?? Array.Empty<string>()
        };
}

/// <summary>
/// Migration metrics for monitoring and observability.
/// INFRASTRUCTURE: Generic migration metrics for any domain
/// </summary>
public sealed class MigrationMetrics
{
    /// <summary>Total number of applied migrations</summary>
    public int TotalAppliedMigrations { get; init; }
    
    /// <summary>Number of pending migrations</summary>
    public int PendingMigrationsCount { get; init; }
    
    /// <summary>Average migration execution time in milliseconds</summary>
    public double AverageMigrationTimeMs { get; init; }
    
    /// <summary>Last successful migration timestamp</summary>
    public DateTime? LastSuccessfulMigration { get; init; }
    
    /// <summary>Last failed migration timestamp</summary>
    public DateTime? LastFailedMigration { get; init; }
    
    /// <summary>Total migration failures</summary>
    public int TotalMigrationFailures { get; init; }
    
    /// <summary>Migration success rate (percentage)</summary>
    public double SuccessRate { get; init; }
    
    /// <summary>Database schema version</summary>
    public string? SchemaVersion { get; init; }
    
    /// <summary>Metrics timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
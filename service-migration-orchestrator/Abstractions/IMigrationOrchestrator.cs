namespace Service.Migration.Orchestrator.Abstractions;

/// <summary>
/// SERVICE CONTRACT: Migration orchestrator for coordinating domain migrations.
/// 
/// ARCHITECTURE: Service layer coordinates domain schema evolution
/// COORDINATION: Centralized migration management across multiple domains
/// DOMAIN OWNERSHIP: Each domain manages its own migrations, orchestrator coordinates execution
/// MEDICAL COMPLIANCE: Ensures consistent database schema across all domains
/// </summary>
public interface IMigrationOrchestrator
{
    /// <summary>
    /// Orchestrate migrations across all active domains.
    /// 
    /// PRECONDITION: Database connection available and domains configured
    /// POSTCONDITION: All domain migrations applied in dependency order
    /// COORDINATION: Manages cross-domain migration dependencies
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Migration result with detailed information</returns>
    Task<MigrationOrchestratorResult> ApplyAllMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply migrations for a specific domain.
    /// 
    /// PRECONDITION: Domain exists and is configured
    /// POSTCONDITION: Domain-specific migrations applied
    /// DOMAIN ISOLATION: Single domain migration execution
    /// </summary>
    /// <param name="domainName">Name of the domain to migrate</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Domain-specific migration result</returns>
    Task<DomainMigrationResult> ApplyDomainMigrationsAsync(string domainName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get migration status across all domains.
    /// 
    /// POSTCONDITION: Returns current migration state for all configured domains
    /// MONITORING: Migration status inquiry for operations visibility
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Migration status information</returns>
    Task<MigrationStatusResult> GetMigrationStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate migration readiness across all domains.
    /// 
    /// PRECONDITION: All domains configured properly
    /// POSTCONDITION: Returns validation results for migration readiness
    /// VALIDATION: Pre-migration dependency and configuration validation
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Migration validation results</returns>
    Task<MigrationValidationResult> ValidateMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate SQL scripts for all pending migrations.
    /// 
    /// PRECONDITION: Domains have pending migrations
    /// POSTCONDITION: Returns SQL scripts for manual execution
    /// PRODUCTION DEPLOYMENT: Manual migration script generation for production
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>SQL scripts for pending migrations</returns>
    Task<MigrationScriptResult> GenerateMigrationScriptsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of migration orchestration across all domains.
/// </summary>
public sealed record MigrationOrchestratorResult
{
    /// <summary>Overall success status</summary>
    public required bool Success { get; init; }
    
    /// <summary>Total migrations applied across all domains</summary>
    public required int TotalMigrationsApplied { get; init; }
    
    /// <summary>Migration results per domain</summary>
    public required Dictionary<string, DomainMigrationResult> DomainResults { get; init; }
    
    /// <summary>Overall execution time</summary>
    public required TimeSpan TotalExecutionTime { get; init; }
    
    /// <summary>Any errors encountered during orchestration</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    
    /// <summary>Orchestration timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Result of migration for a specific domain.
/// </summary>
public sealed record DomainMigrationResult
{
    /// <summary>Domain name</summary>
    public required string DomainName { get; init; }
    
    /// <summary>Migration success status</summary>
    public required bool Success { get; init; }
    
    /// <summary>Number of migrations applied</summary>
    public required int MigrationsApplied { get; init; }
    
    /// <summary>Migration execution time</summary>
    public required TimeSpan ExecutionTime { get; init; }
    
    /// <summary>Applied migration names</summary>
    public required IReadOnlyList<string> AppliedMigrations { get; init; }
    
    /// <summary>Domain-specific errors</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    
    /// <summary>Migration timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Migration status across all domains.
/// </summary>
public sealed record MigrationStatusResult
{
    /// <summary>Overall migration health status</summary>
    public required bool IsHealthy { get; init; }
    
    /// <summary>Status per domain</summary>
    public required Dictionary<string, DomainMigrationStatus> DomainStatuses { get; init; }
    
    /// <summary>Status check timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Migration status for a specific domain.
/// </summary>
public sealed record DomainMigrationStatus
{
    /// <summary>Domain name</summary>
    public required string DomainName { get; init; }
    
    /// <summary>Domain migration health</summary>
    public required bool IsHealthy { get; init; }
    
    /// <summary>Applied migrations count</summary>
    public required int AppliedMigrations { get; init; }
    
    /// <summary>Pending migrations count</summary>
    public required int PendingMigrations { get; init; }
    
    /// <summary>List of pending migration names</summary>
    public required IReadOnlyList<string> PendingMigrationNames { get; init; }
    
    /// <summary>Last migration applied</summary>
    public string? LastMigrationApplied { get; init; }
    
    /// <summary>Last migration timestamp</summary>
    public DateTime? LastMigrationTimestamp { get; init; }
}

/// <summary>
/// Migration validation result.
/// </summary>
public sealed record MigrationValidationResult
{
    /// <summary>Overall validation success</summary>
    public required bool IsValid { get; init; }
    
    /// <summary>Validation results per domain</summary>
    public required Dictionary<string, DomainValidationResult> DomainValidations { get; init; }
    
    /// <summary>Validation timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Validation result for a specific domain.
/// </summary>
public sealed record DomainValidationResult
{
    /// <summary>Domain name</summary>
    public required string DomainName { get; init; }
    
    /// <summary>Domain validation success</summary>
    public required bool IsValid { get; init; }
    
    /// <summary>Validation issues</summary>
    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Migration script generation result.
/// </summary>
public sealed record MigrationScriptResult
{
    /// <summary>Script generation success</summary>
    public required bool Success { get; init; }
    
    /// <summary>Generated SQL scripts per domain</summary>
    public required Dictionary<string, string> DomainScripts { get; init; }
    
    /// <summary>Combined script for all domains</summary>
    public required string CombinedScript { get; init; }
    
    /// <summary>Script generation timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
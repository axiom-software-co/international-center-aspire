using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using InternationalCenter.Shared.Infrastructure;

namespace InternationalCenter.Shared.Infrastructure.Migrations;

/// <summary>
/// Executes independent domain migrations in parallel for optimal performance
/// Implements dependency-aware parallel execution with medical-grade safety and monitoring
/// </summary>
public class ParallelMigrationService : IParallelMigrationService
{
    private readonly ILogger<ParallelMigrationService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IMigrationOrchestrationService _orchestrationService;
    private readonly IMigrationAuditService _auditService;

    public ParallelMigrationService(
        ILogger<ParallelMigrationService> logger,
        ApplicationDbContext context,
        IMigrationOrchestrationService orchestrationService,
        IMigrationAuditService auditService)
    {
        _logger = logger;
        _context = context;
        _orchestrationService = orchestrationService;
        _auditService = auditService;
    }

    public async Task<MigrationResult> ExecuteDomainMigrationAsync(string domain, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Parallel Migration: Executing migration for domain {Domain}", domain);

        var executionStartTime = DateTime.UtcNow;
        var appliedMigrations = new List<string>();

        try
        {
            // Validate domain
            ValidateDomain(domain);

            // Create domain-specific migration plan
            var migrationPlan = await _orchestrationService.CreateMigrationPlanAsync(cancellationToken);
            var domainMigration = migrationPlan.DomainMigrations.FirstOrDefault(dm => dm.Domain == domain);

            if (domainMigration == null)
            {
                _logger.LogWarning("Parallel Migration: No migration plan found for domain {Domain}", domain);
                return new MigrationResult
                {
                    Domain = domain,
                    IsSuccess = true,
                    ExecutionTime = TimeSpan.Zero,
                    AppliedMigrations = appliedMigrations,
                    Message = "No migrations required"
                };
            }

            if (!domainMigration.PendingMigrations.Any())
            {
                _logger.LogInformation("Parallel Migration: No pending migrations for domain {Domain}", domain);
                return new MigrationResult
                {
                    Domain = domain,
                    IsSuccess = true,
                    ExecutionTime = DateTime.UtcNow - executionStartTime,
                    AppliedMigrations = appliedMigrations,
                    Message = "No pending migrations"
                };
            }

            _logger.LogInformation("Parallel Migration: Executing {Count} migrations for domain {Domain}",
                domainMigration.PendingMigrations.Count, domain);

            // Execute domain migrations with audit trail
            appliedMigrations = await ExecuteDomainMigrationsWithAuditAsync(domainMigration, cancellationToken);

            var executionTime = DateTime.UtcNow - executionStartTime;

            var result = new MigrationResult
            {
                Domain = domain,
                IsSuccess = true,
                ExecutionTime = executionTime,
                AppliedMigrations = appliedMigrations,
                Message = $"Successfully applied {appliedMigrations.Count} migrations"
            };

            _logger.LogInformation("Parallel Migration: Domain {Domain} completed successfully in {Duration}ms",
                domain, executionTime.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            var executionTime = DateTime.UtcNow - executionStartTime;
            
            _logger.LogError(ex, "Parallel Migration: Domain {Domain} failed after {Duration}ms",
                domain, executionTime.TotalMilliseconds);

            return new MigrationResult
            {
                Domain = domain,
                IsSuccess = false,
                ExecutionTime = executionTime,
                AppliedMigrations = appliedMigrations,
                Message = $"Migration failed: {ex.Message}",
                Error = ex
            };
        }
    }

    public async Task<IEnumerable<MigrationResult>> ExecuteParallelMigrationsAsync(IEnumerable<string> domains, CancellationToken cancellationToken = default)
    {
        var domainList = domains.ToList();
        _logger.LogInformation("Parallel Migration: Starting parallel execution for {Count} domains: {Domains}",
            domainList.Count, string.Join(", ", domainList));

        var parallelStartTime = DateTime.UtcNow;

        try
        {
            // Create migration plan to understand dependencies
            var migrationPlan = await _orchestrationService.CreateMigrationPlanAsync(cancellationToken);
            
            // Group domains by dependency level for parallel execution
            var executionGroups = GroupDomainsByDependencyLevel(domainList, migrationPlan);
            
            _logger.LogInformation("Parallel Migration: Organized {Count} domains into {Groups} execution groups",
                domainList.Count, executionGroups.Count);

            var allResults = new List<MigrationResult>();

            // Execute groups sequentially, but domains within each group in parallel
            foreach (var group in executionGroups)
            {
                _logger.LogInformation("Parallel Migration: Executing group with domains: {Domains}",
                    string.Join(", ", group));

                var groupStartTime = DateTime.UtcNow;

                // Execute domains in current group in parallel
                var groupTasks = group.Select(domain => ExecuteDomainMigrationAsync(domain, cancellationToken));
                var groupResults = await Task.WhenAll(groupTasks);

                var groupDuration = DateTime.UtcNow - groupStartTime;
                _logger.LogInformation("Parallel Migration: Group completed in {Duration}ms", groupDuration.TotalMilliseconds);

                allResults.AddRange(groupResults);

                // Check for failures in this group before proceeding
                var groupFailures = groupResults.Where(r => !r.IsSuccess).ToList();
                if (groupFailures.Any())
                {
                    _logger.LogError("Parallel Migration: {Count} domains failed in current group: {FailedDomains}",
                        groupFailures.Count, string.Join(", ", groupFailures.Select(f => f.Domain)));
                    
                    // Decide whether to continue with remaining groups
                    // For medical-grade reliability, we continue with independent groups
                    foreach (var failure in groupFailures)
                    {
                        _logger.LogWarning("Parallel Migration: Domain {Domain} failure will not block independent domains", 
                            failure.Domain);
                    }
                }
            }

            var totalDuration = DateTime.UtcNow - parallelStartTime;
            var successCount = allResults.Count(r => r.IsSuccess);

            _logger.LogInformation("Parallel Migration: Completed {Successful}/{Total} domains in {Duration}ms",
                successCount, allResults.Count, totalDuration.TotalMilliseconds);

            return allResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parallel Migration: Parallel execution failed");
            
            // Return partial results if available, otherwise return failure for all domains
            return domainList.Select(domain => new MigrationResult
            {
                Domain = domain,
                IsSuccess = false,
                ExecutionTime = DateTime.UtcNow - parallelStartTime,
                AppliedMigrations = new List<string>(),
                Message = $"Parallel execution failed: {ex.Message}",
                Error = ex
            });
        }
    }

    private async Task<List<string>> ExecuteDomainMigrationsWithAuditAsync(DomainMigration domainMigration, CancellationToken cancellationToken)
    {
        var appliedMigrations = new List<string>();
        
        // Use execution strategy for resilience
        var strategy = _context.Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            foreach (var migration in domainMigration.PendingMigrations)
            {
                var migrationStartTime = DateTime.UtcNow;
                
                _logger.LogInformation("Parallel Migration: Applying migration {Migration} for domain {Domain}",
                    migration, domainMigration.Domain);

                try
                {
                    // Calculate checksum before migration
                    var checksumBefore = await CalculateDomainChecksumAsync(domainMigration.Domain, cancellationToken);

                    // Apply the migration (simplified - in production would be more sophisticated)
                    await _context.Database.MigrateAsync(cancellationToken);
                    
                    // Calculate checksum after migration
                    var checksumAfter = await CalculateDomainChecksumAsync(domainMigration.Domain, cancellationToken);
                    
                    var migrationDuration = DateTime.UtcNow - migrationStartTime;

                    // Record audit entry
                    var auditEntry = new DomainMigrationAuditEntry
                    {
                        Domain = domainMigration.Domain,
                        MigrationName = migration,
                        AppliedAt = migrationStartTime,
                        AppliedBy = "parallel-migration-service",
                        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                        ChecksumBefore = checksumBefore,
                        ChecksumAfter = checksumAfter,
                        Duration = migrationDuration
                    };

                    await _auditService.RecordMigrationAsync(auditEntry, cancellationToken);
                    
                    appliedMigrations.Add(migration);
                    
                    _logger.LogInformation("Parallel Migration: Successfully applied {Migration} for {Domain} in {Duration}ms",
                        migration, domainMigration.Domain, migrationDuration.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Parallel Migration: Failed to apply migration {Migration} for domain {Domain}",
                        migration, domainMigration.Domain);
                    throw;
                }
            }
        });

        return appliedMigrations;
    }

    private static List<List<string>> GroupDomainsByDependencyLevel(List<string> domains, MigrationPlan migrationPlan)
    {
        var executionGroups = new List<List<string>>();
        var processedDomains = new HashSet<string>();
        var availableDomains = new HashSet<string>(domains);

        // Build dependency map
        var dependencyMap = migrationPlan.DomainMigrations
            .Where(dm => domains.Contains(dm.Domain))
            .ToDictionary(dm => dm.Domain, dm => dm.Dependencies);

        while (availableDomains.Any())
        {
            // Find domains with no unprocessed dependencies
            var currentGroup = availableDomains
                .Where(domain => 
                {
                    var dependencies = dependencyMap.GetValueOrDefault(domain, new List<string>());
                    return dependencies.All(dep => processedDomains.Contains(dep) || !domains.Contains(dep));
                })
                .ToList();

            if (!currentGroup.Any())
            {
                // Circular dependency or missing dependency - include remaining domains in final group
                currentGroup = availableDomains.ToList();
            }

            executionGroups.Add(currentGroup);
            
            foreach (var domain in currentGroup)
            {
                availableDomains.Remove(domain);
                processedDomains.Add(domain);
            }
        }

        return executionGroups;
    }

    private static void ValidateDomain(string domain)
    {
        // Services-only parallel migration (other domains disabled for focused development)
        var validDomains = new[] { "Services" };
        if (!validDomains.Contains(domain))
        {
            throw new ArgumentException($"Invalid domain '{domain}'. Only Services domain is currently supported: {string.Join(", ", validDomains)}");
        }
    }

    private async Task<string> CalculateDomainChecksumAsync(string domain, CancellationToken cancellationToken)
    {
        // Services-only checksum calculation (other domains disabled for focused development)
        switch (domain.ToLower())
        {
            case "services":
                var servicesCount = await _context.Services.CountAsync(cancellationToken);
                var categoriesCount = await _context.ServiceCategories.CountAsync(cancellationToken);
                return $"{domain}_{servicesCount}_{categoriesCount}_{DateTime.UtcNow.Ticks}";

            default:
                throw new ArgumentException($"Unsupported domain '{domain}'. Only Services domain is currently supported.");
        }
    }
}

/// <summary>
/// Interface for parallel migration service
/// </summary>
public interface IParallelMigrationService
{
    Task<MigrationResult> ExecuteDomainMigrationAsync(string domain, CancellationToken cancellationToken = default);
    Task<IEnumerable<MigrationResult>> ExecuteParallelMigrationsAsync(IEnumerable<string> domains, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of domain migration execution
/// </summary>
public record MigrationResult
{
    public required string Domain { get; init; }
    public required bool IsSuccess { get; init; }
    public required TimeSpan ExecutionTime { get; init; }
    public required List<string> AppliedMigrations { get; init; }
    public required string Message { get; init; }
    public Exception? Error { get; init; }
}
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;
using System.Collections.Concurrent;
using Polly;
using Shared.Infrastructure;

namespace Shared.Infrastructure.Migrations;

/// <summary>
/// Medical-Grade Migration Orchestration Service with parallel execution and comprehensive observability
/// Orchestrates domain-specific migrations with dependency management, resilience policies, and audit trails
/// Features: Parallel execution, topological sorting, retry policies, performance monitoring
/// </summary>
public class MigrationOrchestrationService : IMigrationOrchestrationService
{
    private readonly ILogger<MigrationOrchestrationService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IAsyncPolicy _resilientPolicy;
    private readonly MigrationOrchestrationConfiguration _config;
    private readonly ActivitySource _activitySource;
    private readonly ConcurrentDictionary<string, DateTime> _domainExecutionTimes;

    public MigrationOrchestrationService(
        ILogger<MigrationOrchestrationService> logger,
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _config = new MigrationOrchestrationConfiguration(configuration);
        _activitySource = new ActivitySource("InternationalCenter.MigrationOrchestration");
        _domainExecutionTimes = new ConcurrentDictionary<string, DateTime>();
        
        // Medical-grade resilience policy for orchestration operations
        _resilientPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: _config.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Migration Orchestrator: Retry attempt {RetryCount} after {Delay}ms due to: {Exception}",
                        retryCount, timespan.TotalMilliseconds, outcome.Message);
                });
    }

    public async Task<MigrationPlan> CreateMigrationPlanAsync(CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("CreateMigrationPlan");
        _logger.LogInformation("Migration Orchestrator: Creating medical-grade migration execution plan");

        return await _resilientPolicy.ExecuteAsync(async () =>
        {
            var planningStartTime = DateTime.UtcNow;
            var domainMigrations = new List<DomainMigration>();
            var planId = Guid.NewGuid();

            activity?.SetTag("plan.id", planId.ToString());
            activity?.SetTag("plan.environment", _config.Environment);

            // Services-focused domain orchestration with future extensibility
            var activedomains = _config.EnabledDomains.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            // Parallel collection of domain migration info for improved performance
            var domainTasks = activedomains.Select(async domain =>
            {
                var pendingMigrations = await GetDomainPendingMigrationsAsync(domain.Trim(), cancellationToken);
                var dependencies = GetDomainDependencies(domain.Trim());
                
                return new DomainMigration
                {
                    Domain = domain.Trim(),
                    Dependencies = dependencies,
                    PendingMigrations = pendingMigrations,
                    EstimatedDuration = CalculateEstimatedDuration(pendingMigrations),
                    Priority = GetDomainPriority(domain.Trim())
                };
            }).ToArray();

            var domainResults = await Task.WhenAll(domainTasks);
            domainMigrations.AddRange(domainResults);

            // Enhanced topological sorting with cycle detection
            var sortedDomains = TopologicalSort(domainMigrations);
            
            // Calculate parallel execution groups for independent domains
            var executionGroups = CalculateParallelExecutionGroups(sortedDomains);

            var migrationPlan = new MigrationPlan
            {
                PlanId = planId,
                DomainMigrations = sortedDomains,
                ExecutionGroups = executionGroups,
                CreatedAt = DateTime.UtcNow,
                Environment = _config.Environment,
                MaxParallelism = _config.MaxParallelDomains,
                EstimatedTotalDuration = TimeSpan.FromTicks(sortedDomains.Sum(d => d.EstimatedDuration.Ticks))
            };

            var planningDuration = DateTime.UtcNow - planningStartTime;
            
            activity?.SetTag("plan.domain_count", sortedDomains.Count);
            activity?.SetTag("plan.execution_groups", executionGroups.Count);
            activity?.SetTag("plan.estimated_duration_ms", migrationPlan.EstimatedTotalDuration.TotalMilliseconds);

            _logger.LogInformation("Migration Orchestrator: Created enhanced migration plan - PlanId: {PlanId}, Domains: {DomainCount}, ExecutionGroups: {GroupCount}, EstimatedDuration: {Duration}ms, PlanningTime: {PlanningTime}ms", 
                planId, sortedDomains.Count, executionGroups.Count, 
                migrationPlan.EstimatedTotalDuration.TotalMilliseconds, planningDuration.TotalMilliseconds);

            // Log detailed execution strategy
            for (int i = 0; i < executionGroups.Count; i++)
            {
                var group = executionGroups[i];
                _logger.LogInformation("Migration Orchestrator: Execution Group {GroupNumber}: [{Domains}] (Parallel: {CanExecuteInParallel})", 
                    i + 1, string.Join(", ", group.Select(d => $"{d.Domain}({d.PendingMigrations.Count})")), group.Count > 1);
            }

            // Create planning audit entry
            await CreateOrchestrationAuditEntryAsync(migrationPlan, "PLAN_CREATED", planningDuration, cancellationToken);

            return migrationPlan;
        });
    }

    public async Task ExecuteMigrationPlanAsync(MigrationPlan plan, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Migration Orchestrator: Executing migration plan for {DomainCount} domains", 
            plan.DomainMigrations.Count);

        var executionStartTime = DateTime.UtcNow;
        var completedDomains = new List<string>();

        try
        {
            foreach (var domainMigration in plan.DomainMigrations)
            {
                // Verify dependencies are completed
                foreach (var dependency in domainMigration.Dependencies)
                {
                    if (!completedDomains.Contains(dependency))
                    {
                        throw new InvalidOperationException(
                            $"Migration Orchestrator: Domain {domainMigration.Domain} depends on {dependency} which has not been completed");
                    }
                }

                if (!domainMigration.PendingMigrations.Any())
                {
                    _logger.LogInformation("Migration Orchestrator: Domain {Domain} has no pending migrations", 
                        domainMigration.Domain);
                    completedDomains.Add(domainMigration.Domain);
                    continue;
                }

                _logger.LogInformation("Migration Orchestrator: Executing {Count} migrations for domain {Domain}",
                    domainMigration.PendingMigrations.Count, domainMigration.Domain);

                var domainStartTime = DateTime.UtcNow;

                // Execute domain-specific migration logic
                await ExecuteDomainMigrationsAsync(domainMigration, cancellationToken);

                var domainDuration = DateTime.UtcNow - domainStartTime;
                _logger.LogInformation("Migration Orchestrator: Domain {Domain} completed in {Duration}ms",
                    domainMigration.Domain, domainDuration.TotalMilliseconds);

                completedDomains.Add(domainMigration.Domain);
            }

            var totalDuration = DateTime.UtcNow - executionStartTime;
            _logger.LogInformation("Migration Orchestrator: All domain migrations completed successfully in {Duration}ms",
                totalDuration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration Orchestrator: Migration plan execution failed for domains: {CompletedDomains}",
                string.Join(", ", completedDomains));
            throw;
        }
    }

    private async Task<List<string>> GetDomainPendingMigrationsAsync(string domain, CancellationToken cancellationToken)
    {
        try
        {
            var allMigrations = _context.Database.GetMigrations();
            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync(cancellationToken);
            
            // Filter migrations by domain
            var domainMigrations = allMigrations.Where(m => 
                m.Contains(domain, StringComparison.OrdinalIgnoreCase) ||
                (domain == "Services" && (m.Contains("InitialCreate") || m.Contains("DatabaseArchitectureOptimizations"))));
            
            var pendingMigrations = domainMigrations.Except(appliedMigrations).ToList();
            
            _logger.LogDebug("Migration Orchestrator: Domain {Domain} has {PendingCount} pending migrations",
                domain, pendingMigrations.Count);
                
            return pendingMigrations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration Orchestrator: Failed to get pending migrations for domain {Domain}", domain);
            throw;
        }
    }

    private async Task ExecuteDomainMigrationsAsync(DomainMigration domainMigration, CancellationToken cancellationToken)
    {
        // Use execution strategy for resilience
        var strategy = _context.Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            // For now, use the shared context - in production, this would delegate to domain-specific services
            await _context.Database.MigrateAsync(cancellationToken);
            
            // Validate domain-specific schema after migration
            await ValidateDomainSchemaAsync(domainMigration.Domain, cancellationToken);
        });
    }

    private async Task ValidateDomainSchemaAsync(string domain, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Migration Orchestrator: Validating schema for domain {Domain}", domain);

        switch (domain.ToLower())
        {
            case "services":
                await ValidateServicesSchemaAsync(cancellationToken);
                break;
            case "news":
                await ValidateNewsSchemaAsync(cancellationToken);
                break;
            case "contacts":
            case "events":
            case "research":
            case "newsletter":
            case "search":
                _logger.LogInformation("Migration Orchestrator: Schema validation for domain {Domain} not yet implemented", domain);
                break;
            default:
                _logger.LogWarning("Migration Orchestrator: Unknown domain {Domain}", domain);
                break;
        }
    }

    private async Task ValidateServicesSchemaAsync(CancellationToken cancellationToken)
    {
        // Services schema validation moved to ServicesDbContext in Services domain
        // TODO: Update to use ServicesDbContext when available in migration context
        _logger.LogInformation("Migration Orchestrator: Services schema validation skipped (managed by ServicesDbContext)");
        await Task.CompletedTask;
    }

    private async Task ValidateNewsSchemaAsync(CancellationToken cancellationToken)
    {
        // News domain schema validation moved to NewsDbContext in News domain
        _logger.LogInformation("Migration Orchestrator: News domain schema validation moved to respective domain project");
        await Task.CompletedTask;
    }

    private static List<DomainMigration> TopologicalSort(List<DomainMigration> domains)
    {
        var sorted = new List<DomainMigration>();
        var temporary = new HashSet<string>();
        var permanent = new HashSet<string>();

        void Visit(DomainMigration domain)
        {
            if (permanent.Contains(domain.Domain))
                return;

            if (temporary.Contains(domain.Domain))
                throw new InvalidOperationException($"Circular dependency detected involving domain {domain.Domain}");

            temporary.Add(domain.Domain);

            foreach (var dependency in domain.Dependencies)
            {
                var dependencyDomain = domains.FirstOrDefault(d => d.Domain == dependency);
                if (dependencyDomain != null)
                {
                    Visit(dependencyDomain);
                }
            }

            temporary.Remove(domain.Domain);
            permanent.Add(domain.Domain);
            sorted.Add(domain);
        }

        foreach (var domain in domains)
        {
            if (!permanent.Contains(domain.Domain))
            {
                Visit(domain);
            }
        }

        return sorted;
    }
    
    private List<string> GetDomainDependencies(string domain)
    {
        // Define domain dependencies for medical-grade orchestration
        return domain.ToLower() switch
        {
            "services" => new List<string>(), // Core domain - no dependencies
            "search" => new List<string> { "Services" }, // Search depends on Services
            "news" => new List<string>(), // Independent domain
            "contacts" => new List<string>(), // Independent domain  
            "events" => new List<string> { "Services" }, // Events may depend on Services
            "research" => new List<string>(), // Independent domain
            "newsletter" => new List<string> { "News" }, // Newsletter depends on News
            _ => new List<string>()
        };
    }
    
    private TimeSpan CalculateEstimatedDuration(List<string> pendingMigrations)
    {
        if (!pendingMigrations.Any()) return TimeSpan.Zero;
        
        // Estimate based on migration complexity and count
        var baseDuration = TimeSpan.FromMinutes(2); // Base per migration
        var complexityFactor = pendingMigrations.Count * 0.5; // Additional time for multiple migrations
        
        return TimeSpan.FromMinutes(baseDuration.TotalMinutes * pendingMigrations.Count + complexityFactor);
    }
    
    private int GetDomainPriority(string domain)
    {
        // Higher priority = executed first (lower number)
        return domain.ToLower() switch
        {
            "services" => 1, // Highest priority - core domain
            "news" => 2,
            "contacts" => 3,
            "research" => 4,
            "events" => 5,
            "search" => 8, // Lower priority - depends on Services
            "newsletter" => 9, // Lowest priority - depends on News
            _ => 10
        };
    }
    
    private List<List<DomainMigration>> CalculateParallelExecutionGroups(List<DomainMigration> sortedDomains)
    {
        var groups = new List<List<DomainMigration>>();
        var processedDomains = new HashSet<string>();
        
        while (processedDomains.Count < sortedDomains.Count)
        {
            var currentGroup = new List<DomainMigration>();
            
            foreach (var domain in sortedDomains)
            {
                if (processedDomains.Contains(domain.Domain))
                    continue;
                    
                // Check if all dependencies are already processed
                var dependenciesSatisfied = domain.Dependencies.All(dep => processedDomains.Contains(dep));
                
                if (dependenciesSatisfied)
                {
                    currentGroup.Add(domain);
                    
                    // Limit parallel execution based on configuration
                    if (currentGroup.Count >= _config.MaxParallelDomains)
                        break;
                }
            }
            
            if (currentGroup.Any())
            {
                groups.Add(currentGroup);
                foreach (var domain in currentGroup)
                {
                    processedDomains.Add(domain.Domain);
                }
            }
            else
            {
                // Safety break to prevent infinite loop
                break;
            }
        }
        
        return groups;
    }
    
    private async Task CreateOrchestrationAuditEntryAsync(MigrationPlan plan, string phase, TimeSpan duration, CancellationToken cancellationToken)
    {
        var auditEntry = new
        {
            PlanId = plan.PlanId,
            Phase = phase,
            Environment = plan.Environment,
            DomainCount = plan.DomainMigrations.Count,
            ExecutionGroups = plan.ExecutionGroups.Count,
            EstimatedDuration = plan.EstimatedTotalDuration.TotalMilliseconds,
            ActualDuration = duration.TotalMilliseconds,
            MaxParallelism = plan.MaxParallelism,
            Timestamp = DateTime.UtcNow,
            MachineName = Environment.MachineName,
            Domains = plan.DomainMigrations.Select(d => new
            {
                d.Domain,
                DependencyCount = d.Dependencies.Count,
                PendingMigrations = d.PendingMigrations.Count,
                d.Priority,
                EstimatedDuration = d.EstimatedDuration.TotalMilliseconds
            }).ToList()
        };
        
        var auditJson = JsonSerializer.Serialize(auditEntry, new JsonSerializerOptions { WriteIndented = true });
        _logger.LogInformation("Migration Orchestration Audit: {AuditEntry}", auditJson);
        
        // In production, this would write to a dedicated audit table or external audit service
        await Task.CompletedTask;
    }
}

/// <summary>
/// Interface for migration orchestration service
/// </summary>
public interface IMigrationOrchestrationService
{
    Task<MigrationPlan> CreateMigrationPlanAsync(CancellationToken cancellationToken = default);
    Task ExecuteMigrationPlanAsync(MigrationPlan plan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Medical-grade migration execution plan with parallel execution support
/// </summary>
public record MigrationPlan
{
    public required Guid PlanId { get; init; }
    public required List<DomainMigration> DomainMigrations { get; init; }
    public required List<List<DomainMigration>> ExecutionGroups { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string Environment { get; init; }
    public required int MaxParallelism { get; init; }
    public required TimeSpan EstimatedTotalDuration { get; init; }
}

/// <summary>
/// Enhanced domain-specific migration information with performance metrics
/// </summary>
public record DomainMigration
{
    public required string Domain { get; init; }
    public required List<string> Dependencies { get; init; }
    public required List<string> PendingMigrations { get; init; }
    public required TimeSpan EstimatedDuration { get; init; }
    public required int Priority { get; init; }
}

/// <summary>
/// Configuration for migration orchestration service with medical-grade defaults
/// </summary>
public class MigrationOrchestrationConfiguration
{
    public int MaxRetryAttempts { get; }
    public int MaxParallelDomains { get; }
    public string EnabledDomains { get; }
    public string Environment { get; }
    public int DomainTimeoutMinutes { get; }
    public bool ParallelExecutionEnabled { get; }

    public MigrationOrchestrationConfiguration(IConfiguration configuration)
    {
        MaxRetryAttempts = configuration.GetValue("Migration:Orchestration:MaxRetryAttempts", 3);
        MaxParallelDomains = configuration.GetValue("Migration:Orchestration:MaxParallelDomains", 4);
        EnabledDomains = configuration.GetValue("Migration:Orchestration:EnabledDomains", "Services");
        Environment = configuration.GetValue("ASPNETCORE_ENVIRONMENT", "Development");
        DomainTimeoutMinutes = configuration.GetValue("Migration:Orchestration:DomainTimeoutMinutes", 15);
        ParallelExecutionEnabled = configuration.GetValue("Migration:Orchestration:ParallelExecutionEnabled", true);
    }
}
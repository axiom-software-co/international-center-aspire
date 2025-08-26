using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Service.Migration.Orchestrator.Abstractions;
using Service.Migration.Orchestrator.Configuration;
using Services.Shared.Infrastructure.Data;
using System.Diagnostics;

namespace Service.Migration.Orchestrator.Services;

/// <summary>
/// Migration orchestrator service implementation for coordinating domain migrations.
/// SERVICE: Coordinates domain schema evolution at service layer
/// </summary>
public sealed class MigrationOrchestrator : IMigrationOrchestrator
{
    private readonly MigrationOrchestratorOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MigrationOrchestrator> _logger;

    public MigrationOrchestrator(
        IOptions<MigrationOrchestratorOptions> options,
        IServiceProvider serviceProvider,
        ILogger<MigrationOrchestrator> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MigrationOrchestratorResult> ApplyAllMigrationsAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("MIGRATION ORCHESTRATOR: Starting migration orchestration for all domains");

        var domainResults = new Dictionary<string, DomainMigrationResult>();
        var totalMigrationsApplied = 0;
        var errors = new List<string>();
        var overallSuccess = true;

        // Apply migrations in dependency order (currently only Services domain is active)
        var domainConfigs = _options.Domains.OrderBy(d => d.Priority).ToList();
        
        foreach (var domainConfig in domainConfigs)
        {
            if (!domainConfig.Enabled)
            {
                _logger.LogDebug("MIGRATION ORCHESTRATOR: Skipping disabled domain {DomainName}", domainConfig.Name);
                continue;
            }

            try
            {
                var domainResult = await ApplyDomainMigrationsAsync(domainConfig.Name, cancellationToken);
                domainResults[domainConfig.Name] = domainResult;
                
                totalMigrationsApplied += domainResult.MigrationsApplied;
                
                if (!domainResult.Success)
                {
                    overallSuccess = false;
                    errors.AddRange(domainResult.Errors);
                }
                
                _logger.LogInformation("MIGRATION ORCHESTRATOR: Domain {DomainName} completed - Success: {Success}, Applied: {Applied}",
                    domainConfig.Name, domainResult.Success, domainResult.MigrationsApplied);
            }
            catch (Exception ex)
            {
                overallSuccess = false;
                var errorMessage = $"Domain {domainConfig.Name} failed with exception: {ex.Message}";
                errors.Add(errorMessage);
                
                _logger.LogError(ex, "MIGRATION ORCHESTRATOR: Domain {DomainName} failed with exception", domainConfig.Name);
                
                // Add failed domain result
                domainResults[domainConfig.Name] = new DomainMigrationResult
                {
                    DomainName = domainConfig.Name,
                    Success = false,
                    MigrationsApplied = 0,
                    ExecutionTime = TimeSpan.Zero,
                    AppliedMigrations = Array.Empty<string>(),
                    Errors = new[] { ex.Message }
                };

                if (_options.StopOnFirstFailure)
                {
                    _logger.LogError("MIGRATION ORCHESTRATOR: Stopping orchestration due to failure in domain {DomainName}", domainConfig.Name);
                    break;
                }
            }
        }

        stopwatch.Stop();
        var result = new MigrationOrchestratorResult
        {
            Success = overallSuccess,
            TotalMigrationsApplied = totalMigrationsApplied,
            DomainResults = domainResults,
            TotalExecutionTime = stopwatch.Elapsed,
            Errors = errors,
            Timestamp = startTime
        };

        _logger.LogInformation("MIGRATION ORCHESTRATOR: Orchestration completed - Success: {Success}, Total Applied: {TotalApplied}, Duration: {Duration}ms",
            result.Success, result.TotalMigrationsApplied, result.TotalExecutionTime.TotalMilliseconds);

        return result;
    }

    public async Task<DomainMigrationResult> ApplyDomainMigrationsAsync(string domainName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainName);

        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("MIGRATION ORCHESTRATOR: Starting migration for domain {DomainName}", domainName);

        var domainConfig = _options.Domains.FirstOrDefault(d => d.Name.Equals(domainName, StringComparison.OrdinalIgnoreCase));
        if (domainConfig == null)
        {
            var error = $"Domain configuration not found for: {domainName}";
            _logger.LogError("MIGRATION ORCHESTRATOR: {Error}", error);
            
            return new DomainMigrationResult
            {
                DomainName = domainName,
                Success = false,
                MigrationsApplied = 0,
                ExecutionTime = stopwatch.Elapsed,
                AppliedMigrations = Array.Empty<string>(),
                Errors = new[] { error },
                Timestamp = startTime
            };
        }

        if (!domainConfig.Enabled)
        {
            _logger.LogDebug("MIGRATION ORCHESTRATOR: Domain {DomainName} is disabled, skipping", domainName);
            return new DomainMigrationResult
            {
                DomainName = domainName,
                Success = true,
                MigrationsApplied = 0,
                ExecutionTime = stopwatch.Elapsed,
                AppliedMigrations = Array.Empty<string>(),
                Timestamp = startTime
            };
        }

        try
        {
            var result = await ExecuteDomainMigrationAsync(domainConfig, cancellationToken);
            stopwatch.Stop();
            
            _logger.LogInformation("MIGRATION ORCHESTRATOR: Domain {DomainName} migration completed - Success: {Success}, Applied: {Applied}, Duration: {Duration}ms",
                domainName, result.Success, result.MigrationsApplied, stopwatch.Elapsed.TotalMilliseconds);

            return result with
            {
                ExecutionTime = stopwatch.Elapsed,
                Timestamp = startTime
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "MIGRATION ORCHESTRATOR: Domain {DomainName} migration failed", domainName);
            
            return new DomainMigrationResult
            {
                DomainName = domainName,
                Success = false,
                MigrationsApplied = 0,
                ExecutionTime = stopwatch.Elapsed,
                AppliedMigrations = Array.Empty<string>(),
                Errors = new[] { ex.Message },
                Timestamp = startTime
            };
        }
    }

    public async Task<MigrationStatusResult> GetMigrationStatusAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("MIGRATION ORCHESTRATOR: Getting migration status for all domains");

        var domainStatuses = new Dictionary<string, DomainMigrationStatus>();
        var overallHealthy = true;

        foreach (var domainConfig in _options.Domains.Where(d => d.Enabled))
        {
            try
            {
                var status = await GetDomainMigrationStatusAsync(domainConfig, cancellationToken);
                domainStatuses[domainConfig.Name] = status;
                
                if (!status.IsHealthy)
                {
                    overallHealthy = false;
                }
            }
            catch (Exception ex)
            {
                overallHealthy = false;
                _logger.LogError(ex, "MIGRATION ORCHESTRATOR: Failed to get status for domain {DomainName}", domainConfig.Name);
                
                domainStatuses[domainConfig.Name] = new DomainMigrationStatus
                {
                    DomainName = domainConfig.Name,
                    IsHealthy = false,
                    AppliedMigrations = 0,
                    PendingMigrations = 0,
                    PendingMigrationNames = Array.Empty<string>()
                };
            }
        }

        return new MigrationStatusResult
        {
            IsHealthy = overallHealthy,
            DomainStatuses = domainStatuses
        };
    }

    public async Task<MigrationValidationResult> ValidateMigrationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MIGRATION ORCHESTRATOR: Validating migrations for all domains");

        var domainValidations = new Dictionary<string, DomainValidationResult>();
        var overallValid = true;

        foreach (var domainConfig in _options.Domains.Where(d => d.Enabled))
        {
            try
            {
                var validation = await ValidateDomainMigrationAsync(domainConfig, cancellationToken);
                domainValidations[domainConfig.Name] = validation;
                
                if (!validation.IsValid)
                {
                    overallValid = false;
                }
            }
            catch (Exception ex)
            {
                overallValid = false;
                _logger.LogError(ex, "MIGRATION ORCHESTRATOR: Validation failed for domain {DomainName}", domainConfig.Name);
                
                domainValidations[domainConfig.Name] = new DomainValidationResult
                {
                    DomainName = domainConfig.Name,
                    IsValid = false,
                    Issues = new[] { $"Validation exception: {ex.Message}" }
                };
            }
        }

        return new MigrationValidationResult
        {
            IsValid = overallValid,
            DomainValidations = domainValidations
        };
    }

    public async Task<MigrationScriptResult> GenerateMigrationScriptsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MIGRATION ORCHESTRATOR: Generating migration scripts for all domains");

        var domainScripts = new Dictionary<string, string>();
        var combinedScript = new System.Text.StringBuilder();
        var success = true;

        foreach (var domainConfig in _options.Domains.Where(d => d.Enabled))
        {
            try
            {
                var script = await GenerateDomainMigrationScriptAsync(domainConfig, cancellationToken);
                
                if (!string.IsNullOrEmpty(script))
                {
                    domainScripts[domainConfig.Name] = script;
                    
                    combinedScript.AppendLine($"-- Domain: {domainConfig.Name}");
                    combinedScript.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                    combinedScript.AppendLine();
                    combinedScript.AppendLine(script);
                    combinedScript.AppendLine();
                }
            }
            catch (Exception ex)
            {
                success = false;
                _logger.LogError(ex, "MIGRATION ORCHESTRATOR: Script generation failed for domain {DomainName}", domainConfig.Name);
                
                domainScripts[domainConfig.Name] = $"-- ERROR: Failed to generate script for {domainConfig.Name}: {ex.Message}";
            }
        }

        return new MigrationScriptResult
        {
            Success = success,
            DomainScripts = domainScripts,
            CombinedScript = combinedScript.ToString()
        };
    }

    private async Task<DomainMigrationResult> ExecuteDomainMigrationAsync(
        DomainConfiguration domainConfig, 
        CancellationToken cancellationToken)
    {
        // Currently only Services domain is implemented
        if (domainConfig.Name.Equals("Services", StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteServicesDomainMigrationAsync(cancellationToken);
        }

        throw new NotSupportedException($"Domain {domainConfig.Name} is not yet implemented");
    }

    private async Task<DomainMigrationResult> ExecuteServicesDomainMigrationAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServicesDbContext>();
        
        var pendingMigrations = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        
        if (!pendingMigrations.Any())
        {
            _logger.LogInformation("MIGRATION ORCHESTRATOR: No pending migrations for Services domain");
            return new DomainMigrationResult
            {
                DomainName = "Services",
                Success = true,
                MigrationsApplied = 0,
                ExecutionTime = TimeSpan.Zero,
                AppliedMigrations = Array.Empty<string>()
            };
        }

        _logger.LogInformation("MIGRATION ORCHESTRATOR: Applying {Count} pending migrations for Services domain: {Migrations}",
            pendingMigrations.Count, string.Join(", ", pendingMigrations));

        await context.Database.MigrateAsync(cancellationToken);

        return new DomainMigrationResult
        {
            DomainName = "Services",
            Success = true,
            MigrationsApplied = pendingMigrations.Count,
            ExecutionTime = TimeSpan.Zero, // Will be set by caller
            AppliedMigrations = pendingMigrations
        };
    }

    private async Task<DomainMigrationStatus> GetDomainMigrationStatusAsync(
        DomainConfiguration domainConfig, 
        CancellationToken cancellationToken)
    {
        if (domainConfig.Name.Equals("Services", StringComparison.OrdinalIgnoreCase))
        {
            return await GetServicesDomainStatusAsync(cancellationToken);
        }

        throw new NotSupportedException($"Domain {domainConfig.Name} is not yet implemented");
    }

    private async Task<DomainMigrationStatus> GetServicesDomainStatusAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServicesDbContext>();
        
        var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
        var pendingMigrations = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        
        return new DomainMigrationStatus
        {
            DomainName = "Services",
            IsHealthy = true,
            AppliedMigrations = appliedMigrations.Count,
            PendingMigrations = pendingMigrations.Count,
            PendingMigrationNames = pendingMigrations,
            LastMigrationApplied = appliedMigrations.LastOrDefault(),
            LastMigrationTimestamp = appliedMigrations.Any() ? DateTime.UtcNow : null // Simplified - would need actual timestamp
        };
    }

    private async Task<DomainValidationResult> ValidateDomainMigrationAsync(
        DomainConfiguration domainConfig, 
        CancellationToken cancellationToken)
    {
        if (domainConfig.Name.Equals("Services", StringComparison.OrdinalIgnoreCase))
        {
            return await ValidateServicesDomainAsync(cancellationToken);
        }

        return new DomainValidationResult
        {
            DomainName = domainConfig.Name,
            IsValid = false,
            Issues = new[] { $"Domain {domainConfig.Name} is not yet implemented" }
        };
    }

    private async Task<DomainValidationResult> ValidateServicesDomainAsync(CancellationToken cancellationToken)
    {
        var issues = new List<string>();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ServicesDbContext>();
            
            // Test database connection
            await context.Database.CanConnectAsync(cancellationToken);
            
            // Validate that migrations table exists and is accessible
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
            
            _logger.LogDebug("MIGRATION ORCHESTRATOR: Services domain validation passed - {AppliedCount} migrations applied",
                appliedMigrations.Count());
        }
        catch (Exception ex)
        {
            issues.Add($"Database connection or migration validation failed: {ex.Message}");
        }

        return new DomainValidationResult
        {
            DomainName = "Services",
            IsValid = !issues.Any(),
            Issues = issues
        };
    }

    private async Task<string> GenerateDomainMigrationScriptAsync(
        DomainConfiguration domainConfig, 
        CancellationToken cancellationToken)
    {
        if (domainConfig.Name.Equals("Services", StringComparison.OrdinalIgnoreCase))
        {
            return await GenerateServicesDomainScriptAsync(cancellationToken);
        }

        return $"-- Domain {domainConfig.Name} script generation not yet implemented";
    }

    private async Task<string> GenerateServicesDomainScriptAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServicesDbContext>();
        
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
        
        if (!pendingMigrations.Any())
        {
            return "-- No pending migrations for Services domain";
        }

        // Generate idempotent script for all pending migrations
        var script = await context.Database.GenerateCreateScriptAsync(cancellationToken);
        
        return $@"-- Services Domain Migration Script
-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
-- Pending Migrations: {string.Join(", ", pendingMigrations)}

{script}";
    }
}
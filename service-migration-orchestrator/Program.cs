using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Service.Migration.Orchestrator.Abstractions;
using Service.Migration.Orchestrator.Configuration;
using Service.Migration.Orchestrator.Services;
using Services.Shared.Infrastructure.Data;
using Infrastructure.Database.Options;
using System.CommandLine;

// Create command line interface for migration orchestrator
var rootCommand = new RootCommand("Service Migration Orchestrator - Coordinates domain migrations across the application")
{
    Name = "migration-orchestrator"
};

var applyCommand = new Command("apply", "Apply all pending migrations across domains");
var statusCommand = new Command("status", "Get migration status for all domains");
var validateCommand = new Command("validate", "Validate migration readiness");
var scriptCommand = new Command("script", "Generate migration scripts for manual execution");

// Add domain-specific commands
var domainOption = new Option<string?>("--domain", "Target specific domain for migration operations");
var dryRunOption = new Option<bool>("--dry-run", "Validate migrations without applying them");
var verboseOption = new Option<bool>("--verbose", "Enable verbose logging output");

applyCommand.AddOption(domainOption);
applyCommand.AddOption(dryRunOption);
applyCommand.AddOption(verboseOption);

statusCommand.AddOption(domainOption);
statusCommand.AddOption(verboseOption);

validateCommand.AddOption(verboseOption);
scriptCommand.AddOption(domainOption);
scriptCommand.AddOption(verboseOption);

rootCommand.AddCommand(applyCommand);
rootCommand.AddCommand(statusCommand);
rootCommand.AddCommand(validateCommand);
rootCommand.AddCommand(scriptCommand);

// Command handlers
applyCommand.SetHandler(async (domain, dryRun, verbose) =>
{
    var host = CreateHost(verbose, dryRun);
    await using var scope = host.Services.CreateAsyncScope();
    var orchestrator = scope.ServiceProvider.GetRequiredService<IMigrationOrchestrator>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        if (!string.IsNullOrEmpty(domain))
        {
            logger.LogInformation("MIGRATION ORCHESTRATOR: Applying migrations for domain {Domain}", domain);
            var result = await orchestrator.ApplyDomainMigrationsAsync(domain);
            
            Console.WriteLine($"Domain: {result.DomainName}");
            Console.WriteLine($"Success: {result.Success}");
            Console.WriteLine($"Applied: {result.MigrationsApplied}");
            Console.WriteLine($"Duration: {result.ExecutionTime.TotalSeconds:F2}s");
            
            if (result.Errors.Any())
            {
                Console.WriteLine("Errors:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }

            Environment.Exit(result.Success ? 0 : 1);
        }
        else
        {
            logger.LogInformation("MIGRATION ORCHESTRATOR: Applying migrations for all domains");
            var result = await orchestrator.ApplyAllMigrationsAsync();
            
            Console.WriteLine($"Overall Success: {result.Success}");
            Console.WriteLine($"Total Applied: {result.TotalMigrationsApplied}");
            Console.WriteLine($"Duration: {result.TotalExecutionTime.TotalSeconds:F2}s");
            Console.WriteLine();
            
            foreach (var domainResult in result.DomainResults)
            {
                Console.WriteLine($"Domain: {domainResult.Key}");
                Console.WriteLine($"  Success: {domainResult.Value.Success}");
                Console.WriteLine($"  Applied: {domainResult.Value.MigrationsApplied}");
                Console.WriteLine($"  Duration: {domainResult.Value.ExecutionTime.TotalSeconds:F2}s");
                
                if (domainResult.Value.Errors.Any())
                {
                    Console.WriteLine("  Errors:");
                    foreach (var error in domainResult.Value.Errors)
                    {
                        Console.WriteLine($"    - {error}");
                    }
                }
                Console.WriteLine();
            }

            Environment.Exit(result.Success ? 0 : 1);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "MIGRATION ORCHESTRATOR: Fatal error during migration");
        Console.WriteLine($"Fatal Error: {ex.Message}");
        Environment.Exit(1);
    }
}, domainOption, dryRunOption, verboseOption);

statusCommand.SetHandler(async (domain, verbose) =>
{
    var host = CreateHost(verbose);
    await using var scope = host.Services.CreateAsyncScope();
    var orchestrator = scope.ServiceProvider.GetRequiredService<IMigrationOrchestrator>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var result = await orchestrator.GetMigrationStatusAsync();
        
        Console.WriteLine($"Overall Healthy: {result.IsHealthy}");
        Console.WriteLine();
        
        foreach (var domainStatus in result.DomainStatuses)
        {
            Console.WriteLine($"Domain: {domainStatus.Key}");
            Console.WriteLine($"  Healthy: {domainStatus.Value.IsHealthy}");
            Console.WriteLine($"  Applied: {domainStatus.Value.AppliedMigrations}");
            Console.WriteLine($"  Pending: {domainStatus.Value.PendingMigrations}");
            
            if (domainStatus.Value.PendingMigrationNames.Any())
            {
                Console.WriteLine("  Pending Migrations:");
                foreach (var migration in domainStatus.Value.PendingMigrationNames)
                {
                    Console.WriteLine($"    - {migration}");
                }
            }
            
            Console.WriteLine($"  Last Applied: {domainStatus.Value.LastMigrationApplied ?? "None"}");
            Console.WriteLine();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "MIGRATION ORCHESTRATOR: Error getting migration status");
        Console.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
}, domainOption, verboseOption);

validateCommand.SetHandler(async (verbose) =>
{
    var host = CreateHost(verbose);
    await using var scope = host.Services.CreateAsyncScope();
    var orchestrator = scope.ServiceProvider.GetRequiredService<IMigrationOrchestrator>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var result = await orchestrator.ValidateMigrationsAsync();
        
        Console.WriteLine($"Overall Valid: {result.IsValid}");
        Console.WriteLine();
        
        foreach (var domainValidation in result.DomainValidations)
        {
            Console.WriteLine($"Domain: {domainValidation.Key}");
            Console.WriteLine($"  Valid: {domainValidation.Value.IsValid}");
            
            if (domainValidation.Value.Issues.Any())
            {
                Console.WriteLine("  Issues:");
                foreach (var issue in domainValidation.Value.Issues)
                {
                    Console.WriteLine($"    - {issue}");
                }
            }
            Console.WriteLine();
        }
        
        Environment.Exit(result.IsValid ? 0 : 1);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "MIGRATION ORCHESTRATOR: Error validating migrations");
        Console.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
}, verboseOption);

scriptCommand.SetHandler(async (domain, verbose) =>
{
    var host = CreateHost(verbose);
    await using var scope = host.Services.CreateAsyncScope();
    var orchestrator = scope.ServiceProvider.GetRequiredService<IMigrationOrchestrator>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var result = await orchestrator.GenerateMigrationScriptsAsync();
        
        if (!string.IsNullOrEmpty(domain))
        {
            if (result.DomainScripts.TryGetValue(domain, out var domainScript))
            {
                Console.WriteLine(domainScript);
            }
            else
            {
                Console.WriteLine($"No script found for domain: {domain}");
                Environment.Exit(1);
            }
        }
        else
        {
            Console.WriteLine(result.CombinedScript);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "MIGRATION ORCHESTRATOR: Error generating migration scripts");
        Console.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
}, domainOption, verboseOption);

// Execute command line interface
await rootCommand.InvokeAsync(args);

// Host creation helper
static IHost CreateHost(bool verbose = false, bool dryRun = false)
{
    var builder = Host.CreateDefaultBuilder()
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            
            if (verbose)
            {
                logging.SetMinimumLevel(LogLevel.Debug);
            }
            else
            {
                logging.SetMinimumLevel(LogLevel.Information);
            }
        })
        .ConfigureServices((context, services) =>
        {
            // Register migration orchestrator options
            services.Configure<MigrationOrchestratorOptions>(options =>
            {
                context.Configuration.GetSection(MigrationOrchestratorOptions.SectionName).Bind(options);
                
                // Override dry run mode if specified
                if (dryRun)
                {
                    options.DryRunMode = true;
                }
                
                // Set default Services domain configuration if not configured
                if (!options.Domains.Any())
                {
                    options.Domains.Add(new DomainConfiguration
                    {
                        Name = "Services",
                        Enabled = true,
                        Priority = 100,
                        ContextTypeName = "ServicesDbContext"
                    });
                }
                
                // Set default database configuration if not configured
                if (string.IsNullOrEmpty(options.Database.ConnectionString))
                {
                    options.Database = new DatabaseConfiguration
                    {
                        ConnectionString = context.Configuration.GetConnectionString("DefaultConnection") 
                                          ?? "Host=localhost;Database=international_center;Username=postgres;Password=postgres"
                    };
                }
            });

            // Register validators
            services.AddSingleton<IValidator<MigrationOrchestratorOptions>, MigrationOrchestratorOptionsValidator>();

            // Validate options on startup
            services.AddOptions<MigrationOrchestratorOptions>()
                .ValidateOnStart()
                .Validate<IValidator<MigrationOrchestratorOptions>>((options, validator) =>
                {
                    var result = validator.Validate(options);
                    return result.IsValid;
                });

            // Register database infrastructure options
            services.Configure<DatabaseConnectionOptions>(dbOptions =>
            {
                var orchestratorOptions = context.Configuration.GetSection(MigrationOrchestratorOptions.SectionName)
                    .Get<MigrationOrchestratorOptions>() ?? new MigrationOrchestratorOptions();
                
                dbOptions.ConnectionString = orchestratorOptions.Database.ConnectionString 
                                            ?? context.Configuration.GetConnectionString("DefaultConnection")
                                            ?? "Host=localhost;Database=international_center;Username=postgres;Password=postgres";
                dbOptions.CommandTimeoutSeconds = orchestratorOptions.Database.CommandTimeoutSeconds;
                dbOptions.RetryPolicy = new RetryPolicyOptions
                {
                    MaxRetryAttempts = orchestratorOptions.Database.RetryAttempts,
                    RetryDelaySeconds = orchestratorOptions.Database.RetryDelaySeconds
                };
            });

            // Register domain DbContexts
            services.AddDbContext<ServicesDbContext>((provider, options) =>
            {
                var dbOptions = provider.GetRequiredService<IOptions<DatabaseConnectionOptions>>().Value;
                options.UseNpgsql(dbOptions.ConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly("Services.Shared");
                    npgsqlOptions.CommandTimeout(dbOptions.CommandTimeoutSeconds);
                });
                
                options.EnableSensitiveDataLogging(verbose);
                options.EnableDetailedErrors(verbose);
            });

            // Register migration orchestrator
            services.AddSingleton<IMigrationOrchestrator, Services.MigrationOrchestrator>();
        });

    return builder.Build();
}
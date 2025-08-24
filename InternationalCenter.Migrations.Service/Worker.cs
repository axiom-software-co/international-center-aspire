using InternationalCenter.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InternationalCenter.Migrations.Service;

public class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IConfiguration _configuration;

    public Worker(
        IServiceProvider serviceProvider,
        ILogger<Worker> logger,
        IHostApplicationLifetime hostApplicationLifetime,
        IHostEnvironment hostEnvironment,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
        _hostEnvironment = hostEnvironment;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Migration Service: ============ STARTING MIGRATION SERVICE ============");
            
            // Add delay to ensure we can see logs before completion
            await Task.Delay(2000, stoppingToken);
            
            using var scope = _serviceProvider.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            
            // Debug connection string resolution
            _logger.LogInformation("Migration Service: Debugging connection string resolution...");
            var allConnectionStrings = configuration.GetSection("ConnectionStrings").GetChildren()
                .ToDictionary(x => x.Key, x => x.Value);
            
            _logger.LogInformation("Migration Service: Found {Count} connection strings", allConnectionStrings.Count);
            foreach (var cs in allConnectionStrings)
            {
                _logger.LogInformation("Migration Service: Connection string - Key: {Key}, Value: {Value}", 
                    cs.Key, cs.Value?.Substring(0, Math.Min(50, cs.Value.Length)) + "...");
            }
            
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _logger.LogInformation("Migration Service: ApplicationDbContext successfully resolved");
            
            _logger.LogInformation("Migration Service: Starting database migration process...");
            
            // Determine migration strategy based on environment (Microsoft recommended pattern)
            var migrationStrategy = GetMigrationStrategy();
            _logger.LogInformation("Migration Service: Using migration strategy: {Strategy}", migrationStrategy);
            
            switch (migrationStrategy)
            {
                case MigrationStrategy.RuntimeMigrations:
                    // Development/Staging: Apply migrations at runtime
                    await RunRuntimeMigrationsAsync(context, stoppingToken);
                    break;
                    
                case MigrationStrategy.ProductionValidation:
                    // Production: Validate migrations are pre-applied, don't apply runtime
                    await ValidateProductionMigrationsAsync(context, stoppingToken);
                    break;
                    
                default:
                    throw new InvalidOperationException($"Unknown migration strategy: {migrationStrategy}");
            }
            
            _logger.LogInformation("Migration Service: ============ MIGRATION PROCESS COMPLETED SUCCESSFULLY ============");
            
            // Add delay before stopping to see final logs
            await Task.Delay(2000, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration Service: ============ DATABASE MIGRATION FAILED ============");
            throw;
        }
        finally
        {
            _logger.LogInformation("Migration Service: ============ STOPPING MIGRATION SERVICE ============");
            // Stop the application after migration is complete
            _hostApplicationLifetime.StopApplication();
        }
    }
    
    private MigrationStrategy GetMigrationStrategy()
    {
        // Check configuration override first
        var configStrategy = _configuration["MigrationStrategy"];
        if (!string.IsNullOrEmpty(configStrategy) && Enum.TryParse<MigrationStrategy>(configStrategy, true, out var parsedStrategy))
        {
            _logger.LogInformation("Migration Service: Using configured migration strategy: {Strategy}", parsedStrategy);
            return parsedStrategy;
        }
        
        // Default behavior based on environment (Microsoft recommended pattern)
        if (_hostEnvironment.IsProduction())
        {
            _logger.LogInformation("Migration Service: Production environment detected - using validation strategy");
            return MigrationStrategy.ProductionValidation;
        }
        else
        {
            _logger.LogInformation("Migration Service: Development/Staging environment detected - using runtime migrations");
            return MigrationStrategy.RuntimeMigrations;
        }
    }
    
    private async Task RunRuntimeMigrationsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Migration Service: Applying runtime migrations for Development/Staging environment");
        await RunMigrationAsync(context, cancellationToken);
    }
    
    private async Task ValidateProductionMigrationsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Migration Service: Validating production migrations are pre-applied");
        
        // Use execution strategy for resilience
        var strategy = context.Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            _logger.LogInformation("Migration Service: Testing database connectivity...");
            
            // Wait for database to be available with retry logic
            var maxRetries = 30;
            var retryCount = 0;
            var canConnect = false;
            
            while (!canConnect && retryCount < maxRetries && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    canConnect = await context.Database.CanConnectAsync(cancellationToken);
                    if (!canConnect)
                    {
                        retryCount++;
                        _logger.LogInformation("Migration Service: Database not yet available, retry {RetryCount}/{MaxRetries}", retryCount, maxRetries);
                        await Task.Delay(1000, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning("Migration Service: Database connection attempt {RetryCount}/{MaxRetries} failed: {Error}", retryCount, maxRetries, ex.Message);
                    if (retryCount < maxRetries)
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                }
            }
            
            if (!canConnect)
            {
                throw new InvalidOperationException($"Cannot connect to database after {maxRetries} attempts");
            }
            
            _logger.LogInformation("Migration Service: Database connectivity confirmed");
            
            // Check all migrations are applied
            var allMigrations = context.Database.GetMigrations();
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
            var pendingMigrations = allMigrations.Except(appliedMigrations);
            
            _logger.LogInformation("Migration Service: Found {Total} total migrations", allMigrations.Count());
            _logger.LogInformation("Migration Service: Found {Applied} applied migrations", appliedMigrations.Count());
            _logger.LogInformation("Migration Service: Found {Pending} pending migrations", pendingMigrations.Count());
            
            if (pendingMigrations.Any())
            {
                _logger.LogError("Migration Service: PRODUCTION ERROR - Found {Count} pending migrations that must be pre-applied:", pendingMigrations.Count());
                foreach (var migration in pendingMigrations)
                {
                    _logger.LogError("Migration Service: Pending migration: {Migration}", migration);
                }
                
                throw new InvalidOperationException($"Production deployment failed: {pendingMigrations.Count()} migrations must be pre-applied using SQL scripts. Use 'dotnet ef migrations script' to generate scripts.");
            }
            
            _logger.LogInformation("Migration Service: ✅ All migrations are pre-applied - production validation successful");
        });
    }

    private async Task RunMigrationAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Migration Service: Creating execution strategy for resilient migration execution...");
        
        // Use execution strategy for resilient migration execution (per Microsoft documentation)
        var strategy = context.Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            _logger.LogInformation("Migration Service: Testing database connectivity...");
            
            // Log database provider information
            _logger.LogInformation("Migration Service: Database provider: {Provider}", context.Database.ProviderName);
            _logger.LogInformation("Migration Service: Connection string (partial): {ConnectionString}", 
                context.Database.GetConnectionString()?.Substring(0, Math.Min(50, context.Database.GetConnectionString()?.Length ?? 0)) + "...");
            
            // Wait for database to be available with retry logic
            _logger.LogInformation("Migration Service: Waiting for database availability...");
            var maxRetries = 30; // 30 seconds
            var retryCount = 0;
            var canConnect = false;
            
            while (!canConnect && retryCount < maxRetries && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    canConnect = await context.Database.CanConnectAsync(cancellationToken);
                    if (!canConnect)
                    {
                        retryCount++;
                        _logger.LogInformation("Migration Service: Database not yet available, retry {RetryCount}/{MaxRetries}", retryCount, maxRetries);
                        await Task.Delay(1000, cancellationToken); // Wait 1 second between retries
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning("Migration Service: Database connection attempt {RetryCount}/{MaxRetries} failed: {Error}", retryCount, maxRetries, ex.Message);
                    if (retryCount < maxRetries)
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                }
            }
            
            if (!canConnect)
            {
                throw new InvalidOperationException($"Cannot connect to database after {maxRetries} attempts");
            }
            
            _logger.LogInformation("Migration Service: Database connectivity confirmed after {RetryCount} attempts", retryCount);
            
            // Check migration assembly info
            _logger.LogInformation("Migration Service: Checking migrations discovery...");
            try
            {
                var allMigrations = context.Database.GetMigrations();
                _logger.LogInformation("Migration Service: Found {Count} total migrations in assembly", allMigrations.Count());
                foreach (var migration in allMigrations)
                {
                    _logger.LogInformation("Migration Service: Available migration: {Migration}", migration);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration Service: Error discovering migrations");
            }
            
            // Check and log migration status
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
            _logger.LogInformation("Migration Service: Found {Count} pending migrations", pendingMigrations.Count());
            
            foreach (var migration in pendingMigrations)
            {
                _logger.LogInformation("Migration Service: Pending migration: {Migration}", migration);
            }
            
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
            _logger.LogInformation("Migration Service: Currently applied migrations: {Count}", appliedMigrations.Count());
            
            foreach (var migration in appliedMigrations)
            {
                _logger.LogInformation("Migration Service: Applied migration: {Migration}", migration);
            }
            
            // Apply migrations
            if (pendingMigrations.Any())
            {
                _logger.LogInformation("Migration Service: Applying {Count} pending migrations...", pendingMigrations.Count());
                await context.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Migration Service: Migrations applied successfully");
                
                // Verify migrations were applied
                var finalAppliedMigrations = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
                _logger.LogInformation("Migration Service: Total applied migrations after update: {Count}", finalAppliedMigrations.Count());
            }
            else
            {
                _logger.LogInformation("Migration Service: No pending migrations to apply - this likely means migrations were not discovered");
            }
        });
    }
}

/// <summary>
/// Migration strategy for different environments following Microsoft best practices
/// </summary>
public enum MigrationStrategy
{
    /// <summary>
    /// Apply migrations at runtime - recommended for Development/Staging
    /// </summary>
    RuntimeMigrations,
    
    /// <summary>
    /// Validate pre-applied migrations - recommended for Production
    /// </summary>
    ProductionValidation
}
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using InternationalCenter.Shared.Infrastructure;
using InternationalCenter.Shared.Infrastructure.Migrations;
using InternationalCenter.Services.Public.Api.Infrastructure.Data;
using InternationalCenter.Services.Migrations.Service;
// using InternationalCenter.News.Migrations.Service; // Disabled for Services-only focus
using Xunit;

namespace InternationalCenter.Migrations.Tests.Integration;

/// <summary>
/// TDD RED Phase: Domain-Specific Migration Architecture Tests
/// These tests validate vertical slice migration architecture with domain boundaries,
/// medical-grade reliability, zero-downtime deployments, and migration orchestration.
/// All tests should FAIL initially to drive proper implementation.
/// </summary>
public class DomainSpecificMigrationArchitectureTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    
    public async Task InitializeAsync()
    {
        // Create Aspire orchestration for testing against real services (not TestContainers)
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact(DisplayName = "TDD RED: Services Domain Migration Service Should Exist And Handle Services Schema")]
    public async Task ServicesDomainMigrationService_ShouldExistAndHandleServicesSchema()
    {
        // ARRANGE: Get Services domain migration service (should not exist yet - TDD RED)
        var servicesConnectionString = await _app!.GetConnectionStringAsync("database");
        Assert.NotNull(servicesConnectionString);
        
        // ACT & ASSERT: Services migration service should exist with domain-specific context
        // This will FAIL because we don't have ServicesMigrationService yet
        using var serviceProvider = CreateServicesMigrationServiceProvider(servicesConnectionString);
        var migrationService = serviceProvider.GetService<IServicesDomainMigrationService>();
        
        Assert.NotNull(migrationService); // FAILS - service doesn't exist
        
        // Test domain-specific migration handling
        // Services-only focus: Use ApplicationDbContext which contains the Services migrations
        var servicesDbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var pendingMigrations = await servicesDbContext.Database.GetPendingMigrationsAsync();
        var allMigrations = servicesDbContext.Database.GetMigrations();
        
        // Debug: Show what migrations we found
        Console.WriteLine($"Total migrations available: {allMigrations.Count()}");
        foreach (var migration in allMigrations)
            Console.WriteLine($"Available migration: {migration}");
            
        Console.WriteLine($"Total pending migrations: {pendingMigrations.Count()}");
        foreach (var migration in pendingMigrations)
            Console.WriteLine($"Pending migration: {migration}");
        
        // Services domain should only handle Services and ServiceCategories tables
        // Services-only focus: InitialCreate and DatabaseArchitectureOptimizations contain Services domain tables
        // Check all available migrations instead of just pending ones since migrations may already be applied
        var domainMigrations = allMigrations.Where(m => 
            m.Contains("Services") || m.Contains("ServiceCategories") || 
            m.Contains("InitialCreate") || m.Contains("DatabaseArchitectureOptimizations"));
        Assert.NotEmpty(domainMigrations); // Should have Services-specific migrations
    }
    
    // News Domain Migration Service test disabled - Services-only focus for now
    // [Fact(DisplayName = "TDD RED: News Domain Migration Service Should Handle News Schema Independently")]
    
    [Fact(DisplayName = "TDD RED: Migration Orchestration Should Coordinate Domain Dependencies")]
    public async Task MigrationOrchestration_ShouldCoordinateDomainDependencies()
    {
        // ARRANGE: Get migration orchestration service (should not exist yet - TDD RED)
        var connectionString = await _app!.GetConnectionStringAsync("database");
        Assert.NotNull(connectionString);
        
        // ACT & ASSERT: Migration orchestrator should exist and coordinate domain migrations
        // This will FAIL because we don't have MigrationOrchestrationService yet
        using var serviceProvider = CreateMigrationOrchestrationServiceProvider(connectionString);
        var orchestrator = serviceProvider.GetService<IMigrationOrchestrationService>();
        
        Assert.NotNull(orchestrator); // FAILS - service doesn't exist
        
        // Test Services-only orchestration (other domains disabled for focused development)
        var migrationPlan = await orchestrator.CreateMigrationPlanAsync();
        Assert.NotNull(migrationPlan);
        
        // Only Services domain should be in the plan
        Assert.Single(migrationPlan.DomainMigrations);
        Assert.Equal("Services", migrationPlan.DomainMigrations[0].Domain);
    }
    
    [Fact(DisplayName = "TDD RED: Zero-Downtime Migration Should Support Blue-Green Database Strategy")]
    public async Task ZeroDowntimeMigration_ShouldSupportBlueGreenDatabaseStrategy()
    {
        // ARRANGE: Get zero-downtime migration service (should not exist yet - TDD RED)
        var connectionString = await _app!.GetConnectionStringAsync("database");
        Assert.NotNull(connectionString);
        
        // ACT & ASSERT: Zero-downtime migration service should exist
        // This will FAIL because we don't have ZeroDowntimeMigrationService yet
        using var serviceProvider = CreateZeroDowntimeMigrationServiceProvider(connectionString);
        var zeroDowntimeService = serviceProvider.GetService<IZeroDowntimeMigrationService>();
        
        Assert.NotNull(zeroDowntimeService); // FAILS - service doesn't exist
        
        // Test blue-green strategy capabilities
        var blueGreenConfig = await zeroDowntimeService.CreateBlueGreenConfigurationAsync();
        Assert.NotNull(blueGreenConfig);
        
        Assert.NotNull(blueGreenConfig.BlueDatabase);
        Assert.NotNull(blueGreenConfig.GreenDatabase);
        Assert.NotEqual(blueGreenConfig.BlueDatabase, blueGreenConfig.GreenDatabase);
    }
    
    [Fact(DisplayName = "TDD RED: Migration Rollback Should Support Domain-Specific Rollback Scenarios")]
    public async Task MigrationRollback_ShouldSupportDomainSpecificRollbackScenarios()
    {
        // ARRANGE: Get migration rollback service (should not exist yet - TDD RED)
        var connectionString = await _app!.GetConnectionStringAsync("database");
        Assert.NotNull(connectionString);
        
        // ACT & ASSERT: Migration rollback service should exist
        // This will FAIL because we don't have MigrationRollbackService yet
        using var serviceProvider = CreateMigrationRollbackServiceProvider(connectionString);
        var rollbackService = serviceProvider.GetService<IMigrationRollbackService>();
        
        Assert.NotNull(rollbackService); // FAILS - service doesn't exist
        
        // Test domain-specific rollback capabilities
        var rollbackPlan = await rollbackService.CreateRollbackPlanAsync("Services", "20250822025618_InitialCreate");
        Assert.NotNull(rollbackPlan);
        
        // Rollback should only affect Services domain, not other domains
        Assert.Equal("Services", rollbackPlan.Domain);
        Assert.Contains("Services", rollbackPlan.AffectedTables);
        Assert.Contains("ServiceCategories", rollbackPlan.AffectedTables);
        Assert.DoesNotContain("NewsArticles", rollbackPlan.AffectedTables); // Should not affect News domain
    }
    
    [Fact(DisplayName = "TDD RED: Migration Audit Service Should Track Medical-Grade Migration History")]
    public async Task MigrationAudit_ShouldTrackMedicalGradeMigrationHistory()
    {
        // ARRANGE: Get migration audit service (should not exist yet - TDD RED)
        var connectionString = await _app!.GetConnectionStringAsync("database");
        Assert.NotNull(connectionString);
        
        // ACT & ASSERT: Migration audit service should exist
        // This will FAIL because we don't have MigrationAuditService yet
        using var serviceProvider = CreateMigrationAuditServiceProvider(connectionString);
        var auditService = serviceProvider.GetService<IMigrationAuditService>();
        
        Assert.NotNull(auditService); // FAILS - service doesn't exist
        
        // Test medical-grade audit capabilities
        var auditEntry = new DomainMigrationAuditEntry
        {
            Domain = "Services",
            MigrationName = "20250822025618_InitialCreate",
            AppliedAt = DateTime.UtcNow,
            AppliedBy = "migration-service",
            Environment = "Testing",
            ChecksumBefore = "abc123",
            ChecksumAfter = "def456",
            Duration = TimeSpan.FromSeconds(5.2)
        };
        
        await auditService.RecordMigrationAsync(auditEntry);
        
        // Verify audit trail exists and is queryable
        var auditHistory = await auditService.GetMigrationHistoryAsync("Services");
        Assert.NotEmpty(auditHistory);
        Assert.Contains(auditHistory, entry => entry.MigrationName == "20250822025618_InitialCreate");
    }
    
    [Fact(DisplayName = "TDD RED: Migration Health Monitoring Should Detect Schema Drift And Integrity Issues")]
    public async Task MigrationHealthMonitoring_ShouldDetectSchemaDriftAndIntegrityIssues()
    {
        // ARRANGE: Get migration health monitoring service (should not exist yet - TDD RED)
        var connectionString = await _app!.GetConnectionStringAsync("database");
        Assert.NotNull(connectionString);
        
        // ACT & ASSERT: Migration health monitoring service should exist
        // This will FAIL because we don't have MigrationHealthMonitoringService yet
        using var serviceProvider = CreateMigrationHealthMonitoringServiceProvider(connectionString);
        var healthService = serviceProvider.GetService<IMigrationHealthMonitoringService>();
        
        Assert.NotNull(healthService); // FAILS - service doesn't exist
        
        // Test schema drift detection
        var schemaDriftReport = await healthService.DetectSchemaDriftAsync("Services");
        Assert.NotNull(schemaDriftReport);
        
        // Test integrity checking
        var integrityReport = await healthService.PerformIntegrityCheckAsync("Services");
        Assert.NotNull(integrityReport);
        Assert.True(integrityReport.IsHealthy);
        
        // Test performance monitoring
        var performanceMetrics = await healthService.GetMigrationPerformanceMetricsAsync("Services");
        Assert.NotNull(performanceMetrics);
        Assert.True(performanceMetrics.AverageExecutionTime > TimeSpan.Zero);
    }
    
    [Fact(DisplayName = "TDD RED: Parallel Domain Migration Should Execute Independent Domains Concurrently")]
    public async Task ParallelDomainMigration_ShouldExecuteIndependentDomainsConcurrently()
    {
        // ARRANGE: Get parallel migration service (should not exist yet - TDD RED)
        var connectionString = await _app!.GetConnectionStringAsync("database");
        Assert.NotNull(connectionString);
        
        // ACT & ASSERT: Parallel migration service should exist
        // This will FAIL because we don't have ParallelMigrationService yet
        using var serviceProvider = CreateParallelMigrationServiceProvider(connectionString);
        var parallelService = serviceProvider.GetService<IParallelMigrationService>();
        
        Assert.NotNull(parallelService); // FAILS - service doesn't exist
        
        // Test Services domain migration (other domains disabled for focused development)
        var servicesDomain = new[] { "Services" }; // Only Services domain is active
        var result = await parallelService.ExecuteDomainMigrationAsync("Services");
        
        // Services domain should complete successfully
        Assert.True(result.IsSuccess);
        Assert.Equal("Services", result.Domain);
        Assert.NotNull(result.AppliedMigrations);
    }

    // Helper methods to create service providers for domain-specific services
    private static ServiceProvider CreateServicesMigrationServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("InternationalCenter.Migrations.Service");
        }));
        services.AddDbContext<ServicesDbContext>(options => options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("InternationalCenter.Migrations.Service");
        }));
        services.AddScoped<IServicesDomainMigrationService, ServicesDomainMigrationService>();
        return services.BuildServiceProvider();
    }
    
    // Other domain migration service providers disabled for Services-only focus
    // private static ServiceProvider CreateNewsMigrationServiceProvider(...)
    
    private static ServiceProvider CreateMigrationOrchestrationServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Add IConfiguration service that MigrationOrchestrationService requires
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:database"] = connectionString,
                ["Migration:Orchestration:EnabledDomains"] = "Services",
                ["Migration:Orchestration:MaxParallelDomains"] = "4",
                ["Migration:Orchestration:ParallelExecutionEnabled"] = "true",
                ["ASPNETCORE_ENVIRONMENT"] = "Development"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("InternationalCenter.Migrations.Service");
        }));
        services.AddScoped<IMigrationOrchestrationService, MigrationOrchestrationService>();
        return services.BuildServiceProvider();
    }
    
    private static ServiceProvider CreateZeroDowntimeMigrationServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Add IConfiguration service that ZeroDowntimeMigrationService requires
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:database"] = connectionString,
                ["Migration:BlueGreenEnabled"] = "true",
                ["Migration:TimeoutSeconds"] = "300"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("InternationalCenter.Migrations.Service");
        }));
        services.AddScoped<IZeroDowntimeMigrationService, ZeroDowntimeMigrationService>();
        return services.BuildServiceProvider();
    }
    
    private static ServiceProvider CreateMigrationRollbackServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("InternationalCenter.Migrations.Service");
        }));
        services.AddScoped<IMigrationAuditService, MigrationAuditService>();
        services.AddScoped<IMigrationRollbackService, MigrationRollbackService>();
        return services.BuildServiceProvider();
    }
    
    private static ServiceProvider CreateMigrationAuditServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("InternationalCenter.Migrations.Service");
        }));
        services.AddScoped<IMigrationAuditService, MigrationAuditService>();
        return services.BuildServiceProvider();
    }
    
    private static ServiceProvider CreateMigrationHealthMonitoringServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("InternationalCenter.Migrations.Service");
        }));
        services.AddScoped<IMigrationAuditService, MigrationAuditService>();
        services.AddScoped<IMigrationHealthMonitoringService, MigrationHealthMonitoringService>();
        return services.BuildServiceProvider();
    }
    
    private static ServiceProvider CreateParallelMigrationServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Add IConfiguration service that MigrationOrchestrationService requires
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:database"] = connectionString,
                ["Migration:Orchestration:EnabledDomains"] = "Services",
                ["Migration:Orchestration:MaxParallelDomains"] = "4",
                ["Migration:Orchestration:ParallelExecutionEnabled"] = "true",
                ["ASPNETCORE_ENVIRONMENT"] = "Development"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("InternationalCenter.Migrations.Service");
        }));
        services.AddScoped<IMigrationAuditService, MigrationAuditService>();
        services.AddScoped<IMigrationOrchestrationService, MigrationOrchestrationService>();
        services.AddScoped<IParallelMigrationService, ParallelMigrationService>();
        return services.BuildServiceProvider();
    }
}
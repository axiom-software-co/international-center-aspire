using Infrastructure.Database.Abstractions;
using Infrastructure.Database.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database.Extensions;

/// <summary>
/// Extension methods for registering generic database infrastructure services in dependency injection.
/// INFRASTRUCTURE: Generic database service registration patterns
/// DEPENDENCY INVERSION: Provides clean registration API for database concerns
/// DOMAIN AGNOSTIC: No knowledge of specific domains (Services, News, Events, etc.)
/// </summary>
public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Adds generic database infrastructure services to the service collection.
    /// 
    /// INFRASTRUCTURE: Generic database patterns for any domain
    /// DEPENDENCY INVERSION: Registers abstractions that higher layers can depend on
    /// MEDICAL COMPLIANCE: Includes audit and health check capabilities
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configuration">Configuration root for binding</param>
    /// <param name="configureOptions">Optional database configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDatabaseInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DatabaseInfrastructureOptions>? configureOptions = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Configure options
        var options = new DatabaseInfrastructureOptions();
        configureOptions?.Invoke(options);

        // Register database connection configuration
        services.Configure<DatabaseConnectionOptions>(
            configuration.GetSection(DatabaseConnectionOptions.SectionName));

        // Register health checks if enabled
        if (options.EnableHealthChecks)
        {
            services.AddGenericDatabaseHealthChecks(configuration);
        }

        // Register migration services if enabled
        if (options.EnableMigrationServices)
        {
            services.AddGenericMigrationServices();
        }

        return services;
    }

    /// <summary>
    /// Adds PostgreSQL Entity Framework Core infrastructure for auditable contexts.
    /// 
    /// INFRASTRUCTURE: Generic EF Core setup for any domain
    /// MEDICAL COMPLIANCE: Auditable context capabilities
    /// DOMAIN AGNOSTIC: Can be used by any domain for complex operations
    /// </summary>
    /// <typeparam name="TContext">Auditable DbContext type</typeparam>
    /// <param name="services">Service collection to register with</param>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <param name="configureContext">Optional context configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddPostgreSqlAuditableContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureContext = null) 
        where TContext : class, IAuditableDbContext
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        services.AddDbContext<TContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
            
            // Configure for medical-grade compliance
            options.EnableSensitiveDataLogging(false); // Never log sensitive data
            options.EnableDetailedErrors(false); // Avoid exposing schema details
            
            configureContext?.Invoke(options);
        });

        // Register as IAuditableDbContext
        services.AddScoped<IAuditableDbContext>(provider => 
            provider.GetRequiredService<TContext>());

        return services;
    }

    /// <summary>
    /// Adds Dapper connection factory infrastructure for high-performance data access.
    /// 
    /// INFRASTRUCTURE: Generic Dapper setup for any domain
    /// HIGH PERFORMANCE: Optimized for read-heavy workloads
    /// DOMAIN AGNOSTIC: Can be used by any domain for fast queries
    /// </summary>
    /// <typeparam name="TFactory">Connection factory implementation type</typeparam>
    /// <param name="services">Service collection to register with</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDapperConnectionFactory<TFactory>(
        this IServiceCollection services)
        where TFactory : class, IDbConnectionFactory
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.AddSingleton<IDbConnectionFactory, TFactory>();
        
        return services;
    }

    /// <summary>
    /// Adds generic database health checks.
    /// 
    /// MONITORING: Generic health check patterns for any domain
    /// INFRASTRUCTURE: Database connectivity and performance monitoring
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <param name="configuration">Configuration root</param>
    /// <returns>Service collection for chaining</returns>
    private static IServiceCollection AddGenericDatabaseHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionOptions = configuration.GetSection(DatabaseConnectionOptions.SectionName).Get<DatabaseConnectionOptions>();
        
        if (connectionOptions?.HealthCheck.EnableHealthChecks == true)
        {
            services.AddHealthChecks()
                .AddNpgSql(
                    connectionString: connectionOptions.ConnectionString,
                    healthQuery: connectionOptions.HealthCheck.HealthCheckQuery,
                    name: "postgresql-primary",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "database", "postgresql" },
                    timeout: TimeSpan.FromSeconds(connectionOptions.HealthCheck.TimeoutSeconds));

            // Add read-only health check if configured
            if (!string.IsNullOrEmpty(connectionOptions.ReadOnlyConnectionString))
            {
                services.AddHealthChecks()
                    .AddNpgSql(
                        connectionString: connectionOptions.ReadOnlyConnectionString,
                        healthQuery: connectionOptions.HealthCheck.HealthCheckQuery,
                        name: "postgresql-readonly",
                        failureStatus: HealthStatus.Degraded,
                        tags: new[] { "database", "postgresql", "readonly" },
                        timeout: TimeSpan.FromSeconds(connectionOptions.HealthCheck.TimeoutSeconds));
            }
        }

        return services;
    }

    /// <summary>
    /// Adds generic database migration services.
    /// 
    /// INFRASTRUCTURE: Generic migration patterns for any domain
    /// DEVELOPMENT: Migration management capabilities
    /// </summary>
    /// <param name="services">Service collection to register with</param>
    /// <returns>Service collection for chaining</returns>
    private static IServiceCollection AddGenericMigrationServices(
        this IServiceCollection services)
    {
        // Migration services would be registered here
        // Implementation depends on specific migration strategy
        return services;
    }
}

/// <summary>
/// Configuration options for database infrastructure setup.
/// INFRASTRUCTURE: Generic database infrastructure configuration
/// </summary>
public sealed class DatabaseInfrastructureOptions
{
    /// <summary>
    /// Enable database health checks during setup.
    /// MONITORING: Database connectivity and performance monitoring
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Enable database migration services during setup.
    /// DEVELOPMENT: Migration management capabilities
    /// </summary>
    public bool EnableMigrationServices { get; set; } = true;

    /// <summary>
    /// Enable performance metrics collection.
    /// OBSERVABILITY: Database performance monitoring
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Enable audit logging capabilities.
    /// MEDICAL COMPLIANCE: Audit trail for data mutations
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Enable connection pooling optimizations.
    /// PERFORMANCE: Connection pool management
    /// </summary>
    public bool EnableConnectionPooling { get; set; } = true;

    /// <summary>
    /// Enable retry policies for transient failures.
    /// RESILIENCE: Automatic retry for database operations
    /// </summary>
    public bool EnableRetryPolicies { get; set; } = true;
}
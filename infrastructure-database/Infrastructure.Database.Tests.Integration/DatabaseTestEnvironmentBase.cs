using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Dapper;
using InternationalCenter.Shared.Tests.Abstractions;
using Infrastructure.Database.Tests.Contracts;
using Xunit.Abstractions;

namespace Infrastructure.Database.Tests;

/// <summary>
/// Base implementation for PostgreSQL database testing environment with Aspire orchestration
/// Provides PostgreSQL container management and test context for medical-grade database testing
/// </summary>
/// <typeparam name="TTestContext">The database-specific test context type</typeparam>
public abstract class DatabaseTestEnvironmentBase<TTestContext> : IDatabaseTestEnvironmentContract<TTestContext>
    where TTestContext : class, IDatabaseTestContext
{
    protected ILogger Logger { get; }
    protected ITestOutputHelper? Output { get; }
    protected Dictionary<string, PostgreSqlContainer> ActiveContainers { get; } = new();
    protected Dictionary<string, NpgsqlConnection> ActiveConnections { get; } = new();
    
    protected DatabaseTestEnvironmentBase(ILogger logger, ITestOutputHelper? output = null)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Output = output;
    }

    /// <summary>
    /// Sets up the PostgreSQL database testing environment with Aspire orchestration
    /// Contract: Must provide isolated PostgreSQL container with proper configuration for medical-grade compliance testing
    /// </summary>
    public virtual async Task<TTestContext> SetupDatabaseTestEnvironmentAsync(
        DatabaseTestEnvironmentOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Setting up database test environment with PostgreSQL container");
            
            // Create and configure PostgreSQL container
            var postgreSqlContainer = await CreatePostgreSqlContainerAsync(options, cancellationToken);
            
            // Start PostgreSQL container
            await postgreSqlContainer.StartAsync(cancellationToken);
            
            var connectionString = postgreSqlContainer.GetConnectionString();
            Logger.LogInformation("PostgreSQL container started with connection string: {ConnectionString}", 
                connectionString.Replace(options.Password, "***"));
            
            // Create PostgreSQL connection
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // Store active resources for cleanup
            var containerId = Guid.NewGuid().ToString();
            ActiveContainers[containerId] = postgreSqlContainer;
            ActiveConnections[containerId] = connection;
            
            // Create test context
            var context = await CreateTestContextAsync(
                connection, 
                connectionString,
                options, 
                containerId, 
                cancellationToken);
            
            // Run migrations if required
            if (options.RunMigrations)
            {
                await RunTestMigrationsAsync(context, cancellationToken);
            }
            
            // Seed test data if required
            if (options.SeedTestData)
            {
                await SeedTestDataAsync(context, cancellationToken);
            }
            
            // Validate PostgreSQL environment
            await ValidateDatabaseSpecificEnvironmentAsync(context, cancellationToken);
            
            Logger.LogInformation("Database test environment setup completed successfully");
            return context;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to setup database test environment");
            throw;
        }
    }

    /// <summary>
    /// Executes a database test operation with performance tracking and transaction management
    /// Contract: Must provide comprehensive error handling and connection lifecycle management
    /// </summary>
    public virtual async Task<T> ExecuteDatabaseTestAsync<T>(
        TTestContext context,
        Func<TTestContext, Task<T>> testOperation,
        string operationName,
        PerformanceThreshold? performanceThreshold = null,
        CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (testOperation == null) throw new ArgumentNullException(nameof(testOperation));
        
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            Logger.LogInformation("Executing database test operation: {OperationName}", operationName);
            
            var result = await testOperation(context);
            
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;
            
            Logger.LogInformation("Database test operation completed: {OperationName} in {Duration}ms", 
                operationName, duration.TotalMilliseconds);
            
            // Validate performance threshold if provided
            if (performanceThreshold != null)
            {
                await ValidatePerformanceThreshold(operationName, duration, performanceThreshold);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Database test operation failed: {OperationName} after {Duration}ms", 
                operationName, stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Validates PostgreSQL database environment configuration and connectivity
    /// Contract: Must validate database server connectivity, audit logging setup, and schema validation
    /// </summary>
    public virtual async Task ValidateDatabaseEnvironmentAsync(
        TTestContext context,
        CancellationToken cancellationToken = default)
    {
        if (context?.Connection == null)
            throw new InvalidOperationException("PostgreSQL connection is not available");
        
        try
        {
            Logger.LogInformation("Validating PostgreSQL database environment");
            
            // Test basic connectivity
            var connectionTest = await context.Connection.QuerySingleAsync<int>("SELECT 1");
            if (connectionTest != 1)
            {
                throw new InvalidOperationException("Basic connectivity test failed");
            }
            
            // Validate database version
            var version = await context.Connection.QuerySingleAsync<string>("SELECT VERSION()");
            Logger.LogInformation("PostgreSQL version: {Version}", version);
            
            // Validate database configuration
            var maxConnections = await context.Connection.QuerySingleAsync<int>("SHOW max_connections");
            Logger.LogInformation("Max connections: {MaxConnections}", maxConnections);
            
            // Test transaction capability
            using var transaction = await context.Connection.BeginTransactionAsync(cancellationToken);
            var transactionTest = await context.Connection.QuerySingleAsync<int>("SELECT 1", transaction: transaction);
            await transaction.RollbackAsync(cancellationToken);
            
            if (transactionTest != 1)
            {
                throw new InvalidOperationException("Transaction capability test failed");
            }
            
            // Validate audit logging setup if enabled
            await ValidateAuditLoggingSetupAsync(context, cancellationToken);
            
            Logger.LogInformation("PostgreSQL database environment validation completed successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PostgreSQL database environment validation failed");
            throw;
        }
    }

    /// <summary>
    /// Cleans up PostgreSQL database environment including container cleanup and data purging
    /// Contract: Must ensure complete cleanup of database data and container resources for test isolation
    /// </summary>
    public virtual async Task CleanupDatabaseEnvironmentAsync(
        TTestContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Cleaning up database test environment");
            
            // Clean up test entities registered in context
            if (context?.CreatedTestEntities != null)
            {
                foreach (var entity in context.CreatedTestEntities)
                {
                    if (entity is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    else if (entity is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                }
                context.CreatedTestEntities.Clear();
            }
            
            // Clean up database data
            if (context?.Connection != null && context.Connection.State == System.Data.ConnectionState.Open)
            {
                await CleanupDatabaseDataAsync(context, cancellationToken);
            }
            
            // Dispose connections and containers
            foreach (var (containerId, connection) in ActiveConnections.ToList())
            {
                try
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        await connection.CloseAsync();
                    }
                    await connection.DisposeAsync();
                    ActiveConnections.Remove(containerId);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to dispose database connection: {ContainerId}", containerId);
                }
            }
            
            foreach (var (containerId, container) in ActiveContainers.ToList())
            {
                try
                {
                    await container.StopAsync(cancellationToken);
                    await container.DisposeAsync();
                    ActiveContainers.Remove(containerId);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to dispose PostgreSQL container: {ContainerId}", containerId);
                }
            }
            
            Logger.LogInformation("Database test environment cleanup completed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cleanup database test environment");
            throw;
        }
    }

    /// <summary>
    /// Creates and configures PostgreSQL container for testing
    /// </summary>
    protected virtual async Task<PostgreSqlContainer> CreatePostgreSqlContainerAsync(
        DatabaseTestEnvironmentOptions options,
        CancellationToken cancellationToken = default)
    {
        var containerBuilder = new PostgreSqlBuilder()
            .WithImage(options.PostgreSQLImage)
            .WithPortBinding(options.PostgreSQLPort, true)
            .WithDatabase(options.DatabaseName)
            .WithUsername(options.Username)
            .WithPassword(options.Password)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilPortIsAvailable(5432)
                .UntilCommandIsCompleted("pg_isready"));
        
        // Add PostgreSQL configuration parameters
        var configParams = new List<string>();
        
        if (options.EnableDatabaseLogging)
        {
            configParams.AddRange(new[]
            {
                "-c", "log_statement=all",
                "-c", "log_duration=on",
                "-c", "log_min_duration_statement=0"
            });
        }
        
        if (options.EnableAuditLogging)
        {
            configParams.AddRange(new[]
            {
                "-c", "log_connections=on",
                "-c", "log_disconnections=on",
                "-c", "log_checkpoints=on"
            });
        }
        
        // Connection pool configuration
        configParams.AddRange(new[]
        {
            "-c", $"max_connections={Math.Max(options.ConnectionPoolSize * 2, 20)}",
            "-c", "shared_preload_libraries=''", // Disable extensions for testing
            "-c", $"statement_timeout={options.CommandTimeout.TotalMilliseconds}ms"
        });
        
        // Add custom PostgreSQL configuration
        foreach (var (key, value) in options.PostgreSQLConfiguration)
        {
            configParams.AddRange(new[] { "-c", $"{key}={value}" });
        }
        
        if (configParams.Count > 0)
        {
            containerBuilder = containerBuilder.WithCommand(configParams.ToArray());
        }
        
        // Set environment variables
        foreach (var (key, value) in options.EnvironmentVariables)
        {
            containerBuilder = containerBuilder.WithEnvironment(key, value);
        }
        
        // Set locale and collation
        containerBuilder = containerBuilder
            .WithEnvironment("LC_ALL", options.Collation)
            .WithEnvironment("LANG", options.Collation);
        
        return containerBuilder.Build();
    }

    /// <summary>
    /// Creates test context with PostgreSQL connection and configuration
    /// </summary>
    protected abstract Task<TTestContext> CreateTestContextAsync(
        NpgsqlConnection connection,
        string connectionString,
        DatabaseTestEnvironmentOptions options,
        string containerId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Runs test migrations during environment setup
    /// </summary>
    protected virtual async Task RunTestMigrationsAsync(
        TTestContext context,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Running test database migrations");
        
        // Create basic test schema
        if (context.Connection != null)
        {
            var createSchemaScript = """
                -- Create test schema with basic tables
                CREATE SCHEMA IF NOT EXISTS test_schema;
                
                -- Audit table for medical-grade compliance
                CREATE TABLE IF NOT EXISTS audit_log (
                    id BIGSERIAL PRIMARY KEY,
                    table_name VARCHAR(100) NOT NULL,
                    operation VARCHAR(10) NOT NULL,
                    user_id VARCHAR(100),
                    user_roles TEXT[],
                    old_values JSONB,
                    new_values JSONB,
                    correlation_id UUID,
                    timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    ip_address INET,
                    user_agent TEXT
                );
                
                CREATE INDEX IF NOT EXISTS idx_audit_log_table_name ON audit_log(table_name);
                CREATE INDEX IF NOT EXISTS idx_audit_log_user_id ON audit_log(user_id);
                CREATE INDEX IF NOT EXISTS idx_audit_log_timestamp ON audit_log(timestamp);
                CREATE INDEX IF NOT EXISTS idx_audit_log_correlation_id ON audit_log(correlation_id);
                
                -- Test entities table
                CREATE TABLE IF NOT EXISTS test_entities (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    name VARCHAR(255) NOT NULL,
                    description TEXT,
                    is_active BOOLEAN DEFAULT TRUE,
                    metadata JSONB,
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    created_by VARCHAR(100),
                    updated_by VARCHAR(100),
                    version INTEGER DEFAULT 1 -- For optimistic locking
                );
                
                CREATE INDEX IF NOT EXISTS idx_test_entities_name ON test_entities(name);
                CREATE INDEX IF NOT EXISTS idx_test_entities_is_active ON test_entities(is_active);
                CREATE INDEX IF NOT EXISTS idx_test_entities_created_at ON test_entities(created_at);
                
                -- Services table for Services API testing
                CREATE TABLE IF NOT EXISTS services (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    name VARCHAR(255) NOT NULL,
                    description TEXT,
                    category_ids UUID[],
                    contact_info JSONB,
                    location_info JSONB,
                    is_published BOOLEAN DEFAULT FALSE,
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    created_by VARCHAR(100),
                    updated_by VARCHAR(100),
                    version INTEGER DEFAULT 1
                );
                
                CREATE INDEX IF NOT EXISTS idx_services_name ON services(name);
                CREATE INDEX IF NOT EXISTS idx_services_is_published ON services(is_published);
                CREATE INDEX IF NOT EXISTS idx_services_category_ids ON services USING GIN(category_ids);
                
                -- Categories table for hierarchical data testing
                CREATE TABLE IF NOT EXISTS categories (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    name VARCHAR(255) NOT NULL,
                    description TEXT,
                    parent_id UUID REFERENCES categories(id),
                    level INTEGER DEFAULT 1,
                    path TEXT, -- Materialized path for efficient queries
                    is_active BOOLEAN DEFAULT TRUE,
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    version INTEGER DEFAULT 1
                );
                
                CREATE INDEX IF NOT EXISTS idx_categories_name ON categories(name);
                CREATE INDEX IF NOT EXISTS idx_categories_parent_id ON categories(parent_id);
                CREATE INDEX IF NOT EXISTS idx_categories_path ON categories(path);
                CREATE INDEX IF NOT EXISTS idx_categories_level ON categories(level);
                """;
            
            await context.Connection.ExecuteAsync(createSchemaScript);
            Logger.LogInformation("Test database migrations completed successfully");
        }
    }
    
    /// <summary>
    /// Seeds test data during environment setup
    /// </summary>
    protected virtual async Task SeedTestDataAsync(
        TTestContext context,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Seeding test data");
        
        if (context.Connection != null)
        {
            // Insert sample categories
            var seedCategoriesScript = """
                INSERT INTO categories (id, name, description, parent_id, level, path) VALUES 
                ('11111111-1111-1111-1111-111111111111', 'Healthcare', 'Healthcare services', NULL, 1, '/healthcare/'),
                ('22222222-2222-2222-2222-222222222222', 'Mental Health', 'Mental health services', '11111111-1111-1111-1111-111111111111', 2, '/healthcare/mental-health/'),
                ('33333333-3333-3333-3333-333333333333', 'Social Services', 'Social support services', NULL, 1, '/social-services/'),
                ('44444444-4444-4444-4444-444444444444', 'Emergency', 'Emergency services', NULL, 1, '/emergency/')
                ON CONFLICT (id) DO NOTHING;
                """;
            
            await context.Connection.ExecuteAsync(seedCategoriesScript);
            
            // Insert sample services
            var seedServicesScript = """
                INSERT INTO services (id, name, description, category_ids, is_published) VALUES 
                ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'General Medical Clinic', 'Primary healthcare services', 
                 ARRAY['11111111-1111-1111-1111-111111111111'::UUID], TRUE),
                ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Crisis Counseling Center', '24/7 mental health crisis support', 
                 ARRAY['22222222-2222-2222-2222-222222222222'::UUID], TRUE),
                ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'Food Bank', 'Emergency food assistance', 
                 ARRAY['33333333-3333-3333-3333-333333333333'::UUID], TRUE)
                ON CONFLICT (id) DO NOTHING;
                """;
            
            await context.Connection.ExecuteAsync(seedServicesScript);
            
            Logger.LogInformation("Test data seeding completed successfully");
        }
    }
    
    /// <summary>
    /// Validates database-specific environment configuration
    /// </summary>
    protected virtual async Task ValidateDatabaseSpecificEnvironmentAsync(
        TTestContext context,
        CancellationToken cancellationToken = default)
    {
        // Override in derived classes for additional validation
        await ValidateDatabaseEnvironmentAsync(context, cancellationToken);
    }
    
    /// <summary>
    /// Validates audit logging setup for medical-grade compliance
    /// </summary>
    protected virtual async Task ValidateAuditLoggingSetupAsync(
        TTestContext context,
        CancellationToken cancellationToken = default)
    {
        if (context?.Connection == null) return;
        
        try
        {
            // Verify audit table exists
            var auditTableExists = await context.Connection.QuerySingleAsync<bool>(
                "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'audit_log')");
            
            if (!auditTableExists)
            {
                throw new InvalidOperationException("Audit log table does not exist - medical-grade compliance requirement not met");
            }
            
            // Test audit logging functionality
            var testAuditSql = """
                INSERT INTO audit_log (table_name, operation, user_id, correlation_id) 
                VALUES ('test_table', 'TEST', 'test_user', gen_random_uuid()) 
                RETURNING id;
                """;
            
            var auditId = await context.Connection.QuerySingleAsync<long>(testAuditSql);
            
            if (auditId <= 0)
            {
                throw new InvalidOperationException("Audit logging test failed");
            }
            
            // Clean up test audit record
            await context.Connection.ExecuteAsync("DELETE FROM audit_log WHERE id = @id", new { id = auditId });
            
            Logger.LogInformation("Audit logging validation completed successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Audit logging validation failed");
            throw;
        }
    }
    
    /// <summary>
    /// Cleans up database data for test isolation
    /// </summary>
    protected virtual async Task CleanupDatabaseDataAsync(
        TTestContext context,
        CancellationToken cancellationToken = default)
    {
        if (context?.Connection == null) return;
        
        try
        {
            // Disable foreign key constraints temporarily
            await context.Connection.ExecuteAsync("SET session_replication_role = replica;");
            
            // Clear test data from all tables
            var clearDataScript = """
                TRUNCATE TABLE services CASCADE;
                TRUNCATE TABLE categories CASCADE;
                TRUNCATE TABLE test_entities CASCADE;
                TRUNCATE TABLE audit_log CASCADE;
                """;
            
            await context.Connection.ExecuteAsync(clearDataScript);
            
            // Re-enable foreign key constraints
            await context.Connection.ExecuteAsync("SET session_replication_role = DEFAULT;");
            
            Logger.LogInformation("Database data cleanup completed");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Database data cleanup encountered issues");
        }
    }
    
    /// <summary>
    /// Validates performance threshold for database operations
    /// </summary>
    protected virtual Task ValidatePerformanceThreshold(
        string operationName,
        TimeSpan actualDuration,
        PerformanceThreshold threshold)
    {
        if (actualDuration > threshold.MaxDuration)
        {
            throw new InvalidOperationException(
                $"Database operation '{operationName}' exceeded performance threshold. " +
                $"Expected: {threshold.MaxDuration.TotalMilliseconds}ms, " +
                $"Actual: {actualDuration.TotalMilliseconds}ms");
        }
        
        Logger.LogInformation("Database operation '{OperationName}' met performance threshold: {Duration}ms <= {Threshold}ms",
            operationName, actualDuration.TotalMilliseconds, threshold.MaxDuration.TotalMilliseconds);
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Disposes resources when environment is disposed
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        // Cleanup all active resources
        var cleanupTasks = new List<Task>();
        
        foreach (var connection in ActiveConnections.Values)
        {
            cleanupTasks.Add(connection.DisposeAsync().AsTask());
        }
        
        foreach (var container in ActiveContainers.Values)
        {
            cleanupTasks.Add(Task.Run(async () =>
            {
                await container.StopAsync();
                await container.DisposeAsync();
            }));
        }
        
        await Task.WhenAll(cleanupTasks);
        
        ActiveConnections.Clear();
        ActiveContainers.Clear();
    }
}
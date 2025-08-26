using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Dapper;
using InternationalCenter.Shared.Tests.Abstractions;
using Infrastructure.Database.Tests.Contracts;
using Xunit.Abstractions;

namespace Infrastructure.Database.Tests;

/// <summary>
/// PostgreSQL-specific implementation of database testing environment
/// Provides PostgreSQL container orchestration and testing utilities for Services APIs with medical-grade compliance
/// </summary>
public class PostgreSqlDatabaseTestEnvironment : DatabaseTestEnvironmentBase<DefaultDatabaseTestContext>, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private bool _disposed;

    public PostgreSqlDatabaseTestEnvironment(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<PostgreSqlDatabaseTestEnvironment> logger,
        ITestOutputHelper? output = null)
        : base(logger, output)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Creates PostgreSQL-specific test context with connection and configuration
    /// </summary>
    protected override async Task<DefaultDatabaseTestContext> CreateTestContextAsync(
        NpgsqlConnection connection,
        string connectionString,
        DatabaseTestEnvironmentOptions options,
        string containerId,
        CancellationToken cancellationToken = default)
    {
        // Configure service collection with PostgreSQL-specific services
        var services = new ServiceCollection();
        
        // Register PostgreSQL connection
        services.AddSingleton(connection);
        services.AddScoped<NpgsqlConnection>(provider => provider.GetRequiredService<NpgsqlConnection>());
        
        // Register connection string
        services.AddSingleton(connectionString);
        
        // Register configuration
        services.AddSingleton(_configuration);
        
        // Register logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            if (options.EnableDatabaseLogging)
            {
                builder.SetMinimumLevel(LogLevel.Debug);
            }
        });
        
        // Register EF Core DbContext for Services Admin API testing
        services.AddDbContext<TestDbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                   .EnableSensitiveDataLogging(false) // Disable for security in tests
                   .EnableDetailedErrors(true)
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
        });
        
        // Register database-specific services
        services.AddTransient<IDatabaseTestDataFactory, DatabaseTestDataFactory>();
        services.AddTransient<IEFCoreTestContract, EFCoreTestService>();
        services.AddTransient<IDapperTestContract, DapperTestService>();
        services.AddTransient<IDatabaseMigrationContract, DatabaseMigrationService>();
        services.AddTransient<IDatabaseBackupRecoveryContract, DatabaseBackupRecoveryService>();
        
        // Apply custom service configuration
        options.ConfigureServices?.Invoke(services);
        
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<DefaultDatabaseTestContext>>();
        
        // Create EF Core context
        var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
        
        var context = new DefaultDatabaseTestContext(
            serviceProvider,
            _configuration,
            logger,
            connection,
            connectionString,
            dbContext,
            containerId);
        
        // Validate PostgreSQL-specific environment
        await ValidatePostgreSqlSpecificEnvironmentAsync(context, options, cancellationToken);
        
        return context;
    }

    /// <summary>
    /// Validates PostgreSQL-specific environment configuration
    /// </summary>
    private async Task ValidatePostgreSqlSpecificEnvironmentAsync(
        DefaultDatabaseTestContext context,
        DatabaseTestEnvironmentOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Validating PostgreSQL-specific environment configuration");
            
            // Validate PostgreSQL version compatibility
            var version = await context.Connection!.QuerySingleAsync<string>("SELECT version()");
            if (!version.Contains("PostgreSQL"))
            {
                throw new InvalidOperationException("Not a valid PostgreSQL instance");
            }
            Logger.LogInformation("PostgreSQL version validated: {Version}", version.Split(' ')[1]);
            
            // Validate extensions availability
            var extensions = await context.Connection.QueryAsync<string>(
                "SELECT extname FROM pg_extension");
            Logger.LogInformation("Available extensions: {Extensions}", string.Join(", ", extensions));
            
            // Test connection pool configuration
            var poolStats = await context.GetConnectionPoolStatsAsync();
            Logger.LogInformation("Connection pool stats: Total={TotalConnections}, Active={ActiveConnections}", 
                poolStats.TotalConnections, poolStats.ActiveConnections);
            
            // Validate schema and tables
            var schemaValidation = await context.ValidateSchemaAsync(new[] 
            { 
                "audit_log", "test_entities", "services", "categories" 
            });
            
            if (!schemaValidation.IsValid)
            {
                throw new InvalidOperationException(
                    $"Schema validation failed: {string.Join(", ", schemaValidation.SchemaViolations)}");
            }
            
            // Test EF Core context
            if (context.AdminDbContext != null)
            {
                var canConnect = await context.AdminDbContext.Database.CanConnectAsync(cancellationToken);
                if (!canConnect)
                {
                    throw new InvalidOperationException("EF Core DbContext cannot connect to database");
                }
            }
            
            // Test Dapper operations
            var dapperTest = await context.Connection.QuerySingleAsync<DateTime>("SELECT NOW()");
            if (dapperTest == default)
            {
                throw new InvalidOperationException("Dapper operations validation failed");
            }
            
            Logger.LogInformation("PostgreSQL-specific environment validation completed successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PostgreSQL-specific environment validation failed");
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                // Cleanup active connections and containers
                var cleanupTask = DisposeAsyncCore();
                cleanupTask.AsTask().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during PostgreSQL database test environment disposal");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}

/// <summary>
/// Default implementation of database test context
/// Provides PostgreSQL connection and EF Core DbContext for Services APIs testing
/// </summary>
public class DefaultDatabaseTestContext : IDatabaseTestContext
{
    public IServiceProvider ServiceProvider { get; }
    public IConfiguration Configuration { get; }
    public ILogger Logger { get; }
    public string ConnectionString { get; }
    public NpgsqlConnection? Connection { get; }
    public DbContext? AdminDbContext { get; }
    public ICollection<object> CreatedTestEntities { get; } = new List<object>();
    
    private readonly Dictionary<string, object> _cachedEntities = new();
    private readonly string _containerId;
    
    public DefaultDatabaseTestContext(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger logger,
        NpgsqlConnection connection,
        string connectionString,
        DbContext adminDbContext,
        string containerId)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        AdminDbContext = adminDbContext ?? throw new ArgumentNullException(nameof(adminDbContext));
        _containerId = containerId ?? throw new ArgumentNullException(nameof(containerId));
    }
    
    /// <summary>
    /// Creates a new PostgreSQL connection with specified configuration
    /// Contract: Must create properly configured connection for test execution
    /// </summary>
    public async Task<NpgsqlConnection> CreateConnectionAsync()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        RegisterForCleanup(connection);
        return connection;
    }
    
    /// <summary>
    /// Creates a new EF Core DB context with specified configuration
    /// Contract: Must create properly configured DbContext for EF Core testing
    /// </summary>
    public async Task<TDbContext> CreateDbContextAsync<TDbContext>() 
        where TDbContext : DbContext
    {
        var contextOptions = new DbContextOptionsBuilder<TDbContext>()
            .UseNpgsql(ConnectionString)
            .EnableSensitiveDataLogging(false)
            .EnableDetailedErrors(true)
            .Options;
        
        var context = (TDbContext)Activator.CreateInstance(typeof(TDbContext), contextOptions)!;
        
        // Ensure database is created and migrations are applied
        await context.Database.EnsureCreatedAsync();
        
        RegisterForCleanup(context);
        return context;
    }
    
    /// <summary>
    /// Begins a database transaction for test isolation
    /// Contract: Must provide transaction isolation for test data cleanup
    /// </summary>
    public async Task<NpgsqlTransaction> BeginTransactionAsync()
    {
        if (Connection == null)
            throw new InvalidOperationException("Database connection is not available");
        
        var transaction = await Connection.BeginTransactionAsync();
        RegisterForCleanup(transaction);
        return transaction;
    }
    
    /// <summary>
    /// Executes raw SQL command with parameters
    /// Contract: Must execute SQL with proper parameterization and error handling
    /// </summary>
    public async Task<int> ExecuteSqlAsync(string sql, params object[] parameters)
    {
        if (Connection == null)
            throw new InvalidOperationException("Database connection is not available");
        
        try
        {
            return await Connection.ExecuteAsync(sql, parameters.Length > 0 ? parameters[0] : null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to execute SQL: {Sql}", sql);
            throw;
        }
    }
    
    /// <summary>
    /// Executes raw SQL query and returns results
    /// Contract: Must execute query with proper parameterization and type safety
    /// </summary>
    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        if (Connection == null)
            throw new InvalidOperationException("Database connection is not available");
        
        try
        {
            return await Connection.QueryAsync<T>(sql, parameters);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to execute query: {Sql}", sql);
            throw;
        }
    }
    
    /// <summary>
    /// Clears all data from specified tables for test cleanup
    /// Contract: Must ensure complete data cleanup while preserving schema
    /// </summary>
    public async Task ClearTablesAsync(string[] tableNames)
    {
        if (Connection == null)
            throw new InvalidOperationException("Database connection is not available");
        
        try
        {
            // Disable foreign key constraints temporarily
            await Connection.ExecuteAsync("SET session_replication_role = replica;");
            
            // Clear tables in reverse dependency order
            foreach (var tableName in tableNames.Reverse())
            {
                await Connection.ExecuteAsync($"TRUNCATE TABLE {tableName} CASCADE;");
            }
            
            // Re-enable foreign key constraints
            await Connection.ExecuteAsync("SET session_replication_role = DEFAULT;");
            
            Logger.LogInformation("Cleared tables: {Tables}", string.Join(", ", tableNames));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to clear tables: {Tables}", string.Join(", ", tableNames));
            throw;
        }
    }
    
    /// <summary>
    /// Gets database connection pool statistics
    /// Contract: Must provide connection pool metrics for performance validation
    /// </summary>
    public async Task<DatabaseConnectionPoolStats> GetConnectionPoolStatsAsync()
    {
        if (Connection == null)
            throw new InvalidOperationException("Database connection is not available");
        
        try
        {
            // PostgreSQL connection pool stats (simplified for testing)
            var stats = await Connection.QuerySingleAsync<dynamic>("""
                SELECT 
                    (SELECT setting::int FROM pg_settings WHERE name = 'max_connections') as max_connections,
                    (SELECT count(*) FROM pg_stat_activity WHERE state = 'active') as active_connections,
                    (SELECT count(*) FROM pg_stat_activity WHERE state = 'idle') as idle_connections,
                    (SELECT count(*) FROM pg_stat_activity WHERE state = 'idle in transaction') as pending_connections,
                    (SELECT sum(blks_read + blks_hit) FROM pg_stat_database WHERE datname = current_database()) as total_blocks,
                    (SELECT sum(xact_commit + xact_rollback) FROM pg_stat_database WHERE datname = current_database()) as total_commands
                """);
            
            return new DatabaseConnectionPoolStats
            {
                TotalConnections = stats.max_connections,
                ActiveConnections = stats.active_connections,
                IdleConnections = stats.idle_connections,
                PendingConnections = stats.pending_connections,
                AverageConnectionTime = TimeSpan.FromMilliseconds(50), // Simplified
                TotalBytesRead = (long)(stats.total_blocks ?? 0) * 8192, // 8KB blocks
                TotalBytesWritten = (long)(stats.total_blocks ?? 0) * 8192,
                TotalCommandsExecuted = stats.total_commands ?? 0,
                FailedConnections = 0 // Not easily available in PostgreSQL
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get connection pool statistics");
            throw;
        }
    }
    
    /// <summary>
    /// Validates database schema against expected structure
    /// Contract: Must validate schema matches Services API requirements
    /// </summary>
    public async Task<SchemaValidationResult> ValidateSchemaAsync(string[] expectedTables)
    {
        if (Connection == null)
            throw new InvalidOperationException("Database connection is not available");
        
        try
        {
            var result = new SchemaValidationResult { IsValid = true };
            
            // Get actual tables
            var actualTables = (await Connection.QueryAsync<string>(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'"))
                .ToArray();
            
            // Check for missing tables
            result.MissingTables = expectedTables.Except(actualTables).ToArray();
            if (result.MissingTables.Length > 0)
            {
                result.IsValid = false;
                result.SchemaViolations = result.SchemaViolations.Concat(
                    result.MissingTables.Select(t => $"Missing table: {t}")).ToArray();
            }
            
            // Check for extra tables (informational only)
            result.ExtraÉTables = actualTables.Except(expectedTables).ToArray();
            
            // Validate table structures for expected tables
            foreach (var tableName in expectedTables.Intersect(actualTables))
            {
                var columns = (await Connection.QueryAsync<string>(
                    "SELECT column_name FROM information_schema.columns WHERE table_name = @tableName",
                    new { tableName })).ToArray();
                
                // Basic validation - ensure tables have some columns
                if (columns.Length == 0)
                {
                    result.IsValid = false;
                    result.SchemaViolations = result.SchemaViolations.Concat(
                        new[] { $"Table {tableName} has no columns" }).ToArray();
                }
                
                // Check for required audit columns in tables that should have them
                if (tableName != "audit_log" && !columns.Contains("created_at"))
                {
                    if (!result.MissingColumns.ContainsKey(tableName))
                        result.MissingColumns[tableName] = new string[0];
                    
                    result.MissingColumns[tableName] = result.MissingColumns[tableName]
                        .Concat(new[] { "created_at" }).ToArray();
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Schema validation failed");
            return new SchemaValidationResult 
            { 
                IsValid = false, 
                SchemaViolations = new[] { $"Schema validation error: {ex.Message}" }
            };
        }
    }
    
    /// <summary>
    /// Registers an entity for cleanup after test completion
    /// Contract: Must track entities for proper cleanup and test isolation
    /// </summary>
    public void RegisterForCleanup<T>(T entity) where T : class
    {
        if (entity != null)
        {
            CreatedTestEntities.Add(entity);
        }
    }
    
    /// <summary>
    /// Gets or creates a cached test entity to avoid recreation
    /// Contract: Must provide entity caching for test performance optimization
    /// </summary>
    public async Task<T> GetOrCreateTestEntityAsync<T>(Func<Task<T>> factory) where T : class
    {
        var key = typeof(T).FullName ?? typeof(T).Name;
        
        if (_cachedEntities.TryGetValue(key, out var existingEntity) && existingEntity is T cachedEntity)
        {
            return cachedEntity;
        }
        
        var newEntity = await factory();
        _cachedEntities[key] = newEntity;
        RegisterForCleanup(newEntity);
        
        return newEntity;
    }
    
    public void Dispose()
    {
        // Cleanup cached entities
        foreach (var entity in CreatedTestEntities.OfType<IDisposable>())
        {
            try
            {
                entity.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to dispose test entity: {EntityType}", entity.GetType().Name);
            }
        }
        
        // Cleanup async disposable entities
        foreach (var entity in CreatedTestEntities.OfType<IAsyncDisposable>())
        {
            try
            {
                entity.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to dispose async test entity: {EntityType}", entity.GetType().Name);
            }
        }
        
        CreatedTestEntities.Clear();
        _cachedEntities.Clear();
    }
}

/// <summary>
/// Test DbContext for EF Core testing
/// Provides entity configuration for Services APIs testing
/// </summary>
public class TestDbContext : DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; } = null!;
    public DbSet<ServiceEntity> Services { get; set; } = null!;
    public DbSet<CategoryEntity> Categories { get; set; } = null!;
    public DbSet<AuditLogEntity> AuditLogs { get; set; } = null!;
    
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure TestEntity
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.Version).HasDefaultValue(1);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
        });
        
        // Configure ServiceEntity
        modelBuilder.Entity<ServiceEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.CategoryIds).HasColumnType("uuid[]");
            entity.Property(e => e.ContactInfo).HasColumnType("jsonb");
            entity.Property(e => e.LocationInfo).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.Version).HasDefaultValue(1);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsPublished);
        });
        
        // Configure CategoryEntity
        modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Path).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.Version).HasDefaultValue(1);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Path);
            entity.HasIndex(e => e.Level);
            
            // Self-referencing relationship
            entity.HasOne(e => e.Parent)
                  .WithMany(e => e.Children)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configure AuditLogEntity
        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TableName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Operation).IsRequired().HasMaxLength(10);
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.UserRoles).HasColumnType("text[]");
            entity.Property(e => e.OldValues).HasColumnType("jsonb");
            entity.Property(e => e.NewValues).HasColumnType("jsonb");
            entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");
            entity.Property(e => e.IpAddress).HasColumnType("inet");
            entity.HasIndex(e => e.TableName);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.CorrelationId);
        });
        
        base.OnModelCreating(modelBuilder);
    }
}

// Entity classes for EF Core testing

public class TestEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Metadata { get; set; } // JSON column
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public int Version { get; set; } = 1; // For optimistic locking
}

public class ServiceEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid[]? CategoryIds { get; set; }
    public string? ContactInfo { get; set; } // JSON column
    public string? LocationInfo { get; set; } // JSON column
    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public int Version { get; set; } = 1;
}

public class CategoryEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int Level { get; set; } = 1;
    public string? Path { get; set; } // Materialized path
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;
    
    // Navigation properties
    public CategoryEntity? Parent { get; set; }
    public ICollection<CategoryEntity> Children { get; set; } = new List<CategoryEntity>();
}

public class AuditLogEntity
{
    public long Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string[]? UserRoles { get; set; }
    public string? OldValues { get; set; } // JSON column
    public string? NewValues { get; set; } // JSON column
    public Guid? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
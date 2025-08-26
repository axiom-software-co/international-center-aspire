using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using InternationalCenter.Shared.Tests.Abstractions;
using Xunit.Abstractions;

namespace Infrastructure.Database.Tests.Contracts;

/// <summary>
/// Contract for PostgreSQL database testing environment
/// Defines comprehensive database testing capabilities with Aspire orchestration
/// Medical-grade database testing ensuring audit compliance and reliability for Services APIs
/// </summary>
/// <typeparam name="TTestContext">The database-specific test context type</typeparam>
public interface IDatabaseTestEnvironmentContract<TTestContext>
    where TTestContext : class, IDatabaseTestContext
{
    /// <summary>
    /// Sets up the PostgreSQL database testing environment with Aspire orchestration
    /// Contract: Must provide isolated PostgreSQL container with proper configuration for medical-grade compliance testing
    /// </summary>
    Task<TTestContext> SetupDatabaseTestEnvironmentAsync(
        DatabaseTestEnvironmentOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a database test operation with performance tracking and transaction management
    /// Contract: Must provide comprehensive error handling and connection lifecycle management
    /// </summary>
    Task<T> ExecuteDatabaseTestAsync<T>(
        TTestContext context,
        Func<TTestContext, Task<T>> testOperation,
        string operationName,
        PerformanceThreshold? performanceThreshold = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates PostgreSQL database environment configuration and connectivity
    /// Contract: Must validate database server connectivity, audit logging setup, and schema validation
    /// </summary>
    Task ValidateDatabaseEnvironmentAsync(
        TTestContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up PostgreSQL database environment including container cleanup and data purging
    /// Contract: Must ensure complete cleanup of database data and container resources for test isolation
    /// </summary>
    Task CleanupDatabaseEnvironmentAsync(
        TTestContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Contract for EF Core database operations testing for Services Admin API
/// Provides comprehensive EF Core validation with medical-grade audit compliance
/// </summary>
public interface IEFCoreTestContract
{
    /// <summary>
    /// Tests EF Core entity operations (CRUD) for Services Admin API
    /// Contract: Must validate entity operations with role-based access control and medical-grade audit logging
    /// </summary>
    Task TestEFCoreEntityOperationsAsync<TEntity, TDbContext>(
        TDbContext dbContext,
        EFCoreEntityTestCase<TEntity>[] entityTestCases,
        ITestOutputHelper? output = null)
        where TEntity : class
        where TDbContext : DbContext;

    /// <summary>
    /// Tests EF Core change tracking and audit logging
    /// Contract: Must validate change tracking accuracy and medical-grade audit trail creation
    /// </summary>
    Task TestEFCoreChangeTrackingAsync<TDbContext>(
        TDbContext dbContext,
        ChangeTrackingTestCase[] changeTrackingTestCases,
        ITestOutputHelper? output = null)
        where TDbContext : DbContext;

    /// <summary>
    /// Tests EF Core query performance and optimization
    /// Contract: Must validate query performance meets Services Admin API response time requirements
    /// </summary>
    Task TestEFCoreQueryPerformanceAsync<TDbContext>(
        TDbContext dbContext,
        QueryPerformanceTestCase[] performanceTestCases,
        PerformanceThreshold threshold,
        ITestOutputHelper? output = null)
        where TDbContext : DbContext;

    /// <summary>
    /// Tests EF Core transaction management and consistency
    /// Contract: Must validate transaction isolation and rollback behavior for medical-grade data integrity
    /// </summary>
    Task TestEFCoreTransactionManagementAsync<TDbContext>(
        TDbContext dbContext,
        TransactionTestCase[] transactionTestCases,
        ITestOutputHelper? output = null)
        where TDbContext : DbContext;

    /// <summary>
    /// Tests EF Core concurrency handling and optimistic locking
    /// Contract: Must validate concurrent access patterns and conflict resolution for Services Admin API
    /// </summary>
    Task TestEFCoreConcurrencyAsync<TDbContext>(
        TDbContext dbContext,
        ConcurrencyTestCase[] concurrencyTestCases,
        ITestOutputHelper? output = null)
        where TDbContext : DbContext;

    /// <summary>
    /// Tests EF Core medical-grade audit logging compliance
    /// Contract: Must validate comprehensive audit trail creation and data retention for healthcare compliance
    /// </summary>
    Task TestEFCoreAuditComplianceAsync<TDbContext>(
        TDbContext dbContext,
        AuditComplianceTestCase[] auditTestCases,
        ITestOutputHelper? output = null)
        where TDbContext : DbContext;
}

/// <summary>
/// Contract for Dapper database operations testing for Services Public API
/// Provides high-performance data access validation for anonymous users
/// </summary>
public interface IDapperTestContract
{
    /// <summary>
    /// Tests Dapper query operations for Services Public API
    /// Contract: Must validate high-performance queries for anonymous access patterns with proper SQL injection protection
    /// </summary>
    Task TestDapperQueryOperationsAsync(
        NpgsqlConnection connection,
        DapperQueryTestCase[] queryTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests Dapper command operations (Insert, Update, Delete)
    /// Contract: Must validate command execution with proper parameterization for Services Public API data access
    /// </summary>
    Task TestDapperCommandOperationsAsync(
        NpgsqlConnection connection,
        DapperCommandTestCase[] commandTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests Dapper performance characteristics for Services Public API
    /// Contract: Must validate query performance meets anonymous user response time requirements (sub-second)
    /// </summary>
    Task TestDapperPerformanceAsync(
        NpgsqlConnection connection,
        DapperPerformanceTestCase[] performanceTestCases,
        PerformanceThreshold threshold,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests Dapper connection management and pooling
    /// Contract: Must validate connection lifecycle and pooling behavior for high-concurrency anonymous access
    /// </summary>
    Task TestDapperConnectionManagementAsync(
        NpgsqlConnection connection,
        ConnectionManagementTestCase[] connectionTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests Dapper SQL injection prevention and security
    /// Contract: Must validate parameterized queries and SQL injection protection for anonymous user inputs
    /// </summary>
    Task TestDapperSecurityAsync(
        NpgsqlConnection connection,
        SecurityTestCase[] securityTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests Dapper bulk operations and batch processing
    /// Contract: Must validate bulk operations performance for Services Public API data synchronization
    /// </summary>
    Task TestDapperBulkOperationsAsync(
        NpgsqlConnection connection,
        BulkOperationsTestCase[] bulkTestCases,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for database migration testing
/// Validates automated development/testing migrations and manual production migration preparation
/// </summary>
public interface IDatabaseMigrationContract
{
    /// <summary>
    /// Tests automated database migrations for development and testing environments
    /// Contract: Must validate migration scripts execute correctly and maintain data integrity
    /// </summary>
    Task TestAutomatedMigrationsAsync(
        NpgsqlConnection connection,
        MigrationTestCase[] migrationTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests database schema validation after migrations
    /// Contract: Must validate schema matches expected structure and constraints for Services APIs
    /// </summary>
    Task TestSchemaValidationAsync(
        NpgsqlConnection connection,
        SchemaValidationTestCase[] schemaTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests migration rollback capabilities
    /// Contract: Must validate migration rollback maintains data integrity and schema consistency
    /// </summary>
    Task TestMigrationRollbackAsync(
        NpgsqlConnection connection,
        MigrationRollbackTestCase[] rollbackTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests production migration scripts generation and validation
    /// Contract: Must validate production migration scripts are safe and maintain medical-grade compliance
    /// </summary>
    Task TestProductionMigrationValidationAsync(
        NpgsqlConnection connection,
        ProductionMigrationTestCase[] productionTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests migration performance and resource usage
    /// Contract: Must validate migrations complete within acceptable timeframes for production deployment
    /// </summary>
    Task TestMigrationPerformanceAsync(
        NpgsqlConnection connection,
        MigrationPerformanceTestCase[] performanceTestCases,
        PerformanceThreshold threshold,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Contract for database backup and recovery testing
/// Validates medical-grade data protection and disaster recovery capabilities
/// </summary>
public interface IDatabaseBackupRecoveryContract
{
    /// <summary>
    /// Tests database backup creation and validation
    /// Contract: Must validate backup integrity and completeness for medical-grade data protection
    /// </summary>
    Task TestDatabaseBackupAsync(
        NpgsqlConnection connection,
        DatabaseBackupTestCase[] backupTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests database recovery and restore operations
    /// Contract: Must validate point-in-time recovery and data integrity restoration
    /// </summary>
    Task TestDatabaseRecoveryAsync(
        NpgsqlConnection connection,
        DatabaseRecoveryTestCase[] recoveryTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests backup retention and archival policies
    /// Contract: Must validate backup retention meets medical-grade compliance requirements
    /// </summary>
    Task TestBackupRetentionAsync(
        NpgsqlConnection connection,
        BackupRetentionTestCase[] retentionTestCases,
        ITestOutputHelper? output = null);

    /// <summary>
    /// Tests disaster recovery scenarios
    /// Contract: Must validate disaster recovery procedures and RTO/RPO compliance
    /// </summary>
    Task TestDisasterRecoveryAsync(
        NpgsqlConnection connection,
        DisasterRecoveryTestCase[] disasterTestCases,
        ITestOutputHelper? output = null);
}

/// <summary>
/// Configuration options for PostgreSQL database test environment setup
/// </summary>
public class DatabaseTestEnvironmentOptions
{
    /// <summary>
    /// Gets or sets whether to use PostgreSQL container or in-memory database (default: true for real PostgreSQL)
    /// </summary>
    public bool UsePostgreSQLContainer { get; set; } = true;

    /// <summary>
    /// Gets or sets the PostgreSQL container image and version
    /// </summary>
    public string PostgreSQLImage { get; set; } = "postgres:16-alpine";

    /// <summary>
    /// Gets or sets the PostgreSQL port for container mapping
    /// </summary>
    public int PostgreSQLPort { get; set; } = 5432;

    /// <summary>
    /// Gets or sets the database name for testing
    /// </summary>
    public string DatabaseName { get; set; } = "testdb";

    /// <summary>
    /// Gets or sets the database username for testing
    /// </summary>
    public string Username { get; set; } = "testuser";

    /// <summary>
    /// Gets or sets the database password for testing
    /// </summary>
    public string Password { get; set; } = "testpass";

    /// <summary>
    /// Gets or sets whether to enable database logging (default: false for performance)
    /// </summary>
    public bool EnableDatabaseLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable detailed query logging
    /// </summary>
    public bool EnableQueryLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable medical-grade audit logging
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to run database migrations during setup
    /// </summary>
    public bool RunMigrations { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to seed test data during setup
    /// </summary>
    public bool SeedTestData { get; set; } = false;

    /// <summary>
    /// Gets or sets the connection pool size for testing
    /// </summary>
    public int ConnectionPoolSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the command timeout for database operations
    /// </summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets additional PostgreSQL configuration parameters
    /// </summary>
    public Dictionary<string, string> PostgreSQLConfiguration { get; set; } = new();

    /// <summary>
    /// Gets or sets test-specific environment variables
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// Gets or sets custom service registrations for DI
    /// </summary>
    public Action<IServiceCollection>? ConfigureServices { get; set; }

    /// <summary>
    /// Gets or sets the database collation for text operations
    /// </summary>
    public string Collation { get; set; } = "en_US.UTF-8";

    /// <summary>
    /// Gets or sets whether to enable SSL for database connections
    /// </summary>
    public bool EnableSSL { get; set; } = false;
}

/// <summary>
/// Context for PostgreSQL database domain testing
/// Provides database-specific testing context and connection management
/// </summary>
public interface IDatabaseTestContext : ITestContext
{
    /// <summary>
    /// Gets the database test service provider
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the database test configuration
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the database test logger
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Gets the PostgreSQL connection string
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// Gets the primary PostgreSQL connection
    /// </summary>
    NpgsqlConnection? Connection { get; }

    /// <summary>
    /// Gets the EF Core DB context for Services Admin API testing
    /// </summary>
    DbContext? AdminDbContext { get; }

    /// <summary>
    /// Gets test entities created during this context
    /// </summary>
    ICollection<object> CreatedTestEntities { get; }

    /// <summary>
    /// Creates a new PostgreSQL connection with specified configuration
    /// Contract: Must create properly configured connection for test execution
    /// </summary>
    Task<NpgsqlConnection> CreateConnectionAsync();

    /// <summary>
    /// Creates a new EF Core DB context with specified configuration
    /// Contract: Must create properly configured DbContext for EF Core testing
    /// </summary>
    Task<TDbContext> CreateDbContextAsync<TDbContext>() where TDbContext : DbContext;

    /// <summary>
    /// Begins a database transaction for test isolation
    /// Contract: Must provide transaction isolation for test data cleanup
    /// </summary>
    Task<NpgsqlTransaction> BeginTransactionAsync();

    /// <summary>
    /// Executes raw SQL command with parameters
    /// Contract: Must execute SQL with proper parameterization and error handling
    /// </summary>
    Task<int> ExecuteSqlAsync(string sql, params object[] parameters);

    /// <summary>
    /// Executes raw SQL query and returns results
    /// Contract: Must execute query with proper parameterization and type safety
    /// </summary>
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null);

    /// <summary>
    /// Clears all data from specified tables for test cleanup
    /// Contract: Must ensure complete data cleanup while preserving schema
    /// </summary>
    Task ClearTablesAsync(string[] tableNames);

    /// <summary>
    /// Gets database connection pool statistics
    /// Contract: Must provide connection pool metrics for performance validation
    /// </summary>
    Task<DatabaseConnectionPoolStats> GetConnectionPoolStatsAsync();

    /// <summary>
    /// Validates database schema against expected structure
    /// Contract: Must validate schema matches Services API requirements
    /// </summary>
    Task<SchemaValidationResult> ValidateSchemaAsync(string[] expectedTables);

    /// <summary>
    /// Registers an entity for cleanup after test completion
    /// Contract: Must track entities for proper cleanup and test isolation
    /// </summary>
    void RegisterForCleanup<T>(T entity) where T : class;

    /// <summary>
    /// Gets or creates a cached test entity to avoid recreation
    /// Contract: Must provide entity caching for test performance optimization
    /// </summary>
    Task<T> GetOrCreateTestEntityAsync<T>(Func<Task<T>> factory) where T : class;
}

/// <summary>
/// Performance threshold for database operations
/// </summary>
public class DatabasePerformanceThreshold : PerformanceThreshold
{
    public TimeSpan MaxQueryTime { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan MaxCommandTime { get; set; } = TimeSpan.FromMilliseconds(500);
    public TimeSpan MaxTransactionTime { get; set; } = TimeSpan.FromSeconds(5);
    public int MinThroughputPerSecond { get; set; } = 1000; // operations per second
    public long MaxMemoryUsageBytes { get; set; } = 500 * 1024 * 1024; // 500MB
    public int MaxConnectionPoolUsage { get; set; } = 80; // 80% of pool
}

/// <summary>
/// Database connection pool statistics
/// </summary>
public class DatabaseConnectionPoolStats
{
    public int TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public int IdleConnections { get; set; }
    public int PendingConnections { get; set; }
    public TimeSpan AverageConnectionTime { get; set; }
    public long TotalBytesRead { get; set; }
    public long TotalBytesWritten { get; set; }
    public int TotalCommandsExecuted { get; set; }
    public int FailedConnections { get; set; }
}

/// <summary>
/// Schema validation result
/// </summary>
public class SchemaValidationResult
{
    public bool IsValid { get; set; }
    public string[] MissingTables { get; set; } = Array.Empty<string>();
    public string[] ExtraÉTables { get; set; } = Array.Empty<string>();
    public string[] SchemaViolations { get; set; } = Array.Empty<string>();
    public Dictionary<string, string[]> MissingColumns { get; set; } = new();
    public Dictionary<string, string[]> MissingIndexes { get; set; } = new();
    public string[] MissingConstraints { get; set; } = Array.Empty<string>();
}
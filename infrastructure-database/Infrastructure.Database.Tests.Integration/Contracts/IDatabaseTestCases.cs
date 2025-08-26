using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace Infrastructure.Database.Tests.Contracts;

/// <summary>
/// Test case for EF Core entity operations
/// Validates CRUD operations with role-based access control and medical-grade audit logging
/// </summary>
public class EFCoreEntityTestCase<TEntity> where TEntity : class
{
    public TEntity Entity { get; set; } = default!;
    public EntityOperation Operation { get; set; } = EntityOperation.Create;
    public string[] UserRoles { get; set; } = Array.Empty<string>();
    public string UserId { get; set; } = string.Empty;
    public bool ExpectSuccess { get; set; } = true;
    public bool TestAuditLogging { get; set; } = true;
    public bool TestRoleBasedAccess { get; set; } = true;
    public bool ValidateBusinessRules { get; set; } = true;
    public Dictionary<string, object>? ExpectedAuditData { get; set; }
    public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromSeconds(1);
    public AuditAction ExpectedAuditAction { get; set; } = AuditAction.Create;
}

/// <summary>
/// Entity operation enumeration
/// </summary>
public enum EntityOperation
{
    Create,
    Read,
    Update,
    Delete,
    BulkInsert,
    BulkUpdate,
    BulkDelete
}

/// <summary>
/// Test case for EF Core change tracking
/// Validates change tracking accuracy and medical-grade audit trail creation
/// </summary>
public class ChangeTrackingTestCase
{
    public string EntityName { get; set; } = string.Empty;
    public object OriginalValues { get; set; } = new();
    public object ModifiedValues { get; set; } = new();
    public string[] TrackedProperties { get; set; } = Array.Empty<string>();
    public bool TestChangeDetection { get; set; } = true;
    public bool TestAuditTrailGeneration { get; set; } = true;
    public bool ValidateChangeAccuracy { get; set; } = true;
    public Dictionary<string, ChangeType>? ExpectedChanges { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime TimestampThreshold { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Change type enumeration for audit tracking
/// </summary>
public enum ChangeType
{
    Added,
    Modified,
    Deleted,
    Unchanged
}

/// <summary>
/// Test case for EF Core query performance
/// Validates query performance meets Services Admin API response time requirements
/// </summary>
public class QueryPerformanceTestCase
{
    public string QueryName { get; set; } = string.Empty;
    public string QueryDescription { get; set; } = string.Empty;
    public Func<DbContext, Task<object>> QueryFunction { get; set; } = _ => Task.FromResult<object>(new());
    public int IterationCount { get; set; } = 100;
    public TimeSpan ExpectedMaxDuration { get; set; } = TimeSpan.FromMilliseconds(100);
    public bool TestQueryPlan { get; set; } = true;
    public bool TestIndexUsage { get; set; } = true;
    public bool MeasureMemoryUsage { get; set; } = true;
    public int ExpectedResultCount { get; set; } = -1; // -1 means don't validate count
    public Dictionary<string, object>? QueryParameters { get; set; }
    public bool TestConcurrency { get; set; } = false;
    public int ConcurrentThreads { get; set; } = 1;
}

/// <summary>
/// Test case for EF Core transaction management
/// Validates transaction isolation and rollback behavior for medical-grade data integrity
/// </summary>
public class TransactionTestCase
{
    public string TransactionName { get; set; } = string.Empty;
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
    public List<Func<DbContext, Task>> Operations { get; set; } = new();
    public bool ShouldCommit { get; set; } = true;
    public bool TestRollback { get; set; } = true;
    public bool TestConcurrentAccess { get; set; } = false;
    public TimeSpan TransactionTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public bool ValidateDataIntegrity { get; set; } = true;
    public string[] EntitiesToValidate { get; set; } = Array.Empty<string>();
    public Dictionary<string, object>? ExpectedFinalState { get; set; }
    public bool TestDeadlockDetection { get; set; } = false;
}

/// <summary>
/// Test case for EF Core concurrency handling
/// Validates concurrent access patterns and conflict resolution for Services Admin API
/// </summary>
public class ConcurrencyTestCase
{
    public string TestName { get; set; } = string.Empty;
    public int ConcurrentOperations { get; set; } = 5;
    public string EntityType { get; set; } = string.Empty;
    public object EntityId { get; set; } = new();
    public ConcurrencyOperation[] Operations { get; set; } = Array.Empty<ConcurrencyOperation>();
    public bool TestOptimisticLocking { get; set; } = true;
    public bool TestPessimisticLocking { get; set; } = false;
    public bool TestConflictResolution { get; set; } = true;
    public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromSeconds(5);
    public ConcurrencyConflictResolution ConflictResolution { get; set; } = ConcurrencyConflictResolution.LastWriterWins;
    public bool ValidateDataConsistency { get; set; } = true;
}

/// <summary>
/// Concurrency operation definition
/// </summary>
public class ConcurrencyOperation
{
    public EntityOperation Operation { get; set; } = EntityOperation.Update;
    public Dictionary<string, object> Data { get; set; } = new();
    public TimeSpan DelayBefore { get; set; } = TimeSpan.Zero;
    public bool ExpectSuccess { get; set; } = true;
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Concurrency conflict resolution strategy enumeration
/// </summary>
public enum ConcurrencyConflictResolution
{
    FirstWriterWins,
    LastWriterWins,
    ThrowException,
    MergeChanges,
    UserIntervention
}

/// <summary>
/// Test case for EF Core audit compliance
/// Validates comprehensive audit trail creation and data retention for healthcare compliance
/// </summary>
public class AuditComplianceTestCase
{
    public string TestName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public AuditAction AuditAction { get; set; } = AuditAction.Create;
    public string UserId { get; set; } = string.Empty;
    public string[] UserRoles { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> EntityData { get; set; } = new();
    public bool TestAuditTrailCreation { get; set; } = true;
    public bool TestDataRetention { get; set; } = true;
    public bool TestComplianceValidation { get; set; } = true;
    public bool TestSensitiveDataHandling { get; set; } = true;
    public TimeSpan AuditRetentionPeriod { get; set; } = TimeSpan.FromDays(2555); // 7 years for medical compliance
    public string[] SensitiveFields { get; set; } = Array.Empty<string>();
    public Dictionary<string, object>? ExpectedAuditFields { get; set; }
    public bool ValidateImmutability { get; set; } = true; // Audit records must be immutable
}

/// <summary>
/// Audit action enumeration for medical-grade compliance
/// </summary>
public enum AuditAction
{
    Create,
    Read,
    Update,
    Delete,
    Login,
    Logout,
    AccessDenied,
    Export,
    Import,
    SystemAccess
}

/// <summary>
/// Test case for Dapper query operations
/// Validates high-performance queries for anonymous access patterns with proper SQL injection protection
/// </summary>
public class DapperQueryTestCase
{
    public string QueryName { get; set; } = string.Empty;
    public string SqlQuery { get; set; } = string.Empty;
    public object? Parameters { get; set; }
    public Type ExpectedResultType { get; set; } = typeof(object);
    public int ExpectedResultCount { get; set; } = -1; // -1 means don't validate count
    public bool TestSqlInjectionPrevention { get; set; } = true;
    public bool TestParameterization { get; set; } = true;
    public bool TestPerformance { get; set; } = true;
    public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromMilliseconds(50);
    public bool TestAnonymousAccess { get; set; } = true;
    public string[] ForbiddenSqlPatterns { get; set; } = { "DROP", "DELETE", "UPDATE", "INSERT", "CREATE", "ALTER" };
    public Func<object, bool>? ResultValidator { get; set; }
    public bool TestConnectionReuse { get; set; } = true;
}

/// <summary>
/// Test case for Dapper command operations
/// Validates command execution with proper parameterization for Services Public API data access
/// </summary>
public class DapperCommandTestCase
{
    public string CommandName { get; set; } = string.Empty;
    public string SqlCommand { get; set; } = string.Empty;
    public object? Parameters { get; set; }
    public CommandType CommandType { get; set; } = CommandType.Text;
    public int ExpectedAffectedRows { get; set; } = -1; // -1 means don't validate
    public bool TestParameterization { get; set; } = true;
    public bool TestRollback { get; set; } = true;
    public bool TestTransaction { get; set; } = false;
    public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromMilliseconds(100);
    public bool ValidateDataIntegrity { get; set; } = true;
    public string[] ValidateTableData { get; set; } = Array.Empty<string>();
    public Dictionary<string, object>? ExpectedFinalState { get; set; }
    public bool TestConcurrentExecution { get; set; } = false;
}

/// <summary>
/// Test case for Dapper performance
/// Validates query performance meets anonymous user response time requirements
/// </summary>
public class DapperPerformanceTestCase
{
    public string TestName { get; set; } = string.Empty;
    public string SqlOperation { get; set; } = string.Empty;
    public object? Parameters { get; set; }
    public int IterationCount { get; set; } = 1000;
    public int ConcurrentConnections { get; set; } = 1;
    public TimeSpan ExpectedMaxDuration { get; set; } = TimeSpan.FromMilliseconds(10);
    public bool MeasureMemoryUsage { get; set; } = true;
    public bool TestConnectionPooling { get; set; } = true;
    public bool TestQueryCaching { get; set; } = false;
    public long MaxMemoryUsageBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public double MinThroughputPerSecond { get; set; } = 1000;
    public Func<object, bool>? ResultValidator { get; set; }
    public bool TestScalability { get; set; } = false;
}

/// <summary>
/// Test case for Dapper connection management
/// Validates connection lifecycle and pooling behavior for high-concurrency anonymous access
/// </summary>
public class ConnectionManagementTestCase
{
    public string TestName { get; set; } = string.Empty;
    public int ConnectionCount { get; set; } = 10;
    public int OperationsPerConnection { get; set; } = 100;
    public TimeSpan ConnectionLifetime { get; set; } = TimeSpan.FromMinutes(1);
    public bool TestConnectionPooling { get; set; } = true;
    public bool TestConnectionReuse { get; set; } = true;
    public bool TestConnectionFailover { get; set; } = false;
    public bool TestConnectionLeakDetection { get; set; } = true;
    public TimeSpan MaxConnectionWaitTime { get; set; } = TimeSpan.FromSeconds(5);
    public int MaxPoolSize { get; set; } = 100;
    public int MinPoolSize { get; set; } = 5;
    public bool ValidateConnectionState { get; set; } = true;
    public string[] TestQueries { get; set; } = { "SELECT 1", "SELECT NOW()", "SELECT VERSION()" };
}

/// <summary>
/// Test case for Dapper security
/// Validates parameterized queries and SQL injection protection for anonymous user inputs
/// </summary>
public class SecurityTestCase
{
    public string TestName { get; set; } = string.Empty;
    public string SqlQuery { get; set; } = string.Empty;
    public string[] MaliciousInputs { get; set; } = {
        "'; DROP TABLE users; --",
        "1' OR '1'='1",
        "'; DELETE FROM users WHERE '1'='1'; --",
        "<script>alert('xss')</script>",
        "UNION SELECT * FROM users",
        "'; EXEC xp_cmdshell('format c:'); --"
    };
    public bool ExpectSecurityViolation { get; set; } = false; // Should NOT have violations
    public bool TestParameterEscaping { get; set; } = true;
    public bool TestInputValidation { get; set; } = true;
    public bool TestQueryWhitelisting { get; set; } = true;
    public string[] AllowedQueries { get; set; } = Array.Empty<string>();
    public string[] ForbiddenKeywords { get; set; } = { "DROP", "DELETE", "UPDATE", "INSERT", "CREATE", "ALTER", "EXEC" };
    public bool TestRoleBasedAccess { get; set; } = false; // Not applicable for anonymous access
    public Func<string, bool>? InputSanitizer { get; set; }
}

/// <summary>
/// Test case for Dapper bulk operations
/// Validates bulk operations performance for Services Public API data synchronization
/// </summary>
public class BulkOperationsTestCase
{
    public string TestName { get; set; } = string.Empty;
    public BulkOperationType OperationType { get; set; } = BulkOperationType.Insert;
    public string TableName { get; set; } = string.Empty;
    public int RecordCount { get; set; } = 1000;
    public int BatchSize { get; set; } = 100;
    public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromSeconds(10);
    public bool TestTransaction { get; set; } = true;
    public bool TestRollback { get; set; } = true;
    public bool ValidateDataIntegrity { get; set; } = true;
    public bool MeasureMemoryUsage { get; set; } = true;
    public Func<int, Dictionary<string, object>>? DataGenerator { get; set; }
    public string[] RequiredColumns { get; set; } = Array.Empty<string>();
    public double MinThroughputPerSecond { get; set; } = 100; // records per second
}

/// <summary>
/// Bulk operation type enumeration
/// </summary>
public enum BulkOperationType
{
    Insert,
    Update,
    Delete,
    Upsert,
    Copy
}

/// <summary>
/// Test case for database migrations
/// Validates migration scripts execute correctly and maintain data integrity
/// </summary>
public class MigrationTestCase
{
    public string MigrationName { get; set; } = string.Empty;
    public string MigrationVersion { get; set; } = string.Empty;
    public string MigrationDescription { get; set; } = string.Empty;
    public string[] PreMigrationValidations { get; set; } = Array.Empty<string>();
    public string[] PostMigrationValidations { get; set; } = Array.Empty<string>();
    public bool TestDataPreservation { get; set; } = true;
    public bool TestSchemaChanges { get; set; } = true;
    public bool TestIndexCreation { get; set; } = true;
    public bool TestConstraintCreation { get; set; } = true;
    public TimeSpan MaxMigrationTime { get; set; } = TimeSpan.FromMinutes(5);
    public Dictionary<string, object>? TestData { get; set; }
    public string[] ExpectedTables { get; set; } = Array.Empty<string>();
    public string[] ExpectedColumns { get; set; } = Array.Empty<string>();
    public bool ValidateRollbackCapability { get; set; } = true;
}

/// <summary>
/// Test case for schema validation
/// Validates schema matches expected structure and constraints for Services APIs
/// </summary>
public class SchemaValidationTestCase
{
    public string TestName { get; set; } = string.Empty;
    public string[] ExpectedTables { get; set; } = Array.Empty<string>();
    public Dictionary<string, string[]> ExpectedColumns { get; set; } = new();
    public Dictionary<string, string[]> ExpectedIndexes { get; set; } = new();
    public string[] ExpectedConstraints { get; set; } = Array.Empty<string>();
    public string[] ExpectedForeignKeys { get; set; } = Array.Empty<string>();
    public bool ValidateDataTypes { get; set; } = true;
    public bool ValidateNullability { get; set; } = true;
    public bool ValidateDefaults { get; set; } = true;
    public Dictionary<string, Type> ExpectedColumnTypes { get; set; } = new();
    public Dictionary<string, bool> ExpectedNullableColumns { get; set; } = new();
    public Dictionary<string, object> ExpectedDefaultValues { get; set; } = new();
    public bool ValidateAuditTables { get; set; } = true;
}

/// <summary>
/// Test case for migration rollback
/// Validates migration rollback maintains data integrity and schema consistency
/// </summary>
public class MigrationRollbackTestCase
{
    public string MigrationName { get; set; } = string.Empty;
    public string FromVersion { get; set; } = string.Empty;
    public string ToVersion { get; set; } = string.Empty;
    public bool TestDataPreservation { get; set; } = true;
    public bool TestSchemaRestoration { get; set; } = true;
    public TimeSpan MaxRollbackTime { get; set; } = TimeSpan.FromMinutes(10);
    public Dictionary<string, object>? TestDataBeforeRollback { get; set; }
    public Dictionary<string, object>? ExpectedDataAfterRollback { get; set; }
    public string[] TablesAffectedByRollback { get; set; } = Array.Empty<string>();
    public bool ValidateDataIntegrity { get; set; } = true;
    public bool TestIndexRollback { get; set; } = true;
    public bool TestConstraintRollback { get; set; } = true;
    public string[] PreRollbackValidations { get; set; } = Array.Empty<string>();
    public string[] PostRollbackValidations { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Test case for production migration validation
/// Validates production migration scripts are safe and maintain medical-grade compliance
/// </summary>
public class ProductionMigrationTestCase
{
    public string MigrationName { get; set; } = string.Empty;
    public string MigrationScript { get; set; } = string.Empty;
    public bool TestDowntimeRequirement { get; set; } = true;
    public TimeSpan MaxAllowedDowntime { get; set; } = TimeSpan.FromMinutes(5);
    public bool TestBackwardCompatibility { get; set; } = true;
    public bool TestDataMigration { get; set; } = true;
    public bool ValidateMedicalCompliance { get; set; } = true;
    public string[] ComplianceRequirements { get; set; } = { "HIPAA", "GDPR", "SOC2" };
    public bool TestRollbackPlan { get; set; } = true;
    public bool TestRecoveryPlan { get; set; } = true;
    public int ExpectedDataVolumeGB { get; set; } = 1;
    public string[] CriticalTables { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> SafetyChecks { get; set; } = new();
    public bool TestPerformanceImpact { get; set; } = true;
}

/// <summary>
/// Test case for migration performance
/// Validates migrations complete within acceptable timeframes for production deployment
/// </summary>
public class MigrationPerformanceTestCase
{
    public string MigrationName { get; set; } = string.Empty;
    public int TestDataSizeMB { get; set; } = 100;
    public TimeSpan ExpectedMigrationTime { get; set; } = TimeSpan.FromMinutes(1);
    public bool TestConcurrentAccess { get; set; } = false;
    public bool MeasureResourceUsage { get; set; } = true;
    public long MaxMemoryUsageMB { get; set; } = 1000; // 1GB
    public double MaxCPUUsagePercent { get; set; } = 80;
    public bool TestLockingImpact { get; set; } = true;
    public bool TestIndexRebuildTime { get; set; } = true;
    public string[] PerformanceCriticalTables { get; set; } = Array.Empty<string>();
    public Dictionary<string, TimeSpan> TableMigrationThresholds { get; set; } = new();
    public bool ValidatePostMigrationPerformance { get; set; } = true;
}

/// <summary>
/// Test case for database backup
/// Validates backup integrity and completeness for medical-grade data protection
/// </summary>
public class DatabaseBackupTestCase
{
    public string BackupName { get; set; } = string.Empty;
    public BackupType BackupType { get; set; } = BackupType.Full;
    public string[] TablesToBackup { get; set; } = Array.Empty<string>();
    public bool TestBackupIntegrity { get; set; } = true;
    public bool TestBackupCompression { get; set; } = true;
    public bool TestBackupEncryption { get; set; } = false; // Not in test environment
    public TimeSpan MaxBackupTime { get; set; } = TimeSpan.FromMinutes(10);
    public long ExpectedBackupSizeBytes { get; set; } = -1; // -1 means don't validate
    public bool ValidateBackupConsistency { get; set; } = true;
    public bool TestPointInTimeRecovery { get; set; } = false;
    public BackupDestination Destination { get; set; } = BackupDestination.LocalFile;
    public string BackupPath { get; set; } = string.Empty;
    public Dictionary<string, object>? BackupMetadata { get; set; }
}

/// <summary>
/// Backup type enumeration
/// </summary>
public enum BackupType
{
    Full,
    Incremental,
    Differential,
    TransactionLog,
    Schema
}

/// <summary>
/// Backup destination enumeration
/// </summary>
public enum BackupDestination
{
    LocalFile,
    NetworkShare,
    CloudStorage,
    DatabaseServer
}

/// <summary>
/// Test case for database recovery
/// Validates point-in-time recovery and data integrity restoration
/// </summary>
public class DatabaseRecoveryTestCase
{
    public string RecoveryName { get; set; } = string.Empty;
    public string BackupSource { get; set; } = string.Empty;
    public DateTime? PointInTimeRecovery { get; set; }
    public RecoveryType RecoveryType { get; set; } = RecoveryType.Complete;
    public bool TestDataIntegrity { get; set; } = true;
    public bool TestSchemaIntegrity { get; set; } = true;
    public TimeSpan MaxRecoveryTime { get; set; } = TimeSpan.FromMinutes(30);
    public string[] TablesToValidate { get; set; } = Array.Empty<string>();
    public Dictionary<string, object>? ExpectedDataAfterRecovery { get; set; }
    public bool ValidateAuditTrail { get; set; } = true;
    public bool TestRecoveryCompleteness { get; set; } = true;
    public string TargetDatabase { get; set; } = string.Empty;
    public bool OverwriteExistingData { get; set; } = false;
}

/// <summary>
/// Recovery type enumeration
/// </summary>
public enum RecoveryType
{
    Complete,
    Partial,
    SchemaOnly,
    DataOnly,
    PointInTime
}

/// <summary>
/// Test case for backup retention
/// Validates backup retention meets medical-grade compliance requirements
/// </summary>
public class BackupRetentionTestCase
{
    public string TestName { get; set; } = string.Empty;
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(2555); // 7 years medical compliance
    public int NumberOfBackupsToGenerate { get; set; } = 30;
    public bool TestAutomaticCleanup { get; set; } = true;
    public bool TestRetentionPolicyEnforcement { get; set; } = true;
    public bool ValidateComplianceRequirements { get; set; } = true;
    public string[] ComplianceStandards { get; set; } = { "HIPAA", "GDPR", "SOC2" };
    public bool TestArchivalProcess { get; set; } = true;
    public bool TestBackupCatalogManagement { get; set; } = true;
    public BackupRetentionStrategy RetentionStrategy { get; set; } = BackupRetentionStrategy.TimeBasedRetention;
    public Dictionary<BackupType, TimeSpan> RetentionByType { get; set; } = new()
    {
        { BackupType.Full, TimeSpan.FromDays(30) },
        { BackupType.Incremental, TimeSpan.FromDays(7) },
        { BackupType.Differential, TimeSpan.FromDays(14) }
    };
}

/// <summary>
/// Backup retention strategy enumeration
/// </summary>
public enum BackupRetentionStrategy
{
    TimeBasedRetention,
    GenerationBasedRetention,
    HierarchicalRetention,
    ComplianceBasedRetention
}

/// <summary>
/// Test case for disaster recovery
/// Validates disaster recovery procedures and RTO/RPO compliance
/// </summary>
public class DisasterRecoveryTestCase
{
    public string ScenarioName { get; set; } = string.Empty;
    public DisasterType DisasterType { get; set; } = DisasterType.DatabaseCorruption;
    public TimeSpan RecoveryTimeObjective { get; set; } = TimeSpan.FromHours(4); // RTO
    public TimeSpan RecoveryPointObjective { get; set; } = TimeSpan.FromMinutes(15); // RPO
    public bool TestFailoverProcedure { get; set; } = true;
    public bool TestFailbackProcedure { get; set; } = true;
    public bool ValidateDataConsistency { get; set; } = true;
    public bool TestBusinessContinuity { get; set; } = true;
    public string[] CriticalSystems { get; set; } = Array.Empty<string>();
    public string[] CriticalData { get; set; } = Array.Empty<string>();
    public Dictionary<string, object>? PreDisasterState { get; set; }
    public Dictionary<string, object>? ExpectedPostRecoveryState { get; set; }
    public bool TestCommunicationPlan { get; set; } = false; // Not applicable in test environment
    public bool ValidateComplianceReporting { get; set; } = true;
}

/// <summary>
/// Disaster type enumeration
/// </summary>
public enum DisasterType
{
    DatabaseCorruption,
    ServerFailure,
    NetworkFailure,
    DataCenterOutage,
    CyberAttack,
    NaturalDisaster,
    HumanError,
    SoftwareFailure
}
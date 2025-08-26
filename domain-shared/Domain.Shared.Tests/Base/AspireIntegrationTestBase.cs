using Aspire.Hosting.Testing;
using InternationalCenter.Tests.Shared.Utils;
using Npgsql;
using Respawn;
using Xunit;
using Xunit.Abstractions;

namespace InternationalCenter.Tests.Shared.Base;

/// <summary>
/// Base class for Aspire integration tests with automated test data cleanup
/// Provides common functionality for distributed application testing with Respawn data isolation
/// WHY: Consistent test infrastructure reduces duplication and ensures reliable test isolation
/// SCOPE: All Services API integration tests (Public and Admin)
/// CONTEXT: Aspire distributed application testing with medical-grade data isolation requirements
/// </summary>
public abstract class AspireIntegrationTestBase : IAsyncLifetime
{
    protected readonly ITestOutputHelper Output;
    protected DistributedApplication? App;
    protected HttpClient? HttpClient;
    private Respawner? _respawner;
    private string? _connectionString;

    protected AspireIntegrationTestBase(ITestOutputHelper output)
    {
        Output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>
    /// Get the service name for HTTP client creation
    /// Override in concrete test classes to specify the correct service
    /// </summary>
    protected abstract string GetServiceName();

    /// <summary>
    /// Initialize distributed application and test data cleanup
    /// Sets up Aspire orchestration and Respawn for database isolation
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        // Create distributed application using Microsoft documented DistributedApplicationTestingBuilder
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        App = await builder.BuildAsync();
        await App.StartAsync();

        // Get HTTP client through Aspire orchestration
        HttpClient = App.CreateHttpClient(GetServiceName());

        // Initialize database cleanup with Respawn
        _connectionString = await App.GetConnectionStringAsync("database");
        if (_connectionString != null)
        {
            await InitializeRespawnerAsync();
        }

        Output.WriteLine($"✅ ASPIRE INTEGRATION: Distributed application started for {GetServiceName()}");
    }

    /// <summary>
    /// Initialize Respawn for reliable database cleanup
    /// Configures PostgreSQL-specific settings for test isolation
    /// </summary>
    private async Task InitializeRespawnerAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            // Reset data but preserve schema and migrations table
            TablesToIgnore = new[] { "__EFMigrationsHistory" },
            
            // Use PostgreSQL-specific configuration
            DbAdapter = DbAdapter.Postgres,
            
            // Reset sequences to ensure consistent IDs
            WithReseed = true,
            
            // Include all schemas (public schema in PostgreSQL)
            SchemasToInclude = new[] { "public" }
        });

        Output.WriteLine($"✅ TEST CLEANUP: Respawner initialized for {GetServiceName()}");
    }

    /// <summary>
    /// Clean database to pristine state for test isolation
    /// Should be called before each test that modifies data
    /// </summary>
    protected async Task CleanDatabaseAsync()
    {
        if (_respawner == null || _connectionString == null)
        {
            throw new InvalidOperationException("Test cleanup not initialized properly");
        }

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);

        Output.WriteLine("✅ TEST CLEANUP: Database reset to clean state");
    }

    /// <summary>
    /// Verify database is in expected clean state
    /// Validates that cleanup was successful for test isolation
    /// </summary>
    protected async Task VerifyCleanDatabaseStateAsync()
    {
        if (_connectionString == null)
        {
            throw new InvalidOperationException("Database connection not available");
        }

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Verify key tables are empty
        var servicesCount = await GetTableCountAsync(connection, "services");
        var categoriesCount = await GetTableCountAsync(connection, "service_categories");

        if (servicesCount > 0 || categoriesCount > 0)
        {
            throw new InvalidOperationException(
                $"Database cleanup verification failed: services={servicesCount}, categories={categoriesCount}");
        }

        Output.WriteLine($"✅ TEST CLEANUP: Verified clean database state (services={servicesCount}, categories={categoriesCount})");
    }

    /// <summary>
    /// Get connection string for direct database operations
    /// Use for setup/cleanup operations that need raw database access
    /// </summary>
    protected async Task<string> GetConnectionStringAsync()
    {
        if (_connectionString == null)
        {
            throw new InvalidOperationException("Database connection string not available");
        }
        
        return _connectionString;
    }

    /// <summary>
    /// Setup minimal test category for tests that need it
    /// Returns the category ID for use in service creation
    /// </summary>
    protected async Task<Guid> SetupTestCategoryAsync()
    {
        var connectionString = await GetConnectionStringAsync();
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var categoryId = Guid.NewGuid();
        var insertQuery = @"
            INSERT INTO service_categories (id, name, slug, description, active, created_at, updated_at)
            VALUES (@Id, @Name, @Slug, @Description, @Active, @CreatedAt, @UpdatedAt)";

        using var command = connection.CreateCommand();
        command.CommandText = insertQuery;
        command.Parameters.AddWithValue("@Id", categoryId);
        command.Parameters.AddWithValue("@Name", "Test Category");
        command.Parameters.AddWithValue("@Slug", "test-category");
        command.Parameters.AddWithValue("@Description", "Category for integration testing");
        command.Parameters.AddWithValue("@Active", true);
        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync();

        Output.WriteLine($"✅ TEST SETUP: Created test category {categoryId}");
        return categoryId;
    }

    /// <summary>
    /// Cleanup resources and dispose of distributed application
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        _respawner?.Dispose();
        
        if (App is not null)
        {
            await App.DisposeAsync();
        }
    }

    /// <summary>
    /// Get count of records in specified table
    /// Helper method for verification operations
    /// </summary>
    private async Task<int> GetTableCountAsync(NpgsqlConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Execute HTTP GET request with exponential backoff retry strategy
    /// Provides reliability for gateway operations in distributed environments
    /// </summary>
    protected async Task<HttpResponseMessage> GetWithRetryAsync(
        string requestUri,
        RetryConfig? retryConfig = null,
        string? operationName = null)
    {
        return await RetryHelper.ExecuteHttpWithRetryAsync(
            () => HttpClient!.GetAsync(requestUri),
            retryConfig ?? RetryHelper.DefaultHttpRetry,
            Output,
            operationName ?? $"GET {requestUri}");
    }

    /// <summary>
    /// Execute HTTP POST request with exponential backoff retry strategy
    /// Provides reliability for gateway operations in distributed environments
    /// </summary>
    protected async Task<HttpResponseMessage> PostWithRetryAsync<T>(
        string requestUri,
        T content,
        RetryConfig? retryConfig = null,
        string? operationName = null)
    {
        return await RetryHelper.ExecuteHttpWithRetryAsync(
            () => HttpClient!.PostAsJsonAsync(requestUri, content),
            retryConfig ?? RetryHelper.DefaultHttpRetry,
            Output,
            operationName ?? $"POST {requestUri}");
    }

    /// <summary>
    /// Execute database operation with exponential backoff retry strategy
    /// Provides reliability for database operations in distributed test environments
    /// </summary>
    protected async Task<T> ExecuteDatabaseOperationWithRetryAsync<T>(
        Func<Task<T>> operation,
        RetryConfig? retryConfig = null,
        string? operationName = null)
    {
        return await RetryHelper.ExecuteWithRetryAsync(
            operation,
            retryConfig ?? RetryHelper.DefaultDatabaseRetry,
            Output,
            operationName ?? "Database operation");
    }

    /// <summary>
    /// Execute database operation without return value with exponential backoff retry strategy
    /// Provides reliability for database operations in distributed test environments
    /// </summary>
    protected async Task ExecuteDatabaseOperationWithRetryAsync(
        Func<Task> operation,
        RetryConfig? retryConfig = null,
        string? operationName = null)
    {
        await RetryHelper.ExecuteWithRetryAsync(
            operation,
            retryConfig ?? RetryHelper.DefaultDatabaseRetry,
            Output,
            operationName ?? "Database operation");
    }

    /// <summary>
    /// Execute cache operation with exponential backoff retry strategy
    /// Provides reliability for cache operations with fast retry cycles
    /// </summary>
    protected async Task<T> ExecuteCacheOperationWithRetryAsync<T>(
        Func<Task<T>> operation,
        RetryConfig? retryConfig = null,
        string? operationName = null)
    {
        return await RetryHelper.ExecuteWithRetryAsync(
            operation,
            retryConfig ?? RetryHelper.DefaultCacheRetry,
            Output,
            operationName ?? "Cache operation");
    }
}
using Npgsql;
using Respawn;
using Xunit.Abstractions;

namespace InternationalCenter.Services.Public.Api.Tests.Integration.Infrastructure;

/// <summary>
/// Test data cleanup service using Respawn for reliable test isolation
/// WHY: Integration tests require clean database state between test runs for reliable TDD cycles
/// SCOPE: Services.Public.Api integration tests with PostgreSQL cleanup
/// CONTEXT: Public API anonymous access patterns require pristine test data isolation
/// </summary>
public class TestDataCleanupService : IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly ITestOutputHelper _output;
    private Respawner? _respawner;

    public TestDataCleanupService(string connectionString, ITestOutputHelper output)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>
    /// Initialize Respawner with proper PostgreSQL configuration
    /// Configures tables to reset between tests while preserving schema
    /// </summary>
    public async Task InitializeAsync()
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

        _output.WriteLine("✅ TEST CLEANUP: Respawner initialized for PostgreSQL test data cleanup");
    }

    /// <summary>
    /// Reset database to clean state between tests
    /// Removes all test data while preserving schema and migrations
    /// </summary>
    public async Task CleanupTestDataAsync()
    {
        if (_respawner == null)
        {
            throw new InvalidOperationException("TestDataCleanupService must be initialized before cleanup");
        }

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await _respawner.ResetAsync(connection);

        _output.WriteLine("✅ TEST CLEANUP: Database reset to clean state using Respawn");
    }

    /// <summary>
    /// Verify database is in expected clean state
    /// Validates that cleanup was successful
    /// </summary>
    public async Task VerifyCleanStateAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Verify key tables are empty
        var servicesTotalQuery = "SELECT COUNT(*) FROM services";
        var categoresTotalQuery = "SELECT COUNT(*) FROM service_categories";

        using var servicesCommand = connection.CreateCommand();
        servicesCommand.CommandText = servicesTotalQuery;
        var servicesCount = Convert.ToInt32(await servicesCommand.ExecuteScalarAsync());

        using var categoriesCommand = connection.CreateCommand();
        categoriesCommand.CommandText = categoresTotalQuery;
        var categoriesCount = Convert.ToInt32(await categoriesCommand.ExecuteScalarAsync());

        if (servicesCount > 0 || categoriesCount > 0)
        {
            throw new InvalidOperationException(
                $"Database cleanup verification failed: services={servicesCount}, categories={categoriesCount}");
        }

        _output.WriteLine($"✅ TEST CLEANUP: Verified clean database state (services={servicesCount}, categories={categoriesCount})");
    }

    /// <summary>
    /// Setup minimal test data required for tests
    /// Creates only essential data needed for test scenarios
    /// </summary>
    public async Task SetupMinimalTestDataAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Create a default test category that tests can use
        var categoryId = Guid.NewGuid();
        var categoryInsertQuery = @"
            INSERT INTO service_categories (id, name, slug, description, active, created_at, updated_at)
            VALUES (@Id, @Name, @Slug, @Description, @Active, @CreatedAt, @UpdatedAt)";

        using var categoryCommand = connection.CreateCommand();
        categoryCommand.CommandText = categoryInsertQuery;
        categoryCommand.Parameters.AddWithValue("@Id", categoryId);
        categoryCommand.Parameters.AddWithValue("@Name", "Test Category");
        categoryCommand.Parameters.AddWithValue("@Slug", "test-category");
        categoryCommand.Parameters.AddWithValue("@Description", "Default test category for integration tests");
        categoryCommand.Parameters.AddWithValue("@Active", true);
        categoryCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        categoryCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

        await categoryCommand.ExecuteNonQueryAsync();

        _output.WriteLine($"✅ TEST CLEANUP: Setup minimal test data (test category: {categoryId})");
    }

    public async ValueTask DisposeAsync()
    {
        _respawner?.Dispose();
        await Task.CompletedTask;
    }
}
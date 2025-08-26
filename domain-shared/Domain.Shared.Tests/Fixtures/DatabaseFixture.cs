using InternationalCenter.Tests.Shared.Containers;
using InternationalCenter.Services.Domain.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Testcontainers.PostgreSql;
using Npgsql;

namespace InternationalCenter.Tests.Shared.Fixtures;

/// <summary>
/// Manages PostgreSQL TestContainer lifecycle for database testing
/// Uses real database instances for integration testing with Microsoft patterns
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private Respawner? _respawner;
    
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        // Create and start PostgreSQL container with Podman
        _container = PodmanContainerConfiguration
            .CreatePostgreSqlContainer()
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // Ensure database schema is created first
        await EnsureDatabaseCreatedAsync<ServicesDbContext>();

        // Initialize Respawner for database cleanup between tests
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a DbContext for testing with the container database
    /// </summary>
    public T CreateDbContext<T>() where T : DbContext
    {
        var services = new ServiceCollection();
        services.AddDbContext<T>(options => 
            options.UseNpgsql(ConnectionString));
        
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<T>();
    }

    /// <summary>
    /// Ensures database schema is created from the model
    /// Uses EnsureCreated for testing purposes instead of migrations
    /// </summary>
    public async Task EnsureDatabaseCreatedAsync<T>() where T : DbContext
    {
        using var context = CreateDbContext<T>();
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Cleans the database between test methods using Respawner
    /// Microsoft recommended pattern for test isolation
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        if (_respawner == null) return;
        
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    /// <summary>
    /// Seeds realistic test data using Bogus data generation
    /// Avoids mock data by generating realistic entities
    /// </summary>
    public async Task SeedTestDataAsync<T>(T context, int count = 10) where T : DbContext
    {
        // This will be implemented based on specific entity requirements
        // Each test class can override this for their specific data needs
        await context.SaveChangesAsync();
    }
}
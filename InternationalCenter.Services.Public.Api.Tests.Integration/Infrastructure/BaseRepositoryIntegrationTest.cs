using InternationalCenter.Services.Public.Api.Infrastructure.Data;
using InternationalCenter.Services.Public.Api.Infrastructure.Repositories;
using InternationalCenter.Tests.Shared.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace InternationalCenter.Services.Public.Api.Tests.Integration.Infrastructure;

/// <summary>
/// Base class for repository integration tests using real PostgreSQL
/// Provides common setup and utilities for testing repository implementations
/// </summary>
public abstract class BaseRepositoryIntegrationTest : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    protected readonly DatabaseFixture DatabaseFixture;
    protected ServicesDbContext DbContext { get; private set; } = null!;

    protected BaseRepositoryIntegrationTest(DatabaseFixture databaseFixture)
    {
        DatabaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        // Create and configure DbContext for testing using test-only constructor
        var options = new DbContextOptionsBuilder<ServicesDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .EnableServiceProviderCaching()
            .EnableSensitiveDataLogging()
            .Options;

        DbContext = new ServicesDbContext(options);

        // Ensure database is created and migrated
        await DbContext.Database.EnsureCreatedAsync();
        
        // Reset database to clean state for each test class
        await DatabaseFixture.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        if (DbContext != null)
        {
            await DbContext.DisposeAsync();
        }
    }

    /// <summary>
    /// Clears all entities from the database between test methods
    /// Ensures test isolation
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        await DatabaseFixture.ResetDatabaseAsync();
    }

    /// <summary>
    /// Creates a new DbContext instance for repository testing
    /// Each repository gets its own context to avoid tracking conflicts
    /// </summary>
    protected ServicesDbContext CreateNewDbContext()
    {
        var options = new DbContextOptionsBuilder<ServicesDbContext>()
            .UseNpgsql(DatabaseFixture.ConnectionString)
            .EnableServiceProviderCaching()
            .EnableSensitiveDataLogging()
            .Options;
            
        // Use test-only constructor
        return new ServicesDbContext(options);
    }

    /// <summary>
    /// Seeds the database with test data and returns the entities
    /// </summary>
    protected async Task<TEntity> SeedAsync<TEntity>(TEntity entity) where TEntity : class
    {
        DbContext.Set<TEntity>().Add(entity);
        await DbContext.SaveChangesAsync();
        
        // Detach to avoid tracking conflicts
        DbContext.Entry(entity).State = EntityState.Detached;
        return entity;
    }

    /// <summary>
    /// Seeds multiple entities and returns them
    /// </summary>
    protected async Task<IReadOnlyList<TEntity>> SeedAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
    {
        var entityList = entities.ToList();
        DbContext.Set<TEntity>().AddRange(entityList);
        await DbContext.SaveChangesAsync();
        
        // Detach all to avoid tracking conflicts
        foreach (var entity in entityList)
        {
            DbContext.Entry(entity).State = EntityState.Detached;
        }
        
        return entityList.AsReadOnly();
    }

    /// <summary>
    /// Creates a ServiceRepository instance with proper logging and context
    /// </summary>
    protected ServiceRepository CreateServiceRepository(ServicesDbContext? context = null)
    {
        var dbContext = context ?? CreateNewDbContext();
        var logger = NullLogger<ServiceRepository>.Instance;
        return new ServiceRepository(dbContext, logger);
    }
}
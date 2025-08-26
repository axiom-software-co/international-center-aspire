using InternationalCenter.Services.Domain.Infrastructure.Data;
using InternationalCenter.Tests.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace InternationalCenter.Tests.Shared.Base;

/// <summary>
/// Base class for repository unit tests with standardized in-memory database setup
/// WHY: Eliminates duplicated infrastructure setup code across repository unit tests
/// SCOPE: All Services API repository unit tests (Public and Admin)
/// CONTEXT: Contract-first testing requires consistent unit test infrastructure with mocks
/// </summary>
/// <typeparam name="TRepository">The repository type being tested</typeparam>
/// <typeparam name="TContext">The DbContext type</typeparam>
public abstract class RepositoryUnitTestBase<TRepository, TContext> : UnitTestBase, IDisposable
    where TRepository : class
    where TContext : DbContext
{
    protected readonly TContext Context;
    protected readonly TRepository Repository;
    private bool _disposed;

    protected RepositoryUnitTestBase(ITestOutputHelper output) : base(output)
    {
        // Create in-memory database with unique name per test instance
        var options = new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        Context = CreateDbContext(options);
        Repository = CreateRepository(Context);

        // Ensure database is created
        Context.Database.EnsureCreated();

        Output.WriteLine($"✅ UNIT TEST SETUP: In-memory database created for {typeof(TRepository).Name}");
    }

    /// <summary>
    /// Factory method for creating the DbContext - override in concrete test classes
    /// </summary>
    protected abstract TContext CreateDbContext(DbContextOptions<TContext> options);

    /// <summary>
    /// Factory method for creating the repository - override in concrete test classes
    /// </summary>
    protected abstract TRepository CreateRepository(TContext context);

    /// <summary>
    /// Create a mock logger for the specified type
    /// Provides standardized mock creation across repository tests
    /// </summary>
    protected Mock<ILogger<T>> CreateMockLogger<T>() where T : class
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// Seeds test entity into the in-memory database
    /// </summary>
    protected async Task SeedAsync<TEntity>(TEntity entity) where TEntity : class
    {
        Context.Set<TEntity>().Add(entity);
        await Context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds multiple test entities into the in-memory database
    /// </summary>
    protected async Task SeedAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
    {
        Context.Set<TEntity>().AddRange(entities);
        await Context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets entity by ID from the in-memory database
    /// </summary>
    protected async Task<TEntity?> GetByIdAsync<TEntity, TKey>(TKey id) where TEntity : class
    {
        return await Context.Set<TEntity>().FindAsync(id);
    }

    /// <summary>
    /// Counts total entities of specified type
    /// </summary>
    protected async Task<int> CountAsync<TEntity>() where TEntity : class
    {
        return await Context.Set<TEntity>().CountAsync();
    }

    /// <summary>
    /// Validates repository performance within unit test thresholds
    /// </summary>
    protected async Task<T> ExecuteRepositoryOperationWithPerformanceValidation<T>(
        Func<Task<T>> operation,
        string operationName)
    {
        return await ExecuteWithPerformanceValidation(
            operation,
            StandardizedTestConfiguration.Timeouts.UnitTestQuick,
            $"Repository {operationName}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        Context?.Dispose();
        _disposed = true;
        
        Output.WriteLine($"✅ UNIT TEST CLEANUP: Disposed {typeof(TRepository).Name} test infrastructure");
    }
}
using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Domain.Infrastructure.Data;
using InternationalCenter.Tests.Shared.Fixtures;
using InternationalCenter.Tests.Shared.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Respawn.Graph;

namespace InternationalCenter.Services.Admin.Api.Tests.Integration.Infrastructure;

/// <summary>
/// Base class for Services Admin API integration tests with real PostgreSQL infrastructure
/// Provides database seeding, cleanup, and test data management using Microsoft patterns
/// </summary>
public abstract class BaseAdminApiIntegrationTest : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    protected readonly DatabaseFixture DatabaseFixture;

    protected BaseAdminApiIntegrationTest(DatabaseFixture databaseFixture)
    {
        DatabaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        // Database is already initialized by the DatabaseFixture
        await Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        // Use DatabaseFixture's cleanup method for database reset between tests
        await DatabaseFixture.ResetDatabaseAsync();
    }

    /// <summary>
    /// Seeds test data into the database for testing
    /// </summary>
    protected async Task SeedAsync<TEntity>(TEntity entity) where TEntity : class
    {
        using var context = DatabaseFixture.CreateDbContext<ServicesDbContext>();
        context.Set<TEntity>().Add(entity);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds multiple test entities into the database
    /// </summary>
    protected async Task SeedAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
    {
        using var context = DatabaseFixture.CreateDbContext<ServicesDbContext>();
        context.Set<TEntity>().AddRange(entities);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets a service category from the database by ID
    /// </summary>
    protected async Task<ServiceCategory?> GetServiceCategoryAsync(string id)
    {
        using var context = DatabaseFixture.CreateDbContext<ServicesDbContext>();
        if (int.TryParse(id, out var idValue))
        {
            var categoryId = ServiceCategoryId.Create(idValue);
            return await context.ServiceCategories.FirstOrDefaultAsync(c => c.Id == categoryId);
        }
        return null;
    }

    /// <summary>
    /// Gets a service from the database by ID
    /// </summary>
    protected async Task<Service?> GetServiceAsync(string id)
    {
        using var context = DatabaseFixture.CreateDbContext<ServicesDbContext>();
        return await context.Services
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id.Value == id);
    }

    /// <summary>
    /// Counts total service categories in the database
    /// </summary>
    protected async Task<int> CountServiceCategoriesAsync()
    {
        using var context = DatabaseFixture.CreateDbContext<ServicesDbContext>();
        return await context.ServiceCategories.CountAsync();
    }

    /// <summary>
    /// Counts total services in the database
    /// </summary>
    protected async Task<int> CountServicesAsync()
    {
        using var context = DatabaseFixture.CreateDbContext<ServicesDbContext>();
        return await context.Services.CountAsync();
    }

    /// <summary>
    /// Creates test service categories for seeding
    /// </summary>
    protected static IEnumerable<ServiceCategory> CreateTestServiceCategories(int count = 3)
    {
        return ServiceTestDataGenerator.GenerateCategories(count);
    }

    /// <summary>
    /// Creates test services for seeding
    /// </summary>
    protected static IEnumerable<Service> CreateTestServices(int count = 5)
    {
        return ServiceTestDataGenerator.GenerateServices(count);
    }
}
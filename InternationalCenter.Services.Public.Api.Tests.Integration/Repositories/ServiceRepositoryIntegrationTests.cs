using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.Specifications;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Public.Api.Infrastructure.Repositories;
using InternationalCenter.Services.Public.Api.Tests.Integration.Infrastructure;
using InternationalCenter.Tests.Shared.Fixtures;
using InternationalCenter.Tests.Shared.TestData;

namespace InternationalCenter.Services.Public.Api.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for ServiceRepository using real PostgreSQL database
/// Tests all repository operations with actual Entity Framework Core context
/// </summary>
public class ServiceRepositoryIntegrationTests : BaseRepositoryIntegrationTest
{
    public ServiceRepositoryIntegrationTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
    }

    [Fact]
    public async Task AddAsync_WithValidService_ShouldPersistToDatabase()
    {
        // Arrange
        using var context = CreateNewDbContext();
        var repository = CreateServiceRepository(context);
        var service = ServiceTestDataGenerator.GenerateService();

        // Act
        await repository.AddAsync(service);
        await repository.SaveChangesAsync();

        // Assert
        var savedService = await repository.GetByIdAsync(service.Id);
        Assert.NotNull(savedService);
        Assert.Equal(service.Id, savedService.Id);
        Assert.Equal(service.Title, savedService.Title);
        Assert.Equal(service.Slug, savedService.Slug);
        Assert.Equal(service.Status, savedService.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingService_ShouldReturnService()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        await SeedAsync(service);

        using var context = CreateNewDbContext();
        var repository = CreateServiceRepository(context);

        // Act
        var result = await repository.GetByIdAsync(service.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(service.Id, result.Id);
        Assert.Equal(service.Title, result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentService_ShouldReturnNull()
    {
        // Arrange
        using var context = CreateNewDbContext();
        var repository = CreateServiceRepository(context);
        var nonExistentId = ServiceId.Create();

        // Act
        var result = await repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySlugAsync_WithExistingSlug_ShouldReturnService()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        await SeedAsync(service);

        using var context = CreateNewDbContext();
        var repository = CreateServiceRepository(context);

        // Act
        var result = await repository.GetBySlugAsync(service.Slug);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(service.Slug, result.Slug);
        Assert.Equal(service.Title, result.Title);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleServices_ShouldReturnAllServices()
    {
        // Arrange
        var services = ServiceTestDataGenerator.GenerateServices(5).ToList();
        await SeedAsync((IEnumerable<Service>)services);

        using var context = CreateNewDbContext();
        var repository = CreateServiceRepository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Equal(services.Count, result.Count);
        Assert.All(services, service => 
            Assert.Contains(result, r => r.Id == service.Id));
    }

    [Fact]
    public async Task GetPagedAsync_WithSpecification_ShouldReturnPagedResults()
    {
        // Arrange
        var services = ServiceTestDataGenerator.GenerateServices(10).ToList();
        
        // Make some services published for testing
        foreach (var service in services.Take(7))
        {
            service.Publish();
        }
        
        await SeedAsync((IEnumerable<Service>)services);

        using var context = CreateNewDbContext();
        var repository = CreateServiceRepository(context);
        var specification = new PublishedServicesSpecification();

        // Act
        var (items, totalCount) = await repository.GetPagedAsync(specification, page: 1, pageSize: 5);

        // Assert
        Assert.Equal(5, items.Count);
        Assert.Equal(7, totalCount); // Total published services
        Assert.All(items, service => Assert.Equal(ServiceStatus.Published, service.Status));
    }

    [Fact]
    public async Task ExistsAsync_WithExistingService_ShouldReturnTrue()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        await SeedAsync(service);

        using var context = CreateNewDbContext();
        var repository = CreateServiceRepository(context);

        // Act
        var exists = await repository.ExistsAsync(service.Id);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentService_ShouldReturnFalse()
    {
        // Arrange
        using var context = CreateNewDbContext();
        var repository = CreateServiceRepository(context);
        var nonExistentId = ServiceId.Create();

        // Act
        var exists = await repository.ExistsAsync(nonExistentId);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task SlugExistsAsync_WithExistingSlug_ShouldReturnTrue()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        await SeedAsync(service);

        using var context = CreateNewDbContext();
        var repository = CreateServiceRepository(context);

        // Act
        var exists = await repository.SlugExistsAsync(service.Slug);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task SlugExistsAsync_WithExcludedId_ShouldIgnoreExcludedService()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        await SeedAsync(service);

        using var context = CreateNewDbContext();
        var repository = CreateServiceRepository(context);

        // Act
        var exists = await repository.SlugExistsAsync(service.Slug, excludeId: service.Id);

        // Assert
        Assert.False(exists); // Should not find the service because it's excluded
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedService_ShouldPersistChanges()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        await SeedAsync(service);

        using var context = CreateNewDbContext();
        var repository = CreateServiceRepository(context);
        
        // Get the service to modify
        var serviceToUpdate = await repository.GetByIdAsync(service.Id);
        Assert.NotNull(serviceToUpdate);
        
        var newTitle = "Updated Service Title";
        serviceToUpdate.UpdateTitle(newTitle);

        // Act
        await repository.UpdateAsync(serviceToUpdate);
        await repository.SaveChangesAsync();

        // Assert
        using var verificationContext = CreateNewDbContext();
        var verificationRepository = CreateServiceRepository(verificationContext);
        var updatedService = await verificationRepository.GetByIdAsync(service.Id);
        
        Assert.NotNull(updatedService);
        Assert.Equal(newTitle, updatedService.Title);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingService_ShouldRemoveFromDatabase()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        await SeedAsync(service);

        using var context = CreateNewDbContext();
        var repository = CreateServiceRepository(context);
        
        var serviceToDelete = await repository.GetByIdAsync(service.Id);
        Assert.NotNull(serviceToDelete);

        // Act
        await repository.DeleteAsync(serviceToDelete);
        await repository.SaveChangesAsync();

        // Assert
        using var verificationContext = CreateNewDbContext();
        var verificationRepository = CreateServiceRepository(verificationContext);
        var deletedService = await verificationRepository.GetByIdAsync(service.Id);
        
        Assert.Null(deletedService);
    }

    [Fact]
    public async Task CountAsync_WithSpecification_ShouldReturnCorrectCount()
    {
        // Arrange
        var services = ServiceTestDataGenerator.GenerateServices(8).ToList();
        
        // Make some services published for testing
        foreach (var service in services.Take(5))
        {
            service.Publish();
        }
        
        await SeedAsync((IEnumerable<Service>)services);

        using var context = CreateNewDbContext();
        var repository = CreateServiceRepository(context);
        var specification = new PublishedServicesSpecification();

        // Act
        var count = await repository.CountAsync(specification);

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task GetBySpecificationAsync_WithFeaturedSpecification_ShouldReturnOnlyFeaturedServices()
    {
        // Arrange
        var services = ServiceTestDataGenerator.GenerateServices(6).ToList();
        
        // Publish all services
        foreach (var service in services)
        {
            service.Publish();
        }
        
        // Make some services featured, published, and available
        foreach (var service in services.Take(3))
        {
            service.SetFeatured(true);
            service.Publish(); // Make sure they're published
            service.SetAvailability(true); // Make sure they're available
        }
        
        await SeedAsync((IEnumerable<Service>)services);

        using var context = CreateNewDbContext();
        var repository = CreateServiceRepository(context);
        var specification = new FeaturedServicesSpecification();

        // Act
        var result = await repository.GetBySpecificationAsync(specification);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, service => Assert.True(service.Featured));
    }
}
using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.Specifications;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Domain.Infrastructure.Data;
using InternationalCenter.Services.Public.Api.Infrastructure.Repositories;
using InternationalCenter.Tests.Shared.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace InternationalCenter.Services.Public.Api.Tests.Unit.Infrastructure.Repositories;

/// <summary>
/// Unit tests for ServiceRepository using in-memory database
/// Tests repository operations with realistic data persistence scenarios
/// </summary>
public class ServiceRepositoryTests : IDisposable
{
    private readonly ServicesDbContext _context;
    private readonly Mock<ILogger<ServiceRepository>> _mockLogger;
    private readonly ServiceRepository _repository;
    private bool _disposed;

    public ServiceRepositoryTests()
    {
        // Create in-memory database with unique name per test instance
        var options = new DbContextOptionsBuilder<ServicesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        _context = new ServicesDbContext(options);
        _mockLogger = new Mock<ILogger<ServiceRepository>>();
        _repository = new ServiceRepository(_context, _mockLogger.Object);

        // Ensure database is created
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ServiceRepository(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert  
        Assert.Throws<ArgumentNullException>(() => 
            new ServiceRepository(_context, null!));
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingService_ShouldReturnServiceWithCategory()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = CreateServiceWithCategory(category);
        await SeedDatabaseAsync(category, service);

        // Act
        var result = await _repository.GetByIdAsync(service.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(service.Id, result.Id);
        Assert.Equal(service.Title, result.Title);
        Assert.NotNull(result.Category);
        Assert.Equal(category.Name, result.Category.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentService_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = ServiceId.Create();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySlugAsync_WithExistingSlug_ShouldReturnServiceWithCategory()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = CreateServiceWithCategory(category);
        await SeedDatabaseAsync(category, service);

        // Act
        var result = await _repository.GetBySlugAsync(service.Slug);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(service.Slug, result.Slug);
        Assert.Equal(service.Title, result.Title);
        Assert.NotNull(result.Category);
        Assert.Equal(category.Name, result.Category.Name);
    }

    [Fact]
    public async Task GetBySlugAsync_WithNonExistentSlug_ShouldReturnNull()
    {
        // Arrange
        var nonExistentSlug = Slug.Create("non-existent-service");

        // Act
        var result = await _repository.GetBySlugAsync(nonExistentSlug);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleServices_ShouldReturnServicesOrderedBySortOrderThenTitle()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service1 = CreateServiceWithCategory(category, "web-development", "Web Development");
        service1.SetSortOrder(2);
        var service2 = CreateServiceWithCategory(category, "mobile-apps", "Mobile Apps");
        service2.SetSortOrder(1);
        var service3 = CreateServiceWithCategory(category, "api-integration", "API Integration");
        service3.SetSortOrder(2); // Same sort order as service1

        await SeedDatabaseAsync(category, service1, service2, service3);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Mobile Apps", result[0].Title); // Sort order 1
        Assert.Equal("API Integration", result[1].Title); // Sort order 2, title alphabetically first
        Assert.Equal("Web Development", result[2].Title); // Sort order 2, title alphabetically second
    }

    [Fact]
    public async Task GetAllAsync_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBySpecificationAsync_WithFeaturedSpecification_ShouldReturnOnlyFeaturedServices()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var featuredService = CreateServiceWithCategory(category, "featured-service", "Featured Service");
        featuredService.SetFeatured(true);
        var regularService = CreateServiceWithCategory(category, "regular-service", "Regular Service");
        regularService.SetFeatured(false);

        await SeedDatabaseAsync(category, featuredService, regularService);

        var specification = new FeaturedServicesSpecification();

        // Act
        var result = await _repository.GetBySpecificationAsync(specification);

        // Assert
        Assert.Single(result);
        Assert.Equal(featuredService.Id, result[0].Id);
        Assert.True(result[0].Featured);
    }

    [Fact]
    public async Task GetPagedAsync_WithSpecification_ShouldReturnCorrectPageAndTotalCount()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var services = new List<Service>();
        
        // Create 5 services for pagination testing
        for (int i = 1; i <= 5; i++)
        {
            var service = CreateServiceWithCategory(category, $"service-{i}", $"Service {i}");
            service.SetSortOrder(i);
            services.Add(service);
        }

        var allEntities = new List<object> { category };
        allEntities.AddRange(services);
        await SeedDatabaseAsync(allEntities.ToArray());

        var specification = new ServicesSearchSpecification(string.Empty, null, null, "title");
        const int page = 2;
        const int pageSize = 2;

        // Act
        var (items, totalCount) = await _repository.GetPagedAsync(specification, page, pageSize);

        // Assert
        Assert.Equal(5, totalCount);
        Assert.Equal(2, items.Count);
        Assert.Equal("Service 3", items[0].Title); // Page 2, items 3-4
        Assert.Equal("Service 4", items[1].Title);
    }

    [Fact]
    public async Task CountAsync_WithSpecification_ShouldReturnCorrectCount()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var availableService = CreateServiceWithCategory(category, "available", "Available Service");
        availableService.SetAvailability(true);
        var unavailableService = CreateServiceWithCategory(category, "unavailable", "Unavailable Service");
        unavailableService.SetAvailability(false);

        await SeedDatabaseAsync(category, availableService, unavailableService);

        var specification = new AvailableServicesSpecification();

        // Act
        var count = await _repository.CountAsync(specification);

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingService_ShouldReturnTrue()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = CreateServiceWithCategory(category);
        await SeedDatabaseAsync(category, service);

        // Act
        var exists = await _repository.ExistsAsync(service.Id);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentService_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = ServiceId.Create();

        // Act
        var exists = await _repository.ExistsAsync(nonExistentId);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task SlugExistsAsync_WithExistingSlug_ShouldReturnTrue()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = CreateServiceWithCategory(category);
        await SeedDatabaseAsync(category, service);

        // Act
        var exists = await _repository.SlugExistsAsync(service.Slug);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task SlugExistsAsync_WithExistingSlugButExcludedId_ShouldReturnFalse()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = CreateServiceWithCategory(category);
        await SeedDatabaseAsync(category, service);

        // Act
        var exists = await _repository.SlugExistsAsync(service.Slug, service.Id);

        // Assert
        Assert.False(exists); // Should return false because service.Id is excluded
    }

    [Fact]
    public async Task SlugExistsAsync_WithNonExistentSlug_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentSlug = Slug.Create("non-existent");

        // Act
        var exists = await _repository.SlugExistsAsync(nonExistentSlug);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task AddAsync_WithValidService_ShouldAddToContext()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = CreateServiceWithCategory(category);

        // Seed category first
        await SeedDatabaseAsync(category);

        // Act
        await _repository.AddAsync(service);

        // Assert
        var addedEntity = _context.Entry(service);
        Assert.Equal(EntityState.Added, addedEntity.State);
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Added service {service.Id}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingService_ShouldMarkAsModified()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = CreateServiceWithCategory(category);
        await SeedDatabaseAsync(category, service);

        // Detach to simulate fresh load
        _context.Entry(service).State = EntityState.Detached;

        // Modify the service
        service.UpdateDescription("Updated Description", "Updated detailed description");

        // Act
        await _repository.UpdateAsync(service);

        // Assert
        var updatedEntity = _context.Entry(service);
        Assert.Equal(EntityState.Modified, updatedEntity.State);
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Updated service {service.Id}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingService_ShouldMarkAsDeleted()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = CreateServiceWithCategory(category);
        await SeedDatabaseAsync(category, service);

        // Act
        await _repository.DeleteAsync(service);

        // Assert
        var deletedEntity = _context.Entry(service);
        Assert.Equal(EntityState.Deleted, deletedEntity.State);
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Removed service {service.Id}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_WithPendingChanges_ShouldPersistAndLogChangeCount()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = CreateServiceWithCategory(category);
        
        await SeedDatabaseAsync(category);
        await _repository.AddAsync(service);

        // Act
        await _repository.SaveChangesAsync();

        // Assert
        var savedService = await _context.Services.FindAsync(service.Id);
        Assert.NotNull(savedService);
        Assert.Equal(service.Title, savedService.Title);
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Saved") && v.ToString()!.Contains("changes")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplySpecification_WithIncludeExpressions_ShouldEagerLoadRelatedEntities()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = CreateServiceWithCategory(category);
        await SeedDatabaseAsync(category, service);

        var specification = new ServiceWithCategorySpecification(service.Id);

        // Act
        var result = await _repository.GetBySpecificationAsync(specification);

        // Assert
        Assert.Single(result);
        Assert.NotNull(result[0].Category);
        Assert.Equal(category.Name, result[0].Category.Name);
    }

    [Fact]
    public async Task ApplySpecification_WithOrderBy_ShouldReturnOrderedResults()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service1 = CreateServiceWithCategory(category, "zebra", "Zebra Service");
        var service2 = CreateServiceWithCategory(category, "alpha", "Alpha Service");
        
        await SeedDatabaseAsync(category, service1, service2);

        var specification = new ServicesByTitleSpecification();

        // Act
        var result = await _repository.GetBySpecificationAsync(specification);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha Service", result[0].Title);
        Assert.Equal("Zebra Service", result[1].Title);
    }

    [Fact] 
    public async Task ApplySpecification_WithComplexSpecification_ShouldApplyAllFilters()
    {
        // Arrange
        var category1 = ServiceTestDataGenerator.GenerateCategoryWith("Tech", "tech");
        var category2 = ServiceTestDataGenerator.GenerateCategoryWith("Design", "design");
        
        var techService = CreateServiceWithCategory(category1, "tech-service", "Tech Service");
        techService.SetFeatured(true);
        techService.SetAvailability(true);
        
        var designService = CreateServiceWithCategory(category2, "design-service", "Design Service");
        designService.SetFeatured(false);
        designService.SetAvailability(true);

        await SeedDatabaseAsync(category1, category2, techService, designService);

        var specification = new ServicesSearchSpecification(
            searchTerm: string.Empty, 
            categoryId: category1.Id, 
            featured: true, 
            sortBy: "title");

        // Act
        var result = await _repository.GetBySpecificationAsync(specification);

        // Assert
        Assert.Single(result);
        Assert.Equal(techService.Id, result[0].Id);
        Assert.True(result[0].Featured);
        Assert.Equal(category1.Id, result[0].CategoryId);
    }

    [Fact]
    public async Task Repository_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _repository.GetAllAsync(cts.Token));
    }

    // Helper method to create service with category relationship
    private static Service CreateServiceWithCategory(ServiceCategory category, string? slug = null, string? title = null)
    {
        var service = ServiceTestDataGenerator.GenerateService();
        
        if (title != null)
            service.UpdateTitle(title);
        
        // Set category relationship
        service.SetCategory(category.Id);
        
        // Make sure service is published and available for specifications to find it
        service.Publish();
        service.SetAvailability(true);
        
        return service;
    }

    private async Task SeedDatabaseAsync(params object[] entities)
    {
        foreach (var entity in entities)
        {
            _context.Add(entity);
        }
        await _context.SaveChangesAsync();
        
        // Clear change tracker to ensure fresh queries
        _context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _context?.Dispose();
            _disposed = true;
        }
    }
}

// Helper specifications for testing
public class FeaturedServicesSpecification : BaseSpecification<Service>
{
    public FeaturedServicesSpecification() : base(s => s.Featured == true)
    {
        AddInclude(s => s.Category!);
    }
}

public class AvailableServicesSpecification : BaseSpecification<Service>
{
    public AvailableServicesSpecification() : base(s => s.Available == true)
    {
    }
}

public class ServiceWithCategorySpecification : BaseSpecification<Service>
{
    public ServiceWithCategorySpecification(ServiceId serviceId) : base(s => s.Id == serviceId)
    {
        AddInclude(s => s.Category!);
    }
}

public class ServicesByTitleSpecification : BaseSpecification<Service>
{
    public ServicesByTitleSpecification()
    {
        ApplyOrderBy(s => s.Title);
    }
}
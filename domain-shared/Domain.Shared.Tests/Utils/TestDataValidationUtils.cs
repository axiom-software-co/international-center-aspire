using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Tests.Shared.TestData;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace InternationalCenter.Tests.Shared.Utils;

/// <summary>
/// Test data validation utilities for consistent test data patterns
/// Extracts common test data setup and validation logic from duplicated contract tests
/// Provides standardized test data validation across Services APIs and integration tests
/// Medical-grade test data validation ensuring consistent test data quality
/// </summary>
public static class TestDataValidationUtils
{
    #region Entity Validation Contracts
    
    /// <summary>
    /// Validates Service entity integrity for contract testing
    /// Common pattern across all Service-related contract tests
    /// </summary>
    public static void ValidateServiceIntegrity(
        Service service,
        ITestOutputHelper? output = null)
    {
        Assert.NotNull(service);
        Assert.NotNull(service.Id);
        Assert.NotEqual(ServiceId.Empty, service.Id);
        
        // Validate required properties
        Assert.NotNull(service.Title);
        Assert.False(string.IsNullOrWhiteSpace(service.Title.Value), "Service title cannot be empty");
        Assert.NotNull(service.Slug);
        Assert.False(string.IsNullOrWhiteSpace(service.Slug.Value), "Service slug cannot be empty");
        
        // Validate domain invariants
        Assert.True(service.Title.Value.Length >= 3, "Service title must be at least 3 characters");
        Assert.True(service.Title.Value.Length <= 200, "Service title must not exceed 200 characters");
        
        // Validate slug format
        var slugValue = service.Slug.Value;
        Assert.True(slugValue.Length >= 3, "Service slug must be at least 3 characters");
        Assert.True(slugValue.Length <= 100, "Service slug must not exceed 100 characters");
        Assert.Matches(@"^[a-z0-9-]+$", slugValue); // Only lowercase, numbers, and hyphens
        
        // Validate timestamps
        Assert.True(service.CreatedAt <= DateTime.UtcNow, "CreatedAt cannot be in the future");
        Assert.True(service.UpdatedAt <= DateTime.UtcNow, "UpdatedAt cannot be in the future");
        Assert.True(service.UpdatedAt >= service.CreatedAt, "UpdatedAt cannot be before CreatedAt");
        
        output?.WriteLine($"✅ SERVICE INTEGRITY: Service {service.Id} passed all integrity checks");
    }
    
    /// <summary>
    /// Validates ServiceCategory entity integrity for contract testing
    /// </summary>
    public static void ValidateServiceCategoryIntegrity(
        ServiceCategory category,
        ITestOutputHelper? output = null)
    {
        Assert.NotNull(category);
        Assert.NotNull(category.Id);
        Assert.NotEqual(ServiceCategoryId.Empty, category.Id);
        
        // Validate required properties
        Assert.NotNull(category.Name);
        Assert.False(string.IsNullOrWhiteSpace(category.Name.Value), "Category name cannot be empty");
        Assert.NotNull(category.Slug);
        Assert.False(string.IsNullOrWhiteSpace(category.Slug.Value), "Category slug cannot be empty");
        
        // Validate domain invariants
        Assert.True(category.Name.Value.Length >= 2, "Category name must be at least 2 characters");
        Assert.True(category.Name.Value.Length <= 100, "Category name must not exceed 100 characters");
        
        // Validate slug format
        var slugValue = category.Slug.Value;
        Assert.True(slugValue.Length >= 2, "Category slug must be at least 2 characters");
        Assert.True(slugValue.Length <= 50, "Category slug must not exceed 50 characters");
        Assert.Matches(@"^[a-z0-9-]+$", slugValue); // Only lowercase, numbers, and hyphens
        
        // Validate sort order
        Assert.True(category.SortOrder >= 0, "Sort order cannot be negative");
        Assert.True(category.SortOrder <= 1000, "Sort order should be reasonable (<=1000)");
        
        // Validate timestamps
        Assert.True(category.CreatedAt <= DateTime.UtcNow, "CreatedAt cannot be in the future");
        Assert.True(category.UpdatedAt <= DateTime.UtcNow, "UpdatedAt cannot be in the future");
        Assert.True(category.UpdatedAt >= category.CreatedAt, "UpdatedAt cannot be before CreatedAt");
        
        output?.WriteLine($"✅ CATEGORY INTEGRITY: Category {category.Id} passed all integrity checks");
    }
    
    #endregion
    
    #region Test Data Relationship Validation
    
    /// <summary>
    /// Validates Service-Category relationship integrity
    /// Common pattern in integration tests requiring related entities
    /// </summary>
    public static void ValidateServiceCategoryRelationship(
        Service service,
        ServiceCategory category,
        ITestOutputHelper? output = null)
    {
        ValidateServiceIntegrity(service, output);
        ValidateServiceCategoryIntegrity(category, output);
        
        // Validate relationship
        Assert.Equal(category.Id, service.CategoryId);
        
        // If service has loaded category navigation property, validate it
        if (service.Category != null)
        {
            Assert.Equal(category.Id, service.Category.Id);
            Assert.Equal(category.Name, service.Category.Name);
            Assert.Equal(category.Slug, service.Category.Slug);
        }
        
        output?.WriteLine($"✅ RELATIONSHIP INTEGRITY: Service {service.Id} properly related to Category {category.Id}");
    }
    
    /// <summary>
    /// Validates collection of services with consistent category relationships
    /// </summary>
    public static void ValidateServiceCollection(
        IEnumerable<Service> services,
        ServiceCategory expectedCategory,
        ITestOutputHelper? output = null)
    {
        var serviceList = services.ToList();
        Assert.NotEmpty(serviceList);
        
        foreach (var service in serviceList)
        {
            ValidateServiceCategoryRelationship(service, expectedCategory, output);
        }
        
        // Validate collection uniqueness
        var uniqueIds = serviceList.Select(s => s.Id).Distinct().Count();
        Assert.Equal(serviceList.Count, uniqueIds);
        
        var uniqueSlugs = serviceList.Select(s => s.Slug.Value).Distinct().Count();
        Assert.Equal(serviceList.Count, uniqueSlugs);
        
        output?.WriteLine($"✅ COLLECTION INTEGRITY: {serviceList.Count} services validated with unique IDs and slugs");
    }
    
    #endregion
    
    #region Test Data Generation Validation
    
    /// <summary>
    /// Validates that generated test data meets quality standards
    /// Ensures generated data is realistic and follows domain rules
    /// </summary>
    public static Service ValidateGeneratedService(
        Service? service = null,
        ITestOutputHelper? output = null)
    {
        service ??= ServiceTestDataGenerator.GenerateService();
        
        ValidateServiceIntegrity(service, output);
        
        // Additional validation for generated data quality
        Assert.False(service.Title.Value.Contains("Test"), 
            "Generated service title should not contain 'Test' - should be realistic");
        Assert.False(service.Description?.Contains("Lorem ipsum") ?? false,
            "Generated service description should not use Lorem ipsum - should be realistic");
        
        // Validate generated data uniqueness indicators
        Assert.NotEqual(Guid.Empty.ToString(), service.Id.Value);
        
        output?.WriteLine($"✅ GENERATED DATA: Service with realistic title '{service.Title.Value}' and slug '{service.Slug.Value}'");
        return service;
    }
    
    /// <summary>
    /// Validates that generated category meets quality standards
    /// </summary>
    public static ServiceCategory ValidateGeneratedCategory(
        ServiceCategory? category = null,
        ITestOutputHelper? output = null)
    {
        category ??= ServiceTestDataGenerator.GenerateCategory();
        
        ValidateServiceCategoryIntegrity(category, output);
        
        // Additional validation for generated data quality
        Assert.False(category.Name.Value.Contains("Test"),
            "Generated category name should not contain 'Test' - should be realistic");
        Assert.False(category.Description?.Contains("Lorem ipsum") ?? false,
            "Generated category description should not use Lorem ipsum - should be realistic");
        
        output?.WriteLine($"✅ GENERATED DATA: Category with realistic name '{category.Name.Value}' and slug '{category.Slug.Value}'");
        return category;
    }
    
    #endregion
    
    #region Test Data Seeding Validation
    
    /// <summary>
    /// Validates that seeded test data is properly persisted and retrievable
    /// Common pattern for validating test data setup in integration tests
    /// </summary>
    public static async Task ValidateDataSeeding<TRepository, TEntity, TId>(
        TRepository repository,
        TEntity seededEntity,
        TId entityId,
        Func<TRepository, TId, Task<TEntity?>> getByIdFunc,
        ITestOutputHelper? output = null)
        where TEntity : class
        where TId : notnull
    {
        output?.WriteLine($"🌱 SEEDING VALIDATION: Verifying {typeof(TEntity).Name} with ID {entityId}");
        
        // Attempt to retrieve the seeded entity
        var retrievedEntity = await getByIdFunc(repository, entityId);
        
        Assert.NotNull(retrievedEntity);
        Assert.Equal(seededEntity, retrievedEntity);
        
        output?.WriteLine($"✅ SEEDING VALIDATION: {typeof(TEntity).Name} successfully seeded and retrievable");
    }
    
    /// <summary>
    /// Validates that multiple entities are properly seeded as a collection
    /// </summary>
    public static async Task ValidateCollectionSeeding<TRepository, TEntity>(
        TRepository repository,
        IEnumerable<TEntity> seededEntities,
        Func<TRepository, Task<IReadOnlyList<TEntity>>> getAllFunc,
        ITestOutputHelper? output = null)
        where TEntity : class
    {
        var expectedEntities = seededEntities.ToList();
        output?.WriteLine($"🌱 COLLECTION SEEDING: Verifying {expectedEntities.Count} {typeof(TEntity).Name} entities");
        
        var retrievedEntities = await getAllFunc(repository);
        
        Assert.NotNull(retrievedEntities);
        Assert.True(retrievedEntities.Count >= expectedEntities.Count, 
            $"Expected at least {expectedEntities.Count} entities, found {retrievedEntities.Count}");
        
        // Verify all seeded entities are present (using object equality or specific comparison)
        foreach (var expectedEntity in expectedEntities)
        {
            Assert.Contains(expectedEntity, retrievedEntities);
        }
        
        output?.WriteLine($"✅ COLLECTION SEEDING: All {expectedEntities.Count} entities properly seeded and retrievable");
    }
    
    #endregion
    
    #region Test Data Cleanup Validation
    
    /// <summary>
    /// Validates that test data cleanup was successful
    /// Critical for test isolation in integration tests
    /// </summary>
    public static async Task ValidateDataCleanup<TRepository, TEntity>(
        TRepository repository,
        Func<TRepository, Task<IReadOnlyList<TEntity>>> getAllFunc,
        string entityTypeName,
        ITestOutputHelper? output = null)
        where TEntity : class
    {
        output?.WriteLine($"🧹 CLEANUP VALIDATION: Verifying {entityTypeName} cleanup");
        
        var remainingEntities = await getAllFunc(repository);
        
        if (remainingEntities.Any())
        {
            var message = $"Found {remainingEntities.Count} remaining {entityTypeName} entities after cleanup";
            output?.WriteLine($"❌ CLEANUP VALIDATION: {message}");
            throw new InvalidOperationException($"Data cleanup validation failed: {message}");
        }
        
        output?.WriteLine($"✅ CLEANUP VALIDATION: All {entityTypeName} entities successfully cleaned up");
    }
    
    /// <summary>
    /// Validates that specific entities were cleaned up
    /// </summary>
    public static async Task ValidateSpecificDataCleanup<TRepository, TEntity, TId>(
        TRepository repository,
        IEnumerable<TId> entityIds,
        Func<TRepository, TId, Task<TEntity?>> getByIdFunc,
        string entityTypeName,
        ITestOutputHelper? output = null)
        where TEntity : class
        where TId : notnull
    {
        var idsToCheck = entityIds.ToList();
        output?.WriteLine($"🧹 SPECIFIC CLEANUP: Verifying {idsToCheck.Count} {entityTypeName} entities are cleaned up");
        
        var stillExists = new List<TId>();
        
        foreach (var id in idsToCheck)
        {
            var entity = await getByIdFunc(repository, id);
            if (entity != null)
            {
                stillExists.Add(id);
            }
        }
        
        if (stillExists.Any())
        {
            var message = $"Entities still exist after cleanup: {string.Join(", ", stillExists)}";
            output?.WriteLine($"❌ SPECIFIC CLEANUP: {message}");
            throw new InvalidOperationException($"Specific data cleanup validation failed: {message}");
        }
        
        output?.WriteLine($"✅ SPECIFIC CLEANUP: All {idsToCheck.Count} {entityTypeName} entities successfully cleaned up");
    }
    
    #endregion
}
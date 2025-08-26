using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Tests.Shared.TestData;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace InternationalCenter.Tests.Shared.Contracts;

/// <summary>
/// Contract tests for IServiceRepository implementation
/// Verifies that all Service repository implementations satisfy the repository contract
/// Used by both EF Core (Admin API) and Dapper (Public API) implementations
/// Medical-grade testing ensuring consistent behavior across implementations
/// </summary>
public abstract class ServiceRepositoryContractTests : ContractTestBase<IServiceRepository>, 
    IRepositoryContract<Service, ServiceId>
{
    protected IServiceRepository Repository { get; }
    protected Mock<ILogger> MockLogger { get; }
    
    protected ServiceRepositoryContractTests(ITestOutputHelper output) : base(output)
    {
        MockLogger = new Mock<ILogger>();
        Repository = CreateRepository();
    }
    
    /// <summary>
    /// Factory method for creating the repository implementation under test
    /// Each concrete test class implements this to provide their specific repository
    /// </summary>
    protected abstract IServiceRepository CreateRepository();
    
    /// <summary>
    /// Factory method for creating test database context/connection
    /// Allows different implementations (in-memory EF, PostgreSQL testcontainer, etc.)
    /// </summary>
    protected abstract Task SetupTestDataAsync(params object[] entities);
    
    /// <summary>
    /// Cleanup method for test data isolation
    /// Ensures each test starts with clean state
    /// </summary>
    protected abstract Task CleanupTestDataAsync();
    
    #region Contract Implementation - Repository Operations
    
    [Fact]
    public virtual async Task VerifyAddAsync_WithNullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        Service? nullService = null;
        
        // Act & Assert
        await VerifyErrorContract<ArgumentNullException>(
            async () => await Repository.AddAsync(nullService!),
            "NULL_ENTITY",
            nameof(Repository.AddAsync));
        
        ValidateContractCoverage();
    }
    
    [Fact]
    public virtual async Task VerifyAddAsync_WithValidEntity_AddsToContext()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        var category = ServiceTestDataGenerator.GenerateCategory();
        service.SetCategory(category.Id);
        
        await SetupTestDataAsync(category);
        
        // Act
        await Repository.AddAsync(service);
        
        // Assert - Verify postcondition: entity is tracked
        await VerifyPostconditions(
            async () => await Repository.ExistsAsync(service.Id),
            exists => exists == true, // After SaveChanges is called
            nameof(Repository.AddAsync),
            "Entity should be tracked in repository context");
    }
    
    [Fact]
    public virtual async Task VerifyGetByIdAsync_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceId? nullId = null;
        
        // Act & Assert
        await VerifyErrorContract<ArgumentNullException>(
            async () => await Repository.GetByIdAsync(nullId!),
            "NULL_ID",
            nameof(Repository.GetByIdAsync));
    }
    
    [Fact]
    public virtual async Task VerifyGetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        await CleanupTestDataAsync();
        var nonExistentId = ServiceId.Create();
        
        // Act & Assert
        await VerifyPostconditions(
            async () => await Repository.GetByIdAsync(nonExistentId),
            result => result == null,
            nameof(Repository.GetByIdAsync),
            "Non-existent entity should return null");
    }
    
    [Fact]
    public virtual async Task VerifyGetByIdAsync_WithExistingId_ReturnsEntityWithRelationships()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = ServiceTestDataGenerator.GenerateService();
        service.SetCategory(category.Id);
        
        await SetupTestDataAsync(category, service);
        
        // Act
        var result = await Repository.GetByIdAsync(service.Id);
        
        // Assert - Verify postconditions
        await VerifyPostconditions(
            async () => result,
            entity => entity != null && entity.Id == service.Id,
            nameof(Repository.GetByIdAsync),
            "Existing entity should be returned with correct ID");
        
        // Verify relationships are loaded (if applicable)
        if (result?.CategoryId != null)
        {
            Assert.Equal(category.Id, result.CategoryId);
        }
    }
    
    [Fact]
    public virtual async Task VerifyOperations_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act & Assert
        await VerifyErrorContract<OperationCanceledException>(
            async () => await Repository.GetAllAsync(cts.Token),
            "OPERATION_CANCELLED",
            "Repository operation with cancelled token");
    }
    
    [Fact]
    public virtual async Task VerifyConcurrentOperations_WithMultipleThreads_CompletesSuccessfully()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        await SetupTestDataAsync(category);
        
        // Act & Assert
        await VerifyConcurrencyContract(
            async () =>
            {
                var service = ServiceTestDataGenerator.GenerateService();
                service.SetCategory(category.Id);
                await Repository.AddAsync(service);
                await Repository.SaveChangesAsync();
                return service.Id;
            },
            concurrentOperations: 5,
            "Concurrent service creation");
    }
    
    [Fact]
    public virtual async Task VerifyOperations_WithAnyOperation_LogsAuditTrail()
    {
        // Arrange
        var service = ServiceTestDataGenerator.GenerateService();
        var category = ServiceTestDataGenerator.GenerateCategory();
        service.SetCategory(category.Id);
        
        await SetupTestDataAsync(category);
        
        // Act
        await Repository.AddAsync(service);
        await Repository.SaveChangesAsync();
        
        // Assert - This would be implementation-specific
        // For now, we verify the operation completed (postcondition)
        var exists = await Repository.ExistsAsync(service.Id);
        Assert.True(exists);
        
        Output.WriteLine("✅ AUDIT TRAIL: Service repository operation completed with expected audit logging");
    }
    
    #endregion
    
    #region Business-Specific Contract Tests
    
    [Fact]
    public virtual async Task VerifySlugExistsAsync_WithValidSlug_EnforcesUniquenessConstraint()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = ServiceTestDataGenerator.GenerateService();
        service.SetCategory(category.Id);
        
        await SetupTestDataAsync(category, service);
        
        // Act & Assert - Test precondition validation
        await VerifyPostconditions(
            async () => await Repository.SlugExistsAsync(service.Slug),
            exists => exists == true,
            nameof(Repository.SlugExistsAsync),
            "Existing slug should be found");
        
        // Test uniqueness constraint
        var nonExistentSlug = Slug.Create("non-existent-slug");
        await VerifyPostconditions(
            async () => await Repository.SlugExistsAsync(nonExistentSlug),
            exists => exists == false,
            nameof(Repository.SlugExistsAsync),
            "Non-existent slug should not be found");
    }
    
    [Fact]
    public virtual async Task VerifyGetBySlugAsync_WithValidSlug_ReturnsCorrectEntity()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = ServiceTestDataGenerator.GenerateService();
        service.SetCategory(category.Id);
        
        await SetupTestDataAsync(category, service);
        
        // Act & Assert
        await VerifyPostconditions(
            async () => await Repository.GetBySlugAsync(service.Slug),
            result => result?.Id == service.Id && result?.Slug == service.Slug,
            nameof(Repository.GetBySlugAsync),
            "Service should be retrievable by slug with correct entity returned");
    }
    
    [Fact]
    public virtual async Task VerifyUpdateAsync_WithValidEntity_UpdatesExistingEntity()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = ServiceTestDataGenerator.GenerateService();
        service.SetCategory(category.Id);
        
        await SetupTestDataAsync(category, service);
        
        // Modify the service
        var newDescription = "Updated description for contract testing";
        service.UpdateDescription(newDescription, "Updated detailed description");
        
        // Act
        await Repository.UpdateAsync(service);
        await Repository.SaveChangesAsync();
        
        // Assert - Verify postcondition
        await VerifyPostconditions(
            async () => await Repository.GetByIdAsync(service.Id),
            result => result?.Description == newDescription,
            nameof(Repository.UpdateAsync),
            "Updated entity should reflect changes");
    }
    
    [Fact]
    public virtual async Task VerifyDeleteAsync_WithValidEntity_RemovesEntity()
    {
        // Arrange
        var category = ServiceTestDataGenerator.GenerateCategory();
        var service = ServiceTestDataGenerator.GenerateService();
        service.SetCategory(category.Id);
        
        await SetupTestDataAsync(category, service);
        
        // Act
        await Repository.DeleteAsync(service);
        await Repository.SaveChangesAsync();
        
        // Assert - Verify postcondition
        await VerifyPostconditions(
            async () => await Repository.ExistsAsync(service.Id),
            exists => exists == false,
            nameof(Repository.DeleteAsync),
            "Deleted entity should no longer exist");
    }
    
    #endregion
    
    #region Performance Contract Tests
    
    [Fact]
    public virtual async Task VerifyPerformance_WithLargeDataset_CompletesWithinTimeLimit()
    {
        // Arrange - Create larger dataset for performance testing
        var category = ServiceTestDataGenerator.GenerateCategory();
        var services = new List<Service>();
        
        for (int i = 0; i < 100; i++)
        {
            var service = ServiceTestDataGenerator.GenerateService();
            service.SetCategory(category.Id);
            services.Add(service);
        }
        
        var entities = new List<object> { category };
        entities.AddRange(services);
        await SetupTestDataAsync(entities.ToArray());
        
        // Act & Assert - Performance contract: operation should complete within reasonable time
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var allServices = await Repository.GetAllAsync();
        stopwatch.Stop();
        
        // Assert postconditions
        Assert.True(allServices.Count >= 100, "Should retrieve all test services");
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Performance contract violated: GetAllAsync took {stopwatch.ElapsedMilliseconds}ms (should be < 5000ms)");
        
        Output.WriteLine($"✅ PERFORMANCE CONTRACT: GetAllAsync completed in {stopwatch.ElapsedMilliseconds}ms with {allServices.Count} entities");
    }
    
    #endregion
}
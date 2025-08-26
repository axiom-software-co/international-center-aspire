using InternationalCenter.Services.Public.Api.Application.UseCases;
using InternationalCenter.Services.Domain.Models;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Public.Api.Infrastructure.Data;
using InternationalCenter.Services.Public.Api.Infrastructure.Repositories;
using InternationalCenter.Services.Public.Api.Tests.Integration.Infrastructure;
using InternationalCenter.Tests.Shared.Fixtures;
using InternationalCenter.Tests.Shared.TestData;
using InternationalCenter.Shared.Proto.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using Xunit;

namespace InternationalCenter.Services.Public.Api.Tests.Integration.UseCases;

/// <summary>
/// TDD RED tests for simplified Use Case architecture
/// Defines contracts for consolidated, medical-grade Use Cases without decorator complexity
/// </summary>
public class ServiceUseCaseIntegrationTests : IClassFixture<DatabaseFixture>, IClassFixture<CacheFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture;
    private readonly CacheFixture _cacheFixture;
    private ServicesDbContext _dbContext = null!;
    private IConnectionMultiplexer _redis = null!;
    private IDistributedCache _cache = null!;

    public ServiceUseCaseIntegrationTests(DatabaseFixture databaseFixture, CacheFixture cacheFixture)
    {
        _databaseFixture = databaseFixture;
        _cacheFixture = cacheFixture;
    }

    public async Task InitializeAsync()
    {
        // Setup database context
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ServicesDbContext>()
            .UseNpgsql(_databaseFixture.ConnectionString)
            .Options;
        _dbContext = new ServicesDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        // Setup Redis cache
        _redis = await ConnectionMultiplexer.ConnectAsync(_cacheFixture.ConnectionString);
        _cache = new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache(
            new Microsoft.Extensions.Options.OptionsWrapper<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions>(
                new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions
                {
                    ConnectionMultiplexerFactory = () => Task.FromResult(_redis)
                }));
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _redis.DisposeAsync();
    }

    private async Task CleanupTestDataAsync()
    {
        try
        {
            // Clear database data using EF Core (more reliable than raw SQL)
            var services = await _dbContext.Services.ToListAsync();
            if (services.Any())
            {
                _dbContext.Services.RemoveRange(services);
            }
            
            var categories = await _dbContext.ServiceCategories.ToListAsync();
            if (categories.Any())
            {
                _dbContext.ServiceCategories.RemoveRange(categories);
            }
            
            // Save all changes in one transaction
            if (_dbContext.ChangeTracker.HasChanges())
            {
                await _dbContext.SaveChangesAsync();
            }
            
            // Clear EF Core change tracker to ensure clean state
            _dbContext.ChangeTracker.Clear();
            
            // Clear Redis cache by removing specific keys (FLUSHDB requires admin mode)
            try 
            {
                var database = _redis.GetDatabase();
                
                // Clear common cache key patterns used by tests
                var keyPatterns = new[]
                {
                    "services:*",
                    "cache:*",
                    "*services*"
                };
                
                foreach (var pattern in keyPatterns)
                {
                    var server = _redis.GetServer(_redis.GetEndPoints().First());
                    await foreach (var key in server.KeysAsync(pattern: pattern))
                    {
                        await database.KeyDeleteAsync(key);
                    }
                }
            }
            catch (Exception cacheEx)
            {
                // Cache cleanup failure shouldn't fail tests - log and continue
                System.Diagnostics.Debug.WriteLine($"Cache cleanup failed: {cacheEx.Message}");
            }
            
            // Verify cleanup completed
            var remainingServices = await _dbContext.Services.CountAsync();
            var remainingCategories = await _dbContext.ServiceCategories.CountAsync();
            
            if (remainingServices > 0 || remainingCategories > 0)
            {
                throw new InvalidOperationException($"Cleanup incomplete: {remainingServices} services, {remainingCategories} categories remain");
            }
        }
        catch (Exception ex)
        {
            // Log cleanup failures but don't fail tests
            System.Diagnostics.Debug.WriteLine($"Cleanup failed: {ex.Message}");
            throw; // Re-throw to make test isolation failures visible
        }
    }

    /// <summary>
    /// TDD RED: Test consolidated service query use case with direct caching
    /// Replaces multiple specialized Use Cases with single, flexible implementation
    /// </summary>
    [Fact]
    public async Task ServiceQueryUseCase_WithDirectCaching_ShouldReturnCachedResults()
    {
        // Ensure clean test isolation
        await CleanupTestDataAsync();
        
        // Arrange
        var services = ServiceTestDataGenerator.GenerateServices(10).ToList();
        foreach (var service in services.Take(5))
        {
            service.Publish();
            service.SetFeatured(true);
        }
        
        await SeedServicesAsync(services);

        var useCase = CreateServiceQueryUseCase();
        var queryParams = new ServicesQueryRequest
        {
            Page = 1,
            PageSize = 20,
            Featured = true,
            UserContext = "test-user-123",
            RequestId = "req-456"
        };

        // Act & Assert - This should fail initially (TDD RED)
        var result = await useCase.ExecuteAsync(queryParams);

        // Expected behavior for simplified architecture:
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Services.Count);
        
        // Second call should use cache
        var cachedResult = await useCase.ExecuteAsync(queryParams);
        Assert.True(cachedResult.IsSuccess);
        Assert.True(cachedResult.Value.FromCache);
        
        // Should have audit log entries
        Assert.True(result.Value.AuditTrail.Any());
        Assert.Contains("ServiceQuery", result.Value.AuditTrail.First().Operation);
    }

    /// <summary>
    /// TDD RED: Test medical-grade audit logging for business operations
    /// </summary>
    [Fact]
    public async Task ServiceQueryUseCase_WithMedicalGradeAudit_ShouldLogBusinessOperations()
    {
        // Ensure clean test isolation
        await CleanupTestDataAsync();
        
        // Arrange
        var services = ServiceTestDataGenerator.GenerateServices(3).ToList();
        await SeedServicesAsync(services);

        var useCase = CreateServiceQueryUseCase();
        var queryParams = new ServicesQueryRequest
        {
            Page = 1,
            PageSize = 10,
            UserContext = "medical-staff-789",
            RequestId = "audit-req-123",
            ClientIpAddress = "192.168.1.100",
            UserAgent = "TestAgent/1.0"
        };

        // Act - This should fail initially (TDD RED)
        var result = await useCase.ExecuteAsync(queryParams);

        // Expected medical-grade audit behavior:
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.AuditTrail);
        
        var auditEntry = result.Value.AuditTrail.First();
        Assert.Equal("ServiceQuery", auditEntry.Operation);
        Assert.Equal("medical-staff-789", auditEntry.UserId);
        Assert.Equal("192.168.1.100", auditEntry.IpAddress);
        Assert.Equal("TestAgent/1.0", auditEntry.UserAgent);
        Assert.True(auditEntry.Timestamp > DateTime.UtcNow.AddSeconds(-5));
        Assert.True(auditEntry.Duration > TimeSpan.Zero);
    }

    /// <summary>
    /// TDD RED: Test performance monitoring and metrics collection
    /// </summary>
    [Fact]
    public async Task ServiceQueryUseCase_WithPerformanceMonitoring_ShouldCollectMetrics()
    {
        // Cleanup any existing test data to ensure isolation
        await CleanupTestDataAsync();
        
        // Arrange
        var services = ServiceTestDataGenerator.GenerateServices(50).ToList();
        foreach (var service in services)
            service.Publish();
            
        await SeedServicesAsync(services);

        var useCase = CreateServiceQueryUseCase();
        var queryParams = new ServicesQueryRequest
        {
            Page = 1,
            PageSize = 25,
            SortBy = "title",
            UserContext = "performance-test-user"
        };

        // Act - This should fail initially (TDD RED)
        var result = await useCase.ExecuteAsync(queryParams);

        // Expected performance monitoring behavior:
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.PerformanceMetrics);
        
        var metrics = result.Value.PerformanceMetrics;
        Assert.True(metrics.QueryDuration > TimeSpan.Zero);
        Assert.True(metrics.CacheCheckDuration >= TimeSpan.Zero);
        Assert.Equal(50, metrics.TotalRecordsScanned);
        Assert.Equal(25, metrics.RecordsReturned);
        Assert.False(metrics.CacheHit); // First call should be cache miss
    }

    /// <summary>
    /// TDD RED: Test input validation and security boundaries
    /// </summary>
    [Fact]
    public async Task ServiceQueryUseCase_WithInvalidInput_ShouldEnforceSecurityValidation()
    {
        // Ensure clean test isolation
        await CleanupTestDataAsync();
        
        // Arrange
        var useCase = CreateServiceQueryUseCase();

        // Test various invalid inputs - These should fail initially (TDD RED)
        
        // Invalid page size
        var invalidPageSizeParams = new ServicesQueryRequest
        {
            Page = 1,
            PageSize = 500, // Too large
            UserContext = "test-user"
        };

        var result1 = await useCase.ExecuteAsync(invalidPageSizeParams);
        Assert.False(result1.IsSuccess);
        Assert.Contains("Page size", result1.Error.Message);

        // Missing user context (security requirement)
        var noUserContextParams = new ServicesQueryRequest
        {
            Page = 1,
            PageSize = 10
            // UserContext missing
        };

        var result2 = await useCase.ExecuteAsync(noUserContextParams);
        Assert.False(result2.IsSuccess);
        Assert.Contains("User context", result2.Error.Message);

        // SQL injection attempt
        var maliciousParams = new ServicesQueryRequest
        {
            Page = 1,
            PageSize = 10,
            SearchTerm = "'; DROP TABLE Services; --",
            UserContext = "test-user"
        };

        var result3 = await useCase.ExecuteAsync(maliciousParams);
        Assert.False(result3.IsSuccess);
        Assert.Contains("Invalid characters", result3.Error.Message);
    }

    /// <summary>
    /// TDD RED: Test cache invalidation and consistency
    /// </summary>
    [Fact]
    public async Task ServiceQueryUseCase_WithCacheInvalidation_ShouldMaintainConsistency()
    {
        // Cleanup any existing test data to ensure isolation
        await CleanupTestDataAsync();
        
        // Arrange
        var services = ServiceTestDataGenerator.GenerateServices(5).ToList();
        foreach (var service in services)
            service.Publish();
            
        await SeedServicesAsync(services);

        var useCase = CreateServiceQueryUseCase();
        var queryParams = new ServicesQueryRequest
        {
            Page = 1,
            PageSize = 10,
            UserContext = "cache-test-user"
        };

        // Act - This should fail initially (TDD RED)
        
        // First call - should cache result
        var result1 = await useCase.ExecuteAsync(queryParams);
        Assert.True(result1.IsSuccess);
        Assert.Equal(5, result1.Value.Services.Count);
        Assert.False(result1.Value.FromCache);

        // Second call - should use cache
        var result2 = await useCase.ExecuteAsync(queryParams);
        Assert.True(result2.IsSuccess);
        Assert.True(result2.Value.FromCache);

        // Update data and invalidate cache
        await useCase.InvalidateCacheAsync("services", CancellationToken.None);

        // Third call - should refresh from database
        var result3 = await useCase.ExecuteAsync(queryParams);
        Assert.True(result3.IsSuccess);
        Assert.False(result3.Value.FromCache);
    }

    // Helper methods for creating test objects
    private InternationalCenter.Services.Public.Api.Application.UseCases.IServiceQueryUseCase CreateServiceQueryUseCase()
    {
        var serviceRepository = CreateServiceRepository();
        return new ServiceQueryUseCase(serviceRepository, _cache, NullLogger<ServiceQueryUseCase>.Instance);
    }

    private IServiceRepository CreateServiceRepository()
    {
        return new ServiceRepository(_dbContext, NullLogger<ServiceRepository>.Instance);
    }

    private async Task SeedServicesAsync(IEnumerable<Domain.Entities.Service> services)
    {
        // Ensure completely clean state before seeding
        await CleanupTestDataAsync();
        
        _dbContext.Services.AddRange(services);
        await _dbContext.SaveChangesAsync();
        
        // Detach to avoid tracking conflicts
        foreach (var service in services)
        {
            _dbContext.Entry(service).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }
        
        // Verify the services were actually saved
        var savedCount = await _dbContext.Services.CountAsync();
        if (savedCount == 0)
        {
            throw new InvalidOperationException("Failed to seed services - database appears to be in inconsistent state");
        }
    }
}

// TDD GREEN: Using types from actual implementation


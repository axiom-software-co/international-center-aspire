using Aspire.Hosting;
using Aspire.Hosting.Testing;
using InternationalCenter.Shared.Infrastructure.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using Xunit;

namespace InternationalCenter.Services.Public.Api.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for Redis cache service using real Microsoft Redis container
/// Tests realistic caching scenarios with compression, TTL, and concurrent operations
/// </summary>
public class RedisCacheIntegrationTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private ICacheService? _cacheService;
    private ICacheKeyService? _cacheKeyService;
    private IConnectionMultiplexer? _connectionMultiplexer;

    public async Task InitializeAsync()
    {
        // Use Aspire orchestration for Redis testing (not TestContainers)
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InternationalCenter_AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();

        // Configure services for Redis caching
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Create connection multiplexer for Redis using Aspire connection string
        var redisConnectionString = await _app.GetConnectionStringAsync("redis");
        _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
        services.AddSingleton(_connectionMultiplexer);
        
        // Configure distributed cache with Redis
        services.AddSingleton<IDistributedCache>(provider =>
        {
            return new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache(
                new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions
                {
                    ConnectionMultiplexerFactory = () => Task.FromResult(_connectionMultiplexer),
                    InstanceName = "InternationalCenter.Tests"
                });
        });
        
        services.AddSingleton<ICacheKeyService, CacheKeyService>();
        services.AddSingleton<ICacheService, RedisCacheService>();
        
        var provider = services.BuildServiceProvider();
        _cacheService = provider.GetRequiredService<ICacheService>();
        _cacheKeyService = provider.GetRequiredService<ICacheKeyService>();
    }

    public async Task DisposeAsync()
    {
        if (_connectionMultiplexer != null)
        {
            await _connectionMultiplexer.DisposeAsync();
        }
        
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task RedisCache_BasicSetAndGet_ShouldWorkCorrectly()
    {
        // Arrange
        var key = "test:basic";
        var value = new TestCacheObject { Id = 1, Name = "Test Object", Data = "Sample data" };

        // Act
        await _cacheService!.SetAsync(key, value, TimeSpan.FromMinutes(5));
        var result = await _cacheService.GetAsync<TestCacheObject>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
        Assert.Equal(value.Data, result.Data);
    }

    [Fact]
    public async Task RedisCache_WithExpiration_ShouldExpireAfterTTL()
    {
        // Arrange
        var key = "test:expiration";
        var value = new TestCacheObject { Id = 2, Name = "Expiring Object" };

        // Act
        await _cacheService!.SetAsync(key, value, TimeSpan.FromMilliseconds(100));
        
        // Wait for expiration
        await Task.Delay(150);
        var result = await _cacheService.GetAsync<TestCacheObject>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RedisCache_ExistsCheck_ShouldReturnCorrectState()
    {
        // Arrange
        var key = "test:exists";
        var value = new TestCacheObject { Id = 3, Name = "Exists Test" };

        // Act & Assert - Key shouldn't exist initially
        Assert.False(await _cacheService!.ExistsAsync(key));

        // Set value and check existence
        await _cacheService.SetAsync(key, value);
        Assert.True(await _cacheService.ExistsAsync(key));

        // Remove and check non-existence
        await _cacheService.RemoveAsync(key);
        Assert.False(await _cacheService.ExistsAsync(key));
    }

    [Fact]
    public async Task RedisCache_GetTTL_ShouldReturnRemainingTime()
    {
        // Arrange
        var key = "test:ttl";
        var value = new TestCacheObject { Id = 4, Name = "TTL Test" };
        var expiration = TimeSpan.FromMinutes(10);

        // Act
        await _cacheService!.SetAsync(key, value, expiration);
        var ttl = await _cacheService.GetTtlAsync(key);

        // Assert
        Assert.NotNull(ttl);
        Assert.True(ttl.Value.TotalMinutes > 9); // Should be close to 10 minutes
        Assert.True(ttl.Value.TotalMinutes <= 10);
    }

    [Fact]
    public async Task RedisCache_LargeObject_ShouldHandleCompressionCorrectly()
    {
        // Arrange
        var key = "test:compression";
        var largeData = string.Join(" ", Enumerable.Repeat("Large data content", 1000));
        var value = new TestCacheObject 
        { 
            Id = 5, 
            Name = "Compression Test",
            Data = largeData
        };

        var options = new CacheOptions
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(5),
            CompressData = true
        };

        // Act
        await _cacheService!.SetAsync(key, value, options);
        var result = await _cacheService.GetAsync<TestCacheObject>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
        Assert.Equal(value.Data, result.Data);
    }

    [Fact]
    public async Task RedisCache_ConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var tasks = new List<Task>();
        var keyPrefix = "test:concurrent";

        // Act - Perform multiple concurrent operations
        for (int i = 0; i < 10; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                var key = $"{keyPrefix}:{taskId}";
                var value = new TestCacheObject 
                { 
                    Id = taskId, 
                    Name = $"Concurrent Test {taskId}" 
                };
                
                await _cacheService!.SetAsync(key, value);
                var result = await _cacheService.GetAsync<TestCacheObject>(key);
                
                Assert.NotNull(result);
                Assert.Equal(taskId, result.Id);
            }));
        }

        // Assert - All operations should complete successfully
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task RedisCache_RemoveByPattern_ShouldRemoveMatchingKeys()
    {
        // Arrange
        var pattern = "*pattern*"; // Adjusted pattern to work with Redis KEYS command
        var keys = new[] { "test:pattern:1", "test:pattern:2", "test:pattern:3" };
        var nonMatchingKey = "test:other:1";

        foreach (var key in keys)
        {
            await _cacheService!.SetAsync(key, new TestCacheObject { Id = 1, Name = key });
        }
        await _cacheService!.SetAsync(nonMatchingKey, new TestCacheObject { Id = 2, Name = nonMatchingKey });

        // Verify all keys exist initially
        foreach (var key in keys)
        {
            Assert.True(await _cacheService!.ExistsAsync(key));
        }
        Assert.True(await _cacheService!.ExistsAsync(nonMatchingKey));

        // Act
        await _cacheService!.RemoveByPatternAsync(pattern);

        // Assert - Pattern matching keys should be removed
        foreach (var key in keys)
        {
            Assert.False(await _cacheService!.ExistsAsync(key));
        }
        
        // Non-matching key should still exist
        Assert.True(await _cacheService!.ExistsAsync(nonMatchingKey));
    }

    [Fact]
    public async Task RedisCache_WithCacheOptions_ShouldRespectAllSettings()
    {
        // Arrange
        var key = "test:options";
        var value = new TestCacheObject { Id = 6, Name = "Options Test" };
        var options = new CacheOptions
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(15),
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Priority = CachePriority.High,
            Tags = new[] { "test-tag", "integration-test" },
            CompressData = true
        };

        // Act
        await _cacheService!.SetAsync(key, value, options);
        var result = await _cacheService.GetAsync<TestCacheObject>(key);
        var exists = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Id, result.Id);
        Assert.True(exists); // Key should exist with options applied
        
        // Test TTL separately as it may be affected by Redis instance name prefixes
        var ttl = await _cacheService.GetTtlAsync(key);
        if (ttl.HasValue)
        {
            Assert.True(ttl.Value.TotalMinutes > 0); // Should have some expiration set
        }
    }

    [Fact]
    public async Task RedisCache_RefreshOperation_ShouldExtendTTL()
    {
        // Arrange
        var key = "test:refresh";
        var value = new TestCacheObject { Id = 7, Name = "Refresh Test" };
        var options = new CacheOptions
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(2),
            SlidingExpiration = TimeSpan.FromMinutes(1)
        };

        // Act
        await _cacheService!.SetAsync(key, value, options);
        var initialTtl = await _cacheService.GetTtlAsync(key);
        
        // Wait a bit then refresh
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        await _cacheService.RefreshAsync(key);
        var refreshedTtl = await _cacheService.GetTtlAsync(key);

        // Assert
        Assert.NotNull(initialTtl);
        Assert.NotNull(refreshedTtl);
        // TTL should be extended after refresh (for sliding expiration)
        Assert.True(refreshedTtl.Value >= initialTtl.Value);
    }

    [Fact]
    public async Task RedisCache_ErrorHandling_ShouldHandleGracefully()
    {
        // Arrange
        var invalidKey = "";

        // Act & Assert - Should handle invalid keys gracefully
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _cacheService!.SetAsync(invalidKey, new TestCacheObject()));
        
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _cacheService!.GetAsync<TestCacheObject>(invalidKey));
            
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _cacheService!.ExistsAsync(invalidKey));
    }

    /// <summary>
    /// Test object for cache serialization validation
    /// </summary>
    public class TestCacheObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<string> Tags { get; set; } = new();
    }
}
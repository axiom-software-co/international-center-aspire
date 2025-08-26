using Bogus;
using Infrastructure.Cache.Tests.Contracts;

namespace Infrastructure.Cache.Tests;

/// <summary>
/// Implementation of cache test data factory for Redis testing
/// Provides realistic test data for cache validation using Bogus library
/// </summary>
public class CacheTestDataFactory : ICacheTestDataFactory
{
    private readonly Faker _faker;

    public CacheTestDataFactory()
    {
        _faker = new Faker();
    }

    /// <summary>
    /// Creates mock service data for Services API caching
    /// Contract: Must generate realistic service data matching Services API responses
    /// </summary>
    public async Task<ServiceCacheData[]> CreateMockServiceDataAsync(
        int count = 100,
        Action<ServiceCacheData>? configure = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken); // Simulate async operation
        
        var serviceFaker = new Faker<ServiceCacheData>()
            .RuleFor(s => s.Id, f => f.Random.Guid())
            .RuleFor(s => s.Name, f => f.Company.CompanyName())
            .RuleFor(s => s.Description, f => f.Lorem.Paragraph())
            .RuleFor(s => s.CategoryIds, f => f.Make(f.Random.Number(1, 5), () => f.Random.Guid()).ToArray())
            .RuleFor(s => s.CreatedAt, f => f.Date.Past())
            .RuleFor(s => s.UpdatedAt, f => f.Date.Recent())
            .RuleFor(s => s.IsActive, f => f.Random.Bool(0.9f)) // 90% active
            .RuleFor(s => s.Metadata, f => new Dictionary<string, object>
            {
                ["type"] = f.PickRandom("healthcare", "social", "education", "emergency"),
                ["rating"] = f.Random.Double(1.0, 5.0),
                ["verified"] = f.Random.Bool(0.8f),
                ["lastUpdated"] = f.Date.Recent().ToString("O")
            });

        var services = serviceFaker.Generate(count);
        
        if (configure != null)
        {
            foreach (var service in services)
            {
                configure(service);
            }
        }
        
        return services.ToArray();
    }

    /// <summary>
    /// Creates mock category data for Services API caching
    /// Contract: Must generate realistic category hierarchy data for Services API caching
    /// </summary>
    public async Task<CategoryCacheData[]> CreateMockCategoryDataAsync(
        int depth = 3,
        int breadth = 5,
        Action<CategoryCacheData>? configure = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken); // Simulate async operation
        
        var categories = new List<CategoryCacheData>();
        var categoryNames = new[]
        {
            "Healthcare", "Mental Health", "Social Services", "Education", "Housing", 
            "Food Security", "Transportation", "Legal Aid", "Emergency Services", "Employment"
        };
        
        var subcategoryNames = new[]
        {
            "Primary Care", "Specialist Care", "Counseling", "Support Groups", "Case Management",
            "Financial Assistance", "Training Programs", "Emergency Housing", "Food Banks", "Legal Consultation"
        };
        
        // Create root categories
        for (int i = 0; i < Math.Min(breadth, categoryNames.Length); i++)
        {
            var rootCategory = new CategoryCacheData
            {
                Id = Guid.NewGuid(),
                Name = categoryNames[i],
                Description = _faker.Lorem.Sentence(),
                ParentId = null,
                Level = 1,
                ServiceCount = _faker.Random.Number(5, 50),
                IsVisible = true,
                CreatedAt = _faker.Date.Past()
            };
            
            categories.Add(rootCategory);
            
            // Create subcategories
            if (depth > 1)
            {
                var subCount = _faker.Random.Number(2, breadth);
                for (int j = 0; j < subCount && j < subcategoryNames.Length; j++)
                {
                    var subCategory = new CategoryCacheData
                    {
                        Id = Guid.NewGuid(),
                        Name = $"{rootCategory.Name} - {subcategoryNames[j]}",
                        Description = _faker.Lorem.Sentence(),
                        ParentId = rootCategory.Id,
                        Level = 2,
                        ServiceCount = _faker.Random.Number(1, 20),
                        IsVisible = true,
                        CreatedAt = _faker.Date.Past()
                    };
                    
                    categories.Add(subCategory);
                    
                    // Create sub-subcategories if depth > 2
                    if (depth > 2)
                    {
                        var subSubCount = _faker.Random.Number(1, 3);
                        for (int k = 0; k < subSubCount; k++)
                        {
                            var subSubCategory = new CategoryCacheData
                            {
                                Id = Guid.NewGuid(),
                                Name = $"{subCategory.Name} - Level 3",
                                Description = _faker.Lorem.Sentence(),
                                ParentId = subCategory.Id,
                                Level = 3,
                                ServiceCount = _faker.Random.Number(1, 10),
                                IsVisible = true,
                                CreatedAt = _faker.Date.Past()
                            };
                            
                            categories.Add(subSubCategory);
                        }
                    }
                }
            }
        }
        
        if (configure != null)
        {
            foreach (var category in categories)
            {
                configure(category);
            }
        }
        
        return categories.ToArray();
    }

    /// <summary>
    /// Creates mock rate limiting data for Public Gateway testing
    /// Contract: Must generate realistic rate limiting scenarios for IP and user-based limits
    /// </summary>
    public async Task<RateLimitCacheData[]> CreateMockRateLimitDataAsync(
        string[] identifiers,
        int requestsPerMinute = 1000,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken); // Simulate async operation
        
        var rateLimitData = new List<RateLimitCacheData>();
        var now = DateTime.UtcNow;
        
        foreach (var identifier in identifiers)
        {
            var currentRequests = _faker.Random.Number(0, requestsPerMinute * 2); // Some may exceed limit
            var windowStart = now.AddMinutes(-_faker.Random.Double(0, 1));
            
            var rateLimitEntry = new RateLimitCacheData
            {
                Identifier = identifier,
                RequestCount = currentRequests,
                WindowStart = windowStart,
                WindowEnd = windowStart.AddMinutes(1),
                Limit = requestsPerMinute,
                IsBlocked = currentRequests > requestsPerMinute,
                BlockedUntil = currentRequests > requestsPerMinute ? 
                    windowStart.AddMinutes(1) - now : null
            };
            
            rateLimitData.Add(rateLimitEntry);
        }
        
        return rateLimitData.ToArray();
    }

    /// <summary>
    /// Creates mock cache keys and values for performance testing
    /// Contract: Must generate realistic cache data for load testing scenarios
    /// </summary>
    public async Task<CacheTestEntry[]> CreateMockCacheEntriesAsync(
        int count = 10000,
        int valueSizeBytes = 1024,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate async operation for large datasets
        
        var entries = new List<CacheTestEntry>();
        var valueTypes = new[] { typeof(string), typeof(int), typeof(bool), typeof(object) };
        var keyPrefixes = new[] { "service:", "category:", "user:", "session:", "config:" };
        
        for (int i = 0; i < count; i++)
        {
            var valueType = _faker.PickRandom(valueTypes);
            var keyPrefix = _faker.PickRandom(keyPrefixes);
            
            object value = valueType switch
            {
                var t when t == typeof(string) => _faker.Lorem.Text().PadRight(valueSizeBytes, 'x')[..valueSizeBytes],
                var t when t == typeof(int) => _faker.Random.Number(),
                var t when t == typeof(bool) => _faker.Random.Bool(),
                _ => new
                {
                    id = _faker.Random.Guid(),
                    name = _faker.Lorem.Word(),
                    data = _faker.Lorem.Text().PadRight(valueSizeBytes / 2, 'x')[..(valueSizeBytes / 2)],
                    timestamp = _faker.Date.Recent(),
                    metadata = _faker.Make(3, () => new { key = _faker.Lorem.Word(), value = _faker.Lorem.Word() })
                }
            };
            
            var entry = new CacheTestEntry
            {
                Key = $"{keyPrefix}{i:D6}",
                Value = value,
                ValueType = valueType,
                Expiry = _faker.Random.Bool(0.7f) ? _faker.Date.Future().Subtract(DateTime.UtcNow) : null,
                CreatedAt = _faker.Date.Past(),
                SizeBytes = valueSizeBytes,
                Tags = _faker.Random.Bool(0.5f) ? 
                    _faker.Make(_faker.Random.Number(1, 3), () => _faker.Lorem.Word()).ToArray() : 
                    null
            };
            
            entries.Add(entry);
        }
        
        return entries.ToArray();
    }

    /// <summary>
    /// Validates cache data quality and realism
    /// Contract: Must ensure cache data follows realistic patterns for Services APIs
    /// </summary>
    public async Task ValidateCacheDataQualityAsync<T>(
        T cacheData,
        CacheDataQualityRules<T>? qualityRules = null)
        where T : class
    {
        await Task.Delay(1); // Simulate async validation
        
        if (cacheData == null)
            throw new ArgumentException("Cache data cannot be null", nameof(cacheData));
        
        var rules = qualityRules ?? new CacheDataQualityRules<T>();
        
        // Validate realism
        if (!rules.IsRealistic(cacheData))
        {
            throw new InvalidOperationException("Cache data failed realism validation");
        }
        
        // Validate API schema compatibility
        if (!rules.MatchesApiSchema(cacheData))
        {
            throw new InvalidOperationException("Cache data does not match expected API schema");
        }
        
        // Validate required fields
        if (!rules.HasRequiredFields(cacheData))
        {
            throw new InvalidOperationException("Cache data is missing required fields");
        }
        
        // Validate business rules
        if (!rules.ValidateBusinessRules(cacheData))
        {
            throw new InvalidOperationException("Cache data violates business rules");
        }
        
        // Check for forbidden patterns in string properties
        var stringProperties = typeof(T).GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => p.GetValue(cacheData) as string)
            .Where(v => v != null);
        
        foreach (var stringValue in stringProperties)
        {
            foreach (var forbiddenPattern in rules.ForbiddenPatterns)
            {
                if (stringValue!.Contains(forbiddenPattern, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Cache data contains forbidden pattern '{forbiddenPattern}' in value '{stringValue}'");
                }
            }
        }
        
        // Validate value size constraints
        if (cacheData is CacheTestEntry entry)
        {
            if (entry.SizeBytes > rules.MaxValueSizeBytes)
            {
                throw new InvalidOperationException(
                    $"Cache entry size {entry.SizeBytes} bytes exceeds maximum {rules.MaxValueSizeBytes} bytes");
            }
            
            if (entry.Expiry.HasValue && entry.Expiry.Value > rules.MaxExpiry)
            {
                throw new InvalidOperationException(
                    $"Cache entry expiry {entry.Expiry} exceeds maximum allowed expiry {rules.MaxExpiry}");
            }
        }
    }
}
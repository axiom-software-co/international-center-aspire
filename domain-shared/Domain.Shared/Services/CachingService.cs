using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Services;

public interface ICachingService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default);
    string BuildCacheKey(string prefix, params string[] parts);
}

/// <summary>
/// Redis-based caching service implementation using distributed cache
/// Provides high-performance caching using Redis server
/// </summary>
public class RedisCachingService : ICachingService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCachingService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCachingService(IDistributedCache cache, ILogger<RedisCachingService> logger)
    {
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedValue = await _cache.GetStringAsync(key, cancellationToken);
            
            if (string.IsNullOrEmpty(cachedValue))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get cached value for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            
            var options = new DistributedCacheEntryOptions();
            if (expiry.HasValue)
            {
                options.SetAbsoluteExpiration(expiry.Value);
            }
            else
            {
                options.SetAbsoluteExpiration(TimeSpan.FromMinutes(5)); // Default 5 minutes
            }

            await _cache.SetStringAsync(key, serializedValue, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cached value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cached value for key: {Key}", key);
        }
    }

    public async Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Garnet pattern deletion would require direct Garnet/Redis-compatible commands
        // For now, this is a placeholder - in production you'd use IConnectionMultiplexer
        _logger.LogInformation("Pattern removal requested for: {Pattern}", pattern);
        await Task.CompletedTask;
    }

    public string BuildCacheKey(string prefix, params string[] parts)
    {
        var sanitizedParts = parts.Select(p => p.Replace(":", "_").Replace(" ", "_")).ToArray();
        return $"{prefix}:{string.Join(":", sanitizedParts)}";
    }
}

public static class CacheKeys
{
    public const string Services = "services";
    public const string News = "news";
    public const string Newsletter = "newsletter";
    public const string Research = "research";
    public const string Events = "events";
    public const string Contacts = "contacts";
    public const string Search = "search";
    public const string ServiceCategories = "service_categories";
    
    public static string GetServiceKey(string slug) => $"{Services}:slug:{slug}";
    public static string GetNewsKey(string slug) => $"{News}:slug:{slug}";
    public static string GetResearchKey(string slug) => $"{Research}:slug:{slug}";
    public static string GetEventKey(string slug) => $"{Events}:slug:{slug}";
    public static string GetSearchKey(string query, string category, int page, int pageSize) => 
        $"{Search}:q:{query}:cat:{category}:p:{page}:s:{pageSize}";
}
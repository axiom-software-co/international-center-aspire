namespace Shared.Infrastructure.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, CacheOptions options, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<TimeSpan?> GetTtlAsync(string key, CancellationToken cancellationToken = default);
    Task RefreshAsync(string key, CancellationToken cancellationToken = default);
}

public sealed class CacheOptions
{
    public TimeSpan? AbsoluteExpiration { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public CachePriority Priority { get; set; } = CachePriority.Normal;
    public bool CompressData { get; set; } = true;
    public IEnumerable<string>? Tags { get; set; }

    public static CacheOptions Default => new();
    
    public static CacheOptions Short => new() 
    { 
        AbsoluteExpiration = TimeSpan.FromMinutes(5),
        Priority = CachePriority.High
    };
    
    public static CacheOptions Medium => new() 
    { 
        AbsoluteExpiration = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(10)
    };
    
    public static CacheOptions Long => new() 
    { 
        AbsoluteExpiration = TimeSpan.FromHours(2),
        SlidingExpiration = TimeSpan.FromMinutes(30),
        Priority = CachePriority.Low
    };
}

public enum CachePriority
{
    Low,
    Normal,
    High,
    NeverRemove
}
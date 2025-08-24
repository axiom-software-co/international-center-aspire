using System.Security.Cryptography;
using System.Text;

namespace InternationalCenter.Shared.Infrastructure.Caching;

public interface ICacheKeyService
{
    string GenerateKey(string prefix, params object[] parameters);
    string GenerateKey(string prefix, IDictionary<string, object?> parameters);
    string[] GenerateKeysForInvalidation(string prefix, params object[] parameters);
    string GenerateVersionedKey(string baseKey, string version);
    string GenerateHashKey(string prefix, params object[] parameters);
}

public sealed class CacheKeyService : ICacheKeyService
{
    private const string KeySeparator = ":";
    private const string ParameterSeparator = "_";
    private const int MaxKeyLength = 250; // Redis key length limit

    public string GenerateKey(string prefix, params object[] parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var keyBuilder = new StringBuilder(prefix);
        
        if (parameters.Length > 0)
        {
            keyBuilder.Append(KeySeparator);
            keyBuilder.Append(string.Join(ParameterSeparator, 
                parameters.Where(p => p != null)
                         .Select(NormalizeParameter)));
        }

        var key = keyBuilder.ToString();
        return key.Length > MaxKeyLength ? GenerateHashKey(prefix, parameters) : key;
    }

    public string GenerateKey(string prefix, IDictionary<string, object?> parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentNullException.ThrowIfNull(parameters);

        var keyBuilder = new StringBuilder(prefix);
        
        if (parameters.Count > 0)
        {
            keyBuilder.Append(KeySeparator);
            
            var sortedParameters = parameters
                .Where(kvp => kvp.Value != null)
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{kvp.Key}={NormalizeParameter(kvp.Value!)}");
                
            keyBuilder.Append(string.Join(ParameterSeparator, sortedParameters));
        }

        var key = keyBuilder.ToString();
        return key.Length > MaxKeyLength ? GenerateHashKey(prefix, parameters) : key;
    }

    public string[] GenerateKeysForInvalidation(string prefix, params object[] parameters)
    {
        var keys = new List<string>();
        
        // Add exact key
        keys.Add(GenerateKey(prefix, parameters));
        
        // Add pattern keys for wildcard invalidation
        keys.Add($"{prefix}:*");
        
        // Add hierarchical keys
        for (int i = 0; i < parameters.Length; i++)
        {
            var partialParams = parameters.Take(i + 1).ToArray();
            keys.Add($"{GenerateKey(prefix, partialParams)}:*");
        }

        return keys.Distinct().ToArray();
    }

    public string GenerateVersionedKey(string baseKey, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        return $"{baseKey}:v{version}";
    }

    public string GenerateHashKey(string prefix, params object[] parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var combinedString = string.Join(ParameterSeparator, 
            parameters.Where(p => p != null)
                     .Select(p => p.ToString() ?? string.Empty));

        var hash = ComputeHash(combinedString);
        return $"{prefix}:hash:{hash}";
    }

    private string GenerateHashKey(string prefix, IDictionary<string, object?> parameters)
    {
        var combinedString = string.Join(ParameterSeparator,
            parameters.Where(kvp => kvp.Value != null)
                     .OrderBy(kvp => kvp.Key)
                     .Select(kvp => $"{kvp.Key}={kvp.Value}"));

        var hash = ComputeHash(combinedString);
        return $"{prefix}:hash:{hash}";
    }

    private static string NormalizeParameter(object parameter)
    {
        return parameter switch
        {
            null => "null",
            bool b => b.ToString().ToLowerInvariant(),
            DateTime dt => dt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            DateTimeOffset dto => dto.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            decimal d => d.ToString("F2"),
            double d => d.ToString("F2"),
            float f => f.ToString("F2"),
            string s => s.ToLowerInvariant().Trim(),
            IEnumerable<object> enumerable => string.Join(",", enumerable.Select(NormalizeParameter)),
            _ => parameter.ToString()?.ToLowerInvariant().Trim() ?? "null"
        };
    }

    private static string ComputeHash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "empty";

        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(inputBytes);
        
        // Use first 16 bytes for shorter hash
        return Convert.ToHexString(hashBytes[..16]).ToLowerInvariant();
    }
}

// Cache key constants for consistency
public static class CacheKeys
{
    public const string Services = "services";
    public const string ServiceBySlug = "service_by_slug";
    public const string ServiceCategories = "service_categories";
    public const string FeaturedServices = "featured_services";
    public const string ServiceSearch = "service_search";
    
    public const string News = "news";
    public const string Events = "events";
    public const string Research = "research";
    public const string Contacts = "contacts";
    public const string Newsletter = "newsletter";
    
    // Tag prefixes for cache invalidation
    public static class Tags
    {
        public const string Services = "services_tag";
        public const string Categories = "categories_tag";
        public const string Content = "content_tag";
    }
}
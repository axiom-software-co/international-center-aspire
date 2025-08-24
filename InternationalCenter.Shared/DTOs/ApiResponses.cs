using System.Text.Json.Serialization;

namespace InternationalCenter.Shared.DTOs;

public class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; } = default!;
    
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class PaginatedResponse<T>
{
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    
    [JsonPropertyName("total")]
    public long Total { get; set; }
    
    [JsonPropertyName("page")]
    public int Page { get; set; }
    
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
    
    [JsonPropertyName("totalPages")]
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
    
    [JsonPropertyName("hasNext")]
    public bool HasNext => Page < TotalPages;
    
    [JsonPropertyName("hasPrevious")]
    public bool HasPrevious => Page > 1;
}

public class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
    
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    
    [JsonPropertyName("success")]
    public bool Success { get; set; } = false;
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "healthy";
    
    [JsonPropertyName("environment")]
    public string Environment { get; set; } = "development";
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies { get; set; } = new();
}

public class BasicSearchRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;
    
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;
    
    [JsonPropertyName("sortBy")]
    public string SortBy { get; set; } = "relevance";
}

public class PaginationRequest
{
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;
    
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("featured")]
    public bool? Featured { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("isUrgent")]
    public bool? IsUrgent { get; set; }
}
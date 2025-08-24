using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternationalCenter.Shared.Models;

[Table("unified_search")]
public class UnifiedSearchIndex : BaseEntity
{
    [Required]
    [MaxLength(255)]
    public string ContentId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ContentType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    
    public string Summary { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
    
    public string[] Tags { get; set; } = Array.Empty<string>();
    
    [MaxLength(255)]
    public string Author { get; set; } = string.Empty;
    
    public DateTime? PublishedAt { get; set; }
    
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;
    
    public bool IsPublished { get; set; } = false;
    
    public bool IsFeatured { get; set; } = false;
    
    public int Priority { get; set; } = 0;
    
    // For full-text search - will be handled by database triggers
    [Column("search_vector")]
    public string? SearchVector { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string Metadata { get; set; } = "{}";
    
    public DateTime LastIndexed { get; set; } = DateTime.UtcNow;
}

public static class ContentTypes
{
    public const string Service = "service";
    public const string News = "news";
    public const string Research = "research";
    public const string Event = "event";
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool FeaturedOnly { get; set; } = false;
    public string SortBy { get; set; } = "relevance";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SearchResult
{
    public string ContentId { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string Author { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsFeatured { get; set; } = false;
    public double Relevance { get; set; } = 0;
}

public class SearchResponse
{
    public SearchResult[] Results { get; set; } = Array.Empty<SearchResult>();
    public long Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string Query { get; set; } = string.Empty;
    public double QueryTime { get; set; }
}
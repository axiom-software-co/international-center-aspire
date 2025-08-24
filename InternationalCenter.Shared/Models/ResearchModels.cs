using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternationalCenter.Shared.Models;

[Table("research_articles")]
public class ResearchArticle : BaseEntity, ISluggable, IPublishable, ISeoEnabled, ICategorized
{
    [Required]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    public string Excerpt { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string AuthorName { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string AuthorEmail { get; set; } = string.Empty;
    
    public DateTime? PublishedAt { get; set; }
    
    public bool Featured { get; set; } = false;
    
    [MaxLength(50)]
    public string Status { get; set; } = "draft";
    
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
    
    public string[] Tags { get; set; } = Array.Empty<string>();
    
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string MetaTitle { get; set; } = string.Empty;
    
    public string MetaDescription { get; set; } = string.Empty;
    
    [Column(TypeName = "jsonb")]
    public string Metadata { get; set; } = "{}";
    
    // For full-text search - will be handled by database triggers
    [Column("search_vector")]
    public string? SearchVector { get; set; }
    
    // Research-specific fields
    [MaxLength(255)]
    public string StudyType { get; set; } = string.Empty;
    
    public string[] Keywords { get; set; } = Array.Empty<string>();
    
    [MaxLength(255)]
    public string DOI { get; set; } = string.Empty;
    
    public DateTime? StudyDate { get; set; }
    
    public string[] Collaborators { get; set; } = Array.Empty<string>();
}

[Table("research_categories")]
public class ResearchCategory : BaseEntity
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;
    
    public int DisplayOrder { get; set; } = 0;
    
    public bool Active { get; set; } = true;
}
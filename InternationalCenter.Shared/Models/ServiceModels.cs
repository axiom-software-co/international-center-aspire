using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternationalCenter.Shared.Models;

[Table("service_categories")]
public class ServiceCategory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;
    
    public int MinPriorityOrder { get; set; } = 1;
    
    public int MaxPriorityOrder { get; set; } = 100;
    
    public int DisplayOrder { get; set; } = 0;
    
    public bool Featured1 { get; set; } = false;
    
    public bool Featured2 { get; set; } = false;
    
    public bool Active { get; set; } = true;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public ICollection<Service> Services { get; set; } = new List<Service>();
}

[Table("services")]
public class Service : BaseEntity, ISluggable, IPublishable, ISeoEnabled, ICategorized
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string DetailedDescription { get; set; } = string.Empty;
    
    public string[] Technologies { get; set; } = Array.Empty<string>();
    
    public string[] Features { get; set; } = Array.Empty<string>();
    
    public string[] DeliveryModes { get; set; } = Array.Empty<string>();
    
    [MaxLength(255)]
    public string Icon { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Image { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "draft";
    
    [Column("priority")]
    public long SortOrder { get; set; }
    
    public int? CategoryId { get; set; }
    
    public bool Available { get; set; } = true;
    
    public bool Featured { get; set; } = false;
    
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string MetaTitle { get; set; } = string.Empty;
    
    public string MetaDescription { get; set; } = string.Empty;
    
    public DateTime? PublishedAt { get; set; }
    
    // Navigation property
    [ForeignKey("CategoryId")]
    public ServiceCategory? ServiceCategory { get; set; }
}
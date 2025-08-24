using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternationalCenter.Shared.Models;

[Table("events")]
public class Event : BaseEntity, ISluggable, IPublishable, ISeoEnabled, ICategorized
{
    [Required]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    [MaxLength(255)]
    public string Location { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;
    
    public bool IsVirtual { get; set; } = false;
    
    [MaxLength(500)]
    public string VirtualLink { get; set; } = string.Empty;
    
    public int MaxAttendees { get; set; } = 0;
    
    public int CurrentAttendees { get; set; } = 0;
    
    public decimal Price { get; set; } = 0;
    
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    public bool IsFree { get; set; } = true;
    
    public bool RequiresRegistration { get; set; } = true;
    
    public DateTime? RegistrationDeadline { get; set; }
    
    [MaxLength(255)]
    public string OrganizerName { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string OrganizerEmail { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string OrganizerPhone { get; set; } = string.Empty;
    
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
    
    public DateTime? PublishedAt { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string Metadata { get; set; } = "{}";
    
    // Navigation properties
    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
}

[Table("event_registrations")]
public class EventRegistration : BaseEntity
{
    [Required]
    public string EventId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string AttendeeName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string AttendeeEmail { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string AttendeePhone { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Status { get; set; } = "registered";
    
    public DateTime? CheckedInAt { get; set; }
    
    public string SpecialRequirements { get; set; } = string.Empty;
    
    [Column(TypeName = "jsonb")]
    public string Metadata { get; set; } = "{}";
    
    // Navigation property
    [ForeignKey("EventId")]
    public Event Event { get; set; } = null!;
}

public static class EventStatus
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Cancelled = "cancelled";
    public const string Completed = "completed";
}

public static class RegistrationStatus
{
    public const string Registered = "registered";
    public const string Confirmed = "confirmed";
    public const string Cancelled = "cancelled";
    public const string Attended = "attended";
    public const string NoShow = "no_show";
}
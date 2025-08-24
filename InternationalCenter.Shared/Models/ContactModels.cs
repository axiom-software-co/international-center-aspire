using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternationalCenter.Shared.Models;

[Table("contacts")]
public class Contact : BaseEntity
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Status { get; set; } = "new";
    
    [MaxLength(100)]
    public string Type { get; set; } = "general";
    
    [MaxLength(100)]
    public string Source { get; set; } = "website";
    
    public bool IsUrgent { get; set; } = false;
    
    public DateTime? ResponseSentAt { get; set; }
    
    [MaxLength(255)]
    public string RespondedBy { get; set; } = string.Empty;
    
    public string InternalNotes { get; set; } = string.Empty;
    
    [Column(TypeName = "jsonb")]
    public string Metadata { get; set; } = "{}";
    
    // GDPR compliance
    public bool ConsentGiven { get; set; } = false;
    
    public DateTime? ConsentDate { get; set; }
    
    public DateTime? DataRetentionDate { get; set; }
}

public static class ContactTypes
{
    public const string General = "general";
    public const string Appointment = "appointment";
    public const string Information = "information";
    public const string Support = "support";
    public const string Partnership = "partnership";
    public const string Media = "media";
}
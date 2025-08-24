using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternationalCenter.Shared.Models;

[Table("newsletter_subscriptions")]
public class NewsletterSubscription : BaseEntity
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Status { get; set; } = "pending";
    
    public string[] Preferences { get; set; } = Array.Empty<string>();
    
    [MaxLength(100)]
    public string Source { get; set; } = "website";
    
    public DateTime? ConfirmedAt { get; set; }
    
    public DateTime? UnsubscribedAt { get; set; }
    
    [MaxLength(255)]
    public string ConfirmationToken { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string UnsubscribeToken { get; set; } = string.Empty;
    
    public bool ConsentGiven { get; set; } = false;
    
    public DateTime? ConsentDate { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string Metadata { get; set; } = "{}";
}

public static class NewsletterStatus
{
    public const string Pending = "pending";
    public const string Confirmed = "confirmed";
    public const string Unsubscribed = "unsubscribed";
    public const string Bounced = "bounced";
}

public static class NewsletterPreferences
{
    public const string WeeklyUpdates = "weekly_updates";
    public const string HealthTips = "health_tips";
    public const string ResearchNews = "research_news";
    public const string Events = "events";
    public const string Promotions = "promotions";
}
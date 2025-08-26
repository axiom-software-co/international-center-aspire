using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models;

public abstract class BaseEntity : IAuditable
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public interface IIdentifiable
{
    string Id { get; set; }
}

public interface ISluggable
{
    string Slug { get; set; }
}

public interface ITimestamped
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

public interface ICategorized
{
    string Category { get; set; }
}

public interface IPublishable
{
    bool Featured { get; set; }
    string Status { get; set; }
    DateTime? PublishedAt { get; set; }
}

public interface ISeoEnabled
{
    string MetaTitle { get; set; }
    string MetaDescription { get; set; }
}

public static class EntityValidationRules
{
    public static readonly string[] ValidArticleStatuses = { "draft", "published", "archived" };
    public static readonly string[] ValidContactStatuses = { "new", "read", "replied", "closed" };
    public static readonly string[] ValidServiceStatuses = { "draft", "published", "archived" };
}
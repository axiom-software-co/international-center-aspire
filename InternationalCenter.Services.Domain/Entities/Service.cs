using InternationalCenter.Services.Domain.ValueObjects;

namespace InternationalCenter.Services.Domain.Entities;

public sealed class Service
{
    public ServiceId Id { get; private set; }
    public string Title { get; private set; }
    public Slug Slug { get; private set; }
    public string Description { get; private set; }
    public string DetailedDescription { get; private set; }
    public ServiceStatus Status { get; private set; }
    public int SortOrder { get; private set; }
    public bool Available { get; private set; }
    public bool Featured { get; private set; }
    public ServiceCategoryId? CategoryId { get; private set; }
    public ServiceCategory? Category { get; private set; }
    public ServiceMetadata Metadata { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Service() { } // For EF Core

    public Service(
        ServiceId id,
        string title,
        Slug slug,
        string description,
        string detailedDescription,
        ServiceMetadata metadata)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Title = !string.IsNullOrWhiteSpace(title) ? title : throw new ArgumentException("Title cannot be empty", nameof(title));
        Slug = slug ?? throw new ArgumentNullException(nameof(slug));
        Description = description ?? string.Empty;
        DetailedDescription = detailedDescription ?? string.Empty;
        Status = ServiceStatus.Draft;
        Available = true;
        Featured = false;
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
            
        Title = title;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string description, string detailedDescription)
    {
        Description = description ?? string.Empty;
        DetailedDescription = detailedDescription ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        if (Status == ServiceStatus.Published)
            return;

        if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Description))
            throw new InvalidOperationException("Service must have title and description to be published");

        Status = ServiceStatus.Published;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnPublish()
    {
        if (Status != ServiceStatus.Published)
            return;

        Status = ServiceStatus.Draft;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = ServiceStatus.Archived;
        Available = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAvailability(bool available)
    {
        Available = available;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetFeatured(bool featured)
    {
        Featured = featured;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCategory(ServiceCategoryId? categoryId)
    {
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = Math.Max(0, sortOrder);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsActive => Status == ServiceStatus.Published && Available;
    public bool CanBePublished => !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Description);
}
using Services.Shared.ValueObjects;

namespace Services.Shared.Entities;

public sealed class ServiceCategory
{
    public ServiceCategoryId Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Slug Slug { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool Active { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<Service> _services = new();
    public IReadOnlyCollection<Service> Services => _services.AsReadOnly();

    private ServiceCategory() // For EF Core
    {
        // Initialize required properties to avoid nullable reference warnings
        // EF Core will set actual values during entity materialization
        Id = null!;
        Name = null!;
        Description = null!;
        Slug = null!;
    }

    public ServiceCategory(
        ServiceCategoryId id,
        string name,
        string description,
        Slug slug,
        int displayOrder = 0)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Name cannot be empty", nameof(name));
        Description = description ?? string.Empty;
        Slug = slug ?? throw new ArgumentNullException(nameof(slug));
        DisplayOrder = Math.Max(0, displayOrder);
        Active = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
            
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string description)
    {
        Description = description ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSlug(Slug slug)
    {
        Slug = slug ?? throw new ArgumentNullException(nameof(slug));
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDisplayOrder(int displayOrder)
    {
        DisplayOrder = Math.Max(0, displayOrder);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (Active) return;
        
        Active = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!Active) return;
        
        Active = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanBeDeleted => !_services.Any(s => s.IsActive);
}
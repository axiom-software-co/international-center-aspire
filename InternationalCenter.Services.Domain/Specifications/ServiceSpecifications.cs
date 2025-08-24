using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.ValueObjects;

namespace InternationalCenter.Services.Domain.Specifications;

public class ActiveServicesSpecification : BaseSpecification<Service>
{
    public ActiveServicesSpecification() : base(s => s.Status == ServiceStatus.Published && s.Available)
    {
        ApplyOrderBy(s => s.SortOrder);
        ApplyThenBy(s => s.Title);
        AddInclude(s => s.Category);
    }
}

public class FeaturedServicesSpecification : BaseSpecification<Service>
{
    public FeaturedServicesSpecification(int limit = 5) : base(s => s.Status == ServiceStatus.Published && s.Available && s.Featured)
    {
        ApplyOrderBy(s => s.SortOrder);
        ApplyThenBy(s => s.Title);
        ApplyPaging(0, limit);
        AddInclude(s => s.Category);
    }
}

public class ServicesByCategorySpecification : BaseSpecification<Service>
{
    public ServicesByCategorySpecification(ServiceCategoryId categoryId) : base(s => 
        s.Status == ServiceStatus.Published && 
        s.Available && 
        s.CategoryId == categoryId)
    {
        ApplyOrderBy(s => s.SortOrder);
        ApplyThenBy(s => s.Title);
        AddInclude(s => s.Category);
    }
}

public class ServicesSearchSpecification : BaseSpecification<Service>
{
    public ServicesSearchSpecification(
        string searchTerm,
        ServiceCategoryId? categoryId = null,
        bool? featured = null,
        string sortBy = "priority") : base(BuildCriteria(searchTerm, categoryId, featured))
    {
        ApplySorting(sortBy);
        AddInclude(s => s.Category);
    }

    private static System.Linq.Expressions.Expression<Func<Service, bool>> BuildCriteria(
        string searchTerm, 
        ServiceCategoryId? categoryId, 
        bool? featured)
    {
        return s => 
            s.Status == ServiceStatus.Published && 
            s.Available &&
            (string.IsNullOrEmpty(searchTerm) || 
             s.Title.Contains(searchTerm) || 
             s.Description.Contains(searchTerm) || 
             s.DetailedDescription.Contains(searchTerm)) &&
            (categoryId == null || s.CategoryId == categoryId) &&
            (featured == null || s.Featured == featured.Value);
    }

    private void ApplySorting(string sortBy)
    {
        switch (sortBy?.ToLowerInvariant())
        {
            case "title-asc":
                ApplyOrderBy(s => s.Title);
                break;
            case "title-desc":
                ApplyOrderByDescending(s => s.Title);
                break;
            case "date-asc":
                ApplyOrderBy(s => s.CreatedAt);
                break;
            case "date-desc":
                ApplyOrderByDescending(s => s.CreatedAt);
                break;
            case "priority":
            default:
                ApplyOrderBy(s => s.SortOrder);
                ApplyThenBy(s => s.Title);
                break;
        }
    }
}

public class ServicesByStatusSpecification : BaseSpecification<Service>
{
    public ServicesByStatusSpecification(ServiceStatus status) : base(s => s.Status == status)
    {
        ApplyOrderBy(s => s.UpdatedAt);
        AddInclude(s => s.Category);
    }
}

public class ServiceBySlugSpecification : BaseSpecification<Service>
{
    public ServiceBySlugSpecification(Slug slug) : base(s => s.Slug == slug && s.Status == ServiceStatus.Published && s.Available)
    {
        AddInclude(s => s.Category);
    }
}

public class PublishedServicesSpecification : BaseSpecification<Service>
{
    public PublishedServicesSpecification() : base(s => s.Status == ServiceStatus.Published)
    {
        ApplyOrderBy(s => s.SortOrder);
        ApplyThenBy(s => s.Title);
        AddInclude(s => s.Category);
    }
}
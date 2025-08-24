using Bogus;
using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.ValueObjects;

namespace InternationalCenter.Tests.Shared.TestData;

/// <summary>
/// Generates realistic test data for Services domain using Bogus
/// Avoids mock data by creating valid domain entities with realistic values
/// </summary>
public static class ServiceTestDataGenerator
{
    private static readonly Faker<Service> ServiceFaker = new Faker<Service>()
        .CustomInstantiator(f => new Service(
            ServiceId.Create(),
            f.Company.CompanyName() + " Service",
            Slug.Create(f.Lorem.Slug()),
            f.Lorem.Paragraph(),
            f.Lorem.Paragraphs(3),
            ServiceMetadata.Create(
                icon: f.Image.PlaceImgUrl(),
                image: f.Image.PlaceImgUrl(),
                metaTitle: f.Lorem.Sentence(),
                metaDescription: f.Lorem.Paragraph(),
                technologies: f.Lorem.Words(f.Random.Int(2, 5)),
                features: f.Lorem.Words(f.Random.Int(3, 8)),
                deliveryModes: f.PickRandom(new[] { "Online", "In-Person", "Hybrid" }, f.Random.Int(1, 3))
            )
        ));

    private static readonly Faker<ServiceCategory> CategoryFaker = new Faker<ServiceCategory>()
        .CustomInstantiator(f => new ServiceCategory(
            ServiceCategoryId.Create(f.Random.Int(1, 1000)),
            f.Commerce.Department(),
            f.Lorem.Sentence(),
            Slug.Create(f.Lorem.Slug()),
            f.Random.Int(0, 100)
        ));

    /// <summary>
    /// Generates a single service with realistic data
    /// </summary>
    public static Service GenerateService()
    {
        return ServiceFaker.Generate();
    }

    /// <summary>
    /// Generates multiple services with realistic data
    /// </summary>
    public static IEnumerable<Service> GenerateServices(int count = 10)
    {
        return ServiceFaker.Generate(count);
    }

    /// <summary>
    /// Generates a service with specific properties for testing
    /// </summary>
    public static Service GenerateServiceWith(
        string? title = null,
        string? slug = null,
        ServiceStatus? status = null,
        bool? featured = null)
    {
        var service = ServiceFaker.Generate();
        
        if (title != null)
            service.UpdateTitle(title);
            
        if (status.HasValue)
        {
            switch (status.Value)
            {
                case ServiceStatus.Published:
                    service.Publish();
                    break;
                case ServiceStatus.Draft:
                    service.UnPublish();
                    break;
                case ServiceStatus.Archived:
                    service.Archive();
                    break;
            }
        }
        
        if (featured.HasValue)
        {
            service.SetFeatured(featured.Value);
        }
        
        return service;
    }

    /// <summary>
    /// Generates a single service category with realistic data
    /// </summary>
    public static ServiceCategory GenerateCategory()
    {
        return CategoryFaker.Generate();
    }

    /// <summary>
    /// Generates multiple service categories with realistic data
    /// </summary>
    public static IEnumerable<ServiceCategory> GenerateCategories(int count = 5)
    {
        return CategoryFaker.Generate(count);
    }

    /// <summary>
    /// Generates a category with specific properties for testing
    /// </summary>
    public static ServiceCategory GenerateCategoryWith(
        string? name = null,
        string? slug = null,
        bool? active = null)
    {
        var category = CategoryFaker.Generate();
        
        if (name != null)
            category.UpdateName(name);
            
        if (slug != null)
            category.UpdateSlug(Slug.Create(slug));
            
        if (active.HasValue)
        {
            if (active.Value)
                category.Activate();
            else
                category.Deactivate();
        }
        
        return category;
    }

    /// <summary>
    /// Generates services belonging to specific categories
    /// </summary>
    public static IEnumerable<Service> GenerateServicesForCategories(
        IEnumerable<ServiceCategory> categories,
        int servicesPerCategory = 3)
    {
        var services = new List<Service>();
        
        foreach (var category in categories)
        {
            for (var i = 0; i < servicesPerCategory; i++)
            {
                var service = ServiceFaker.Generate();
                service.SetCategory(category.Id);
                services.Add(service);
            }
        }
        
        return services;
    }
}
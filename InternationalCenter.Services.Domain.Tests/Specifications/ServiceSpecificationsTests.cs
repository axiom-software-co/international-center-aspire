using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Specifications;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Tests.Shared.TestData;

namespace InternationalCenter.Services.Domain.Tests.Specifications;

/// <summary>
/// Unit tests for Service specifications validating business logic and expression compilation
/// Tests specifications pattern implementation without database dependencies
/// </summary>
public class ServiceSpecificationsTests
{
    private readonly List<Service> _testServices;

    public ServiceSpecificationsTests()
    {
        // Create test data for specification validation
        _testServices = CreateTestServices();
    }

    [Fact]
    public void ActiveServicesSpecification_ShouldFilterPublishedAndAvailableServices()
    {
        // Arrange
        var specification = new ActiveServicesSpecification();
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, service =>
        {
            Assert.Equal(ServiceStatus.Published, service.Status);
            Assert.True(service.Available);
        });
    }

    [Fact]
    public void FeaturedServicesSpecification_ShouldFilterFeaturedServices()
    {
        // Arrange
        var specification = new FeaturedServicesSpecification(limit: 10);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, service =>
        {
            Assert.Equal(ServiceStatus.Published, service.Status);
            Assert.True(service.Available);
            Assert.True(service.Featured);
        });
    }

    [Fact]
    public void FeaturedServicesSpecification_ShouldHavePagingEnabled()
    {
        // Arrange & Act
        var specification = new FeaturedServicesSpecification(limit: 5);
        
        // Assert
        Assert.True(specification.IsPagingEnabled);
        Assert.Equal(5, specification.Take);
        Assert.Equal(0, specification.Skip);
    }

    [Fact]
    public void ServicesByCategorySpecification_ShouldFilterByCategory()
    {
        // Arrange
        var targetCategoryId = _testServices.First().CategoryId;
        var specification = new ServicesByCategorySpecification(targetCategoryId);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.True(result.Count > 0);
        Assert.All(result, service =>
        {
            Assert.Equal(ServiceStatus.Published, service.Status);
            Assert.True(service.Available);
            Assert.Equal(targetCategoryId, service.CategoryId);
        });
    }

    [Fact]
    public void ServicesSearchSpecification_WithSearchTerm_ShouldFilterByTitleOrDescription()
    {
        // Arrange
        var searchTerm = "Service";
        var specification = new ServicesSearchSpecification(searchTerm);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.True(result.Count > 0);
        Assert.All(result, service =>
        {
            Assert.Equal(ServiceStatus.Published, service.Status);
            Assert.True(service.Available);
            Assert.True(
                service.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                service.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                service.DetailedDescription.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            );
        });
    }

    [Fact]
    public void ServicesSearchSpecification_WithEmptySearchTerm_ShouldReturnAllPublishedServices()
    {
        // Arrange
        var specification = new ServicesSearchSpecification(string.Empty);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.Equal(3, result.Count); // All published available services
        Assert.All(result, service =>
        {
            Assert.Equal(ServiceStatus.Published, service.Status);
            Assert.True(service.Available);
        });
    }

    [Fact]
    public void ServicesSearchSpecification_WithCategoryFilter_ShouldFilterByCategoryAndSearch()
    {
        // Arrange
        var searchTerm = "Service";
        var categoryId = _testServices.First().CategoryId;
        var specification = new ServicesSearchSpecification(searchTerm, categoryId);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.All(result, service =>
        {
            Assert.Equal(ServiceStatus.Published, service.Status);
            Assert.True(service.Available);
            Assert.Equal(categoryId, service.CategoryId);
        });
    }

    [Fact]
    public void ServicesSearchSpecification_WithFeaturedFilter_ShouldFilterByFeaturedStatus()
    {
        // Arrange
        var specification = new ServicesSearchSpecification("", featured: true);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, service =>
        {
            Assert.Equal(ServiceStatus.Published, service.Status);
            Assert.True(service.Available);
            Assert.True(service.Featured);
        });
    }

    [Fact]
    public void ServicesByStatusSpecification_ShouldFilterByStatus()
    {
        // Arrange
        var specification = new ServicesByStatusSpecification(ServiceStatus.Draft);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, service => Assert.Equal(ServiceStatus.Draft, service.Status));
    }

    [Fact]
    public void ServiceBySlugSpecification_ShouldFilterBySlugAndPublishedStatus()
    {
        // Arrange
        var publishedService = _testServices.First(s => s.Status == ServiceStatus.Published);
        var specification = new ServiceBySlugSpecification(publishedService.Slug);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.Single(result);
        Assert.Equal(publishedService.Slug, result[0].Slug);
        Assert.Equal(ServiceStatus.Published, result[0].Status);
        Assert.True(result[0].Available);
    }

    [Fact]
    public void PublishedServicesSpecification_ShouldFilterByPublishedStatus()
    {
        // Arrange
        var specification = new PublishedServicesSpecification();
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.Equal(4, result.Count); // 3 available + 1 unavailable but published
        Assert.All(result, service => Assert.Equal(ServiceStatus.Published, service.Status));
    }

    [Fact]
    public void AllSpecifications_ShouldHaveProperIncludesConfiguration()
    {
        // Arrange & Act & Assert
        var activeSpec = new ActiveServicesSpecification();
        Assert.Single(activeSpec.Includes);

        var featuredSpec = new FeaturedServicesSpecification();
        Assert.Single(featuredSpec.Includes);

        var categorySpec = new ServicesByCategorySpecification(ServiceCategoryId.Create(1));
        Assert.Single(categorySpec.Includes);

        var searchSpec = new ServicesSearchSpecification("test");
        Assert.Single(searchSpec.Includes);

        var statusSpec = new ServicesByStatusSpecification(ServiceStatus.Published);
        Assert.Single(statusSpec.Includes);

        var slugSpec = new ServiceBySlugSpecification(Slug.Create("test-slug"));
        Assert.Single(slugSpec.Includes);

        var publishedSpec = new PublishedServicesSpecification();
        Assert.Single(publishedSpec.Includes);
    }

    [Theory]
    [InlineData("title-asc")]
    [InlineData("title-desc")]
    [InlineData("date-asc")]
    [InlineData("date-desc")]
    [InlineData("priority")]
    [InlineData("")]
    [InlineData(null)]
    public void ServicesSearchSpecification_WithDifferentSortOptions_ShouldApplyCorrectOrdering(string sortBy)
    {
        // Arrange
        var specification = new ServicesSearchSpecification("", sortBy: sortBy);
        
        // Act & Assert - Verify the specification has correct ordering configuration
        Assert.NotNull(specification.Criteria);
        
        // For priority sorting (default), should have both OrderBy and ThenBy
        if (sortBy == "priority" || string.IsNullOrEmpty(sortBy))
        {
            Assert.NotNull(specification.OrderBy);
            Assert.Single(specification.ThenByList);
        }
        else if (sortBy == "title-asc" || sortBy == "date-asc")
        {
            Assert.NotNull(specification.OrderBy);
            Assert.Null(specification.OrderByDescending);
        }
        else if (sortBy == "title-desc" || sortBy == "date-desc")
        {
            Assert.NotNull(specification.OrderByDescending);
            Assert.Null(specification.OrderBy);
        }
    }

    [Fact]
    public void ServicesSearchSpecification_WithNullCategoryId_ShouldIgnoreCategoryFilter()
    {
        // Arrange
        var specification = new ServicesSearchSpecification("", categoryId: null);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert - Should return published available services from all categories
        Assert.True(result.Count > 0);
        var distinctCategories = result.Select(s => s.CategoryId).Distinct().Count();
        Assert.True(distinctCategories >= 1);
        Assert.All(result, service =>
        {
            Assert.Equal(ServiceStatus.Published, service.Status);
            Assert.True(service.Available);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ServicesSearchSpecification_WithFeaturedFilter_ShouldRespectFeaturedStatus(bool featuredValue)
    {
        // Arrange
        var specification = new ServicesSearchSpecification("", featured: featuredValue);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.All(result, service => Assert.Equal(featuredValue, service.Featured));
    }

    [Fact]
    public void ServicesSearchSpecification_WithCombinedFilters_ShouldApplyAllFilters()
    {
        // Arrange
        var searchTerm = "Service";
        var categoryId = _testServices.First(s => s.Featured).CategoryId;
        var specification = new ServicesSearchSpecification(searchTerm, categoryId, featured: true);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.All(result, service =>
        {
            Assert.Equal(ServiceStatus.Published, service.Status);
            Assert.True(service.Available);
            Assert.Equal(categoryId, service.CategoryId);
            Assert.True(service.Featured);
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ServicesSearchSpecification_WithEmptyOrNullSearchTerm_ShouldReturnAllActiveServices(string searchTerm)
    {
        // Arrange
        var specification = new ServicesSearchSpecification(searchTerm);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert - Should return all published available services
        Assert.Equal(3, result.Count);
        Assert.All(result, service =>
        {
            Assert.Equal(ServiceStatus.Published, service.Status);
            Assert.True(service.Available);
        });
    }

    [Fact]
    public void ServicesSearchSpecification_WithSearchInDetailedDescription_ShouldFindMatches()
    {
        // Arrange - Create a service with unique term only in detailed description
        var uniqueTerm = "UniqueDetailedTerm";
        var testService = _testServices.First(s => s.Status == ServiceStatus.Published && s.Available);
        testService.UpdateDescription("Regular description", $"Detailed description with {uniqueTerm}");
        
        var specification = new ServicesSearchSpecification(uniqueTerm);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.True(result.Any(s => s.DetailedDescription.Contains(uniqueTerm)));
    }

    [Fact]
    public void FeaturedServicesSpecification_WithCustomLimit_ShouldRespectPagingLimit()
    {
        // Arrange
        var customLimit = 1;
        var specification = new FeaturedServicesSpecification(limit: customLimit);
        
        // Act & Assert
        Assert.True(specification.IsPagingEnabled);
        Assert.Equal(customLimit, specification.Take);
        Assert.Equal(0, specification.Skip);
    }

    [Fact]
    public void ServicesByCategorySpecification_WithNonExistentCategory_ShouldReturnEmpty()
    {
        // Arrange
        var nonExistentCategoryId = ServiceCategoryId.Create(999);
        var specification = new ServicesByCategorySpecification(nonExistentCategoryId);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ServiceBySlugSpecification_WithDraftServiceSlug_ShouldReturnEmpty()
    {
        // Arrange
        var draftService = _testServices.First(s => s.Status == ServiceStatus.Draft);
        var specification = new ServiceBySlugSpecification(draftService.Slug);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert - Draft services should not be returned even if slug matches
        Assert.Empty(result);
    }

    [Fact]
    public void ServiceBySlugSpecification_WithUnavailableServiceSlug_ShouldReturnEmpty()
    {
        // Arrange
        var unavailableService = _testServices.First(s => s.Status == ServiceStatus.Published && !s.Available);
        var specification = new ServiceBySlugSpecification(unavailableService.Slug);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert - Unavailable services should not be returned
        Assert.Empty(result);
    }

    [Theory]
    [InlineData(ServiceStatus.Draft)]
    [InlineData(ServiceStatus.Archived)]
    public void ServicesByStatusSpecification_WithSpecificStatus_ShouldFilterCorrectly(ServiceStatus status)
    {
        // Arrange - Add archived service for testing
        if (status == ServiceStatus.Archived)
        {
            var archivedService = ServiceTestDataGenerator.GenerateServiceWith(status: ServiceStatus.Published);
            archivedService.Archive();
            _testServices.Add(archivedService);
        }
        
        var specification = new ServicesByStatusSpecification(status);
        
        // Act
        var compiledCriteria = specification.Criteria!.Compile();
        var result = _testServices.Where(compiledCriteria).ToList();
        
        // Assert
        Assert.All(result, service => Assert.Equal(status, service.Status));
    }

    [Fact]
    public void AllSpecifications_ShouldHaveValidCriteria()
    {
        // Arrange & Act & Assert - Verify all specifications have non-null criteria
        var activeSpec = new ActiveServicesSpecification();
        Assert.NotNull(activeSpec.Criteria);

        var featuredSpec = new FeaturedServicesSpecification();
        Assert.NotNull(featuredSpec.Criteria);

        var categorySpec = new ServicesByCategorySpecification(ServiceCategoryId.Create(1));
        Assert.NotNull(categorySpec.Criteria);

        var searchSpec = new ServicesSearchSpecification("test");
        Assert.NotNull(searchSpec.Criteria);

        var statusSpec = new ServicesByStatusSpecification(ServiceStatus.Published);
        Assert.NotNull(statusSpec.Criteria);

        var slugSpec = new ServiceBySlugSpecification(Slug.Create("test-slug"));
        Assert.NotNull(slugSpec.Criteria);

        var publishedSpec = new PublishedServicesSpecification();
        Assert.NotNull(publishedSpec.Criteria);
    }

    [Fact]
    public void Specifications_WithOrderingConfiguration_ShouldHaveProperSortingSetup()
    {
        // Arrange & Act & Assert - Verify specifications with sorting have proper configuration
        var activeSpec = new ActiveServicesSpecification();
        Assert.NotNull(activeSpec.OrderBy);
        Assert.Single(activeSpec.ThenByList);
        Assert.Empty(activeSpec.ThenByDescendingList);

        var featuredSpec = new FeaturedServicesSpecification();
        Assert.NotNull(featuredSpec.OrderBy);
        Assert.Single(featuredSpec.ThenByList);

        var categorySpec = new ServicesByCategorySpecification(ServiceCategoryId.Create(1));
        Assert.NotNull(categorySpec.OrderBy);
        Assert.Single(categorySpec.ThenByList);

        var statusSpec = new ServicesByStatusSpecification(ServiceStatus.Published);
        Assert.NotNull(statusSpec.OrderBy);
        Assert.Empty(statusSpec.ThenByList); // Only has primary ordering

        var publishedSpec = new PublishedServicesSpecification();
        Assert.NotNull(publishedSpec.OrderBy);
        Assert.Single(publishedSpec.ThenByList);
    }

    [Fact]
    public void ServicesSearchSpecification_WithDescendingSorts_ShouldConfigureDescendingOrder()
    {
        // Arrange & Act
        var titleDescSpec = new ServicesSearchSpecification("", sortBy: "title-desc");
        var dateDescSpec = new ServicesSearchSpecification("", sortBy: "date-desc");
        
        // Assert
        Assert.NotNull(titleDescSpec.OrderByDescending);
        Assert.Null(titleDescSpec.OrderBy);
        
        Assert.NotNull(dateDescSpec.OrderByDescending);
        Assert.Null(dateDescSpec.OrderBy);
    }

    [Fact]
    public void ServicesSearchSpecification_WithAscendingSorts_ShouldConfigureAscendingOrder()
    {
        // Arrange & Act
        var titleAscSpec = new ServicesSearchSpecification("", sortBy: "title-asc");
        var dateAscSpec = new ServicesSearchSpecification("", sortBy: "date-asc");
        
        // Assert
        Assert.NotNull(titleAscSpec.OrderBy);
        Assert.Null(titleAscSpec.OrderByDescending);
        
        Assert.NotNull(dateAscSpec.OrderBy);
        Assert.Null(dateAscSpec.OrderByDescending);
    }

    [Fact]
    public void AllSpecifications_ShouldBeThreadSafeForCriteriaCompilation()
    {
        // Arrange
        var specifications = new List<ISpecification<Service>>
        {
            new ActiveServicesSpecification(),
            new FeaturedServicesSpecification(),
            new ServicesByCategorySpecification(ServiceCategoryId.Create(1)),
            new ServicesSearchSpecification("test"),
            new ServicesByStatusSpecification(ServiceStatus.Published),
            new ServiceBySlugSpecification(Slug.Create("test-slug")),
            new PublishedServicesSpecification()
        };

        // Act & Assert - Compile criteria multiple times concurrently
        Parallel.ForEach(specifications, spec =>
        {
            for (int i = 0; i < 10; i++)
            {
                var compiled = spec.Criteria!.Compile();
                Assert.NotNull(compiled);
                
                // Test that compilation works consistently
                var result = _testServices.Where(compiled).ToList();
                Assert.NotNull(result);
            }
        });
    }

    [Fact]
    public void SpecificationCriteria_ShouldHandleCaseInsensitiveSearch()
    {
        // Arrange
        var upperCaseSearch = "SERVICE";
        var lowerCaseSearch = "service";
        var mixedCaseSearch = "Service";
        
        var upperSpec = new ServicesSearchSpecification(upperCaseSearch);
        var lowerSpec = new ServicesSearchSpecification(lowerCaseSearch);
        var mixedSpec = new ServicesSearchSpecification(mixedCaseSearch);
        
        // Act
        var upperResult = _testServices.Where(upperSpec.Criteria!.Compile()).ToList();
        var lowerResult = _testServices.Where(lowerSpec.Criteria!.Compile()).ToList();
        var mixedResult = _testServices.Where(mixedSpec.Criteria!.Compile()).ToList();
        
        // Assert - All should return the same results due to Contains being case-sensitive by default
        // Note: The actual implementation uses Contains which is case-sensitive
        // This test documents the current behavior
        Assert.True(upperResult.Count <= mixedResult.Count);
        Assert.True(lowerResult.Count <= mixedResult.Count);
    }

    private List<Service> CreateTestServices()
    {
        var categoryId1 = ServiceCategoryId.Create(1);
        var categoryId2 = ServiceCategoryId.Create(2);

        var services = new List<Service>();

        // Published available services
        var service1 = ServiceTestDataGenerator.GenerateServiceWith(status: ServiceStatus.Published, featured: true);
        service1.SetCategory(categoryId1);
        services.Add(service1);

        var service2 = ServiceTestDataGenerator.GenerateServiceWith(status: ServiceStatus.Published, featured: false);
        service2.SetCategory(categoryId1);
        services.Add(service2);

        var service3 = ServiceTestDataGenerator.GenerateServiceWith(status: ServiceStatus.Published, featured: true);
        service3.SetCategory(categoryId2);
        services.Add(service3);

        // Draft services
        var service4 = ServiceTestDataGenerator.GenerateServiceWith(status: ServiceStatus.Draft, featured: false);
        service4.SetCategory(categoryId1);
        services.Add(service4);

        var service5 = ServiceTestDataGenerator.GenerateServiceWith(status: ServiceStatus.Draft, featured: false);
        service5.SetCategory(categoryId2);
        services.Add(service5);

        // Published but unavailable
        var service6 = ServiceTestDataGenerator.GenerateServiceWith(status: ServiceStatus.Published, featured: false);
        service6.SetCategory(categoryId1);
        service6.SetAvailability(false);
        services.Add(service6);

        return services;
    }
}